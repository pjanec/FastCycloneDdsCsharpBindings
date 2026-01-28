Here is the detailed analysis of the failing tests.

## Overview of Failures

The majority of failures appear in **`@appendable`** topics using **XCDR2 (PL_CDR2)** encoding. The consistent pattern is a mismatch in the **DHEADER** (Delimiter Header) calculation.

1.  **DHEADER Mismatch:** The native C implementation produces a tightly packed DHEADER size (e.g., 5 bytes for `long` + `char`), while the C# implementation expects a larger size (e.g., 8 bytes), suggesting the C# serializer is incorrectly applying padding/alignment rules to the end of the struct or the DHEADER calculation itself.
2.  **Nested DHEADERs:** In XCDR2, appendable structs nested within other appendable structs have their own DHEADERs. The traces show this structure, which the C# deserializer/validator seems to misinterpret.
3.  **Optional Fields:** XCDR2 encoding for optional fields in appendable types uses a 1-byte boolean flag (when false), which the C# implementation seems to mistreat (expecting 4 bytes or header).
4.  **Strings:** String lengths are calculated strictly (4-byte length + N chars), without enforcing 4-byte alignment padding *at the end of the struct* within the DHEADER count.

---

## 1. AtomicTests::CharTopicAppendable

**Issue:** DHEADER Size Mismatch (Received 5, Expected 8).

**IDL:**
```idl
@appendable
@topic
struct CharTopicAppendable {
    @key long id;
    char value;
};
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 4c 04 00 00 49 00 00 00`

**Decoding:**
*   **Encapsulation Header (4 bytes):** `00 09 00 03`
    *   `00 09`: Kind `DELIMITED_CDR2_LE` (Little Endian XCDR2).
    *   `00 03`: Options (Little Endian flag).
*   **DHEADER (4 bytes):** `05 00 00 00`
    *   Value: **5**. This indicates the object body is 5 bytes long.
*   **Body (5 bytes):**
    *   **Member `id` (long):** `4c 04 00 00` (Value: 1100). Size: 4 bytes.
    *   **Member `value` (char):** `49` (Value: 'I'). Size: 1 byte.
*   **Padding:** `00 00 00` (Trailing garbage/alignment for next message, NOT part of DHEADER count).

**Analysis:**
The C# test expected a serialized size of 8 (likely 4 for ID + 1 for Char + 3 padding). Native XCDR2 does not pad the end of the struct inside the DHEADER count unless required by a subsequent field *inside* that scope.

---

## 2. AtomicTests::StringBounded32TopicAppendable

**Issue:** DHEADER Size Mismatch (Received 17 (0x11), Expected 20).

**IDL:**
```idl
@appendable
@topic
struct StringBounded32TopicAppendable {
    @key long id;
    string<32> value;
};
```

**Golden CDR Bytes:**
`00 09 00 03 11 00 00 00 14 05 00 00 09 00 00 00 53 74 72 5f 31 33 30 30 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `11 00 00 00` -> **17 bytes**.
*   **Body:**
    *   **Member `id` (long):** `14 05 00 00` (Value: 1300). Size: 4 bytes.
    *   **Member `value` (string):**
        *   **Length:** `09 00 00 00` (Length: 9 including null).
        *   **Chars:** `53 74 72 5f 31 33 30 30 00` ("Str_1300\0"). Size: 9 bytes.
*   **Total Body Size:** 4 (id) + 4 (str len) + 9 (chars) = **17 bytes**.

**Analysis:**
The C# serializer expected 20 bytes. It likely tried to align the 9-byte string content to a 4-byte boundary (9 -> 12), resulting in 4 + 4 + 12 = 20. Native XCDR2 packed it tightly.

---

## 3. AtomicTests::Nested3DTopicAppendable

**Issue:** Nested Struct DHEADER handling.

**IDL:**
```idl
@appendable
struct Point3DAppendable {
    double x;
    double y;
    double z;
};

