# Adding Atomic Test Topics to IdlJson.Tests

**Step-by-Step Guide for Integrating All 72 Topics**

---

## Overview

This document provides a systematic approach to adding all atomic test topics from `tests/CsharpToC.Roundtrip.Tests/idl/atomic_tests.idl` to the `tests/IdlJson.Tests` verification framework.

**Why this matters**: Before running any roundtrip tests, we must verify that the JSON metadata matches the C compiler's ABI. This ensures our C# bindings work with the correct memory layout and serialization opcodes.

---

## Strategy: Incremental Addition

Add topics in batches by category, verify each batch before moving to the next.

### Batch 1: Basic Primitives (14 topics)

Add to `tests/IdlJson.Tests/verification.idl`:

```idl
module AtomicTests {
    @topic
    struct BooleanTopic {
        @key long id;
        boolean value;
    };
    
    @topic
    struct CharTopic {
        @key long id;
        char value;
    };
    
    @topic
    struct OctetTopic {
        @key long id;
        octet value;
    };
    
    @topic
    struct Int16Topic {
        @key long id;
        short value;
    };
    
    @topic
    struct UInt16Topic {
        @key long id;
        unsigned short value;
    };
    
    @topic
    struct Int32Topic {
        @key long id;
        long value;
    };
    
    @topic
    struct UInt32Topic {
        @key long id;
        unsigned long value;
    };
    
    @topic
    struct Int64Topic {
        @key long id;
        long long value;
    };
    
    @topic
    struct UInt64Topic {
        @key long id;
        unsigned long long value;
    };
    
    @topic
    struct Float32Topic {
        @key long id;
        float value;
    };
    
    @topic
    struct Float64Topic {
        @key long id;
        double value;
    };
    
    @topic
    struct StringUnboundedTopic {
        @key long id;
        string value;
    };
    
    @topic
    struct StringBounded32Topic {
        @key long id;
        string<32> value;
    };
    
    @topic
    struct StringBounded256Topic {
        @key long id;
        string<256> value;
    };
}
```

**Generate and verify:**

```powershell
cd tests/IdlJson.Tests
idlc verification.idl
idlc -l json verification.idl
```

**Add to `verifier.c` (in `main()`):**

```c
// Define macro for AtomicTests module
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

// In main():
VERIFY_ATOMIC_TOPIC("BooleanTopic", BooleanTopic);
VERIFY_ATOMIC_TOPIC("CharTopic", CharTopic);
VERIFY_ATOMIC_TOPIC("OctetTopic", OctetTopic);
VERIFY_ATOMIC_TOPIC("Int16Topic", Int16Topic);
VERIFY_ATOMIC_TOPIC("UInt16Topic", UInt16Topic);
VERIFY_ATOMIC_TOPIC("Int32Topic", Int32Topic);
VERIFY_ATOMIC_TOPIC("UInt32Topic", UInt32Topic);
VERIFY_ATOMIC_TOPIC("Int64Topic", Int64Topic);
VERIFY_ATOMIC_TOPIC("UInt64Topic", UInt64Topic);
VERIFY_ATOMIC_TOPIC("Float32Topic", Float32Topic);
VERIFY_ATOMIC_TOPIC("Float64Topic", Float64Topic);
VERIFY_ATOMIC_TOPIC("StringUnboundedTopic", StringUnboundedTopic);
VERIFY_ATOMIC_TOPIC("StringBounded32Topic", StringBounded32Topic);
VERIFY_ATOMIC_TOPIC("StringBounded256Topic", StringBounded256Topic);
```

**Build and run:**

```powershell
cd build
cmake --build .
./verifier ../verification.json
```

**Expected output:**

```
[PASS] sizeof(AtomicTests::BooleanTopic): 8
[PASS] All 12 Opcodes match.
[PASS] sizeof(AtomicTests::CharTopic): 8
[PASS] All 12 Opcodes match.
...
Total Errors: 0
```

✅ **Checkpoint**: All 14 primitive topics verified.

---

### Batch 2: Enumerations (2 topics + enums)

Add to `verification.idl`:

