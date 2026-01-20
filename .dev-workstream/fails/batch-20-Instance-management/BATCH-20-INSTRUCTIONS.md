# BATCH-20: Test Hardening + Instance Management (Keyed Topics)

**Batch Number:** BATCH-20  
**Tasks:** BATCH-19 Test Fixes (DisposeWithListener_NoLeaks), FCDC-EXT05 (Instance Management for Keyed Topics)  
**Phase:** Stage 3.75 - Extended DDS API - Modern C# Idioms  
**Estimated Effort:** 4-5 days (0.5 day test fix + 3.5-4 days EXT05)  
**Priority:** **HIGH** (Complete keyed topic support + fix test quality gap)  
**Dependencies:** BATCH-19 complete (Async/Await + Events infrastructure)

---

## 📋 Onboarding & Workflow

### Developer Instructions

Welcome to **BATCH-20**, continuing **Stage 3.75: Extended DDS API**! This batch has two parts:

**Part 1: Test Quality Fix (0.5 day)**
- Harden `DisposeWithListener_NoLeaks` test from BATCH-19 to actually verify cleanup

**Part 2: Instance Management (3.5-4 days)**
- Implement keyed topic instance lookup and filtering (O(1) access by instance handle)
- Create keyed test type (`KeyedTestMessage`) with `[DdsKey]` attribute
- Re-enable 3 skipped tests from BATCH-18 (DisposeInstance, UnregisterInstance lifecycle)
- Add comprehensive multi-instance tests

**Why These Together:**
- Test fix is quick (warm-up task)
- Instance Management completes the keyed topic story started in BATCH-14 (lifecycle methods)
- Both improve production readiness

### Required Reading (IN ORDER)

**READ THESE BEFORE STARTING:**

1. **Workflow Guide:** `d:\Work\FastCycloneDdsCsharpBindings\.dev-workstream\README.md`  
   - Batch system, report requirements, testing standards

2. **Previous Batch Review:** `d:\Work\FastCycloneDdsCsharpBindings\.dev-workstream\reviews\BATCH-19-REVIEW.md`  
   - **Issue 1 (lines 58-83)** – Read carefully to understand what needs fixing
   - See test quality analysis

3. **Task Definitions:** `d:\Work\FastCycloneDdsCsharpBindings\docs\SERDATA-TASK-MASTER.md`  
   - Section: FCDC-EXT05 (lines 1858-1933) – Instance Management details

4. **Design Document:** `d:\Work\FastCycloneDdsCsharpBindings\docs\EXTENDED-DDS-API-DESIGN.md`  
   - **Section 8: Instance Management (Keyed Topics)** – Implementation patterns, P/Invoke details, examples

5. **Previous Batch Instructions:** `d:\Work\FastCycloneDdsCsharpBindings\.dev-workstream\batches\BATCH-14-INSTRUCTIONS.md`  
   - Background on DisposeInstance/UnregisterInstance (FCDC-S022b)

### Repository Structure

```
d:\Work\FastCycloneDdsCsharpBindings\
├── Src\
│   └── CycloneDDS.Runtime\           # Runtime API
│       ├── DdsReader.cs              # ← MODIFY (add instance methods)
│       ├── DdsInstanceHandle.cs      # ← NEW FILE (handle wrapper)
│       └── Interop\
│           └── DdsApi.cs             # ← MODIFY (add instance P/Invoke)
│
├── tests\
│   └── CycloneDDS.Runtime.Tests\     # Runtime tests
│       ├── AsyncTests.cs             # ← MODIFY (fix DisposeWithListener_NoLeaks)
│       ├── KeyedTestMessage.cs       # ← NEW FILE (keyed test type)
│       ├── InstanceTests.cs          # ← NEW FILE (6+ tests for EXT05)
│       └── IntegrationTests.cs       # ← MODIFY (re-enable 3 skipped tests)
│
└── .dev-workstream\
    ├── batches\
    │   └── BATCH-20-INSTRUCTIONS.md  # ← This file
    └── reports\
        └── BATCH-20-REPORT.md        # ← Submit your report here
```

### Critical Tool & Library Locations

**DDS Native Library:**
- **Location:** `d:\Work\FastCycloneDdsCsharpBindings\cyclone-compiled\bin\ddsc.dll`
- **Usage:** Runtime tests link against this (custom build with serdata + listener + instance exports)
- **Do NOT modify:** Already configured