@appendable
@topic
struct Nested3DTopicAppendable {
    @key long id;
    Point3DAppendable point;
};
```

**Golden CDR Bytes:**
`00 09 00 00 20 00 00 00 9a 08 00 00 18 00 00 00 ... (data) ...`

**Decoding:**
*   **Encapsulation:** `00 09 00 00`
*   **Outer DHEADER:** `20 00 00 00` -> **32 bytes**.
*   **Body:**
    *   **Member `id` (long):** `9a 08 00 00` (Value: 2202). Size: 4 bytes.
    *   **Member `point` (Nested Struct):**
        *   **Inner DHEADER:** `18 00 00 00` -> **24 bytes**.
        *   **Inner Body:**
            *   `x` (double): 8 bytes.
            *   `y` (double): 8 bytes.
            *   `z` (double): 8 bytes.
            *   Total Inner: 24 bytes.
*   **Total Outer Body:** 4 (id) + 4 (Inner DHEADER) + 24 (Inner Body) = **32 bytes**.

**Analysis:**
The C# failure (`Validation failed`) suggests it either didn't read the Inner DHEADER correctly (treating the first bytes of the point as data) or failed to calculate the offset for the nested struct. In XCDR2, an appendable struct inside another appendable struct MUST have its own DHEADER.

---

## 4. AtomicTests::OptionalInt32TopicAppendable

**Issue:** Optional Field Encoding (1 byte vs 4 bytes).

**IDL:**
```idl
@appendable
@topic
struct OptionalInt32TopicAppendable {
    @key long id;
    @optional long opt_value;
};
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 fd 08 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation:** `00 09 00 03`
*   **DHEADER:** `05 00 00 00` -> **5 bytes**.
*   **Body:**
    *   **Member `id` (long):** `fd 08 00 00` (Value: 2301). Size: 4 bytes.
    *   **Member `opt_value` (optional):**
        *   Byte: `00`.
        *   This is a boolean flag. `00` = False (Not present).
        *   Since it is not present, no value follows.
        *   Size: 1 byte.
*   **Total Body:** 4 + 1 = 5 bytes.

**Analysis:**
C# expected 8 bytes, suggesting it expected a 4-byte header/flag or alignment padding. XCDR2 encodes absent optional primitives in appendable types (when not using MemberHeaders) as a single byte `0`.

---

## 5. AtomicTests::BoundedSequenceInt32TopicAppendable

**Issue:** Sequence DHEADER vs Payload.

**IDL:**
```idl
@appendable
@topic
struct BoundedSequenceInt32TopicAppendable {
    @key long id;
    sequence<long, 10> values;
};
```

**Golden CDR Bytes:**
`00 09 00 00 14 00 00 00 36 08 00 00 03 00 00 00 36 08 00 00 37 08 00 00 38 08 00 00`

**Decoding:**
*   **Encapsulation:** `00 09 00 00`
*   **DHEADER:** `14 00 00 00` -> **20 bytes**.
*   **Body:**
    *   **Member `id` (long):** `36 08 00 00` (Value: 2102). Size: 4 bytes.
    *   **Member `values` (sequence):**
        *   **Length:** `03 00 00 00` (3 elements). Size: 4 bytes.
        *   **Elements:**
            *   `36 08 00 00` (2102)
            *   `37 08 00 00` (2103)
            *   `38 08 00 00` (2104)
        *   Elements Size: 3 * 4 = 12 bytes.
*   **Total Body:** 4 (id) + 4 (seq len) + 12 (data) = **20 bytes**.

**Analysis:**
Matches the DHEADER exactly. C# failure suggests improper calculation of the sequence size within the DHEADER context.

---

## 6. AtomicTests::UnionEnumDiscTopicAppendable

**Issue:** Union Discriminator Encoding in XCDR2.

**IDL:**
```idl
enum ColorEnum { RED, GREEN, BLUE, YELLOW, MAGENTA, CYAN }; // Red=0, Green=1, Blue=2...

@appendable
union ColorUnionAppendable switch(ColorEnum) {
    case RED: long red_data;
    case GREEN: double green_data;
    case BLUE: string<32> blue_data;
    case YELLOW: Point2DAppendable yellow_point;
};

@appendable
@topic
struct UnionEnumDiscTopicAppendable {
    @key long id;
    ColorUnionAppendable data;
};
```

**Golden CDR Bytes:**
`00 09 00 02 1a 00 00 00 d2 07 00 00 12 00 00 00 02 00 00 00 0a 00 00 00 42 6c 75 65 5f 32 30 30 32 00 00 00`

**Decoding:**
*   **Encapsulation:** `00 09 00 02`
*   **Outer DHEADER:** `1a 00 00 00` -> **26 bytes** (0x1A).
*   **Body:**
    *   **Member `id` (long):** `d2 07 00 00` (Value: 2002). Size: 4 bytes.
    *   **Member `data` (Union):**
        *   **Union DHEADER:** `12 00 00 00` -> **18 bytes** (0x12).
        *   **Discriminator (Enum):** `02 00 00 00` (Value: BLUE). Enums are 4 bytes.
        *   **Selected Member (blue_data - String):**
            *   **Length:** `0a 00 00 00` (10).
            *   **Content:** `42 6c 75 65 5f 32 30 30 32 00` ("Blue_2002\0"). Size: 10 bytes.
        *   **Total Union Body:** 4 (Disc) + 4 (StrLen) + 10 (Content) = 18 bytes.
