# Quick Start Guide - C# to C Roundtrip Tests

**Get started with minimalistic DDS interoperability testing in 5 steps**

---

## Step 1: Verify Prerequisites

Ensure you have:

- [ ] Cyclone DDS installed (`idlc` in PATH)
- [ ] CMake 3.16+
- [ ] .NET 8.0 SDK
- [ ] C compiler (MSVC on Windows, GCC/Clang on Linux)
- [ ] Working `tests/IdlJson.Tests` setup

**Quick test:**
```powershell
idlc --version
cmake --version
dotnet --version
```

---

## Step 2: Validate IDL Topics in IdlJson.Tests

Before any roundtrip testing, validate the topics:

```powershell
# Navigate to IdlJson.Tests
cd tests/IdlJson.Tests

# Copy atomic test topics to verification.idl
# (Or add them manually - see IDLJSON-INTEGRATION-GUIDE.md)

# Generate C header and JSON
idlc verification.idl
idlc -l json verification.idl

# Build verifier
cd build
cmake ..
cmake --build .

# Run verification
./verifier ../verification.json
```

**Expected output:**
```
[PASS] sizeof(BooleanTopic): 8
[PASS] All 12 Opcodes match.
...
Total Errors: 0
Status: ALL TESTS PASSED
```

**If this fails, STOP. Fix IdlJson issues first before proceeding.**

---

## Step 3: Build Native Test Library

```powershell
# Navigate to Native directory
cd tests/CsharpToC.Roundtrip.Tests/Native

# Create build directory
mkdir build
cd build

# Configure
cmake ..

# Build
cmake --build .
```

On Windows with MSVC:
```powershell
cmake --build . --config Debug
```

**Expected output:**
- `CsharpToC.Roundtrip.Native.dll` (Windows)
- `libCsharpToC.Roundtrip.Native.so` (Linux)

**Verify the DLL exists:**
```powershell
ls *.dll  # Windows
ls *.so   # Linux
```

---

## Step 4: Build C# Test Application