```idl
module AtomicTests {
    enum SimpleEnum { 
        FIRST, 
        SECOND, 
        THIRD 
    };
    
    @topic
    struct EnumTopic {
        @key long id;
        SimpleEnum value;
    };
    
    enum ColorEnum { 
        RED, 
        GREEN, 
        BLUE, 
        YELLOW, 
        MAGENTA, 
        CYAN 
    };
    
    @topic
    struct ColorEnumTopic {
        @key long id;
        ColorEnum color;
    };
}
```

**Add to `verifier.c`:**

```c
VERIFY_ATOMIC_TOPIC("EnumTopic", EnumTopic);
VERIFY_ATOMIC_TOPIC("ColorEnumTopic", ColorEnumTopic);
```

**Regenerate, rebuild, verify:**

```powershell
idlc verification.idl
idlc -l json verification.idl
cd build && cmake --build . && ./verifier ../verification.json
```

✅ **Checkpoint**: Enums verified.

---

### Batch 3: Nested Structures (4 topics + structs)

Add to `verification.idl`:

```idl
module AtomicTests {
    struct Point2D {
        double x;
        double y;
    };
    
    @topic
    struct NestedStructTopic {
        @key long id;
        Point2D point;
    };
    
    struct Point3D {
        double x;
        double y;
        double z;
    };
    
    @topic
    struct Nested3DTopic {
        @key long id;
        Point3D point;
    };
    
    struct Box {
        Point2D topLeft;
        Point2D bottomRight;
    };
    
    @topic
    struct DoublyNestedTopic {
        @key long id;
        Box box;
    };
    
    struct Container {
        long count;
        Point3D center;
        double radius;
    };
    
    @topic
    struct ComplexNestedTopic {
        @key long id;
        Container container;
    };
}
```

**Also verify non-topic structs:**

```c
// Verify supporting structs (no topic descriptor)
VERIFY_SIZE("Point2D", Point2D);
VERIFY_SIZE("Point3D", Point3D);
VERIFY_SIZE("Box", Box);
VERIFY_SIZE("Container", Container);

// Verify topics
VERIFY_ATOMIC_TOPIC("NestedStructTopic", NestedStructTopic);
VERIFY_ATOMIC_TOPIC("Nested3DTopic", Nested3DTopic);
VERIFY_ATOMIC_TOPIC("DoublyNestedTopic", DoublyNestedTopic);
VERIFY_ATOMIC_TOPIC("ComplexNestedTopic", ComplexNestedTopic);
```

✅ **Checkpoint**: Nested structs verified.

---

### Batch 4: Unions (4 topics + union types)

Add to `verification.idl`:

```idl
module AtomicTests {
    // Reuse enums and structs from previous batches
    
    union SimpleUnion switch(long) {
        case 1: long int_value;
        case 2: double double_value;
        case 3: string<64> string_value;
    };
    
    @topic
    struct UnionLongDiscTopic {
        @key long id;
        SimpleUnion data;
    };
    
    union BoolUnion switch(boolean) {
        case TRUE: long true_val;
        case FALSE: double false_val;
    };
    
    @topic
    struct UnionBoolDiscTopic {
        @key long id;
        BoolUnion data;
    };
    
    union ColorUnion switch(ColorEnum) {
        case RED: long red_data;
        case GREEN: double green_data;
        case BLUE: string<32> blue_data;
        case YELLOW: Point2D yellow_point;
    };
    
    @topic
    struct UnionEnumDiscTopic {
        @key long id;
        ColorUnion data;
    };
    
    union ShortUnion switch(short) {
        case 1: octet byte_val;
        case 2: short short_val;
        case 3: long long_val;
        case 4: float float_val;
    };
    
    @topic
    struct UnionShortDiscTopic {
        @key long id;
        ShortUnion data;
    };
}
```

**Add to `verifier.c`:**

```c
VERIFY_SIZE("SimpleUnion", SimpleUnion);
VERIFY_SIZE("BoolUnion", BoolUnion);
VERIFY_SIZE("ColorUnion", ColorUnion);
VERIFY_SIZE("ShortUnion", ShortUnion);

VERIFY_ATOMIC_TOPIC("UnionLongDiscTopic", UnionLongDiscTopic);
VERIFY_ATOMIC_TOPIC("UnionBoolDiscTopic", UnionBoolDiscTopic);
VERIFY_ATOMIC_TOPIC("UnionEnumDiscTopic", UnionEnumDiscTopic);
VERIFY_ATOMIC_TOPIC("UnionShortDiscTopic", UnionShortDiscTopic);
```