*   **Total Outer Body:** 4 (id) + 4 (Union DHEADER) + 18 (Union Body) = **26 bytes**.

**Analysis:**
C# reported receiving `1a` (26) but serialized `1c` (28). This suggests C# added 2 bytes of padding to the string or the union structure.

---

## 7. AtomicTests::NestedKeyGeoTopicAppendable

**Issue:** Nested Struct Keys in Appendable Topic.

**IDL:**
```idl
@appendable
struct CoordinatesAppendable {
    @key double latitude;
    @key double longitude;
};

@appendable
@topic
struct NestedKeyGeoTopicAppendable {
    @key CoordinatesAppendable coords;
    string<128> location_name;
};
```

**Golden CDR Bytes:**
`00 09 00 00 1c 00 00 00 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 04 00 00 00 4c 6f 63 00`

**Decoding:**
*   **Encapsulation:** `00 09 00 00`
*   **Outer DHEADER:** `1c 00 00 00` -> **28 bytes**.
*   **Body:**
    *   **Member `coords` (Nested Key Struct):**
        *   **Inner DHEADER:** `10 00 00 00` -> **16 bytes**.
        *   **Latitude:** `00 00...` (0.0). 8 bytes.
        *   **Longitude:** `00 00...` (0.0). 8 bytes.
        *   Inner Body Total: 16 bytes.
    *   **Member `location_name` (String):**
        *   **Length:** `04 00 00 00`. 4 bytes.
        *   **Content:** `4c 6f 63 00` ("Loc\0"). 4 bytes.
*   **Total Outer Body:** 4 (Inner DHEADER) + 16 (Inner Body) + 4 (StrLen) + 4 (Str) = **28 bytes**.

**Analysis:**
The structure is: [DHEADER] [ [DHEADER_NESTED] [Lat] [Long] ] [StrLen] [Str].
The `coords` struct is treated as a nested appendable struct, so it gets a DHEADER.

---

## 8. AtomicTests::DeepNestedStructTopic (Final)

**Issue:** Plain CDR (XCDR1) validation failure.

**IDL:**
```idl
// Structure hierarchy implies flat layout of longs for XCDR1
@final
@topic
struct DeepNestedStructTopic {
    @key long id;
    Level1 nested1;
};
```

**Golden CDR Bytes:**
`00 01 00 00 cc 09 00 00 cc 09 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00`

**Decoding:**
*   **Header:** `00 01 00 00` (CDR LE).
*   **Body (24 bytes):**
    *   `id` (long): `cc 09 00 00` (2508).
    *   `nested1.value1` (long): `cc 09 00 00` (2508).
    *   `nested1.nested2.value2` (long): `00...` (0).
    *   `nested1.nested2.nested3.value3` (long): `00...` (0).
    *   `nested1.nested2.nested3.nested4.value4` (long): `00...` (0).
    *   `nested1.nested2.nested3.nested4.nested5.value5` (long): `00...` (0).
*   **Total:** 24 bytes of data.

**Analysis:**
The serialization is correct (flat longs). The failure "Validation failed" implies the C# object created from these bytes did not match the expected values (e.g., C# expected values other than 0 for the nested levels, or the test logic is flawed regarding the seed generation for nested types).

---

## 9. AtomicTests::OptionalFloat64TopicAppendable

**Issue:** `IndexOutOfRangeException` in Deserializer.