```powershell
# Navigate to App directory
cd tests/CsharpToC.Roundtrip.Tests/App

# Restore dependencies
dotnet restore

# Build (this triggers code generation from IDL)
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Verify generated types:**
```powershell
ls obj/Debug/net8.0/Generated/*.cs
```

You should see:
- `BooleanTopic.cs`
- `Int32Topic.cs`
- `SequenceInt32Topic.cs`
- etc.

---

## Step 5: Run Your First Test

### Test a Single Topic (Recommended for First Run)

```powershell
cd tests/CsharpToC.Roundtrip.Tests/App
dotnet run -- BooleanTopic
```

**Expected output (if successful):**
```
========================================
C# to C Roundtrip Test Framework
========================================

[TEST] BooleanTopic (seed=42)
  [Phase 1] Native → C# Receive    ✓ PASS (15ms)
    Deserialized: { id=42, value=true }
    Expected:     { id=42, value=true }
  
  [Phase 2] C# Serialization       ✓ PASS
    CDR bytes match native (12 bytes)
  
  [Phase 3] C# → Native Send       ✓ PASS
    Native validated successfully
  
  CDR Dumps:
    Output/cdr_dumps/BooleanTopic_seed_42_native.hex
    Output/cdr_dumps/BooleanTopic_seed_42_csharp.hex

========================================
SUMMARY: 1 passed, 0 failed
========================================
```

### Inspect CDR Dumps

```powershell
cat Output/cdr_dumps/BooleanTopic_seed_42_native.hex
```

Example output:
```
# Topic: BooleanTopic
# Seed: 42
# Direction: Native → C#
# Timestamp: 2026-01-25T15:30:00Z
# Size: 12 bytes

00 01 00 00  # XCDR2 header
00 00 00 2A  # id = 42
01 00 00 00  # value = true
```

---

## Next Steps

### Test All Basic Primitives

```powershell
dotnet run -- --category primitives
```

This will run:
- BooleanTopic
- CharTopic
- OctetTopic
- Int16Topic, UInt16Topic
- Int32Topic, UInt32Topic
- Int64Topic, UInt64Topic
- Float32Topic, Float64Topic
- StringUnboundedTopic, StringBounded32Topic

### Test Sequences (The Current Challenge)

```powershell
dotnet run -- SequenceInt32Topic
```

If this fails, you'll see detailed output showing where the mismatch occurs.

### Test All Topics

```powershell
dotnet run
```

**Warning**: This runs 72+ topics. Use only after basics pass.

---

## Troubleshooting

### Issue: "Topic descriptor not found"

**Cause**: Code generation didn't run or IDL wasn't processed.

**Fix:**
```powershell
cd tests/CsharpToC.Roundtrip.Tests/App
dotnet clean
dotnet build
```

### Issue: "Native DLL not found"

**Cause**: Native library not built or not in PATH.

**Fix:**
```powershell
cd tests/CsharpToC.Roundtrip.Tests/Native/build
cmake --build .

# Copy DLL to App output folder
cp CsharpToC.Roundtrip.Native.dll ../../App/bin/Debug/net8.0/
```

### Issue: "Phase 1 timeout"

**Cause**: DDS discovery issue or native publisher not sending.

**Fix:**
- Check Cyclone DDS logs: `CYCLONEDDS_URI=file://cyclonedds.xml dotnet run`
- Increase timeout in code
- Verify native library is loaded: Add logging to `Native_Init`

### Issue: "Phase 2 byte mismatch"

**Cause**: C# serializer produces different bytes than native.

**Debug:**
```powershell
# Compare hex dumps
diff Output/cdr_dumps/BooleanTopic_seed_42_native.hex \
     Output/cdr_dumps/BooleanTopic_seed_42_csharp.hex

# Check serializer alignment settings
# Review SerializerEmitter.cs
```

### Issue: "IdlJson verification fails"

**Cause**: Mismatch between C compiler layout and JSON metadata.

**Fix:**
1. Re-run `idlc` commands
2. Check for alignment pragmas in IDL
3. Verify `idlc` version matches between C and JSON generation
4. See [IDLJSON-INTEGRATION-GUIDE.md](../../docs/IDLJSON-INTEGRATION-GUIDE.md)

---

## Testing Strategy

**Recommended order:**

1. **Week 1**: Primitives (BooleanTopic → Float64Topic)
2. **Week 2**: Enums, Nested Structs
3. **Week 3**: Sequences (this is the blocker - focus here)
4. **Week 4**: Arrays, Optionals
5. **Week 5**: Unions, Keys
6. **Week 6**: Extensibility, Advanced Combinations

**Don't move forward until current category is 100% passing.**

---

## Understanding Test Results

### All Phases Pass ✓

```
[Phase 1] ✓ PASS  →  C# deserializer works
[Phase 2] ✓ PASS  →  C# serializer produces correct bytes
[Phase 3] ✓ PASS  →  Native can read C# data
```

**This is the goal for every topic.**

### Phase 1 Fails ✗

```
[Phase 1] ✗ FAIL  →  C# deserializer bug
```

**Action**: Debug `Deserializer.cs`, check CDR reading logic.

### Phase 2 Fails ✗

```
[Phase 1] ✓ PASS
[Phase 2] ✗ FAIL  →  C# serializer bug
```

**Action**: Debug `Serializer.cs`, compare hex dumps, check alignment.

### Phase 3 Fails ✗

```
[Phase 1] ✓ PASS
[Phase 2] ✓ PASS
[Phase 3] ✗ FAIL  →  Native interpretation issue or key mismatch
```

**Action**: Check native validation logic, verify key handling.

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `idl/atomic_tests.idl` | IDL definitions (72+ topics) |
| `Native/atomic_tests_native.c` | Native handlers (generate/validate) |
| `Native/test_registry.c` | Topic lookup table |
| `App/Program.cs` | Test orchestrator |
| `App/TestRunner.cs` | Per-topic test execution |
| `App/CdrDumper.cs` | Hex dump utility |
| `Output/cdr_dumps/*.hex` | CDR byte stream captures |

---

## Success Criteria Checklist

For each topic, verify:

- [ ] IdlJson verification passes
- [ ] Phase 1 (Receive) passes
- [ ] Phase 2 (Serialize) passes
- [ ] Phase 3 (Send) passes
- [ ] CDR dumps are byte-identical

---

## Getting Help

1. **Check the hex dumps first** - they show exactly what's happening on the wire
2. **Review design docs**:
   - [CSHARP-TO-C-ROUNDTRIP-DESIGN.md](../../docs/CSHARP-TO-C-ROUNDTRIP-DESIGN.md)
   - [IDLJSON-INTEGRATION-GUIDE.md](../../docs/IDLJSON-INTEGRATION-GUIDE.md)
3. **Compare working vs failing topics** - what's different?
4. **Start with simplest topic** - BooleanTopic is the baseline

**Remember**: The framework is designed to isolate problems. If BooleanTopic passes but SequenceInt32Topic fails, the issue is specifically with sequence handling, not with primitives or keys.

---

## Next Steps After First Success

Once `BooleanTopic` passes:

1. Test all primitive types one by one
2. Document which ones pass/fail
3. For failures, capture and analyze hex dumps
4. Fix C# serializer/deserializer incrementally
5. Re-test to verify fixes don't break working topics
6. Move to next category (enums, structs, etc.)

**Good luck! Start simple, build confidence, scale up gradually.**
