# IdlJson.Tests Integration Guide

**Version:** 1.0  
**Date:** January 25, 2026

---

## Overview

This guide explains how to add new topics to the `tests/IdlJson.Tests` verification framework. Every topic used in roundtrip testing **must first pass** IdlJson verification to ensure the JSON metadata matches the C compiler's ABI.

---

## Prerequisites

- Cyclone DDS `idlc` compiler installed
- CMake build system
- C compiler (MSVC on Windows, GCC/Clang on Linux)

---

## Step-by-Step Integration

### Step 1: Add Topic to `verification.idl`

Navigate to:
```
tests/IdlJson.Tests/verification.idl
```

Add your new topic definition. **Important**: Group related topics in the same module.

**Example - Adding BooleanTopic:**

```idl
module AtomicTests {
    @topic
    struct BooleanTopic {
        @key long id;
        boolean value;
    };
}
```

**Example - Adding Multiple Topics:**

```idl
module AtomicTests {
    @topic
    struct BooleanTopic {
        @key long id;
        boolean value;
    };
    
    @topic
    struct Int32Topic {
        @key long id;
        long value;
    };
    
    @topic
    struct Float64Topic {
        @key long id;
        double value;
    };
    
    enum SimpleEnum { FIRST, SECOND, THIRD };
    
    @topic
    struct EnumTopic {
        @key long id;
        SimpleEnum value;
    };
}
```

---

### Step 2: Generate C Header and JSON

From the `tests/IdlJson.Tests` directory:

```powershell
# Generate C header (for C compiler ABI)
idlc verification.idl

# Generate JSON metadata (for C# bindings)
idlc -l json verification.idl
```

This creates:
- `verification.h` - C structs and descriptors
- `verification.json` - JSON metadata

**Verify the files were created:**
```powershell
ls verification.h, verification.json
```

---

### Step 3: Add Verification to `verifier.c`

Open `tests/IdlJson.Tests/verifier.c` and locate the `main()` function.

**Add verification macro calls for your topics:**

```c
int main(int argc, char** argv) {
    // ... existing code ...
    
    int errors = 0;
    
    // Existing verifications
    VERIFY_TOPIC("AllPrimitives", AllPrimitives);
    VERIFY_TOPIC("CompositeKey", CompositeKey);
    
    // ADD YOUR NEW TOPICS HERE
    VERIFY_TOPIC("BooleanTopic", BooleanTopic);
    VERIFY_TOPIC("Int32Topic", Int32Topic);
    VERIFY_TOPIC("Float64Topic", Float64Topic);
    VERIFY_TOPIC("EnumTopic", EnumTopic);
    
    // ... rest of code ...
}
```

**Important**: The macro parameters are:
1. **First**: Topic name as string (e.g., `"BooleanTopic"`)
2. **Second**: C type name without module prefix (e.g., `BooleanTopic`, not `AtomicTests_BooleanTopic`)

The `VERIFY_TOPIC` macro automatically:
- Checks `sizeof(AtomicTests_BooleanTopic)` against JSON
- Validates all opcodes in the topic descriptor
- Compares key definitions

---

### Step 4: Specify the Module Prefix (if different from Verification)

If you're using a module other than `Verification`, you need to update the macro or use a different one.

**Option A: Define a new macro in `verifier.c`**

```c
#define VERIFY_ATOMIC_TOPIC(TYPE_NAME, C_TYPE) \
    do { \
        cJSON* jNode = find_type(json, "AtomicTests::" TYPE_NAME); \
        if (jNode) { \
            ASSERT_EQ("sizeof(AtomicTests::" TYPE_NAME ")", sizeof(AtomicTests_##C_TYPE), \
                      cJSON_GetObjectItem(jNode, "Size")->valueint); \
            verify_descriptor("AtomicTests::" TYPE_NAME, &AtomicTests_##C_TYPE##_desc, jNode, &errors); \
        } else { \
            printf("[SKIP] Type AtomicTests::%s not found in JSON\n", TYPE_NAME); \
        } \
    } while(0)
```

**Then use it:**

```c
VERIFY_ATOMIC_TOPIC("BooleanTopic", BooleanTopic);
VERIFY_ATOMIC_TOPIC("Int32Topic", Int32Topic);
```

**Option B: Keep everything in the Verification module**

Simpler approach - just use `module Verification` in `verification.idl` and use the existing `VERIFY_TOPIC` macro.

---

### Step 5: Build the Verifier

Navigate to the build directory:

```powershell
cd tests/IdlJson.Tests/build

# Configure (first time only)
cmake ..

# Build
cmake --build .
```

On Windows with MSVC:
```powershell
cmake --build . --config Debug
```

**Expected output:**
```
Building C object CMakeFiles/verifier.dir/verifier.c.obj
Linking C executable verifier.exe
```

---

### Step 6: Run the Verifier

From the `build` directory:

```powershell
./verifier ../verification.json
```

Or on Windows:
```powershell
.\Debug\verifier.exe ..\verification.json
```

**Expected output for successful verification:**

```
==================================================
VERIFYING LAYOUT AGAINST C COMPILER ABI
==================================================

--- Checking Topic Descriptor: BooleanTopic ---
[PASS] Ops Count: 12
[PASS] All 12 Opcodes match.

--- Checking Topic Descriptor: Int32Topic ---
[PASS] Ops Count: 12
[PASS] All 12 Opcodes match.

--- Checking Topic Descriptor: Float64Topic ---
[PASS] Ops Count: 12
[PASS] All 12 Opcodes match.

==================================================
SUMMARY
==================================================
Total Errors: 0
Status: ALL TESTS PASSED
==================================================
```