✅ **Checkpoint**: Unions verified.

---

### Batch 5: Optional Fields (6 topics)

Add to `verification.idl`:

```idl
module AtomicTests {
    @topic
    struct OptionalInt32Topic {
        @key long id;
        @optional long opt_value;
    };
    
    @topic
    struct OptionalFloat64Topic {
        @key long id;
        @optional double opt_value;
    };
    
    @topic
    struct OptionalStringTopic {
        @key long id;
        @optional string<64> opt_string;
    };
    
    @topic
    struct OptionalStructTopic {
        @key long id;
        @optional Point2D opt_point;
    };
    
    @topic
    struct OptionalEnumTopic {
        @key long id;
        @optional SimpleEnum opt_enum;
    };
    
    @topic
    struct MultiOptionalTopic {
        @key long id;
        @optional long opt_int;
        @optional double opt_double;
        @optional string<32> opt_string;
    };
}
```

**Add to `verifier.c`:**

```c
VERIFY_ATOMIC_TOPIC("OptionalInt32Topic", OptionalInt32Topic);
VERIFY_ATOMIC_TOPIC("OptionalFloat64Topic", OptionalFloat64Topic);
VERIFY_ATOMIC_TOPIC("OptionalStringTopic", OptionalStringTopic);
VERIFY_ATOMIC_TOPIC("OptionalStructTopic", OptionalStructTopic);
VERIFY_ATOMIC_TOPIC("OptionalEnumTopic", OptionalEnumTopic);
VERIFY_ATOMIC_TOPIC("MultiOptionalTopic", MultiOptionalTopic);
```

✅ **Checkpoint**: Optionals verified.

---

### Batch 6: Sequences (11 topics) **← CRITICAL BATCH**

This is where the current issues are. Add carefully:

```idl
module AtomicTests {
    @topic
    struct SequenceInt32Topic {
        @key long id;
        sequence<long> values;
    };
    
    @topic
    struct BoundedSequenceInt32Topic {
        @key long id;
        sequence<long, 10> values;
    };
    
    @topic
    struct SequenceInt64Topic {
        @key long id;
        sequence<long long> values;
    };
    
    @topic
    struct SequenceFloat32Topic {
        @key long id;
        sequence<float> values;
    };
    
    @topic
    struct SequenceFloat64Topic {
        @key long id;
        sequence<double> values;
    };
    
    @topic
    struct SequenceBooleanTopic {
        @key long id;
        sequence<boolean> values;
    };
    
    @topic
    struct SequenceOctetTopic {
        @key long id;
        sequence<octet> bytes;
    };
    
    @topic
    struct SequenceStringTopic {
        @key long id;
        sequence<string<32>> values;
    };
    
    @topic
    struct SequenceEnumTopic {
        @key long id;
        sequence<SimpleEnum> values;
    };
    
    @topic
    struct SequenceStructTopic {
        @key long id;
        sequence<Point2D> points;
    };
    
    @topic
    struct SequenceUnionTopic {
        @key long id;
        sequence<SimpleUnion> unions;
    };
}
```

**Add to `verifier.c`:**

```c
VERIFY_ATOMIC_TOPIC("SequenceInt32Topic", SequenceInt32Topic);
VERIFY_ATOMIC_TOPIC("BoundedSequenceInt32Topic", BoundedSequenceInt32Topic);
VERIFY_ATOMIC_TOPIC("SequenceInt64Topic", SequenceInt64Topic);
VERIFY_ATOMIC_TOPIC("SequenceFloat32Topic", SequenceFloat32Topic);
VERIFY_ATOMIC_TOPIC("SequenceFloat64Topic", SequenceFloat64Topic);
VERIFY_ATOMIC_TOPIC("SequenceBooleanTopic", SequenceBooleanTopic);
VERIFY_ATOMIC_TOPIC("SequenceOctetTopic", SequenceOctetTopic);
VERIFY_ATOMIC_TOPIC("SequenceStringTopic", SequenceStringTopic);
VERIFY_ATOMIC_TOPIC("SequenceEnumTopic", SequenceEnumTopic);
VERIFY_ATOMIC_TOPIC("SequenceStructTopic", SequenceStructTopic);
VERIFY_ATOMIC_TOPIC("SequenceUnionTopic", SequenceUnionTopic);
```