**Code Generator (for KeyedTestMessage):**
- **Location:** `d:\Work\FastCycloneDdsCsharpBindings\tools\CycloneDDS.CodeGen\bin\Debug\net8.0\CycloneDDS.CodeGen.dll`
- **Trigger:** Build `CycloneDDS.Runtime.Tests` project after adding `KeyedTestMessage.cs`
- **Output:** `KeyedTestMessage.Generated.cs` with serialization + descriptor

**Projects to Build:**

Build order (dependencies):
```powershell
# 1. Schema (if adding KeyedTestMessage)
dotnet build d:\Work\FastCycloneDdsCsharpBindings\Src\CycloneDDS.Schema\CycloneDDS.Schema.csproj

# 2. Runtime (DDS API)
dotnet build d:\Work\FastCycloneDdsCsharpBindings\Src\CycloneDDS.Runtime\CycloneDDS.Runtime.csproj

# 3. Tests (triggers code generation for KeyedTestMessage)
dotnet build d:\Work\FastCycloneDdsCsharpBindings\tests\CycloneDDS.Runtime.Tests\CycloneDDS.Runtime.Tests.csproj

# 4. Run all tests
dotnet test d:\Work\FastCycloneDdsCsharpBindings\tests\CycloneDDS.Runtime.Tests\CycloneDDS.Runtime.Tests.csproj
```

### Report Submission

**When done, submit your report to:**  
`d:\Work\FastCycloneDdsCsharpBindings\.dev-workstream\reports\BATCH-20-REPORT.md`

**If you have questions, create:**  
`d:\Work\FastCycloneDdsCsharpBindings\.dev-workstream\questions\BATCH-20-QUESTIONS.md`

---

## 🔄 MANDATORY WORKFLOW: Test-Driven Task Progression

**CRITICAL: You MUST complete tasks in sequence with passing tests:**

1. **Task 1 (Test Fix):** Fix test → Verify → **ALL tests pass** ✅
2. **Task 2 (Keyed Type):** Create KeyedTestMessage → Build → **Generates code** ✅
3. **Task 3 (Instance APIs):** Implement → Write tests → **ALL tests pass** ✅  
4. **Task 4 (Re-enable Tests):** Fix 3 skipped tests → **ALL tests pass** ✅

**DO NOT** move to the next task until:
- ✅ Current task implementation complete
- ✅ Current task tests written/fixed
- ✅ **ALL tests passing** (including BATCH-19 tests: 57 passing)

**After EACH task completion:**
```powershell
# Verify ALL tests pass (not just new ones)
dotnet test d:\Work\FastCycloneDdsCsharpBindings\tests\CycloneDDS.Runtime.Tests\CycloneDDS.Runtime.Tests.csproj --no-build

# Expected progression:
# After Task 1: 57 tests passing (fixed test)
# After Task 2: 57 tests passing (KeyedTestMessage ready)
# After Task 3: 63+ tests passing (57 + 6 new instance tests)
# After Task 4: 66+ tests passing (63 + 3 re-enabled)
```

---

## Context

This batch has two objectives:

1. **Fix Test Quality Gap:** BATCH-19 review identified weak test that needs hardening
2. **Complete Keyed Topic Story:** BATCH-14 added lifecycle methods (Dispose/Unregister), but no keyed tests existed. This batch adds:
   - Keyed test type (`KeyedTestMessage`)
   - Instance lookup/filtering APIs
   - Comprehensive multi-instance tests
   - Re-enables 3 skipped lifecycle tests

