# Instance Lifecycle Management Design

**Feature:** Sample Disposing and Instance Unregistration  
**Priority:** HIGH  
**Stage:** 3.5 (Post-Runtime Integration)  
**Status:** Design Complete, Ready for Implementation

---

## Overview

DDS instance lifecycle management enables proper disposal and unregistration of keyed instances, which is critical for:
- Resource cleanup (memory, network)
- Instance state management (lifecycle tracking)
- Ownership transfer (exclusive ownership  QoS)
- Graceful shutdown (preventing reader timeouts)

---

## DDS Instance Lifecycle States

**For Readers (Instance States):**
1. **ALIVE:** Instance has live writers and valid data
2. **NOT_ALIVE_DISPOSED:** Instance explicitly disposed (deleted)
3. **NOT_ALIVE_NO_WRITERS:** No live writers remain for instance

**Writer Operations:**
1. **Write:** Update instance with new data
2. **Dispose:** Mark instance as deleted/dead
3. **Unregister:** Writer stops updating instance (gives up ownership)

---

## Architecture

### 1. Native API Extensions

**Required:** Export additional serdata APIs in custom `ddsc.dll`

**File:** `cyclonedds/src/core/ddsc/src/dds_writer.c`

```c
DDS_EXPORT dds_return_t dds_dispose_serdata(dds_entity_t writer, dds_serdata_t *sd)
{
  return write_impl(writer, sd, 0, DDS_CMD_DISPOSE);
}

DDS_EXPORT dds_return_t dds_unregister_serdata(dds_entity_t writer, dds_serdata_t *sd)
{
  return write_impl(writer, sd, 0, DDS_CMD_UNREGISTER);
}
```

**Note:** Uses same internal `write_impl` as `dds_writecdr`, just different command flag.

---

### 2. P/Invoke Declarations

**File:** `Src/CycloneDDS.Runtime/Interop/DdsApi.cs`

```csharp
[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
public static extern int dds_dispose_serdata(DdsEntity writer, IntPtr serdata);

[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
public static extern int dds_unregister_serdata(DdsEntity writer, IntPtr serdata);
```

---

### 3. DdsWriter Implementation

**File:** `Src/CycloneDDS.Runtime/DdsWriter.cs`

#### Option A: Separate Methods (Initial Implementation)

```csharp
public void DisposeInstance(in T sample)
{
    if (_writerHandle == null) 
        throw new ObjectDisposedException(nameof(DdsWriter<T>));

    PerformOperation(sample, DdsApi.dds_dispose_serdata);
}

public void UnregisterInstance(in T sample)
{
    if (_writerHandle == null) 
        throw new ObjectDisposedException(nameof(DdsWriter<T>));

    PerformOperation(sample, DdsApi.dds_unregister_serdata);
}
```

#### Option B: Unified Implementation (Recommended)

```csharp
private enum DdsOperation { Write, Dispose, Unregister }

private void PerformOperation(in T sample, DdsOperation op)
{
    if (_writerHandle == null) 
        throw new ObjectDisposedException(nameof(DdsWriter<T>));

    // 1. Get Size
    int payloadSize = _sizer!(sample, 4);
    int totalSize = payloadSize + 4;

    // 2. Rent Buffer
    byte[] buffer = Arena.Rent(totalSize);
    
    try
    {
        // 3. Serialize (same for all operations)
        var span = buffer.AsSpan(0, totalSize);
        var cdr = new CdrWriter(span);
        
        // Header
        if (BitConverter.IsLittleEndian)
        {
            cdr.WriteByte(0x00); cdr.WriteByte(0x01);
        }
        else
        {
            cdr.WriteByte(0x00); cdr.WriteByte(0x00);
        }
        cdr.WriteByte(0x00); cdr.WriteByte(0x00);
        
        _serializer!(sample, ref cdr);
        cdr.Complete();
        
        // 4. Execute Operation
        unsafe
        {
            fixed (byte* p = buffer)
            {
                IntPtr serdata = DdsApi.dds_create_serdata_from_cdr(
                    _topicHandle.NativeHandle,
                    (IntPtr)p,
                    (uint)totalSize);

                if (serdata == IntPtr.Zero)
                    throw new DdsException(DdsApi.DdsReturnCode.Error, 
                        "Failed to create serdata");

                try
                {
                    int ret = op switch
                    {
                        DdsOperation.Write => 
                            DdsApi.dds_writecdr(_writerHandle.NativeHandle, serdata),
                        DdsOperation.Dispose => 
                            DdsApi.dds_dispose_serdata(_writerHandle.NativeHandle, serdata),
                        DdsOperation.Unregister => 
                            DdsApi.dds_unregister_serdata(_writerHandle.NativeHandle, serdata),
                        _ => throw new ArgumentException()
                    };

                    if (ret < 0)
                        throw new DdsException((DdsApi.DdsReturnCode)ret, 
                            $"{op} failed");
                }
                finally
                {
                    // Note: Check lifecycle - some operations consume ref, some don't
                    // For safety, always unref (Cyclone increments if needed)
                    DdsApi.ddsi_serdata_unref(serdata);
                }
            }
        }
    }
    finally
    {
        Arena.Return(buffer);
    }
}

// Public API
public void Write(in T sample) => PerformOperation(sample, DdsOperation.Write);
public void DisposeInstance(in T sample) => PerformOperation(sample, DdsOperation.Dispose);
public void UnregisterInstance(in T sample) => PerformOperation(sample, DdsOperation.Unregister);
```