**IDL:**
```idl
@appendable
@topic
struct OptionalFloat64TopicAppendable {
    @key long id;
    @optional double opt_value;
};
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 fe 08 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation:** `00 09 00 03`
*   **DHEADER:** `05` (5 bytes).
*   **Body:**
    *   **ID:** `fe 08 00 00` (2302). 4 bytes.
    *   **Optional Flag:** `00` (False). 1 byte.
*   **Padding:** `00 00 00` (Ignore).

**Analysis:**
The C# Deserializer threw an `IndexOutOfRangeException` at `CdrReader.ReadDouble()`.
This confirms the C# deserializer saw the `Optional` definition and immediately tried to read a full `double` (8 bytes) or a 4-byte header, running off the end of the stream because the stream only contains a 1-byte "False" flag. It must check the flag first!

---

## Summary of Fixes Required for C# Serializer

1.  **XCDR2 DHEADER Calculation:** Calculate DHEADER size as the exact sum of bytes of the members. Do **not** apply struct end-padding to the DHEADER count.
2.  **XCDR2 Nested Structs:** If an `@appendable` struct contains another `@appendable` struct, the inner struct must be serialized with its own DHEADER (Length + Body).
3.  **XCDR2 Optional Fields:** For `@appendable` types, optional primitives are serialized as:
    *   If present: `byte(1)` + `value`.
    *   If absent: `byte(0)`.
    *   (Note: Verify if alignment is required *before* the value if present. Usually, yes, relative to stream/payload).
4.  **String/Sequence Sizing:** Do not pad the end of strings/sequences inside the DHEADER count.




Here is the detailed analysis of the remaining failing tests.

## Summary of Findings (New Patterns)

In addition to the patterns found previously (DHEADER sizing, Optional Flags), this set of tests reveals two critical behaviors in the native XCDR2 implementation:

1.  **Sequence Wrapping:** Sequences of **non-primitive** types (e.g., `sequence<string>`, `sequence<struct>`) are wrapped in their own **DHEADER** when inside an appendable struct. Sequences of primitives (e.g., `sequence<int>`, `sequence<float>`) are **not**.
2.  **Multi-Optional Packing:** Multiple `@optional` fields are serialized as a contiguous block of boolean flags (bytes) immediately following the previous field, before any of the optional values.
3.  **Strict Alignment Padding:** Alignment padding is inserted *between* fields to satisfy the alignment requirements of the *next* field (e.g., padding to 4 bytes before a String Length, or to 8 bytes before a Double), but it is **not** added to the end of the DHEADER unless strictly necessary.

---

## 1. AtomicTests::OptionalEnumTopicAppendable

**Issue:** 1-byte Optional Flag vs 4-byte expectation.

**IDL:**
```idl
enum SimpleEnum { FIRST, SECOND, THIRD }; // 32-bit int

@appendable
@topic
struct OptionalEnumTopicAppendable {
    @key long id;
    @optional SimpleEnum opt_enum;
};
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 01 09 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `05 00 00 00` (5 bytes).
*   **Body:**
    *   **`id`:** `01 09 00 00` (2305). Size: 4.
    *   **`opt_enum` flag:** `00` (Absent). Size: 1.
*   **Total:** 5 bytes.

**Fix:** Treat optional Enums in appendable structs exactly like optional primitives: 1 byte flag.

---

## 2. AtomicTests::ThreeKeyTopicAppendable

**Issue:** DHEADER Mismatch due to String/Short alignment.

**IDL:**
```idl
@appendable
@topic
struct ThreeKeyTopicAppendable {
    @key long key1;
    @key string<32> key2;
    @key short key3;
    double value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 14 00 00 00 63 09 00 00 02 00 00 00 4b 00 00 00 00 00 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `14 00 00 00` (**20 bytes**).
*   **Body:**
    *   **`key1` (long):** `63 09 00 00` (2403). Size: 4.
    *   **`key2` (string):**
        *   Length: `02 00 00 00`.
        *   Data: `4b 00` ("K\0").
        *   Size: 4 + 2 = 6 bytes.
    *   **`key3` (short):** `00 00`. Size: 2.
        *   *Note:* Current offset = 4 + 6 = 10. `short` needs 2-byte align. 10 is aligned. No padding.
    *   **`value` (double):** `00 00 00 00 00 00 00 00`. Size: 8.
        *   *Note:* Current offset = 12. `double` needs 8-byte align.
        *   **CRITICAL:** The raw bytes show NO padding between `key3` and `value`.
        *   Bytes: `[k1:4] [str_len:4] [str_char:2] [k3:2] [val:8]` = 20 bytes.
        *   Wait, 12 is NOT 8-byte aligned. XCDR2 standard requires alignment relative to the encoding start.
        *   However, if `xcdr1` rules or specific `appendable` optimizations apply, alignment might be ignored?
        *   *Re-reading dump:* `63 09 00 00` (4) | `02 00 00 00` (4) | `4b 00` (2) | `00 00` (2) | `00...` (8).
        *   This sums to 20.
        *   **Conclusion:** In this specific layout, the native serializer did **not** pad the double to 8 bytes. This implies the struct packing might be tighter than standard C# alignment rules expect.

---

## 3. AtomicTests::OptionalStructTopicAppendable

**Issue:** Optional Flag logic for Structs.

**IDL:**
```idl
@appendable
@topic
struct OptionalStructTopicAppendable {
    @key long id;
    @optional Point2DAppendable opt_point;
};
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 00 09 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `05 00 00 00` (5 bytes).
*   **Body:**
    *   **`id`:** `00 09 00 00` (2304). Size: 4.
    *   **`opt_point` flag:** `00` (Absent). Size: 1.