**IMPORTANT**: If verification fails here, this is expected. Document the failures and analyze the opcodes.

✅ **Checkpoint**: Sequences verified (or failures documented for later fix).

---

### Batch 7: Arrays (6 topics)

```idl
module AtomicTests {
    @topic
    struct ArrayInt32Topic {
        @key long id;
        long values[5];
    };
    
    @topic
    struct ArrayFloat64Topic {
        @key long id;
        double values[5];
    };
    
    @topic
    struct ArrayStringTopic {
        @key long id;
        string<16> names[3];
    };
    
    @topic
    struct Array2DInt32Topic {
        @key long id;
        long matrix[3][4];
    };
    
    @topic
    struct Array3DInt32Topic {
        @key long id;
        long cube[2][3][4];
    };
    
    @topic
    struct ArrayStructTopic {
        @key long id;
        Point2D points[3];
    };
}
```

```c
VERIFY_ATOMIC_TOPIC("ArrayInt32Topic", ArrayInt32Topic);
VERIFY_ATOMIC_TOPIC("ArrayFloat64Topic", ArrayFloat64Topic);
VERIFY_ATOMIC_TOPIC("ArrayStringTopic", ArrayStringTopic);
VERIFY_ATOMIC_TOPIC("Array2DInt32Topic", Array2DInt32Topic);
VERIFY_ATOMIC_TOPIC("Array3DInt32Topic", Array3DInt32Topic);
VERIFY_ATOMIC_TOPIC("ArrayStructTopic", ArrayStructTopic);
```

✅ **Checkpoint**: Arrays verified.

---

### Batch 8: Extensibility (6 topics)

```idl
module AtomicTests {
    @appendable
    @topic
    struct AppendableInt32Topic {
        @key long id;
        long value;
    };
    
    @appendable
    @topic
    struct AppendableStructTopic {
        @key long id;
        Point2D point;
    };
    
    @final
    @topic
    struct FinalInt32Topic {
        @key long id;
        long value;
    };
    
    @final
    @topic
    struct FinalStructTopic {
        @key long id;
        Point2D point;
    };
    
    @mutable
    @topic
    struct MutableInt32Topic {
        @key long id;
        @id(100) long value;
    };
    
    @mutable
    @topic
    struct MutableStructTopic {
        @key long id;
        @id(200) Point2D point;
    };
}
```

```c
VERIFY_ATOMIC_TOPIC("AppendableInt32Topic", AppendableInt32Topic);
VERIFY_ATOMIC_TOPIC("AppendableStructTopic", AppendableStructTopic);
VERIFY_ATOMIC_TOPIC("FinalInt32Topic", FinalInt32Topic);
VERIFY_ATOMIC_TOPIC("FinalStructTopic", FinalStructTopic);
VERIFY_ATOMIC_TOPIC("MutableInt32Topic", MutableInt32Topic);
VERIFY_ATOMIC_TOPIC("MutableStructTopic", MutableStructTopic);
```

✅ **Checkpoint**: Extensibility variants verified.

---

### Batch 9: Composite Keys (4 topics)

```idl
module AtomicTests {
    @topic
    struct TwoKeyInt32Topic {
        @key long key1;
        @key long key2;
        double value;
    };
    
    @topic
    struct TwoKeyStringTopic {
        @key string<32> key1;
        @key string<32> key2;
        double value;
    };
    
    @topic
    struct ThreeKeyTopic {
        @key long key1;
        @key string<32> key2;
        @key short key3;
        double value;
    };
    
    @topic
    struct FourKeyTopic {
        @key long key1;
        @key long key2;
        @key long key3;
        @key long key4;
        string<64> description;
    };
}
```