---

## Key Design Decisions

### 1. Full Serialization vs Key-Only

**Decision:** Serialize full sample (not just keys)

**Rationale:**
- Simpler: Reuses existing `_serializer` delegate
- Zero-alloc: No special key-only path needed
- Safe: Cyclone DDS ignores non-key fields for dispose/unregister
- Performance: Cost is negligible for small samples

**Future Optimization:** Stage 6 could add key-only serialization for very large samples.

---

### 2. Method Naming

**Options Considered:**
- `Dispose(T)` vs `DisposeInstance(T)`
- `Unregister(T)` vs `UnregisterInstance(T)`

**Decision:** Use `DisposeInstance` and `UnregisterInstance`

**Rationale:**
- Avoids confusion with `IDisposable.Dispose()` (resource cleanup)
- Makes intent clearer (disposing DDS instance, not writer)
- Matches DDS specification terminology

---

### 3. Performance Characteristics

**Zero-Allocation Path:**
- ✅ Arena.Rent (pooled buffer)
- ✅ CdrWriter (ref struct, stack)
- ✅ Serializer (span-based)
- ✅ Native call (P/Invoke)
- ✅ Arena.Return

**Result:** Same zero-alloc characteristics as Write()

---

## Usage Examples

### Example 1: Lifecycle Management

```csharp
[DdsTopic("Sensor")]
public partial struct SensorData
{
    [DdsKey] public int SensorId;
    public double Temperature;
    public long Timestamp;
}

using var participant = new DdsParticipant(0);
using var descriptor = new DescriptorContainer(
    SensorData.GetDescriptorOps(), ...);
using var writer = new DdsWriter<SensorData>(
    participant, "Sensor", descriptor.Ptr);

// 1. Publish data
writer.Write(new SensorData { 
    SensorId = 42, 
    Temperature = 25.5, 
    Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds() 
});

// 2. Update instance
writer.Write(new SensorData { 
    SensorId = 42, 
    Temperature = 26.1, 
    Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds() 
});

// 3. Sensor removed/failed - dispose instance
writer.DisposeInstance(new SensorData { 
    SensorId = 42 
    // Non-key fields ignored
});
```

### Example 2: Graceful Shutdown

```csharp
[DdsTopic("Robot")]
public partial struct RobotState
{
    [DdsKey] public int RobotId;
    public string Status;
    public double BatteryLevel;
}

using var writer = new DdsWriter<RobotState>(...);

// Application running
writer.Write(new RobotState { 
    RobotId = 5, 
    Status = "Active", 
    BatteryLevel = 85.0 
});

// Application shutting down gracefully
// Unregister to notify readers this writer is going offline
writer.UnregisterInstance(new RobotState { RobotId = 5 });

// Resource cleanup
writer.Dispose();
```

### Example 3: Ownership Transfer

```csharp
// Scenario: Hot standby with exclusive ownership

// Primary writer (Strength = 10)
using var primaryWriter = new DdsWriter<ControlData>(
    participant, "Control", descriptor.Ptr);

primaryWriter.Write(new ControlData { 
    ControllerId = 1, 
    Command = "START" 
});

// Primary fails - unregister to allow failover
primaryWriter.UnregisterInstance(new ControlData { ControllerId = 1 });

// Secondary writer (Strength = 5) automatically takes over
```