*   **Total:** 5 bytes.

**Fix:** Optional Structs in appendable types behave like optional primitives: 1 byte flag if absent.

---

## 4. AtomicTests::SequenceStringTopicAppendable

**Issue:** **Non-Primitive Sequence Wrapping.**

**IDL:**
```idl
@appendable
@topic
struct SequenceStringTopicAppendable {
    @key long id;
    sequence<String32> values;
};
```

**Golden CDR Bytes:**
`00 09 00 00 0c 00 00 00 3b 08 00 00 04 00 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `0c 00 00 00` (**12 bytes**).
*   **Body:**
    *   **`id`:** `3b 08 00 00` (2107). Size: 4.
    *   **Sequence Member:**
        *   **Wrapper DHEADER:** `04 00 00 00` (4 bytes).
        *   **Sequence Length:** `00 00 00 00` (0). Size: 4.
*   **Total:** 4 (id) + 4 (Wrapper) + 4 (Len) = 12 bytes.

**Analysis:**
Unlike `sequence<int>`, `sequence<string>` (complex type) is wrapped in a DHEADER. The "wrapper" tells us the size of the sequence payload (which is just the 4-byte length `0`).

**Fix:** If serializing a sequence of constructed types (strings, structs) inside an appendable struct, wrap it in a DHEADER.

---

## 5. AtomicTests::SequenceStructTopicAppendable

**Issue:** **Non-Primitive Sequence Wrapping.**

**IDL:**
```idl
@appendable
@topic
struct SequenceStructTopicAppendable {
    @key long id;
    sequence<Point2D> points;
};
```

**Golden CDR Bytes:**
`00 09 00 00 0c 00 00 00 3c 08 00 00 04 00 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `0c` (12 bytes).
*   **Body:**
    *   **`id`:** `3c 08...` (2108). Size: 4.
    *   **Sequence Member:**
        *   **Wrapper DHEADER:** `04 00 00 00`.
        *   **Sequence Length:** `00 00 00 00`.
*   **Total:** 12 bytes.

**Fix:** Same as String sequence. `sequence<Struct>` gets a DHEADER wrapper.

---

## 6. AtomicTests::MultiOptionalTopicAppendable

**Issue:** **Contiguous Optional Flags.**

**IDL:**
```idl
@appendable
@topic
struct MultiOptionalTopicAppendable {
    @key long id;
    @optional long opt_int;
    @optional double opt_double;
    @optional string<32> opt_string;
};
```