```c
VERIFY_ATOMIC_TOPIC("TwoKeyInt32Topic", TwoKeyInt32Topic);
VERIFY_ATOMIC_TOPIC("TwoKeyStringTopic", TwoKeyStringTopic);
VERIFY_ATOMIC_TOPIC("ThreeKeyTopic", ThreeKeyTopic);
VERIFY_ATOMIC_TOPIC("FourKeyTopic", FourKeyTopic);
```

✅ **Checkpoint**: Composite keys verified.

---

### Batch 10: Nested Keys (3 topics + key structs)

```idl
module AtomicTests {
    struct Location {
        @key long building;
        @key short floor;
    };
    
    @topic
    struct NestedKeyTopic {
        @key Location loc;
        double temperature;
    };
    
    struct Coordinates {
        @key double latitude;
        @key double longitude;
    };
    
    @topic
    struct NestedKeyGeoTopic {
        @key Coordinates coords;
        string<128> location_name;
    };
    
    struct TripleKey {
        @key long id1;
        @key long id2;
        @key long id3;
    };
    
    @topic
    struct NestedTripleKeyTopic {
        @key TripleKey keys;
        string<64> data;
    };
}
```

```c
VERIFY_SIZE("Location", Location);
VERIFY_SIZE("Coordinates", Coordinates);
VERIFY_SIZE("TripleKey", TripleKey);

VERIFY_ATOMIC_TOPIC("NestedKeyTopic", NestedKeyTopic);
VERIFY_ATOMIC_TOPIC("NestedKeyGeoTopic", NestedKeyGeoTopic);
VERIFY_ATOMIC_TOPIC("NestedTripleKeyTopic", NestedTripleKeyTopic);
```

✅ **Checkpoint**: Nested keys verified.

---

## Final Verification Script

Create `tests/IdlJson.Tests/verify_all_atomic.sh`:

```bash
#!/bin/bash

echo "=================================================="
echo "Verifying All Atomic Test Topics"
echo "=================================================="

cd "$(dirname "$0")"

# Regenerate everything
echo "[Step 1] Generating C header..."
idlc verification.idl || exit 1

echo "[Step 2] Generating JSON metadata..."
idlc -l json verification.idl || exit 1

echo "[Step 3] Building verifier..."
cd build
cmake --build . || exit 1

echo "[Step 4] Running verification..."
./verifier ../verification.json

EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "=================================================="
    echo "✓ ALL TOPICS VERIFIED SUCCESSFULLY"
    echo "=================================================="
else
    echo "=================================================="
    echo "✗ VERIFICATION FAILED"
    echo "=================================================="
fi

exit $EXIT_CODE
```

**Run:**

```powershell
cd tests/IdlJson.Tests
chmod +x verify_all_atomic.sh
./verify_all_atomic.sh
```

---

## Summary Checklist

- [ ] Batch 1: Primitives (14 topics) verified
- [ ] Batch 2: Enums (2 topics) verified
- [ ] Batch 3: Nested structs (4 topics) verified
- [ ] Batch 4: Unions (4 topics) verified
- [ ] Batch 5: Optionals (6 topics) verified
- [ ] Batch 6: Sequences (11 topics) verified **← Focus here**
- [ ] Batch 7: Arrays (6 topics) verified
- [ ] Batch 8: Extensibility (6 topics) verified
- [ ] Batch 9: Composite keys (4 topics) verified
- [ ] Batch 10: Nested keys (3 topics) verified

**Total: 60+ topics to verify in IdlJson.Tests**

Once all pass, proceed to roundtrip testing in `tests/CsharpToC.Roundtrip.Tests`.

---

## What to Do When Verification Fails

1. **Document the failure**: Save the output showing which opcodes mismatch
2. **Inspect JSON**: Open `verification.json`, find the topic, examine the `Ops` array
3. **Compare with C header**: Look at the generated `_desc` in `verification.h`
4. **Check alignment**: Verify `sizeof()` matches between C and JSON
5. **File an issue**: If it's an `idlc` bug, document and report
6. **Workaround**: If necessary, manually adjust JSON (not recommended long-term)

**Remember**: IdlJson verification is the foundation. Don't proceed to roundtrip tests until this is solid.