**Related Tasks:**
- [BATCH-19 Issue 1](../reviews/BATCH-19-REVIEW.md#issue-1-weak-test---disposewithlisten_noleaks) – Test fix details
- [FCDC-EXT05](../docs/SERDATA-TASK-MASTER.md#fcdc-ext05-instance-management-keyed-topics) – Instance Management task
- [FCDC-S022b](../docs/SERDATA-TASK-MASTER.md#fcdc-s022b-instance-lifecycle-management-disposeunregister) – Lifecycle methods (BATCH-14)

---

## 🎯 Batch Objectives

**Goal 1:** Ensure test quality meets production standards (fix weak disposal test)

**Goal 2:** Enable full keyed topic support:
- O(1) instance lookup by handle
- Per-instance Read/Take filtering
- Complete lifecycle testing (dispose, unregister, multi-instance)

**Why It Matters:**
- **Keyed topics** are essential for multi-instance DDS systems (fleet management, sensor networks, etc.)
- Without proper testing, lifecycle methods from BATCH-14 remain unverified
- Production systems need robust tests, not shallow ones

---

## ✅ Tasks

### Task 1: Fix BATCH-19 Test - DisposeWithListener_NoLeaks

**Priority:** HIGH  
**Estimated Effort:** 0.5 day  
**Review Reference:** `BATCH-19-REVIEW.md` Issue 1 (lines 58-83)

#### Problem Statement

Current test only checks "no crash on dispose":

```csharp
// AsyncTests.cs (lines 81-89) - CURRENT (WEAK)
[Fact]
public async Task DisposeWithListener_NoLeaks()
{
     using (var reader = new DdsReader<TestMessage, TestMessage>(...))
     {
         var t = reader.WaitDataAsync(); 
         // It created listener
     }
     // Should not crash on dispose
}
```

**What's Wrong:**
- Doesn't verify GCHandle is freed
- Doesn't verify listener is deleted
- Doesn't verify no memory leaks
- Only tests "doesn't throw exception"

#### Files to Modify

**File:** `tests\CycloneDDS.Runtime.Tests\AsyncTests.cs`

Replace weak test with comprehensive verification.

#### Implementation Requirements

**Option A: Use Weak References to Verify Cleanup**

```csharp
[Fact]
public void DisposeWithListener_NoLeaks()
{
    WeakReference weakReader = null;
    
    // Scope 1: Create reader with listener
    {
        var reader = new DdsReader<TestMessage, TestMessage>(_participant, _topicName);
        var t = reader.WaitDataAsync(); // Creates listener
        
        // Get weak reference before disposal
        weakReader = new WeakReference(reader);
        
        // Dispose reader (should cleanup GCHandle, listener)
        reader.Dispose();
        
        // Force GC to collect if weak reference is only reference
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
    
    // Verify reader was collected (proves GCHandle was freed)
    Assert.False(weakReader.IsAlive, 
        "Reader still alive after dispose + GC - GCHandle may not be freed");
}
```

**Option B: Use Reflection to Verify Internal State**

```csharp
[Fact]
public void DisposeWithListener_NoLeaks()
{
    using var reader = new DdsReader<TestMessage, TestMessage>(_participant, _topicName);
    
    // Trigger listener creation
    var t = reader.WaitDataAsync();
    Thread.Sleep(100); // Ensure listener created
    
    // Verify listener was created
    var listenerField = typeof(DdsReader<TestMessage, TestMessage>)
        .GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);
    IntPtr listenerBefore = (IntPtr)listenerField.GetValue(reader);
    Assert.NotEqual(IntPtr.Zero, listenerBefore);
    
    // Dispose
    reader.Dispose();
    
    // Verify listener was freed
    IntPtr listenerAfter = (IntPtr)listenerField.GetValue(reader);
    Assert.Equal(IntPtr.Zero, listenerAfter);
    
    // Additional: Verify GCHandle field (if accessible)
    var handleField = typeof(DdsReader<TestMessage, TestMessage>)
        .GetField("_paramHandle", BindingFlags.NonPublic | BindingFlags.Instance);
    if (handleField != null)
    {
        GCHandle handle = (GCHandle)handleField.GetValue(reader);
        Assert.False(handle.IsAllocated, "GCHandle still allocated after dispose");
    }
}
```

**Choose the approach that best verifies cleanup.** Option A is more robust (proves no leaks via GC), Option B is more direct (verifies internal state).

#### Success Criteria

- ✅ Test verifies listener is deleted (IntPtr.Zero or GC collected)
- ✅ Test verifies GCHandle is freed (not allocated or weak reference collected)
- ✅ Test still passes (cleanup is correct in implementation)
- ✅ No other tests break

---

### Task 2: Create Keyed Test Type (KeyedTestMessage)

**Priority:** CRITICAL (Foundation for EXT05)  
**Estimated Effort:** 0.5 day  
**Design Reference:** `SERDATA-TASK-MASTER.md` lines 1912-1925

#### Overview

Create a new test message type with `[DdsKey]` attribute to enable testing of keyed topic features (instance lookup, per-instance operations, lifecycle).

#### Files to Create

**File:** `tests\CycloneDDS.Runtime.Tests\KeyedTestMessage.cs` (NEW)

```csharp
using CycloneDDS.Schema;

namespace CycloneDDS.Runtime.Tests
{
    [DdsTopic("KeyedTestTopic")]
    public partial struct KeyedTestMessage
    {
        [DdsKey, DdsId(0)]
        public int SensorId;   // KEY FIELD - Identifies instance
        
        [DdsId(1)]
        public int Value;      // Data field
    }
}
```

**CRITICAL: Must use `[DdsKey]` attribute on `SensorId` field.**

#### Verification Steps

After creating the file:

1. **Build tests project:**
   ```powershell
   dotnet build d:\Work\FastCycloneDdsCsharpBindings\tests\CycloneDDS.Runtime.Tests\CycloneDDS.Runtime.Tests.csproj
   ```

2. **Verify code generation:**
   - Check for `KeyedTestMessage.Generated.cs` in `obj\Debug\net8.0\generated\`
   - Should contain `Serialize()`, `GetSerializedSize()`, `Deserialize()`, `GetDescriptorOps()`

3. **Verify key field in descriptor:**
   - Generated descriptor should mark `SensorId` as key field
   - Look for key metadata in `GetDescriptorOps()` return value

#### Success Criteria

- ✅ `KeyedTestMessage.cs` created with `[DdsKey]` on `SensorId`
- ✅ Code generator produces `KeyedTestMessage.Generated.cs`
- ✅ All existing tests still pass (no regressions)

---

### Task 3: FCDC-EXT05 - Instance Management APIs

**Priority:** HIGH  
**Estimated Effort:** 2-3 days  
**Design Reference:** `EXTENDED-DDS-API-DESIGN.md` Section 8

#### Overview

Implement O(1) instance lookup and per-instance filtering for keyed topics. Critical for systems tracking many objects (e.g., fleet management with 1000s of vehicles).

#### Files to Create/Modify

**1. Instance Handle Wrapper:** `Src\CycloneDDS.Runtime\DdsInstanceHandle.cs` (NEW)

```csharp
using System;
using System.Runtime.InteropServices;

namespace CycloneDDS.Runtime
{
    /// <summary>
    /// Represents a DDS instance handle for keyed topics.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DdsInstanceHandle : IEquatable<DdsInstanceHandle>
    {
        private readonly long _value;
        
        public static readonly DdsInstanceHandle Nil = new DdsInstanceHandle(0);
        
        internal DdsInstanceHandle(long value)
        {
            _value = value;
        }
        
        public bool IsValid => _value != 0;
        
        public bool Equals(DdsInstanceHandle other) => _value == other._value;
        
        public override bool Equals(object? obj) 
            => obj is DdsInstanceHandle other && Equals(other);
        
        public override int GetHashCode() => _value.GetHashCode();
        
        public static bool operator ==(DdsInstanceHandle left, DdsInstanceHandle right) 
            => left.Equals(right);
        
        public static bool operator !=(DdsInstanceHandle left, DdsInstanceHandle right) 
            => !left.Equals(right);
    }
}
```

**2. P/Invoke Layer:** `Src\CycloneDDS.Runtime\Interop\DdsApi.cs`

Add instance APIs:

```csharp
// Instance handle operations
[DllImport(DLL_NAME)]
public static extern long dds_lookup_instance(int reader, IntPtr data);

[DllImport(DLL_NAME)]
public static extern int dds_read_instance(
    int reader,
    IntPtr[] buffers,
    DdsSampleInfo[] infos,
    int maxSamples,
    long instanceHandle);

[DllImport(DLL_NAME)]
public static extern int dds_take_instance(
    int reader,
    IntPtr[] buffers,
    DdsSampleInfo[] infos,
    int maxSamples,
    long instanceHandle);
```

**Design doc reference:** Section 8.2 – P/Invoke signatures

**3. Reader Implementation:** `Src\CycloneDDS.Runtime\DdsReader.cs`

Add methods:

```csharp
/// <summary>
/// Lookup instance handle for the given key sample.
/// </summary>
/// <param name="keySample">Sample containing key fields (other fields ignored)</param>
/// <returns>Instance handle, or DdsInstanceHandle.Nil if instance not found</returns>
public DdsInstanceHandle LookupInstance(in T keySample)
{
    if (_readerHandle == null) throw new ObjectDisposedException(nameof(DdsReader<T, TView>));
    
    // Serialize sample to get key representation
    int size = _sizer(keySample, 0);
    byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
    try
    {
        var writer = new CdrWriter(buffer);
        _serializer(keySample, ref writer);
        
        // Create serdata from CDR
        fixed (byte* ptr = buffer)
        {
            IntPtr serdata = DdsApi.dds_create_serdata_from_cdr(
                _topicHandle,
                new IntPtr(ptr),
                size);
            
            try
            {
                long handle = DdsApi.dds_lookup_instance(_readerHandle.NativeHandle.Handle, serdata);
                return new DdsInstanceHandle(handle);
            }
            finally
            {
                if (serdata != IntPtr.Zero)
                    DdsApi.dds_free_serdata(serdata);
            }
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}

/// <summary>
/// Take samples for a specific instance only.
/// </summary>
public ViewScope<TView> TakeInstance(DdsInstanceHandle handle, int maxSamples = 32)
{
    return ReadOrTakeInstance(maxSamples, handle, true);
}

/// <summary>
/// Read samples for a specific instance only (non-destructive).
/// </summary>
public ViewScope<TView> ReadInstance(DdsInstanceHandle handle, int maxSamples = 32)
{
    return ReadOrTakeInstance(maxSamples, handle, false);
}

private ViewScope<TView> ReadOrTakeInstance(int maxSamples, DdsInstanceHandle handle, bool take)
{
    if (_readerHandle == null) throw new ObjectDisposedException(nameof(DdsReader<T, TView>));
    if (!handle.IsValid) throw new ArgumentException("Invalid instance handle", nameof(handle));
    
    IntPtr[] buffers = ArrayPool<IntPtr>.Shared.Rent(maxSamples);
    DdsApi.DdsSampleInfo[] infos = ArrayPool<DdsApi.DdsSampleInfo>.Shared.Rent(maxSamples);
    
    try
    {
        int count = take 
            ? DdsApi.dds_take_instance(_readerHandle.NativeHandle.Handle, buffers, infos, maxSamples, handle._value)
            : DdsApi.dds_read_instance(_readerHandle.NativeHandle.Handle, buffers, infos, maxSamples, handle._value);
        
        if (count < 0)
        {
            // Error handling
            return new ViewScope<TView>(Array.Empty<TView>(), Array.Empty<DdsApi.DdsSampleInfo>(), _filter);
        }
        
        // Deserialize samples (same logic as ReadOrTake)
        // ... (implementation details)
        
        return new ViewScope<TView>(views, infos, _filter);
    }
    finally
    {
        ArrayPool<IntPtr>.Shared.Return(buffers);
        ArrayPool<DdsApi.DdsSampleInfo>.Shared.Return(infos);
    }
}
```

**Design doc reference:** Section 8.3-8.5 – Implementation patterns

#### Edge Cases to Handle

1. **Invalid handle:** Throw `ArgumentException` if `DdsInstanceHandle.Nil` passed to ReadInstance/TakeInstance
2. **Unknown instance:** `LookupInstance` returns `DdsInstanceHandle.Nil` (not exception)
3. **No samples for instance:** Return empty `ViewScope` (count = 0)
4. **Disposed reader:** Throw `ObjectDisposedException`

#### Tests Required (Minimum 6)

**File:** `tests\CycloneDDS.Runtime.Tests\InstanceTests.cs` (NEW)

1. **`LookupInstance_ReturnsValidHandle`**
   - Write sample with `SensorId=5`
   - Lookup with key `{SensorId=5}`
   - Verify: Returns non-Nil handle

2. **`TakeInstance_OnlyReturnsMatchingData`**
   - Write `SensorId=1` and `SensorId=2`
   - Lookup handle for `SensorId=1`, TakeInstance
   - Verify: Only `SensorId=1` returned, `SensorId=2` remains

3. **`LookupInstance_UnknownKey_ReturnsNil`**
   - Never write `SensorId=999`
   - Lookup `{SensorId=999}`
   - Verify: Returns `DdsInstanceHandle.Nil`

4. **`ReadInstance_NonDestructive_DataRemains`**
   - Write `SensorId=1`, `SensorId=2`
   - Lookup instance 1, ReadInstance
   - ReadInstance again
   - Verify: Both reads return same data

5. **`MultipleInstances_IndependentLifecycles`**
   - Write 5 instances (SensorId 1-5)
   - Take instance 1, dispose instance 3, unregister instance 5
   - Verify: Instance states correct (ALIVE for 2,4; DISPOSED for 3; NO_WRITERS for 5; taken for 1)

6. **`InstanceHandle_PersistsAcrossUpdates`**
   - Write `{SensorId=10, Value=100}`
   - Lookup handle H1
   - Write `{SensorId=10, Value=200}` (update)
   - Lookup handle H2
   - Verify: H1 == H2 (same instance)

#### Success Criteria

- ✅ `DdsInstanceHandle` struct created
- ✅ P/Invoke APIs added (lookup, read_instance, take_instance)
- ✅ `LookupInstance()`, `TakeInstance()`, `ReadInstance()` implemented on `DdsReader`
- ✅ All 6+ tests pass
- ✅ O(1) lookup verified (no iteration through all samples)

---

### Task 4: Re-Enable Lifecycle Tests from BATCH-18

**Priority:** HIGH  
**Estimated Effort:** 0.5 day  
**Background:** BATCH-14 added lifecycle methods, BATCH-18 tests were skipped (no keyed type)

#### Files to Modify

**File:** `tests\CycloneDDS.Runtime.Tests\IntegrationTests.cs`

Modify 3 skipped tests (lines 365-428):

**Current State (SKIPPED):**
```csharp
[Fact(Skip = "Requires keyed topic support")]
public void DisposeInstance_RemovesInstance() { ... }

[Fact(Skip = "Requires keyed topic support")]
public void UnregisterInstance_RemovesWriterOwnership() { ... }

[Fact(Skip = "Requires keyed topic support (multiple instances)")]
public void InstanceLifecycle_MultipleInstances_TrackedSeparately() { ... }
```

**Required Changes:**

1. **Remove `Skip` attribute from all 3 tests**

2. **Replace `TestMessage` with `KeyedTestMessage`:**
   ```csharp
   // OLD: using var writer = new DdsWriter<TestMessage>(...);
   // NEW:
   using var writer = new DdsWriter<KeyedTestMessage>(_participant, topicName);
   using var reader = new DdsReader<KeyedTestMessage, KeyedTestMessage>(_participant, topicName);
   ```

3. **Update test logic to use keyed samples:**
   ```csharp
   // Test 1: DisposeInstance_RemovesInstance
   writer.Write(new KeyedTestMessage { SensorId = 1, Value = 100 });
   writer.DisposeInstance(new KeyedTestMessage { SensorId = 1, Value = 0 }); // Value ignored
   
   using var scope = reader.Take();
   Assert.Single(scope.Samples);
   Assert.Equal(DdsInstanceState.NotAliveDisposed, scope.Infos[0].InstanceState);
   
   // Test 2: UnregisterInstance_RemovesWriterOwnership
   writer.Write(new KeyedTestMessage { SensorId = 2, Value = 200 });
   writer.UnregisterInstance(new KeyedTestMessage { SensorId = 2, Value = 0 });
   
   using var scope = reader.Take();
   Assert.Single(scope.Samples);
   Assert.Equal(DdsInstanceState.NotAliveNoWriters, scope.Infos[0].InstanceState);
   
   // Test 3: InstanceLifecycle_MultipleInstances_TrackedSeparately
   writer.Write(new KeyedTestMessage { SensorId = 1, Value = 100 });
   writer.Write(new KeyedTestMessage { SensorId = 2, Value = 200 });
   writer.Write(new KeyedTestMessage { SensorId = 3, Value = 300 });
   
   writer.DisposeInstance(new KeyedTestMessage { SensorId = 2 });
   
   using var scope = reader.Take();
   Assert.Equal(3, scope.Samples.Length);
   
   // Find instance 2 (disposed)
   var instance2Info = scope.Infos.First(info => /* match SensorId=2 */);
   Assert.Equal(DdsInstanceState.NotAliveDisposed, instance2Info.InstanceState);
   ```

**Design doc reference:** Section 8 of `EXTENDED-DDS-API-DESIGN.md` for instance state handling

#### Success Criteria

- ✅ All 3 tests re-enabled (Skip attribute removed)
- ✅ Tests use `KeyedTestMessage` instead of `TestMessage`
- ✅ All 3 tests pass
- ✅ Instance states correctly verified (DISPOSED, NO_WRITERS)

---

## 🧪 Testing Requirements

### Test Counts

**Part 1 (Test Fix):** 1 test hardened (still 57 passing)  
**Part 2 (Instance Management):** 6+ new tests + 3 re-enabled = 9+ tests  
**Target Total:** 66+ tests passing

### Test Categories

1. **Disposal Test (1 hardened):**
   - Verify listener cleanup
   - Verify GCHandle freed

2. **Instance Lookup Tests (3):**
   - Valid handle lookup
   - Unknown key returns Nil
   - Per-instance filtering

3. **Multi-Instance Tests (3):**
   - Independent lifecycles
   - Handle persistence
   - Non-destructive ReadInstance

4. **Lifecycle Tests (3 re-enabled):**
   - DisposeInstance marks DISPOSED
   - UnregisterInstance marks NO_WRITERS
   - Multiple instances tracked separately

### Test Quality Standards

**⚠️ CRITICAL: ALL TESTS MUST VERIFY ACTUAL BEHAVIOR**

❌ **NOT ACCEPTABLE:**
```csharp
[Fact]
public void LookupInstance_Works()
{
    var handle = reader.LookupInstance(sample);
    Assert.NotNull(handle); // Tests nothing about validity
}
```

✅ **REQUIRED:**
```csharp
[Fact]
public void LookupInstance_ReturnsValidHandle()
{
    // Write data
    writer.Write(new KeyedTestMessage { SensorId = 5, Value = 100 });
    Thread.Sleep(100); // Wait for propagation
    
    // Lookup
    var handle = reader.LookupInstance(new KeyedTestMessage { SensorId = 5 });
    
    // Verify valid
    Assert.NotEqual(DdsInstanceHandle.Nil, handle);
    Assert.True(handle.IsValid);
    
    // Verify usable (can fetch data)
    using var scope = reader.ReadInstance(handle);
    Assert.Single(scope.Samples);
    Assert.Equal(5, scope.Samples[0].SensorId);
}
```

### Verification Commands

After completing EACH task:
```powershell
# Build
dotnet build d:\Work\FastCycloneDdsCsharpBindings\tests\CycloneDDS.Runtime.Tests\CycloneDDS.Runtime.Tests.csproj

# Run ALL tests
dotnet test d:\Work\FastCycloneDdsCsharpBindings\tests\CycloneDDS.Runtime.Tests\CycloneDDS.Runtime.Tests.csproj --no-build

# Expected progression:
# After Task 1: 57 tests passing (hardened test)
# After Task 2: 57 tests passing (KeyedTestMessage ready)
# After Task 3: 63+ tests passing (57 + 6 instance tests)
# After Task 4: 66+ tests passing (63 + 3 re-enabled)
```

---

## 📊 Report Requirements

### Report File

Submit to: `d:\Work\FastCycloneDdsCsharpBindings\.dev-workstream\reports\BATCH-20-REPORT.md`

Use template: `d:\Work\FastCycloneDdsCsharpBindings\.dev-workstream\templates\BATCH-REPORT-TEMPLATE.md`

### Mandatory Sections

**1. Completion Checklist**
- [ ] Task 1: DisposeWithListener_NoLeaks hardened
- [ ] Task 2: KeyedTestMessage created + code generated
- [ ] Task 3: Instance Management APIs complete (6+ tests)
- [ ] Task 4: 3 lifecycle tests re-enabled
- [ ] All tests passing (66+ total)
- [ ] No compiler warnings

**2. Test Results**
```
Total tests: XX
Passed: XX
Failed: 0
Skipped: XX (if any - justify)

Test breakdown:
- BATCH-19 (existing): 57 passing
- DisposeWithListener_NoLeaks: 1 hardened
- InstanceTests.cs: X new tests
- IntegrationTests.cs: 3 re-enabled
```

**3. Implementation Notes**

Document for EACH task:
- Approach taken (which test verification method?)
- Challenges encountered
- Design decisions beyond spec
- KeyedTestMessage structure
- Instance lookup implementation details

**4. Developer Insights (CRITICAL)**

Answer these questions:

**Q1: Test Hardening Approach**
Which approach did you use to verify cleanup (weak references vs reflection)? Why? What did you discover?

**Q2: Keyed Type Generation**
Did the code generator handle `[DdsKey]` correctly? Any issues with descriptor generation? How did you verify key metadata?

**Q3: Instance Lookup Implementation**
How does `LookupInstance` create serdata for key-only lookup? What's the performance characteristic? Any edge cases discovered?

**Q4: Per-Instance Operations**
How efficient is `TakeInstance` vs `Take` + manual filtering? Did you measure performance difference?

**Q5: Lifecycle Testing**
What challenges did you face re-enabling the lifecycle tests? Did instance states behave as expected? Any surprises?

**Q6: Code Quality**
What areas of the instance management code could be improved? Any technical debt introduced? Refactoring opportunities?

---

## 🎯 Success Criteria

This batch is DONE when:

- ✅ **Task 1 Complete:**
  - `DisposeWithListener_NoLeaks` actually verifies cleanup
  - Test uses weak references or reflection to check state
  - Test still passes (implementation is correct)

- ✅ **Task 2 Complete:**
  - `KeyedTestMessage.cs` created with `[DdsKey]` attribute
  - Code generation successful (Generated.cs file exists)
  - Descriptor includes key metadata

- ✅ **Task 3 Complete:**
  - `DdsInstanceHandle` struct implemented
  - P/Invoke APIs added (lookup, read_instance, take_instance)
  - `LookupInstance`, `TakeInstance`, `ReadInstance` implemented
  - 6+ tests passing (lookup, filtering, multi-instance)

- ✅ **Task 4 Complete:**
  - 3 lifecycle tests re-enabled (Skip removed)
  - Tests use `KeyedTestMessage`
  - All 3 tests pass (instance states correct)

- ✅ **Quality Standards:**
  - All tests verify ACTUAL BEHAVIOR (not shallow)
  - No compiler warnings
  - No memory leaks (verified by hardened test)
  - Code compiles and runs on first try

- ✅ **Documentation:**
  - Report submitted with all mandatory sections
  - Developer insights capture approach and discoveries
  - Code comments for complex logic (instance lookup, handle management)

---

## ⚠️ Common Pitfalls to Avoid

### Instance Handle Management

❌ **Don't:** Return raw `long` from lookup
```csharp
// WRONG - exposes internal representation
public long LookupInstance(in T sample) { ... }
```

✅ **Do:** Use `DdsInstanceHandle` wrapper
```csharp
// RIGHT - type-safe handle
public DdsInstanceHandle LookupInstance(in T sample)
{
    long rawHandle = DdsApi.dds_lookup_instance(...);
    return new DdsInstanceHandle(rawHandle);
}
```

### Key-Only Serialization

❌ **Don't:** Use full sample serialization for lookup
```csharp
// WRONG - serializes all fields (wasteful)
_serializer(keySample, ref writer); // Includes non-key fields
```

✅ **Do:** Use full serialization but rely on DDS to extract keys
```csharp
// RIGHT - DDS extracts keys from serdata
// Full serialization OK because DDS only uses key fields for lookup
_serializer(keySample, ref writer);
IntPtr serdata = DdsApi.dds_create_serdata_from_cdr(...);
long handle = DdsApi.dds_lookup_instance(reader, serdata);
// DDS internally extracts keys, ignores other fields
```

**Note:** Cyclone DDS `dds_lookup_instance` extracts key fields from serdata internally. We provide full serialized data for simplicity.

### Instance Handle Validation

❌ **Don't:** Allow operations on Nil handle
```csharp
// WRONG - no validation
public ViewScope<TView> TakeInstance(DdsInstanceHandle handle)
{
    return ReadOrTakeInstance(..., handle, true); // What if Nil?
}
```

✅ **Do:** Validate before native call
```csharp
public ViewScope<TView> TakeInstance(DdsInstanceHandle handle)
{
    if (!handle.IsValid)
        throw new ArgumentException("Invalid instance handle", nameof(handle));
    return ReadOrTakeInstance(..., handle, true);
}
```

### Test Verification

❌ **Don't:** Only check handle is not null
```csharp
// WRONG - doesn't verify handle is actually usable
var handle = reader.LookupInstance(sample);
Assert.NotEqual(DdsInstanceHandle.Nil, handle);
```

✅ **Do:** Verify handle is usable
```csharp
var handle = reader.LookupInstance(sample);
Assert.NotEqual(DdsInstanceHandle.Nil, handle);

// Verify usable - can actually fetch data
using var scope = reader.ReadInstance(handle);
Assert.Single(scope.Samples);
Assert.Equal(expectedSensorId, scope.Samples[0].SensorId);
```

### Lifecycle Test Updates

❌ **Don't:** Forget to update variable types
```csharp
// WRONG - still using TestMessage
using var writer = new DdsWriter<TestMessage>(...); // No [DdsKey]!
writer.DisposeInstance(new TestMessage { Id = 1 });
```

✅ **Do:** Update all references to KeyedTestMessage
```csharp
using var writer = new DdsWriter<KeyedTestMessage>(...);
writer.DisposeInstance(new KeyedTestMessage { SensorId = 1 });
```

---

## 📚 Reference Materials

### Task Definitions
- **SERDATA-TASK-MASTER.md:**
  - FCDC-EXT05 (lines 1858-1933) – Instance Management
  - FCDC-S022b (lines 1519-1614) – Instance Lifecycle (BATCH-14)

### Design Documents
- **EXTENDED-DDS-API-DESIGN.md:**
  - Section 8: Instance Management (Keyed Topics)

### Review Documents
- **BATCH-19-REVIEW.md:**
  - Issue 1 (lines 58-83) – Test fix requirements

### Previous Work
- **BATCH-19 Instructions:** `.dev-workstream\batches\BATCH-19-INSTRUCTIONS.md`
- **BATCH-14 Instructions:** `.dev-workstream\batches\BATCH-14-INSTRUCTIONS.md` (lifecycle methods)

### External References
- **DDS Instance Handles:** https://cyclonedds.io/docs/cyclonedds/latest/api/instance.html

---

**Good luck! Focus on test quality (both fixing and adding), proper instance handle management, and completing the keyed topic story.** 🚀