**Golden CDR Bytes:**
`00 09 00 01 07 00 00 00 02 09 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `07 00 00 00` (7 bytes).
*   **Body:**
    *   **`id`:** `02 09 00 00` (2306). Size: 4.
    *   **Flags:**
        *   `opt_int`: `00` (False).
        *   `opt_double`: `00` (False).
        *   `opt_string`: `00` (False).
*   **Total:** 4 + 1 + 1 + 1 = 7 bytes.

**Fix:** Serialize all boolean presence flags for consecutive optional fields contiguously before writing any values. Do not pad between flags.

---

## 7. AtomicTests::TwoKeyStringTopicAppendable

**Issue:** Alignment Padding within Payload.

**IDL:**
```idl
@appendable
@topic
struct TwoKeyStringTopicAppendable {
    @key string<32> key1;
    @key string<32> key2;
    double value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 18 00 00 00 03 00 00 00 4b 31 00 00 03 00 00 00 4b 32 00 00 00 00 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `18 00 00 00` (**24 bytes**).
*   **Body:**
    *   **`key1`:**
        *   Length: `03 00 00 00`. (4 bytes).
        *   Chars: `4b 31 00` ("K1\0"). (3 bytes).
    *   **Padding:** `00`. (1 byte). Aligns cursor to 4 bytes for next field length.
    *   **`key2`:**
        *   Length: `03 00 00 00`. (4 bytes).
        *   Chars: `4b 32 00` ("K2\0"). (3 bytes).
    *   **Padding:** `00`. (1 byte). Aligns cursor to 8 bytes for double? No, current pos is 4+3+1 + 4+3+1 = 16. 16 is 8-byte aligned.
    *   **`value`:** `00 00 00 00 00 00 00 00`. (8 bytes).
*   **Total:** 8 (Key1 aligned) + 8 (Key2 aligned) + 8 (Value) = 24 bytes.

**Fix:** Ensure alignment padding (to 4 bytes) is inserted after string content if the next field requires 4-byte alignment (like `key2` length).

---

## 8. AtomicTests::NestedStructAppendable

**Issue:** Nested DHEADER.

**IDL:**
```idl
@appendable
struct Point2DAppendable { double x; double y; };
@appendable
@topic
struct NestedStructTopicAppendable { @key long id; Point2DAppendable point; };
```

**Golden CDR Bytes:**
`00 09 00 00 18 00 00 00 99 08 00 00 10 00 00 00 ...`

**Decoding:**
*   **Outer DHEADER:** `18` (24).
*   **`id`:** `99 08...` (4).
*   **`point` (Nested):**
    *   **Inner DHEADER:** `10` (16).
    *   **Body:** 16 bytes (2 doubles).
*   **Total:** 4 + 4 + 16 = 24.

**Fix:** Ensure nested appendable structs are prefixed with their own DHEADER.

---

## 9. AtomicTests::UnboundedStringTopicAppendable

**Issue:** DHEADER sizing for simple string.

**IDL:**
```idl
@appendable
@topic
struct UnboundedStringTopicAppendable { @key long id; string value; };
```

**Golden CDR Bytes:**
`00 09 00 02 0a 00 00 00 c6 09 00 00 02 00 00 00 53 00 00 00`

**Decoding:**
*   **DHEADER:** `0a` (10 bytes).
*   **Body:**
    *   **`id`:** `c6 09...` (4 bytes).
    *   **`value`:**
        *   Length: `02 00 00 00`. (4 bytes).
        *   Chars: `53 00` ("S\0"). (2 bytes).
*   **Total:** 4 + 4 + 2 = 10.

**Fix:** Do not pad the string at the end of the struct.

---

## 10. AtomicTests::DeepNestedStructTopicAppendable

**Issue:** Recursive Nested DHEADERs.

**IDL:**
```idl
// Level1 -> Level2 -> ... Level5. All Appendable.
```

**Golden CDR Bytes:**
`00 09 00 00 2c 00 00 00 cd 09 00 00 24 00 00 00 cd 09 00 00 1c 00 00 00 ...`

**Decoding:**
*   **L1 DHEADER:** `2c` (44 bytes).
    *   `id`: 4 bytes.
    *   **L2 DHEADER:** `24` (36 bytes).
        *   `val1`: 4 bytes.
        *   **L3 DHEADER:** `1c` (28 bytes).
            *   ...and so on.

**Fix:** Recursively calculate DHEADERs for every layer.

---

## 11. AtomicTests::DoublyNestedTopicAppendable

**Issue:** Nested DHEADERs (Struct containing Struct).

**IDL:**
```idl
@appendable struct BoxAppendable { Point2DAppendable p1; Point2DAppendable p2; };
```

**Golden CDR Bytes:**
`00 09 00 00 30 00 00 00 9b 08 00 00 28 00 00 00 10 00 00 00 ...`

**Decoding:**
*   **Outer DHEADER:** `30` (48).
*   `id`: 4.
*   **Box DHEADER:** `28` (40).
    *   **P1 DHEADER:** `10` (16).
        *   Data: 16.
    *   **P2 DHEADER:** `10` (16).
        *   Data: 16.
*   Total: 4 + 4 + (4+16+4+16) = 48.

**Fix:** Serialize DHEADER for `box`, then DHEADER for `p1`, then data, then DHEADER for `p2`, then data.

---

## 12. AtomicTests::SequenceFloat32Appendable (and Int32/Float64/Octet/Boolean)

**Issue:** Primitive Sequences vs. DHEADER.

**IDL:**
```idl
sequence<float> values;
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 37 08 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `08` (8 bytes).
*   **Body:**
    *   `id`: 4.
    *   `seq_len`: 4 (Value 0).
*   **Total:** 8.

**Analysis:**
Primitive sequences are **NOT** wrapped in an internal DHEADER. They are serialized directly.

---

## 13. AtomicTests::SequenceOctetAppendable

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 3a 08 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** 8.
*   `id`: 4.
*   `seq_len`: 4.

**Analysis:**
Consistent with `SequenceFloat32`. Primitive sequence = No wrapper.

---

## 14. AtomicTests::ComplexNestedAppendable

**Issue:** Mixed DHEADERs.

**IDL:**
```idl
struct ContainerAppendable { long count; Point3DAppendable center; double radius; }
```

**Golden CDR Bytes:**
`00 09 00 00 30 00 00 00 ...` (Outer DHEADER 48).

**Decoding:**
*   `id`: 4.
*   **Container DHEADER:** `28 00 00 00` (40).
    *   `count`: 4.
    *   **Center DHEADER:** `18 00 00 00` (24).
        *   `x,y,z`: 24.
    *   `radius`: 8.
    *   Container Body: 4 + 4 + 24 + 8 = 40.
*   Total: 4 + 4 + 40 = 48.

**Fix:** Recursively apply DHEADERs to the nested `Container` and the nested `Center`.

---

## 15. AtomicTests::MaxLengthSequenceTopic (Final)

**Issue:** XCDR1 serialization of bounded sequence.

**Golden CDR Bytes:**
`00 01 00 00 ca 09 00 00 00 00 00 00`

**Decoding:**
*   **Header:** `00 01` (XCDR1 LE).
*   **`id`:** `ca 09...` (4).
*   **`seq_len`:** `00 00 00 00` (4).
*   **Total:** 8 bytes.

**Analysis:**
The C# test received 12 bytes but failed. Wait, the log says received 12 bytes: `00 01 00 00 ca 09 00 00 00 00 00 00`.
Actually, that is 12 bytes total (4 header + 8 body).
The failure "Validation failed" implies the C# deserializer constructed a Sequence correctly (size 0) but maybe the `MaxLength` property caused it to pre-allocate or expect padding bytes that weren't there. In XCDR1, `sequence<T, N>` is serialized exactly like `sequence<T>`: Length + Data. No capacity padding.

---

## Conclusion & Action Plan

To fix the C# implementation, enforce the following rules for **XCDR2 (@appendable)** serialization:

1.  **DHEADER Calculation:** Sum the size of members exactly. Do **not** round up the total body size for alignment unless a subsequent field *inside* that DHEADER scope requires it.
2.  **Optional Fields:**
    *   Primitives/Enums/Structs: Serialize as **1 byte** (0=Absent, 1=Present). Do not add alignment or headers if absent.
    *   Multiple Optionals: Serialize flags contiguously (e.g., 3 optional fields = 3 bytes of flags).
3.  **Sequences:**
    *   **Primitive** (`long`, `float`, `byte`, `bool`): Serialize Length + Data. **No DHEADER wrapper.**
    *   **Constructed** (`string`, `struct`, `union`): Serialize as **DHEADER (4 bytes) + Length + Data**. The DHEADER value is (LengthSize + DataSize).
4.  **Nested Structs:**
    *   If the inner struct is `@appendable`, it **must** be prefixed by its own DHEADER.
5.  **Alignment:**
    *   Insert alignment padding bytes (0x00) *after* a variable-length field (like String) only if the *next* field requires it.





# CONSOLIDATED DDS SERIALIZATION RULES
**Derived from empirical analysis of 110 CycloneDDS Test Cases**

This document serves as the technical specification for implementing the C# Serializer/Deserializer to match native CycloneDDS behavior.

## 1. Encoding Formats

*   **@final Types (XCDR1 / Plain CDR):**
    *   Encapsulation Header: `00 01 00 00` (Little Endian) or `00 01 00 03` (if padding is required/used).
    *   Strict alignment rules relative to the **Stream Start**.
    *   Padding is inserted **before** fields to meet alignment.
    *   Padding is inserted **at the end** of structs to align to the largest member's alignment (usually 4 or 8).

*   **@appendable / @mutable Types (XCDR2 / PL_CDR2):**
    *   Encapsulation Header: `00 09 00 00` (Little Endian) or `00 09 00 02` / `00 09 00 03`.
    *   Uses **DHEADER** (Delimiter Header) before composite types.
    *   Alignment is tighter; end-of-struct padding is suppressed inside DHEADERs.

## 2. XCDR2 (@appendable) Specific Rules

### A. DHEADER Calculation
*   **Definition:** A 4-byte unsigned integer indicating the size of the object body.
*   **Calculation:** `Size = Sum(Member_Sizes)`.
*   **Rule:** Do **NOT** include "End of Struct" alignment padding in the DHEADER count. The DHEADER count reflects exactly the bytes used by the members.

### B. Sequence Serialization
The most critical distinction found in the tests is how sequences are serialized within an appendable struct:

1.  **Primitive Sequences:** ( `sequence<bool/byte/char/short/int/long/float/double>` )
    *   **NO Wrapper DHEADER.**
    *   Format: `[Length (4)]` + `[Data (N * Size)]`.
2.  **Constructed Sequences:** ( `sequence<String>`, `sequence<Struct>`, `sequence<Union>` )
    *   **HAS Wrapper DHEADER.**
    *   Format: `[DHEADER (4)]` + `[Length (4)]` + `[Data]`.
    *   *Note:* The DHEADER value = `4 (Length size) + Data size`.
3.  **Enum Sequences:** ( `sequence<Enum>` )
    *   **HAS Wrapper DHEADER.**
    *   *Crucial:* Enums are treated as constructed types in this context.

### C. Optional Fields
1.  **Primitives / Enums / Structs:**
    *   **Absent:** Serialize **1 byte** `0x00`.
    *   **Present:** Serialize **1 byte** `0x01` followed by the value (aligned).
2.  **Strings:**
    *   **Absent:** Serialize **1 byte** `0x00`.
3.  **Multiple Optionals:**
    *   Serialize all presence flags contiguously as bytes (`0x00` or `0x01`) before serializing any values.
    *   Example: 3 optionals -> 3 bytes of flags -> (Padding if needed) -> Value 1 -> ...

### D. Nested Structs
*   If an `@appendable` struct contains another `@appendable` struct member, the inner struct **MUST** be prefixed by its own DHEADER.

### E. Unions in XCDR2
*   **Structure:** `[DHEADER] + [Discriminator] + [Selected Member]`.
*   **Discriminator:** Serialized as 4 bytes (even for boolean/short) in observed tests, or aligned to 4 bytes.
*   **Alignment:** Unlike XCDR1, XCDR2 unions showed **no padding** between a 4-byte discriminator and an 8-byte value (e.g., double) in specific test cases (Test 106). The payload is packed tightly.

## 3. XCDR1 (@final) Specific Rules

### A. Alignment & Padding
*   **Start Alignment:** All alignment is calculated relative to the 4-byte Encapsulation Header.
    *   *Offset 0* = First byte of Encapsulation Header.
    *   *Offset 4* = First byte of Payload.
*   **Internal Padding:** Insert `0x00` bytes *before* a field if the current `Offset % Alignment != 0`.
*   **Trailing Padding:** Insert `0x00` bytes *after* the last field until `TotalSize % MaxMemberAlignment == 0`.
    *   *Example:* A struct with `long` (4) + `char` (1) has size 5. Max alignment is 4. Pad 3 bytes -> Total 8.

### B. Sequences
*   Format: `[Length (4)]` + `[Padding (if needed)]` + `[Data]`.
*   **Padding Rule:** Unlike XCDR2, there is no DHEADER wrapper. However, alignment rules apply to the *elements*. If the element type requires 8-byte alignment (e.g., `double`), and the Length takes 4 bytes, the data is usually written immediately (Offset 4 relative to seq start) because standard CDR often aligns sequence elements relative to the *buffer*, not the sequence container.
*   *Observation:* Tests 102/103 (`int64`/`double` sequence) showed **NO padding** between Length(4) and Data(8). This suggests the native serializer treats the sequence buffer start as the alignment reference, or `int64`/`double` inside sequences in this implementation tolerate 4-byte alignment.

### C. Strings
*   Format: `[Length (4)]` + `[Characters]` + `[Null Terminator]`.
*   **Padding:** The total string size (4 + chars + 1) is padded to align to 4 bytes (or the max struct alignment) if it is the last field or followed by a field requiring alignment.

## 4. Common Pitfalls Checklist for Developer

1.  [ ] **Do not wrap** primitive sequences (`int`, `double`) in DHEADERs in XCDR2.
2.  [ ] **Do wrap** Enum sequences in DHEADERs in XCDR2.
3.  [ ] **Do wrap** String sequences in DHEADERs in XCDR2.
4.  [ ] **Do not add padding** to the end of XCDR2 DHEADERs.
5.  [ ] **Check Optional Flags:** Read 1 byte. Do not assume 4-byte headers for optionals in XCDR2.
6.  [ ] **Recursion:** Ensure `GetSize()` calls are recursive for nested appendable types to calculate DHEADERs correctly.
7.  [ ] **Multi-Optional:** Read *all* flags for a scope before reading *any* values.
8.  [ ] **Visual Debugging:** Don't confuse the lower 32-bits of a `double` (often `00 00 00 00`) with padding. Check the values.
---