**If you see failures:**

```
[FAIL] Opcode[5]: C 0x251E0004 != JSON 0x251E0008
```

This indicates a mismatch between the C compiler's layout and the JSON metadata. Investigate:
1. Alignment issues
2. Incorrect IDL annotations
3. Bug in `idlc -l json`

---

## Step 7: Commit Changes

Once verification passes, commit the changes:

```powershell
git add tests/IdlJson.Tests/verification.idl
git add tests/IdlJson.Tests/verification.h
git add tests/IdlJson.Tests/verification.json
git add tests/IdlJson.Tests/verifier.c
git commit -m "Add BooleanTopic, Int32Topic, Float64Topic to IdlJson verification"
```

---

## Common Issues and Solutions

### Issue 1: "Type not found in JSON"

**Symptom:**
```
[SKIP] Type AtomicTests::BooleanTopic not found in JSON
```

**Solution:**
- Ensure you ran `idlc -l json verification.idl`
- Check the module name matches (e.g., `AtomicTests` vs `Verification`)
- Inspect `verification.json` to see what names are actually present

### Issue 2: "Undefined reference to descriptor"

**Symptom:**
```
error: 'AtomicTests_BooleanTopic_desc' undeclared
```

**Solution:**
- Ensure you ran `idlc verification.idl` (not just `-l json`)
- The C header must be generated for the compiler to know about descriptors
- Include the generated header: `#include "verification.h"`

### Issue 3: Opcode count mismatch

**Symptom:**
```
[FAIL] Ops Count: C-Compiler 14 != JSON 12
```

**Solution:**
- Regenerate both files: `idlc verification.idl && idlc -l json verification.idl`
- Ensure you're using the same version of `idlc` for both
- Check if there are any `#pragma` directives affecting generation

### Issue 4: Size mismatch

**Symptom:**
```
[FAIL] sizeof(BooleanTopic): C-Compiler 12 != JSON 8
```

**Solution:**
- Check struct alignment/padding rules
- Verify field types match between IDL and generated C
- Use `#pragma pack` if needed (but avoid unless necessary)

---

## Advanced: Verifying Non-Topic Structs

To verify a struct that is **not** a topic (no `@topic` annotation), use `VERIFY_SIZE`:

```c
VERIFY_SIZE("Point2D", Point2D);
VERIFY_SIZE("Location", Location);
```

This only checks the `sizeof()` match, not the topic descriptor (since non-topics don't have one).

---

## Advanced: Verifying Unions

Unions are verified the same way:

```idl
union SimpleUnion switch(long) {
    case 1: long int_value;
    case 2: double double_value;
};

@topic
struct UnionTopic {
    @key long id;
    SimpleUnion data;
};
```

```c
VERIFY_SIZE("SimpleUnion", SimpleUnion);  // Check union size
VERIFY_TOPIC("UnionTopic", UnionTopic);    // Check topic with union
```

---

## Advanced: Custom Verification

For fine-grained checks (e.g., specific field offsets), you can write custom verification:

```c
cJSON* jNode = find_type(json, "AtomicTests::BooleanTopic");
cJSON* jMember = find_member(jNode, "value");

size_t cOffset = offsetof(AtomicTests_BooleanTopic, value);
int jOffset = cJSON_GetObjectItem(jMember, "Offset")->valueint;

ASSERT_EQ("BooleanTopic.value offset", cOffset, jOffset);
```

---

## Testing Checklist

Before moving to roundtrip tests, ensure:

- [ ] Topic added to `verification.idl`
- [ ] `idlc verification.idl` executed successfully
- [ ] `idlc -l json verification.idl` executed successfully
- [ ] Topic added to `verifier.c` using `VERIFY_TOPIC` or `VERIFY_ATOMIC_TOPIC`
- [ ] Verifier builds without errors
- [ ] Verifier runs and reports `[PASS]` for all opcodes and sizes
- [ ] Zero errors in summary

---

## Next Steps

Once IdlJson verification passes, the topic is ready for:

1. **C# Code Generation**: Add to C# project, regenerate types
2. **Roundtrip Testing**: Add to `CsharpToC.Roundtrip.Tests`
3. **Native Implementation**: Write handler in `atomic_tests_native.c`

See [CSHARP-TO-C-ROUNDTRIP-DESIGN.md](CSHARP-TO-C-ROUNDTRIP-DESIGN.md) for the next steps.

---

## Quick Reference

**File Locations:**
- IDL: `tests/IdlJson.Tests/verification.idl`
- Verifier: `tests/IdlJson.Tests/verifier.c`
- Build: `tests/IdlJson.Tests/build/`

**Commands:**
```powershell
# Regenerate files
cd tests/IdlJson.Tests
idlc verification.idl
idlc -l json verification.idl

# Build verifier
cd build
cmake --build .

# Run verification
./verifier ../verification.json
```

**Macros:**
- `VERIFY_TOPIC(name, type)` - Full verification (topic only)
- `VERIFY_SIZE(name, type)` - Size check only
- `ASSERT_EQ(name, actual, expected)` - Custom assertion

---

## Support

For issues or questions:
1. Check `cyclonedds/src/tools/idljson/IDLJSON-README.md`
2. Review existing examples in `verification.idl`
3. Inspect `verification.json` to understand the structure
4. Enable verbose logging in `verifier.c` for debugging

**Remember**: IdlJson verification is the foundation. Don't skip this step!