---

## Testing Requirements

### Unit Tests

**File:** `tests/CycloneDDS.Runtime.Tests/DdsWriterLifecycleTests.cs`

1. ✅ `DisposeInstance_ValidSample_Succeeds`
2. ✅ `DisposeInstance_AfterWrite_SendsDisposalMessage`
3. ✅ `UnregisterInstance_ValidSample_Succeeds`
4. ✅ `UnregisterInstance_AfterWrite_SendsUnregisterMessage`
5. ✅ `DisposeInstance_NonKeySample_IgnoresNonKeyFields`
6. ✅ `DisposeInstance_AfterDispose_Throws`
7. ✅ `UnregisterInstance_MultipleWriters_HandlesCorrectly`

### Integration Tests

**File:** `tests/CycloneDDS.Runtime.Tests/InstanceLifecycleIntegrationTests.cs`

1. ✅ `WriteDisposeRead_VerifiesInstanceState`
   - Write sample, dispose, verify reader sees NOT_ALIVE_DISPOSED
2. ✅ `WriteUnregisterRead_VerifiesInstanceState`
   - Write sample, unregister, verify reader sees NOT_ALIVE_NO_WRITERS
3. ✅ `MultipleWritersUnregister_VerifiesOwnership`
   - Two writers, one unregisters, verify instance still ALIVE
4. ✅ `DisposeInstance_ZeroAllocation`
   - Verify no GC allocations for 1000 dispose operations

---

## Implementation Checklist

### Phase 1: Native Extension
- [ ] Export `dds_dispose_serdata` in ddsc.dll
- [ ] Export `dds_unregister_serdata` in ddsc.dll
- [ ] Rebuild cyclone-bin with exports
- [ ] Verify exports with `dumpbin` or equivalent

### Phase 2: P/Invoke Layer
- [ ] Add `dds_dispose_serdata` to DdsApi.cs
- [ ] Add `dds_unregister_serdata` to DdsApi.cs
- [ ] Verify signatures match native headers

### Phase 3: DdsWriter Implementation
- [ ] Add `DisposeInstance(in T)` method
- [ ] Add `UnregisterInstance(in T)` method
- [ ] Add private `PerformOperation` helper
- [ ] Add DdsOperation enum
- [ ] Update existing `Write` to use unified pattern

### Phase 4: Testing
- [ ] Write 7 unit tests (lifecycle operations)
- [ ] Write 4 integration tests (state verification)
- [ ] Verify zero-allocation performance
- [ ] Test with keyed and keyless topics

### Phase 5: Documentation
- [ ] Update DdsWriter XML docs
- [ ] Add usage examples to README.md
- [ ] Document dispose vs unregister semantics

---

## Performance Expectations

**Dispose/Unregister vs Write:**
- Same serialization cost
- Same buffer management cost  
- Same P/Invoke overhead
- ✅ **Zero additional allocations**

**Benchmark Target:** ~40 bytes/1000 operations (same as Write)

---

## Future Enhancements (Stage 6)

1. **Key-Only Serialization**
   - Generate `SerializeKeys()` method in code generator
   - Reduce payload size for large samples
   - Optimization for bandwidth-constrained systems

2. **Batch Dispose/Unregister**
   - Dispose/unregister multiple instances in single call
   - Useful for cleanup scenarios

3. **Auto-Unregister on Dispose**
   - Option to auto-unregister when writer disposed
   - Configurable behavior via attribute

---

## References

- **DDS Spec:** OMG DDS 1.4 - Section 2.2.2.4.1.4 (Instance Lifecycle)
- **Cyclone Source:** `dds_writer.c` - `write_impl()` function
- **Design Discussion:** `docs/design-talk.md` lines 5106-5412

---

## Priority Justification

**HIGH Priority because:**
1. **Production Requirement:** Proper lifecycle management is mandatory for real systems
2. **Resource Leaks:** Without dispose, readers accumulate stale instances
3. **Graceful Shutdown:** Critical for high-availability systems
4. **Ownership QoS:** Required for exclusive ownership patterns
5. **Low Complexity:** Straightforward extension of existing Write() pattern

**Estimated Effort:** 2-3 days (including testing)

**Dependencies:** Stage 3 complete (BATCH-13.3 ✅)

**Blocks:** None (can be implemented independently)
