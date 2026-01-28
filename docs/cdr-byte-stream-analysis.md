# Analysis of CDR byte stream from a native Cyclone DDS sender
This document contains the detailed analysis of a CDR byte stream produced by a native Cyclone DDS implementation.
It shows using examples how to understand the data encoding when implementing serialization and deserialization for
test topics combining different of data types and extensibility.
---

## Summary for Serializer Developer

The analysis of 110 tests reveals distinct rules for XCDR1 (Final) and XCDR2 (Appendable):

1.  **XCDR2 (Appendable) Rules:**
    *   **DHEADER Sizing:** Strictly `Sum(MemberSizes)`. Do **not** include end-of-struct alignment padding in the DHEADER count.
    *   **Primitives:** Tight packing. 8-byte types (double, uint64) often appear 4-byte aligned relative to the DHEADER start, not 8-byte aligned.
    *   **Sequences:**
        *   `sequence<int/float/bool...>` (Primitive): **Raw** (Length + Data). No DHEADER.
        *   `sequence<Enum>`: **Wrapped** (DHEADER + Length + Data).
        *   `sequence<String/Struct/Union>`: **Wrapped** (DHEADER + Length + Data).
    *   **Optionals:** Absent field = **1 byte** (`00`). Present field = **1 byte** (`01`) + Value. Multiple optionals = Contiguous byte flags.
    *   **Nested Structs:** Inner appendable structs must have their own DHEADER.

2.  **XCDR1 (Final) Rules:**
    *   **Padding:** Strict alignment padding is inserted *before* fields to satisfy alignment requirements relative to the stream start.
    *   **Struct Size:** Structs are padded at the end to a multiple of their largest member's alignment.
    *   **Sequences:** Length (4 bytes) + Data. Data alignment depends on element type, but observed behavior for `double/int64` showed no padding after length (offset 12).

3.  **Union Discriminators:**
    *   XCDR2: Discriminator (4 bytes usually, even for boolean/short in DHEADER layout?) -> Value. **No padding** between Disc and Value observed in Appendable Unions.
    *   XCDR1: Discriminator -> Padding (to align Value) -> Value.

4.  **Enums:**
    *   Treated as 32-bit integers, EXCEPT in XCDR2 sequences where they trigger "Wrapped" serialization logic.

---


### Test List

1.  AtomicTests::ArrayFloat64Topic
2.  AtomicTests::ArrayStringTopic
3.  AtomicTests::ArrayStructTopic
4.  AtomicTests::Array2DInt32Topic
5.  AtomicTests::ArrayFloat64TopicAppendable
6.  AtomicTests::Array3DInt32Topic
7.  AtomicTests::ArrayStringTopicAppendable
8.  AtomicTests::ArrayInt32Topic
9.  AtomicTests::ArrayInt32TopicAppendable
10. AtomicTests::CharTopicAppendable
11. AtomicTests::Float64TopicAppendable
12. AtomicTests::Float32TopicAppendable
13. AtomicTests::Int16TopicAppendable
14. AtomicTests::BooleanTopicAppendable
15. AtomicTests::StringBounded32TopicAppendable
16. AtomicTests::Int64TopicAppendable
17. AtomicTests::StringUnboundedTopicAppendable
18. AtomicTests::Int32TopicAppendable
19. AtomicTests::ColorEnumTopicAppendable
20. AtomicTests::UInt64TopicAppendable
21. AtomicTests::EnumTopicAppendable
22. AtomicTests::UInt16TopicAppendable
23. AtomicTests::UInt32TopicAppendable
24. AtomicTests::StringBounded256TopicAppendable
25. AtomicTests::OctetTopicAppendable
26. AtomicTests::StringBounded256Topic
27. AtomicTests::Float32Topic
28. AtomicTests::StringUnboundedTopic
29. AtomicTests::CharTopic
30. AtomicTests::Int32Topic
31. AtomicTests::EnumTopic
32. AtomicTests::Int16Topic
33. AtomicTests::BooleanTopic
34. AtomicTests::ColorEnumTopic
35. AtomicTests::Float64Topic
36. AtomicTests::UInt16Topic
37. AtomicTests::StringBounded32Topic
38. AtomicTests::OctetTopic
39. AtomicTests::UInt32Topic
40. AtomicTests::Int64Topic
41. AtomicTests::UInt64Topic
42. AtomicTests::TwoKeyStringTopic
43. AtomicTests::ThreeKeyTopic
44. AtomicTests::FourKeyTopic
45. AtomicTests::TwoKeyInt32Topic
46. AtomicTests::NestedKeyTopic
47. AtomicTests::NestedTripleKeyTopic
48. AtomicTests::NestedKeyGeoTopic
49. AtomicTests::NestedStructTopic
50. AtomicTests::ComplexNestedTopic
51. AtomicTests::Nested3DTopic
52. AtomicTests::DoublyNestedTopic
53. AtomicTests::Nested3DTopicAppendable
54. AtomicTests::MaxSizeStringTopicAppendable
55. AtomicTests::EmptySequenceTopicAppendable
56. AtomicTests::UnionWithOptionalTopicAppendable
57. AtomicTests::OptionalInt32TopicAppendable
58. AtomicTests::FourKeyTopicAppendable
59. AtomicTests::OptionalEnumTopicAppendable
60. AtomicTests::ThreeKeyTopicAppendable
61. AtomicTests::OptionalStructTopicAppendable
62. AtomicTests::UnboundedStringTopicAppendable
63. AtomicTests::BoundedSequenceInt32TopicAppendable
64. AtomicTests::UnionEnumDiscTopicAppendable
65. AtomicTests::AllPrimitivesAtomicTopicAppendable
66. AtomicTests::SequenceFloat32TopicAppendable
67. AtomicTests::MaxSizeStringTopic
68. AtomicTests::NestedKeyGeoTopicAppendable
69. AtomicTests::OptionalStringTopicAppendable
70. AtomicTests::SequenceFloat64TopicAppendable
71. AtomicTests::TwoKeyStringTopicAppendable
72. AtomicTests::ComplexNestedTopicAppendable
73. AtomicTests::UnionBoolDiscTopicAppendable
74. AtomicTests::MaxLengthSequenceTopic
75. AtomicTests::UnionWithOptionalTopic
76. AtomicTests::DeepNestedStructTopicAppendable
77. AtomicTests::TwoKeyInt32TopicAppendable
78. AtomicTests::SequenceStringTopicAppendable
79. AtomicTests::MaxLengthSequenceTopicAppendable
80. AtomicTests::SequenceInt64TopicAppendable
81. AtomicTests::DoublyNestedTopicAppendable
82. AtomicTests::UnionShortDiscTopicAppendable
83. AtomicTests::SequenceOctetTopicAppendable
84. AtomicTests::SequenceBooleanTopicAppendable
85. AtomicTests::MultiOptionalTopicAppendable
86. AtomicTests::NestedStructTopicAppendable
87. AtomicTests::SequenceStructTopicAppendable
88. AtomicTests::NestedTripleKeyTopicAppendable
89. AtomicTests::NestedKeyTopicAppendable
90. AtomicTests::DeepNestedStructTopic
91. AtomicTests::OptionalFloat64TopicAppendable
92. AtomicTests::SequenceStructTopic
93. AtomicTests::SequenceFloat32Topic
94. AtomicTests::BoundedSequenceInt32Topic
95. AtomicTests::SequenceStringTopic
96. AtomicTests::SequenceUnionAppendableTopic
97. AtomicTests::SequenceInt32Topic
98. AtomicTests::SequenceEnumAppendableTopic
99. AtomicTests::SequenceInt32TopicAppendable
100. AtomicTests::SequenceUnionTopic
101. AtomicTests::SequenceEnumTopic
102. AtomicTests::SequenceInt64Topic
103. AtomicTests::SequenceFloat64Topic
104. AtomicTests::SequenceBooleanTopic
105. AtomicTests::SequenceOctetTopic
106. AtomicTests::UnionLongDiscTopicAppendable
107. AtomicTests::UnionBoolDiscTopic
108. AtomicTests::UnionLongDiscTopic
109. AtomicTests::UnionShortDiscTopic
110. AtomicTests::UnionEnumDiscTopic

---

### 1. AtomicTests::ArrayFloat64Topic

**IDL Definition:**
```idl
@final
@topic
struct ArrayFloat64Topic {
    @key long id;
    double values[5];
};
```

**Golden CDR Bytes:**
`00 01 00 00 9a 01 00 00 00 00 00 00 01 00 00 00 00 30 7c 40 9a 99 99 99 99 41 7c 40 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
    *   Kind: `CDR_LE` (XCDR1 Little Endian).
    *   Options: 0.
*   **Member `id` (long):** `9a 01 00 00`
    *   Value: 410.
    *   Size: 4 bytes.
    *   Current Alignment: 4 (Header) + 4 (id) = 8.
*   **Padding:** `00 00 00 00`
    *   Why? The next field `values` is an array of `double`. Doubles require **8-byte alignment**.
    *   Current offset relative to body start is 4. Need to advance to 8.
    *   Size: 4 bytes.
*   **Member `values` (double[5]):**
    *   **Value[0]:** `01 00 00 00 00 30 7c 40` (Value: ~410.0001).
    *   **Value[1]:** `9a 99 99 99 99 41 7c 40` (Value: ~410.1).
    *   ... remaining 3 doubles ...

**Analysis:**
Standard XCDR1 alignment. The compiler inserted 4 bytes of padding after the `long` ID to align the `double` array to an 8-byte boundary.

---

### 2. AtomicTests::ArrayStringTopic

**IDL Definition:**
```idl
@final
@topic
struct ArrayStringTopic {
    @key long id;
    string<16> names[5];
};
```

**Golden CDR Bytes:**
`00 01 00 00 a4 01 00 00 08 00 00 00 53 5f 34 32 30 5f 30 00 08 00 00 00 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `a4 01 00 00`
    *   Value: 420.
*   **Member `names` (string[5]):**
    *   **Name[0] Length:** `08 00 00 00` (8).
        *   Note: No padding needed. `long` (id) ends at offset 4. String length (long) needs 4-byte alignment. 4 is a multiple of 4.
    *   **Name[0] Chars:** `53 5f 34 32 30 5f 30 00` ("S_420_0\0").
        *   Size: 8 bytes.
    *   **Name[1] Length:** `08 00 00 00`.
        *   Previous string ended at offset 4+4+8 = 16. 16 is aligned to 4. No padding.
    *   ... remaining strings ...

**Analysis:**
Standard XCDR1. Arrays of strings are serialized contiguously. Padding would only appear if a string length + content didn't end on a 4-byte boundary.

---

### 3. AtomicTests::ArrayStructTopic

**IDL Definition:**
```idl
@final
struct Point2D { double x; double y; };

@final
@topic
struct ArrayStructTopic {
    @key long id;
    Point2D points[3];
};
```

**Golden CDR Bytes:**
`00 01 00 00 fe 01 00 00 00 00 00 00 00 00 00 00 00 e0 7f 40 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `fe 01 00 00`
    *   Value: 510.
*   **Padding:** `00 00 00 00`
    *   Why? `Point2D`'s first member is `double`. Struct alignment is determined by its largest member (8 bytes).
    *   Offset is 4. Need to align to 8.
*   **Member `points` (Point2D[3]):**
    *   **Point[0].x:** `00 00 00 00 00 e0 7f 40`.
    *   **Point[0].y:** ...
    *   **Point[1].x:** ...

**Analysis:**
Standard XCDR1. Struct array requires alignment based on the struct's alignment requirement.

---

### 4. AtomicTests::Array2DInt32Topic

**IDL Definition:**
```idl
@final
@topic
struct Array2DInt32Topic {
    @key long id;
    long matrix[3][4];
};
```

**Golden CDR Bytes:**
`00 01 00 00 f4 01 00 00 f4 01 00 00 f5 01 00 00 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `f4 01 00 00` (500).
*   **Member `matrix` (long[3][4]):**
    *   Multidimensional arrays are flattened. Total elements: 12 longs.
    *   **[0][0]:** `f4 01 00 00` (500).
    *   **[0][1]:** `f5 01 00 00` (501).
    *   ...

**Analysis:**
Standard XCDR1. Multidimensional arrays are flattened into a contiguous sequence of elements.

---

### 5. AtomicTests::ArrayFloat64TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct ArrayFloat64TopicAppendable {
    @key long id;
    long dummy_pad; // <--- Note this explicit field
    double values[5];
};
```

**Golden CDR Bytes:**
`00 09 00 00 30 00 00 00 82 05 00 00 00 00 00 00 01 00 00 00 00 3c 98 40 ...`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00` (PL_CDR2 LE).
*   **DHEADER:** `30 00 00 00`
    *   Value: 48 bytes.
*   **Member `id` (long):** `82 05 00 00`
    *   Value: 1410. Size: 4.
*   **Member `dummy_pad` (long):** `00 00 00 00`
    *   Value: 0. Size: 4.
    *   Current Offset: 4+4=8.
*   **Member `values` (double[5]):**
    *   **Value[0]:** `01 00 00 00 00 3c 98 40`.
    *   Alignment: Offset 8 is 8-byte aligned. No padding needed.
    *   Size: 5 * 8 = 40 bytes.
*   **Total Body Size:** 4 + 4 + 40 = 48 bytes. Matches DHEADER.

**Analysis:**
This test passes because the IDL manually inserted `dummy_pad`. If `dummy_pad` were missing, XCDR2 would typically *not* add padding after `id` inside the DHEADER count, but the serializer might need to insert alignment padding *bytes* during serialization if `values` weren't aligned. Here, the structure is perfectly packed.

---

### 6. AtomicTests::Array3DInt32Topic

**IDL Definition:**
```idl
@final
@topic
struct Array3DInt32Topic {
    @key long id;
    long cube[2][3][4];
};
```

**Golden CDR Bytes:**
`00 01 00 00 08 02 00 00 08 02 00 00 09 02 00 00 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `08 02 00 00` (520).
*   **Member `cube`:**
    *   Flattened 24 longs (2*3*4).
    *   **[0][0][0]:** `08 02 00 00` (520).
    *   **[0][0][1]:** `09 02 00 00` (521).

**Analysis:**
Standard XCDR1. 3D arrays are flattened just like 2D arrays.

---

### 7. AtomicTests::ArrayStringTopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct ArrayStringTopicAppendable {
    @key long id;
    string<16> names[5];
};
```

**Golden CDR Bytes:**
`00 09 00 03 55 00 00 00 8c 05 00 00 4d 00 00 00 09 00 00 00 53 5f 31 34 32 30 5f 30 00 ...`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER (Outer):** `55 00 00 00`
    *   Value: 85 bytes.
*   **Member `id` (long):** `8c 05 00 00` (1420). Size: 4.
*   **Member `names` (string[5]):**
    *   **Array DHEADER (Wrapper):** `4d 00 00 00`
        *   **CRITICAL FINDING:** In XCDR2, arrays of constructed types (like strings) inside appendable structs are wrapped in their own DHEADER.
        *   Value: 77 bytes.
    *   **Array Content:**
        *   **Str[0] Len:** `09 00 00 00`.
        *   **Str[0] Val:** `53 5f 31 34 32 30 5f 30 00` ("S_1420_0\0"). Size: 9.
        *   (Repeat for 5 strings).
        *   Total String Content: 5 * (4 + 9) = 65 bytes.
        *   Wait, 65 != 77. There must be padding?
        *   Let's check alignment.
        *   Str[0]: 4+9 = 13 bytes.
        *   Next is Str[1] Length. Lengths are `long` (4 bytes).
        *   Offset 13 requires 3 bytes padding to reach 16.
        *   So: (4+9+3) * 4 strings + (4+9) last string?
        *   16 * 4 + 13 = 64 + 13 = 77.
        *   **YES.**
    *   **Array DHEADER Verification:** 77 bytes matches the padded size of the array content.
*   **Outer DHEADER Verification:** 4 (id) + 4 (Array DHEADER) + 77 (Array Body) = 85 bytes. Matches exactly.

**Analysis:**
This test reveals two complex XCDR2 behaviors:
1.  **Array Wrapping:** Arrays of strings are prefixed with a DHEADER.
2.  **Element Alignment:** Within the array, each variable-length element is padded (0x00) to ensure the *next* element starts on a 4-byte boundary.

---

### 8. AtomicTests::ArrayInt32Topic

**IDL Definition:**
```idl
@final
@topic
struct ArrayInt32Topic {
    @key long id;
    long values[5];
};
```

**Golden CDR Bytes:**
`00 01 00 00 90 01 00 00 90 01 00 00 91 01 00 00 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `90 01 00 00` (400).
*   **Member `values` (long[5]):**
    *   Contiguous longs.
    *   Value[0]: `90 01 00 00` (400).
    *   Value[1]: `91 01 00 00` (401).

**Analysis:**
Standard XCDR1. Simple contiguous array of primitives.


Here is the detailed CDR stream analysis for the second batch of test cases (Primitive Appendable Types).

### 9. AtomicTests::ArrayInt32TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct ArrayInt32TopicAppendable {
    @key long id;
    long values[5];
};
```

**Golden CDR Bytes:**
`00 09 00 00 18 00 00 00 78 05 00 00 78 05 00 00 79 05 00 00 7a 05 00 00 7b 05 00 00 7c 05 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00` (PL_CDR2 LE).
*   **DHEADER:** `18 00 00 00`
    *   Value: **24 bytes**.
*   **Member `id` (long):** `78 05 00 00`
    *   Value: 1400. Size: 4 bytes.
*   **Member `values` (long[5]):**
    *   Primitive arrays are serialized flatly.
    *   Size: 5 elements * 4 bytes/element = 20 bytes.
    *   Values: 1400, 1401, 1402, 1403, 1404.
*   **Total Body Size:** 4 (id) + 20 (values) = 24 bytes. Matches DHEADER.

**Analysis:**
Unlike arrays of strings (seen in Batch 1), arrays of **primitives** (int32) in appendable structs are **not** wrapped in a separate DHEADER. They are packed contiguously.

---

### 10. AtomicTests::CharTopicAppendable

*Note: The test framework marked this as PASS, but the logs indicated a "CDR Verify" mismatch (Received 05, Serialized 08). This indicates the C# serializer incorrectly added padding.*

**IDL Definition:**
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
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `05 00 00 00`
    *   Value: **5 bytes**.
*   **Member `id` (long):** `4c 04 00 00`
    *   Value: 1100. Size: 4 bytes.
*   **Member `value` (char):** `49`
    *   Value: 'I'. Size: 1 byte.
*   **Total Body Size:** 4 + 1 = 5 bytes.

**Analysis:**
XCDR2 does **not** pad the struct to a 4-byte boundary at the end of the DHEADER scope. The size is exactly the sum of the fields.

---

### 11. AtomicTests::Float64TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct Float64TopicAppendable {
    @key long id;
    double value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 0c 00 00 00 6c 07 00 00 86 41 ad aa 06 51 b7 40`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `0c 00 00 00`
    *   Value: **12 bytes**.
*   **Member `id` (long):** `6c 07 00 00`
    *   Value: 1900. Size: 4 bytes.
    *   *Offset Check:* Payload starts at index 4 (byte 0 of DHEADER). ID is at offset 4 (relative to payload). Next field is at offset 8.
*   **Member `value` (double):** `86 41 ... 40`
    *   Value: ~5972.16. Size: 8 bytes.
    *   *Alignment Check:* Offset 8 is 8-byte aligned relative to the stream start. No padding bytes required.
*   **Total Body Size:** 4 (id) + 8 (value) = 12 bytes.

**Analysis:**
Perfect packing because the `double` happened to fall on an 8-byte aligned boundary naturally after the 4-byte ID (header 4 bytes + DHEADER 4 bytes + ID 4 bytes = 12 bytes absolute offset).

---

### 12. AtomicTests::Float32TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct Float32TopicAppendable {
    @key long id;
    float value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 08 07 00 00 e6 b6 b0 45`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `08 00 00 00`
    *   Value: **8 bytes**.
*   **Member `id` (long):** `08 07 00 00` (1800). Size: 4.
*   **Member `value` (float):** `e6 b6 b0 45`. Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard packing of two 4-byte primitives.

---

### 13. AtomicTests::Int16TopicAppendable

*Note: Log showed "CDR Verify" failure (Received 06, Serialized 08).*

**IDL Definition:**
```idl
@appendable
@topic
struct Int16TopicAppendable {
    @key long id;
    short value;
};
```

**Golden CDR Bytes:**
`00 09 00 02 06 00 00 00 14 05 00 00 6c 9d 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 02`
*   **DHEADER:** `06 00 00 00`
    *   Value: **6 bytes**.
*   **Member `id` (long):** `14 05 00 00` (1300). Size: 4.
*   **Member `value` (short):** `6c 9d` (-25236). Size: 2.
*   **Total Body Size:** 6 bytes.

**Analysis:**
Confirms no end-of-struct padding in XCDR2 DHEADER.

---

### 14. AtomicTests::BooleanTopicAppendable

*Note: Log showed "CDR Verify" failure (Received 05, Serialized 08).*

**IDL Definition:**
```idl
@appendable
@topic
struct BooleanTopicAppendable {
    @key long id;
    boolean value;
};
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 4c 04 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `05 00 00 00`
    *   Value: **5 bytes**.
*   **Member `id` (long):** `4c 04 00 00` (1100). Size: 4.
*   **Member `value` (boolean):** `00` (False). Size: 1.
*   **Total Body Size:** 5 bytes.

**Analysis:**
Identical to `CharTopicAppendable`.

---

### 15. AtomicTests::StringBounded32TopicAppendable

*Note: Log showed "CDR Verify" failure (Received 11 (0x11), Serialized 14 (0x14)).*

**IDL Definition:**
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
*   **DHEADER:** `11 00 00 00`
    *   Value: **17 bytes** (0x11).
*   **Member `id` (long):** `14 05 00 00` (1300). Size: 4.
*   **Member `value` (string):**
    *   **Length:** `09 00 00 00` (9). Size: 4.
    *   **Chars:** `53...00` ("Str_1300\0"). Size: 9.
*   **Total Body Size:** 4 + 4 + 9 = 17 bytes.

**Analysis:**
Strings are strictly `Length(4) + Content(N)`. No alignment padding is added at the end of the string *unless* a subsequent field requires it. Since this is the last field in the DHEADER scope, no padding is added. The C# serializer likely aligned the 9-byte string to 12 bytes.

---

### 16. AtomicTests::Int64TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct Int64TopicAppendable {
    @key long id;
    long long value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 0c 00 00 00 40 06 00 00 00 10 5e 5f 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `0c 00 00 00`
    *   Value: **12 bytes**.
*   **Member `id` (long):** `40 06 00 00` (1600). Size: 4.
*   **Member `value` (long long):** `00 10 5e 5f ...`. Size: 8.
*   **Total Body Size:** 12 bytes.

**Analysis:**
Similar to `Float64`, the `long long` naturally falls on an 8-byte aligned boundary relative to the stream start.


Here is the detailed CDR stream analysis for the third batch of test cases.

This batch is particularly useful because it compares **@appendable** (XCDR2) string handling against **@final** (XCDR1) string handling (Tests 24 vs 26), highlighting a critical difference in padding rules.

### 17. AtomicTests::StringUnboundedTopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct StringUnboundedTopicAppendable {
    @key long id;
    string value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 18 00 00 00 34 08 00 00 10 00 00 00 53 74 72 55 6e 62 6f 75 6e 64 5f 32 31 30 30 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `18 00 00 00`
    *   Value: **24 bytes**.
*   **Member `id` (long):** `34 08 00 00`
    *   Value: 2100. Size: 4 bytes.
*   **Member `value` (string):**
    *   **Length:** `10 00 00 00`
        *   Value: 16 characters (including null). Size: 4 bytes.
    *   **Content:** `53...00` ("StrUnbound_2100\0").
        *   Size: 16 bytes.
*   **Total Body Size:** 4 (id) + 4 (len) + 16 (content) = 24 bytes.

**Analysis:**
In this specific case, the string content length (16) is naturally a multiple of 4. Therefore, even if alignment rules existed, no padding would be visible.

---

### 18. AtomicTests::Int32TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct Int32TopicAppendable {
    @key long id;
    long value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 b0 04 00 00 4f 50 7d b3`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `08 00 00 00` (8 bytes).
*   **Member `id` (long):** `b0 04 00 00` (1200). Size: 4.
*   **Member `value` (long):** `4f 50 7d b3` (-1283649457). Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard contiguous packing of two 32-bit integers.

---

### 19. AtomicTests::ColorEnumTopicAppendable

**IDL Definition:**
```idl
enum ColorEnum { RED, GREEN, BLUE, ... }; // 32-bit Enum

@appendable
@topic
struct ColorEnumTopicAppendable {
    @key long id;
    ColorEnum color;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 28 0a 00 00 02 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `08 00 00 00` (8 bytes).
*   **Member `id` (long):** `28 0a 00 00` (2600). Size: 4.
*   **Member `color` (enum):** `02 00 00 00`
    *   Value: 2 (BLUE). Size: 4 bytes.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Enums are treated exactly like 32-bit integers in XCDR2.

---

### 20. AtomicTests::UInt64TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct UInt64TopicAppendable {
    @key long id;
    unsigned long long value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 0c 00 00 00 a4 06 00 00 00 f1 53 65 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `0c 00 00 00` (12 bytes).
*   **Member `id` (long):** `a4 06 00 00` (1700). Size: 4.
*   **Member `value` (uint64):** `00 f1 53 65 00 00 00 00` (1700000000). Size: 8.
    *   *Alignment Check:* Stream offset 8. 8-byte aligned. No padding.
*   **Total Body Size:** 12 bytes.

**Analysis:**
Standard packing. The 4-byte DHEADER + 4-byte ID creates an 8-byte offset, perfectly aligning the 8-byte value.

---

### 21. AtomicTests::EnumTopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct EnumTopicAppendable {
    @key long id;
    SimpleEnum value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 c4 09 00 00 01 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `08 00 00 00` (8 bytes).
*   **Member `id` (long):** `c4 09 00 00` (2500). Size: 4.
*   **Member `value` (enum):** `01 00 00 00` (SECOND). Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard 32-bit enum serialization.

---

### 22. AtomicTests::UInt16TopicAppendable

*Note: Log indicated "CDR Verify" failure (Received 06, Serialized 08).*

**IDL Definition:**
```idl
@appendable
@topic
struct UInt16TopicAppendable {
    @key long id;
    unsigned short value;
};
```

**Golden CDR Bytes:**
`00 09 00 02 06 00 00 00 78 05 00 00 88 a9 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 02`
*   **DHEADER:** `06 00 00 00` (**6 bytes**).
*   **Member `id` (long):** `78 05 00 00` (1400). Size: 4.
*   **Member `value` (uint16):** `88 a9` (43400). Size: 2.
*   **Total Body Size:** 6 bytes.

**Analysis:**
Strict packing. 4 + 2 = 6. No padding at the end.

---

### 23. AtomicTests::UInt32TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct UInt32TopicAppendable {
    @key long id;
    unsigned long value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 dc 05 00 00 8b e7 40 d1`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `08 00 00 00` (8 bytes).
*   **Member `id` (long):** `dc 05 00 00` (1500). Size: 4.
*   **Member `value` (uint32):** `8b e7 40 d1`. Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard contiguous packing.

---

### 24. AtomicTests::StringBounded256TopicAppendable

*Note: Log indicated "CDR Verify" failure (Received 19 (0x19), Serialized 1c (28)).*

**IDL Definition:**
```idl
@appendable
@topic
struct StringBounded256TopicAppendable {
    @key long id;
    string<256> value;
};
```

**Golden CDR Bytes:**
`00 09 00 03 19 00 00 00 98 08 00 00 11 00 00 00 53 74 72 42 6f 75 6e 64 32 35 36 5f 32 32 30 30 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `19 00 00 00` (**25 bytes**).
*   **Member `id` (long):** `98 08 00 00` (2200). Size: 4.
*   **Member `value` (string):**
    *   **Length:** `11 00 00 00` (17 chars). Size: 4.
    *   **Content:** `53...00` ("StrBound256_2200\0").
        *   Size: 17 bytes.
*   **Total Body Size:** 4 + 4 + 17 = 25 bytes.

**Analysis:**
This confirms XCDR2 rule: **Do not pad the end of a string** inside an appendable struct unless necessary for the *next* field. Since there is no next field, the total size is exactly 25. The C# serializer likely tried to pad 17 -> 20 bytes (28 total), causing the failure.

---

### 25. AtomicTests::OctetTopicAppendable

*Note: Log indicated "CDR Verify" failure (Received 05, Serialized 08).*

**IDL Definition:**
```idl
@appendable
@topic
struct OctetTopicAppendable {
    @key long id;
    octet value;
};
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 b0 04 00 00 b0 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `05 00 00 00` (**5 bytes**).
*   **Member `id` (long):** `b0 04 00 00` (1200). Size: 4.
*   **Member `value` (octet):** `b0`. Size: 1.
*   **Total Body Size:** 5 bytes.

**Analysis:**
Strict packing. 4 + 1 = 5.

---

### 26. AtomicTests::StringBounded256Topic

**IDL Definition:**
```idl
// @final implies XCDR1 (Plain CDR)
@final
@topic
struct StringBounded256Topic {
    @key long id;
    string<256> value;
};
```

**Golden CDR Bytes:**
`00 01 00 03 b0 04 00 00 11 00 00 00 53 74 72 42 6f 75 6e 64 32 35 36 5f 31 32 30 30 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 03`
    *   `00 01`: CDR_LE (XCDR1).
*   **Member `id` (long):** `b0 04 00 00` (1200). Size: 4.
*   **Member `value` (string):**
    *   **Length:** `11 00 00 00` (17). Size: 4.
    *   **Content:** `53...00` ("StrBound256_1200\0"). Size: 17 bytes.
    *   **Padding:** `00 00 00`. (3 bytes).
        *   Why? **XCDR1/CDR Rule:** Strings are treated as a sequence of bytes. In standard CDR, the length of the serialization of a type must align to its own alignment or 4? No, standard CDR usually aligns the *start* of the length.
        *   However, if we look at the raw bytes, there are 3 bytes of zero at the end (`...30 00` is the null terminator, followed by `00 00 00`).
        *   17 bytes content + 3 bytes padding = 20 bytes.
        *   20 is a multiple of 4.
*   **Total Payload Size:** 4 (id) + 4 (len) + 20 (padded content) = 28 bytes.
*   **Total Message Size:** 4 (Header) + 28 = 32 bytes.

**Analysis:**
Comparison with Test 24 is crucial:
*   **Test 24 (@appendable / XCDR2):** String content (17 bytes) was **NOT** padded. Total DHEADER=25.
*   **Test 26 (@final / XCDR1):** String content (17 bytes) **WAS** padded to 20 bytes.
*   **Conclusion:** XCDR1 enforces 4-byte padding on strings. XCDR2 does NOT enforce it at the end of the DHEADER scope.


Here is the detailed CDR stream analysis for the fourth batch of test cases.

This batch focuses on **@final** (XCDR1 / Plain CDR) atomic types.
**Key Pattern Identified:** Unlike the `@appendable` (XCDR2) tests in previous batches which packed data tightly (e.g., 5 bytes for `long` + `char`), these `@final` tests consistently show **padding at the end of the structure** to align the total size to a 4-byte boundary (or the structure's max alignment).

### 27. AtomicTests::Float32Topic

**IDL Definition:**
```idl
@final
@topic
struct Float32Topic {
    @key long id;
    float value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 20 03 00 00 5a 14 1d 45`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00` (CDR_LE).
*   **Member `id` (long):** `20 03 00 00` (800). Size: 4.
*   **Member `value` (float):** `5a 14 1d 45`. Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard contiguous packing of two 4-byte primitives.

---

### 28. AtomicTests::StringUnboundedTopic

**IDL Definition:**
```idl
@final
@topic
struct StringUnboundedTopic {
    @key long id;
    string value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 4c 04 00 00 10 00 00 00 53 74 72 55 6e 62 6f 75 6e 64 5f 31 31 30 30 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `4c 04 00 00` (1100). Size: 4.
*   **Member `value` (string):**
    *   **Length:** `10 00 00 00` (16). Size: 4.
    *   **Content:** `53...00` ("StrUnbound_1100\0"). Size: 16 bytes.
*   **Total Body Size:** 24 bytes.

**Analysis:**
The string content (16 bytes) is a multiple of 4, so no padding is required.
*Comparison:* Matches XCDR2 behavior only because the length is naturally aligned.

---

### 29. AtomicTests::CharTopic

**IDL Definition:**
```idl
@final
@topic
struct CharTopic {
    @key long id;
    char value;
};
```

**Golden CDR Bytes:**
`00 01 00 03 96 00 00 00 55 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 03`
*   **Member `id` (long):** `96 00 00 00` (150). Size: 4.
*   **Member `value` (char):** `55` ('U'). Size: 1.
*   **Padding:** `00 00 00`. (3 bytes).
*   **Total Body Size:** 8 bytes.

**Analysis:**
**CRITICAL DIFFERENCE vs XCDR2:**
*   **XCDR2 (Test 10):** Size was **5 bytes**.
*   **XCDR1 (This Test):** Size is **8 bytes**.
*   **Reason:** XCDR1 enforces that the structure size is padded to satisfy alignment requirements (typically 4 bytes in DDS for structures containing >1 byte types). The C# serializer must correctly handle this "End of Struct" padding for `@final` types.

---

### 30. AtomicTests::Int32Topic

**IDL Definition:**
```idl
@final
@topic
struct Int32Topic {
    @key long id;
    long value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 c8 00 00 00 87 ad 46 50`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `c8 00 00 00` (200). Size: 4.
*   **Member `value` (long):** `87 ad 46 50`. Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard packing.

---

### 31. AtomicTests::EnumTopic

**IDL Definition:**
```idl
@final
@topic
struct EnumTopic {
    @key long id;
    SimpleEnum value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 fc 08 00 00 02 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `fc 08 00 00` (2300). Size: 4.
*   **Member `value` (enum):** `02 00 00 00` (THIRD). Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Enum is serialized as a 32-bit integer.

---

### 32. AtomicTests::Int16Topic

**IDL Definition:**
```idl
@final
@topic
struct Int16Topic {
    @key long id;
    short value;
};
```

**Golden CDR Bytes:**
`00 01 00 02 2c 01 00 00 54 24 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 02`
*   **Member `id` (long):** `2c 01 00 00` (300). Size: 4.
*   **Member `value` (short):** `54 24`. Size: 2.
*   **Padding:** `00 00`. (2 bytes).
*   **Total Body Size:** 8 bytes.

**Analysis:**
**Padding enforced.** 4 (id) + 2 (short) + 2 (padding) = 8 bytes.
*Comparison vs XCDR2 (Test 13):* XCDR2 size was 6 bytes. XCDR1 size is 8 bytes.

---

### 33. AtomicTests::BooleanTopic

**IDL Definition:**
```idl
@final
@topic
struct BooleanTopic {
    @key long id;
    boolean value;
};
```

**Golden CDR Bytes:**
`00 01 00 03 64 00 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 03`
*   **Member `id` (long):** `64 00 00 00` (100). Size: 4.
*   **Member `value` (boolean):** `00` (False). Size: 1.
*   **Padding:** `00 00 00`. (3 bytes).
*   **Total Body Size:** 8 bytes.

**Analysis:**
Same as `CharTopic`. Padded to 8 bytes.

---

### 34. AtomicTests::ColorEnumTopic

**IDL Definition:**
```idl
@final
@topic
struct ColorEnumTopic {
    @key long id;
    ColorEnum color;
};
```

**Golden CDR Bytes:**
`00 01 00 00 60 09 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `60 09 00 00` (2400). Size: 4.
*   **Member `color` (enum):** `00 00 00 00` (RED). Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard 4+4 packing.

---

### 35. AtomicTests::Float64Topic

**IDL Definition:**
```idl
@final
@topic
struct Float64Topic {
    @key long id;
    double value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 84 03 00 00 00 00 00 00 3c ed 0f e5 dd 16 a6 40`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `84 03 00 00` (900). Size: 4.
*   **Padding:** `00 00 00 00`. (4 bytes).
    *   **Why?** The next field is `double`. In XCDR1, `double` must be aligned to 8 bytes. Current offset is 4. Padding is added to reach offset 8.
*   **Member `value` (double):** `3c...40`. Size: 8.
*   **Total Body Size:** 4 (id) + 4 (pad) + 8 (val) = 16 bytes.

**Analysis:**
Standard XCDR1 alignment padding.
*Comparison:* Note that in XCDR2 (Appendable), we saw `Float64TopicAppendable` (Test 11) pack as 12 bytes (`4 + 8`) because the `id` + 4-byte DHEADER made the stream offset 8 bytes (aligned). Here in XCDR1, we don't have a DHEADER, so `id` (4) is followed by 4 bytes of padding to align the `double`.

---

### 36. AtomicTests::UInt16Topic

**IDL Definition:**
```idl
@final
@topic
struct UInt16Topic {
    @key long id;
    unsigned short value;
};
```

**Golden CDR Bytes:**
`00 01 00 02 90 01 00 00 70 30 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 02`
*   **Member `id` (long):** `90 01 00 00` (400). Size: 4.
*   **Member `value` (ushort):** `70 30`. Size: 2.
*   **Padding:** `00 00`. (2 bytes).
*   **Total Body Size:** 8 bytes.

**Analysis:**
Consistent with `Int16Topic`. Padded to 8.



Here is the detailed CDR stream analysis for the fourth batch of test cases.

This batch focuses on **@final** (XCDR1 / Plain CDR) atomic types and composite keys.
**Key Pattern Identified:**
1.  **Padding for Alignment:** In XCDR1, padding is inserted *before* a field if its alignment requirement (relative to the payload start) is not met.
2.  **End-of-Struct Padding:** Structs are padded at the end to match the alignment of their largest member.
3.  **Visual Trap:** Zeros belonging to the lower bits of floating-point numbers (e.g., `2400.0`) often look like padding in the byte stream. Careful counting is required.

---

### 37. AtomicTests::StringBounded32Topic

**IDL Definition:**
```idl
@final
@topic
struct StringBounded32Topic {
    @key long id;
    string<32> value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 2c 01 00 00 08 00 00 00 53 74 72 5f 33 30 30 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `2c 01 00 00` (300). Size: 4.
*   **Member `value` (string):**
    *   **Length:** `08 00 00 00` (8). Size: 4.
    *   **Content:** `53...00` ("Str_300\0"). Size: 8 bytes.
*   **Total Body Size:** 4 + 4 + 8 = 16 bytes.

**Analysis:**
The string content (8 bytes) is naturally aligned to 4 bytes. No padding required.

---

### 38. AtomicTests::OctetTopic

**IDL Definition:**
```idl
@final
@topic
struct OctetTopic {
    @key long id;
    octet value;
};
```

**Golden CDR Bytes:**
`00 01 00 03 c8 00 00 00 c8 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 03`
*   **Member `id` (long):** `c8 00 00 00` (200). Size: 4.
*   **Member `value` (octet):** `c8`. Size: 1.
*   **Padding:** `00 00 00`. (3 bytes).
    *   *Why?* The struct contains a `long` (4 bytes). To ensure the struct can be packed in an array without misalignment, the total size must be a multiple of the largest member alignment (4).
    *   4 + 1 = 5. Pad 3 -> 8.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard XCDR1 struct size padding.

---

### 39. AtomicTests::UInt32Topic

**IDL Definition:**
```idl
@final
@topic
struct UInt32Topic {
    @key long id;
    unsigned long value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 f4 01 00 00 c3 44 0a 6e`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `f4 01 00 00` (500). Size: 4.
*   **Member `value` (uint32):** `c3 44 0a 6e`. Size: 4.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Packed contiguously.

---

### 40. AtomicTests::Int64Topic

**IDL Definition:**
```idl
@final
@topic
struct Int64Topic {
    @key long id;
    long long value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 58 02 00 00 00 00 00 00 00 46 c3 23 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `58 02 00 00` (600). Size: 4.
*   **Padding:** `00 00 00 00`. (4 bytes).
    *   *Why?* `long long` requires 8-byte alignment.
    *   Relative offset is 4. Add 4 bytes to reach 8.
*   **Member `value` (int64):** `00 46 c3 23 00 00 00 00` (Values are little endian).
*   **Total Body Size:** 4 + 4 + 8 = 16 bytes.

**Analysis:**
XCDR1 alignment padding inserted before 64-bit field.

---

### 41. AtomicTests::UInt64Topic

**IDL Definition:**
```idl
@final
@topic
struct UInt64Topic {
    @key long id;
    unsigned long long value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 bc 02 00 00 00 00 00 00 00 27 b9 29 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `bc 02 00 00` (700). Size: 4.
*   **Padding:** `00 00 00 00`. (4 bytes).
*   **Member `value` (uint64):** `00 27 b9 29...`. Size: 8.
*   **Total Body Size:** 16 bytes.

**Analysis:**
Same as Int64Topic.

---

### 42. AtomicTests::TwoKeyStringTopic

**IDL Definition:**
```idl
@final
@topic
struct TwoKeyStringTopic {
    @key string<32> key1;
    @key string<32> key2;
    double value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 08 00 00 00 6b 31 5f 31 36 31 30 00 08 00 00 00 6b 32 5f 31 36 31 30 00 00 00 00 00 00 72 af 40`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `key1` (string):**
    *   Length: `08 00 00 00` (8).
    *   Content: `6b...00` ("k1_1610\0"). 8 bytes.
    *   Total: 12 bytes.
*   **Member `key2` (string):**
    *   Length: `08 00 00 00` (8).
        *   *Alignment Check:* Current relative offset is 12. `Length` (long) needs 4-byte align. 12 is aligned.
    *   Content: `6b...00` ("k2_1610\0"). 8 bytes.
    *   Total: 12 bytes.
*   **Member `value` (double):**
    *   *Alignment Check:* Current relative offset is 12+12 = 24. `double` needs 8-byte align. 24 is aligned. **No padding needed.**
    *   Value: `00 00 00 00 00 72 af 40` (4025.0).
*   **Total Body Size:** 12 + 12 + 8 = 32 bytes.

**Analysis:**
Packed without padding because the string lengths + contents naturally aligned the subsequent fields.

---

### 43. AtomicTests::ThreeKeyTopic

**IDL Definition:**
```idl
@final
@topic
struct ThreeKeyTopic {
    @key long key1;
    @key string<32> key2;
    @key short key3;
    double value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 54 06 00 00 08 00 00 00 6b 32 5f 31 36 32 30 00 14 00 00 00 00 00 00 00 00 00 00 00 00 26 b6 40`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `key1` (long):** `54 06 00 00` (1620). Size: 4.
*   **Member `key2` (string):**
    *   Length: `08 00 00 00` (8).
    *   Content: `6b...00` ("k2_1620\0"). 8 bytes.
*   **Member `key3` (short):** `14 00` (20). Size: 2.
    *   Current relative offset: 4 + 12 = 16. Short needs 2. Aligned.
*   **Padding:** `00 00 00 00 00 00` (6 bytes).
    *   *Alignment Check:* Current offset: 16 + 2 = 18.
    *   Next field is `double` (needs 8-byte align).
    *   Next multiple of 8 is 24.
    *   Padding needed: 24 - 18 = 6 bytes.
*   **Member `value` (double):** `00...40`. Size: 8.
*   **Total Body Size:** 4 + 12 + 2 + 6 + 8 = 32 bytes.

**Analysis:**
Demonstrates complex padding calculation (6 bytes) to align the final double.

---

### 44. AtomicTests::FourKeyTopic

**IDL Definition:**
```idl
@final
@topic
struct FourKeyTopic {
    @key long k1; @key long k2; @key long k3; @key long k4;
    string<64> description;
};
```

**Golden CDR Bytes:**
`00 01 00 02 5e 06 00 00 5f 06 00 00 60 06 00 00 61 06 00 00 0a 00 00 00 44 65 73 63 5f 31 36 33 30 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 02`
*   **Keys (4x long):** 16 bytes.
*   **Member `description` (string):**
    *   Length: `0a 00 00 00` (10).
    *   Content: `44...00` ("Desc_1630\0"). 10 bytes.
*   **Padding:** `00 00` (2 bytes).
    *   *Why?* Struct contains `long`. Size must be multiple of 4.
    *   Current size: 16 + 4 + 10 = 30 bytes.
    *   Pad to 32.
*   **Total Body Size:** 32 bytes.

**Analysis:**
End-of-struct padding applied to the variable length string field.

---

### 45. AtomicTests::TwoKeyInt32Topic

**IDL Definition:**
```idl
@final
@topic
struct TwoKeyInt32Topic {
    @key long key1;
    @key long key2;
    double value;
};
```

**Golden CDR Bytes:**
`00 01 00 00 40 06 00 00 41 06 00 00 00 00 00 00 00 c0 a2 40`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `key1` (long):** `40 06 00 00` (1600). Size: 4.
*   **Member `key2` (long):** `41 06 00 00` (1601). Size: 4.
*   **Member `value` (double):** `00 00 00 00 00 c0 a2 40` (2400.0).
    *   *Visual Check:* The stream bytes `00 00 00 00` after `key2` are **NOT** padding. They are the lower 4 bytes of the double value.
    *   *Alignment Check:* Relative offset is 8. Double needs 8. Aligned.
    *   So `00 00 00 00 00 c0 a2 40` is the 8-byte double.
*   **Total Body Size:** 4 + 4 + 8 = 16 bytes.

**Analysis:**
**Visual Trap:** The lower 32-bits of `2400.0` (double) are zero. In Little Endian, this looks like `00 00 00 00` immediately following the keys, resembling padding. However, because offset 8 is already 8-byte aligned, these are actually data bytes.


Here is the detailed CDR stream analysis for the fifth batch of test cases.

This batch covers **Nested Keys**, **Nested Structures** (both XCDR1 and XCDR2), and one failure case involving nested appendable structures.

### 46. AtomicTests::NestedKeyTopic

**IDL Definition:**
```idl
@final
struct Location {
    @key long building;
    @key short floor;
};

@final
@topic
struct NestedKeyTopic {
    @key Location loc;
    double temperature;
};
```

**Golden CDR Bytes:**
`00 01 00 00 a4 06 00 00 00 00 00 00 00 00 00 00 00 e0 9a 40`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00` (CDR_LE).
*   **Member `loc` (Location):**
    *   `building` (long): `a4 06 00 00` (1700). Size: 4.
    *   `floor` (short): `00 00` (0). Size: 2.
    *   *Struct Padding:* `Location` contains a `long` (alignment 4). Total size must be multiple of 4.
    *   Padding: `00 00`. (2 bytes).
    *   Total `loc` size: 8 bytes.
*   **Member `temperature` (double):**
    *   *Alignment Check:* Current offset is 8. Double needs 8. Aligned.
    *   Value: `00 00 00 00 00 e0 9a 40` (1720.0). Size: 8.
*   **Total Body Size:** 16 bytes.

**Analysis:**
Standard XCDR1 struct padding applied to the nested key structure.

---

### 47. AtomicTests::NestedTripleKeyTopic

**IDL Definition:**
```idl
@final
struct TripleKey {
    @key long id1;
    @key long id2;
    @key long id3;
};

@final
@topic
struct NestedTripleKeyTopic {
    @key TripleKey keys;
    string<64> data;
};
```

**Golden CDR Bytes:**
`00 01 00 02 b8 06 00 00 b9 06 00 00 ba 06 00 00 0a 00 00 00 44 61 74 61 5f 31 37 32 30 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 02`
*   **Member `keys` (TripleKey):**
    *   `id1`, `id2`, `id3`: 3 * 4 bytes = 12 bytes.
    *   Alignment is perfect (multiple of 4). No internal padding.
*   **Member `data` (string):**
    *   Length: `0a 00 00 00` (10).
    *   Content: `44 61...00` ("Data_1720\0"). 10 bytes.
*   **Padding:** `00 00`. (2 bytes).
    *   *Why?* The struct alignment is 4 (due to longs).
    *   Current size: 12 (keys) + 4 (len) + 10 (str) = 26 bytes.
    *   Next multiple of 4 is 28.
    *   Padding: 2 bytes.
*   **Total Body Size:** 28 bytes.

**Analysis:**
End-of-struct padding applied to the topic struct.

---

### 48. AtomicTests::NestedKeyGeoTopic

**IDL Definition:**
```idl
@final
struct Coordinates {
    @key double latitude;
    @key double longitude;
};

@final
@topic
struct NestedKeyGeoTopic {
    @key Coordinates coords;
    string<128> location_name;
};
```

**Golden CDR Bytes:**
`00 01 00 03 00 00 00 00 00 60 65 40 00 00 00 00 00 60 75 40 09 00 00 00 4c 6f 63 5f 31 37 31 30 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 03`
*   **Member `coords` (Coordinates):**
    *   `latitude`: 8 bytes.
    *   `longitude`: 8 bytes.
    *   Total: 16 bytes.
*   **Member `location_name` (string):**
    *   Length: `09 00 00 00` (9).
    *   Content: `4c...00` ("Loc_1710\0"). 9 bytes.
*   **Padding:** `00 00 00`. (3 bytes).
    *   *Why?* Struct contains `double` (alignment 8).
    *   Current size: 16 + 4 + 9 = 29 bytes.
    *   Next multiple of 8 is 32.
    *   Padding: 3 bytes.
*   **Total Body Size:** 32 bytes.

**Analysis:**
Padding aligns the total size to 8 bytes.

---

### 49. AtomicTests::NestedStructTopic

**IDL Definition:**
```idl
@final
struct Point2D { double x; double y; };

@final
@topic
struct NestedStructTopic {
    @key long id;
    Point2D point;
};
```

**Golden CDR Bytes:**
`00 01 00 00 58 02 00 00 00 00 00 00 00 00 00 00 00 a0 84 40 00 00 00 00 00 a0 94 40`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `58 02 00 00` (600). Size: 4.
*   **Padding:** `00 00 00 00`. (4 bytes).
    *   *Why?* Next field `point` is a struct containing `double`. Struct alignment is 8.
    *   Offset 4 -> 8 requires 4 bytes padding.
*   **Member `point` (Point2D):**
    *   `x`: 8 bytes.
    *   `y`: 8 bytes.
*   **Total Body Size:** 4 (id) + 4 (pad) + 16 (point) = 24 bytes.

**Analysis:**
Padding inserted *before* the nested structure to satisfy its alignment requirements.

---

### 50. AtomicTests::ComplexNestedTopic

**IDL Definition:**
```idl
@final
struct Container {
    long count;
    Point2D center; // { double x, y; }
    double radius;
};

@final
@topic
struct ComplexNestedTopic {
    @key long id;
    Container container;
};
```

**Golden CDR Bytes:**
`00 01 00 00 76 02 00 00 76 02 00 00 cd cc cc cc cc b0 83 40 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `76 02 00 00` (630). Size: 4.
*   **Member `container` (Container):**
    *   **Member `count` (long):** `76 02 00 00`. Size: 4.
        *   *Alignment Check:* `container` struct alignment is 8 (due to nested Point2D).
        *   However, `container` starts at offset 4? No.
        *   Let's check the Golden Bytes again.
        *   Bytes: `id` (4) | `count` (4) | `center.x` (8) ...
        *   Wait. If `Container` has alignment 8, there should be padding after `id`.
        *   *Correction:* In XCDR1, a struct's alignment is enforced at its start. If `Container` contains a `double`, it needs 8-byte alignment.
        *   `id` is at 0-4. Next is offset 4.
        *   The bytes show `count` *immediately* following `id`.
        *   **Why?** Let's look at `Container`. It starts with `long count`.
        *   DDS XCDR1 rules (and C packing): The alignment of a struct is the max alignment of its members. The *start* of the struct must be aligned. BUT, if the first member of the struct (`long`) has lower alignment (4) than the struct (8), does the compiler optimize?
        *   Actually, usually `Container` would be padded.
        *   **Hypothesis:** The `count` member is 4 bytes. `center` is 8-byte aligned. `count` (4) + `id` (4) = 8.
        *   So `container` starts at offset 4. `count` occupies 4-8. `center` starts at 8. `center` is aligned.
        *   Does `Container` itself need to start at 8?
        *   In this trace, it seems `Container` was allowed to start at offset 4 because its *first member* only required 4-byte alignment, and the internal padding of `Container` (none needed between count and center?) handled the rest.
        *   Wait: `id` (4) + `count` (4) = 8. `center` (double) starts at 8. This works perfectly without padding.
*   **Total Body Size:** 40 bytes.

**Analysis:**
XCDR1 allows "packing" where the start of a struct corresponds to the alignment of its first member, provided internal padding satisfies subsequent members. Here, `id` and `count` packed perfectly to align the subsequent `double`.

---

### 51. AtomicTests::Nested3DTopic

**IDL Definition:**
```idl
@final
struct Point3D { double x; double y; double z; };

@final
@topic
struct Nested3DTopic {
    @key long id;
    Point3D point;
};
```

**Golden CDR Bytes:**
`00 01 00 00 62 02 00 00 00 00 00 00 00 00 00 00 00 18 83 40 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** `62 02 00 00` (610). Size: 4.
*   **Padding:** `00 00 00 00`. (4 bytes).
    *   *Why?* `Point3D` contains doubles. Needs 8-byte alignment.
    *   Offset 4 -> 8.
*   **Member `point` (Point3D):**
    *   `x, y, z`: 3 * 8 = 24 bytes.
*   **Total Body Size:** 32 bytes.

**Analysis:**
Standard padding. Contrast with Test 50: Here `Point3D` starts with a double, so the struct *must* start on an 8-byte boundary. In Test 50, `Container` started with a `long`, so it could effectively start on a 4-byte boundary.

---

### 52. AtomicTests::DoublyNestedTopic

**IDL Definition:**
```idl
@final
struct Box { Point2D p1; Point2D p2; };

@final
@topic
struct DoublyNestedTopic {
    @key long id;
    Box box;
};
```

**Golden CDR Bytes:**
`00 01 00 00 6c 02 00 00 00 00 00 00 00 00 00 00 00 60 83 40 ...`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00`
*   **Member `id` (long):** 4 bytes.
*   **Padding:** 4 bytes. (Align `Box` -> `Point2D` -> `double`).
*   **Member `box`:**
    *   `p1`: 16 bytes.
    *   `p2`: 16 bytes.
*   **Total Body Size:** 40 bytes.

**Analysis:**
Standard padding.

---

### 53. AtomicTests::Nested3DTopicAppendable [FAIL]

**IDL Definition:**
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
`00 09 00 00 20 00 00 00 9a 08 00 00 18 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00` (PL_CDR2 LE).
*   **Outer DHEADER:** `20 00 00 00`
    *   Value: **32 bytes**.
*   **Member `id` (long):** `9a 08 00 00` (2202). Size: 4.
*   **Member `point` (Nested Appendable Struct):**
    *   **Inner DHEADER:** `18 00 00 00`
        *   Value: **24 bytes**.
    *   **Inner Body (Point3DAppendable):**
        *   `x`: 8 bytes.
        *   `y`: 8 bytes.
        *   `z`: 8 bytes.
        *   Total Inner Body: 24 bytes. matches Inner DHEADER.
*   **Outer Body Total:** 4 (id) + 4 (Inner DHEADER) + 24 (Inner Body) = 32 bytes. matches Outer DHEADER.

**Analysis of Failure:**
The test failed with `Validation failed`.
The C# serializer likely failed to wrap the `point` struct in its own DHEADER.
*   **Correct Logic:** `OuterDHeader + ID + InnerDHeader + X + Y + Z`
*   **Likely Incorrect Logic:** `OuterDHeader + ID + X + Y + Z` (missing inner header) OR incorrect size calculation.

**Fix:** When serializing a member that is a mutable or appendable struct, it must be encapsulated (DHEADER).



Here is the detailed CDR stream analysis for the sixth batch of test cases.

This batch focuses on **Appendable** types with **Sequences**, **Unions**, **Keys**, and **Optionals**.
**Key Pattern:** The "CDR Verify" errors in the passing tests (Tests 54, 58) confirm that the C# serializer consistently adds padding to the end of variable-length fields (Strings) inside DHEADER scopes, whereas the native XCDR2 implementation **packs them tightly**. The failing tests (57, 59, 61) confirm that optional fields (even structs and enums) use a **single byte flag** when absent.

### 54. AtomicTests::MaxSizeStringTopicAppendable

*Note: Log indicated "CDR Verify FAILED: Byte mismatch at index 4: Received 0a, Serialized 0c".*

**IDL Definition:**
```idl
@appendable
@topic
struct MaxSizeStringTopicAppendable {
    @key long id;
    string<8192> max_string;
};
```

**Golden CDR Bytes:**
`00 09 00 02 0a 00 00 00 c9 09 00 00 02 00 00 00 53 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 02`
*   **DHEADER:** `0a 00 00 00`
    *   Value: **10 bytes**.
*   **Member `id` (long):** `c9 09 00 00` (2505). Size: 4.
*   **Member `max_string` (string):**
    *   **Length:** `02 00 00 00` (1 char + null). Size: 4.
    *   **Content:** `53 00` ("S\0"). Size: 2.
*   **Total Body Size:** 4 + 4 + 2 = 10 bytes.

**Analysis:**
The string content is 2 bytes. XCDR2 does **not** pad this to 4 bytes at the end of the structure.
*C# Error:* Serialized 12 bytes (padded 2 bytes -> 4 bytes).

---

### 55. AtomicTests::EmptySequenceTopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct EmptySequenceTopicAppendable {
    @key long id;
    sequence<long> empty_seq;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 c5 09 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `08 00 00 00` (8 bytes).
*   **Member `id` (long):** `c5 09 00 00` (2501). Size: 4.
*   **Member `empty_seq` (sequence<long>):**
    *   **Length:** `00 00 00 00` (0). Size: 4.
    *   **Content:** Empty.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Correct. Primitive sequence length 0 is just 4 bytes. No DHEADER wrapper for primitive sequence.

---

### 56. AtomicTests::UnionWithOptionalTopicAppendable

**IDL Definition:**
```idl
@appendable
union UnionWithOptionalAppendable switch(long) {
    case 1: long int_val;
    case 2: string<64> opt_str_val;
    case 3: double double_val;
};

@appendable
@topic
struct UnionWithOptionalTopicAppendable {
    @key long id;
    UnionWithOptionalAppendable data;
};
```

**Golden CDR Bytes:**
`00 09 00 00 10 00 00 00 cf 09 00 00 08 00 00 00 01 00 00 00 cf 09 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **Outer DHEADER:** `10 00 00 00` (**16 bytes**).
*   **Member `id` (long):** `cf 09 00 00` (2511). Size: 4.
*   **Member `data` (Union):**
    *   **Union DHEADER:** `08 00 00 00` (**8 bytes**).
    *   **Discriminator:** `01 00 00 00` (1). Size: 4.
    *   **Selected Member (`int_val`):** `cf 09 00 00` (2511). Size: 4.
    *   **Total Union:** 4 + 4 = 8 bytes.
*   **Total Body Size:** 4 (id) + 4 (UnionHdr) + 8 (UnionBody) = 16 bytes.

**Analysis:**
Unions inside appendable structs are wrapped in a DHEADER.

---

### 57. AtomicTests::OptionalInt32TopicAppendable [FAIL]

**IDL Definition:**
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
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `05 00 00 00` (5 bytes).
*   **Member `id` (long):** `fd 08 00 00` (2301). Size: 4.
*   **Member `opt_value` (optional):**
    *   **Flag:** `00` (Absent). Size: 1 byte.
*   **Total Body Size:** 5 bytes.

**Analysis:**
**Failure Cause:** The C# serializer likely expected 8 bytes (4 for ID + 4 for header/padding) or aligned the 1 byte flag to 4 bytes. XCDR2 uses a single byte flag for absent optional fields in appendable structs.

---

### 58. AtomicTests::FourKeyTopicAppendable

*Note: Log indicated "CDR Verify FAILED... Received 15, Serialized 18".*

**IDL Definition:**
```idl
@appendable
@topic
struct FourKeyTopicAppendable {
    @key long k1; @key long k2; @key long k3; @key long k4;
    string<64> description;
};
```

**Golden CDR Bytes:**
`00 09 00 03 15 00 00 00 64 09 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `15 00 00 00` (**21 bytes**).
*   **Keys (4x long):** 16 bytes.
*   **Member `description` (string):**
    *   **Length:** `01 00 00 00` (Empty string, just null). Size: 4.
    *   **Content:** `00` (Null terminator). Size: 1.
*   **Total Body Size:** 16 + 4 + 1 = 21 bytes.

**Analysis:**
Tight packing (21 bytes). C# serialized 18? No, log said "Received 15, Serialized 18" (hex).
Received `15` (21 dec). Serialized `18` (24 dec).
C# added 3 bytes of padding to align the 1-byte string content to 4 bytes. Native XCDR2 did not.

---

### 59. AtomicTests::OptionalEnumTopicAppendable [FAIL]

**IDL Definition:**
```idl
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
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `05 00 00 00` (5 bytes).
*   **Member `id` (long):** `01 09 00 00` (2305). Size: 4.
*   **Member `opt_enum` (optional):**
    *   **Flag:** `00` (Absent). Size: 1 byte.
*   **Total Body Size:** 5 bytes.

**Analysis:**
Identical to OptionalInt32. Optional Enums use a 1-byte flag if absent.

---

### 60. AtomicTests::ThreeKeyTopicAppendable [FAIL]

**IDL Definition:**
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
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `14 00 00 00` (**20 bytes**).
*   **Member `key1` (long):** `63 09 00 00` (2403). Size: 4.
*   **Member `key2` (string):**
    *   **Length:** `02 00 00 00` (2). Size: 4.
    *   **Content:** `4b 00` ("K\0"). Size: 2.
    *   *Subtotal:* 4 + 2 = 6 bytes.
*   **Member `key3` (short):** `00 00`. Size: 2.
    *   *Alignment Check:* Stream offset: 4(Hdr)+4(DH)+4(k1)+6(k2) = 18.
    *   18 is multiple of 2. Aligned.
*   **Member `value` (double):** `00 00 00 00 00 00 00 00`. Size: 8.
    *   *Alignment Check:* Stream offset: 18 + 2 (k3) = 20.
    *   **CRITICAL OBSERVATION:** Offset 20 is **NOT** 8-byte aligned. (Multiple of 8 is 16, 24).
    *   The native serializer put the `double` at offset 20 immediately following the `short`.
    *   This implies that inside an XCDR2 DHEADER scope, **alignment padding for 8-byte primitives might be suppressed** or calculated differently than standard CDR. Or, perhaps since it's "Delimited", the internal alignment logic is relaxed for tight packing.
*   **Total Body Size:** 4 + 6 + 2 + 8 = 20 bytes.

**Analysis:**
The C# serializer likely inserted 4 bytes of padding before `value` to align it to 8 bytes (making the size 24). Native sent 20.

---

### 61. AtomicTests::OptionalStructTopicAppendable [FAIL]

**IDL Definition:**
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
*   **Encapsulation Header:** `00 09 00 03`
*   **DHEADER:** `05 00 00 00` (5 bytes).
*   **Member `id` (long):** `00 09 00 00` (2304). Size: 4.
*   **Member `opt_point` (optional):**
    *   **Flag:** `00` (Absent). Size: 1 byte.
*   **Total Body Size:** 5 bytes.

**Analysis:**
Optional Structs use a 1-byte flag if absent. Same as primitives/enums.



Here is the detailed CDR stream analysis for the sixth batch of test cases.

This batch provides the **definitive proof for XCDR2 alignment rules**.
Test 65 (`AllPrimitivesAtomicTopicAppendable`) demonstrates that **XCDR2 DOES enforce alignment padding between fields** to satisfy the alignment requirements of the next field. This contrasts with previous observations where strings at the end of structs weren't padded—they weren't padded because there was no "next field" requiring alignment.

### 62. AtomicTests::UnboundedStringTopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable
@topic
struct UnboundedStringTopicAppendable {
    @key long id;
    string value;
};
```

**Golden CDR Bytes:**
`00 09 00 02 0a 00 00 00 c6 09 00 00 02 00 00 00 53 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 02`
*   **DHEADER:** `0a 00 00 00`
    *   Value: **10 bytes**.
*   **Member `id` (long):** `c6 09 00 00` (2502). Size: 4.
*   **Member `value` (string):**
    *   **Length:** `02 00 00 00` (2). (1 char + null).
    *   **Content:** `53 00` ("S\0"). Size: 2.
*   **Total Body Size:** 4 + 4 + 2 = 10 bytes.

**Analysis:**
Strict packing at the end of the DHEADER. Content "S\0" is 2 bytes. XCDR2 does not pad this to 4.

---

### 63. AtomicTests::BoundedSequenceInt32TopicAppendable [FAIL]

**IDL Definition:**
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
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `14 00 00 00` (**20 bytes**).
*   **Member `id` (long):** `36 08 00 00` (2102). Size: 4.
*   **Member `values` (sequence):**
    *   **Length:** `03 00 00 00` (3). Size: 4.
    *   **Elements:** 3 * 4 bytes = 12 bytes.
*   **Total Body Size:** 4 + 4 + 12 = 20 bytes.

**Analysis:**
**Primitive Sequence Rule:** A sequence of primitives (`long`) is serialized directly (Length + Data). It is **NOT** wrapped in its own DHEADER inside an appendable struct.
*Likely Failure Cause:* C# serializer tried to wrap the sequence in a DHEADER (like it must for String/Struct sequences).

---

### 65. AtomicTests::AllPrimitivesAtomicTopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct AllPrimitivesAtomicTopicAppendable {
    @key long id;
    boolean bool_val;
    char char_val;
    octet octet_val;
    short short_val;
    unsigned short ushort_val;
    long long_val;
    unsigned long ulong_val;
    long long llong_val;
    unsigned long long ullong_val;
    float float_val;
    double double_val;
};
```

**Golden CDR Bytes:**
`00 09 00 00 30 00 00 00 c7 09 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `30 00 00 00` (**48 bytes**).
*   **Member `id` (long):** `c7 09 00 00`. Size: 4. (Offset 4 relative to payload start).
*   **`bool_val` (bool):** `00`. Size: 1. (Offset 5).
*   **`char_val` (char):** `00`. Size: 1. (Offset 6).
*   **`octet_val` (octet):** `00`. Size: 1. (Offset 7).
*   **`short_val` (short):**
    *   *Alignment check:* Offset 7. Short requires 2-byte alignment.
    *   **PADDING:** `00` (1 byte). Offset becomes 8.
    *   Value: `00 00`. Size: 2. (Offset 10).
*   **`ushort_val` (ushort):** `00 00`. Size: 2. (Offset 12).
*   **`long_val` (long):** `00 00 00 00`. Size: 4. (Offset 16).
*   **`ulong_val` (ulong):** `00 00 00 00`. Size: 4. (Offset 20).
*   **`llong_val` (long long):**
    *   *Alignment check:* Offset 24. LongLong requires 8-byte alignment. 24 is aligned.
    *   Value: `00...00`. Size: 8. (Offset 32).
*   **`ullong_val` (ulong long):** `00...00`. Size: 8. (Offset 40).
*   **`float_val` (float):** `00 00 00 00`. Size: 4. (Offset 44).
*   **`double_val` (double):**
    *   *Alignment check:* Offset 44. Double requires 8-byte alignment.
    *   **PADDING:** `00 00 00 00` (4 bytes). Offset becomes 48.
    *   *Wait, check trace bytes...*
    *   Trace: `c7 09 00 00` (id) `00 00 00` (bool/char/oct) `00` (pad) `00 00` (short) `00 00` (ushort) `00 00 00 00` (long) `00 00 00 00` (ulong) ...
    *   It seems my alignment calculation works perfectly without the final double padding?
    *   Let's check the size:
        4(id) + 3(b/c/o) + 1(pad) + 2(s) + 2(us) + 4(l) + 4(ul) + 8(ll) + 8(ull) + 4(f) + 8(d) = 48 bytes.
    *   Wait, `float` (4) is at offset 40 (after ull). `float` takes 40->44.
    *   `double` (8) is at offset 44. 44 is **NOT** 8-byte aligned (40, 48).
    *   Therefore, there **MUST** be 4 bytes of padding before the double.
    *   So: 48 (calculated bytes) + 4 (padding) = 52 bytes?
    *   But DHEADER says **48**.
    *   **RE-EVALUATION:**
        *   Does XCDR2 relax alignment for doubles? Or was the `float` placed differently?
        *   Let's look at the byte stream. `00 00 00 00` is repeated many times.
        *   Maybe the compiler reordered fields? No, IDL order is strict.
        *   **Hypothesis:** The `double` at the end was NOT padded to 8 bytes.
        *   If `double` started at 44, the total size is 52.
        *   If `double` started at 44 and consumed 8 bytes, total is 52.
        *   If `double` started at 44 and NO padding: 4(id)+3+1(pad)+2+2+4+4+8+8+4+8 = 48.
        *   **CRITICAL FINDING:** It appears **XCDR2 did NOT pad the double to 8 bytes** at offset 44. This suggests that inside the DHEADER scope (appendable), alignment is either relaxed or strictly packed for types that would push the DHEADER size up?
        *   *Correction:* In XCDR2, `double` alignment is 8.
        *   Let's check the start of the payload. `00 09 00 00` (Header). `30...` (DHEADER). `c7...` (ID). Offset 4.
        *   If `double` alignment is respected, DHEADER should be 52.
        *   Since DHEADER is 48, the native serializer **did not align the double**.

---

### 66. AtomicTests::SequenceFloat32TopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable
@topic
struct SequenceFloat32TopicAppendable {
    @key long id;
    sequence<float> values;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 37 08 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `08 00 00 00` (8 bytes).
*   **Member `id` (long):** `37 08 00 00` (2103). Size: 4.
*   **Member `values` (sequence):**
    *   **Length:** `00 00 00 00`. Size: 4.
    *   **Data:** None.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Confirmed: Primitive sequences are **not wrapped** in a DHEADER. The failure "Received 16 bytes" suggests C# wrapped it (Header 4 + Length 4 + Padding/etc).

---

### 67. AtomicTests::MaxSizeStringTopic

**IDL Definition:**
```idl
@final
@topic
struct MaxSizeStringTopic {
    @key long id;
    string<8192> max_string;
};
```

**Golden CDR Bytes:**
`00 01 00 02 c8 09 00 00 02 00 00 00 53 00 00 00`

**Decoding:**
*   **Header:** `00 01 00 02` (XCDR1).
*   **ID:** `c8 09 00 00` (2504). Size: 4.
*   **String Length:** `02 00 00 00`. Size: 4.
*   **String Content:** `53 00` ("S\0"). Size: 2.
*   **Padding:** `00 00`. Size: 2.
*   **Total Body:** 12 bytes.

**Analysis:**
XCDR1 (@final) enforces padding to 4-byte boundary.

---

### 68. AtomicTests::NestedKeyGeoTopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable struct CoordinatesAppendable { @key double latitude; @key double longitude; };
@appendable @topic struct NestedKeyGeoTopicAppendable { @key CoordinatesAppendable coords; string<128> location_name; };
```

**Golden CDR Bytes:**
`00 09 00 00 1c 00 00 00 10 00 00 00 ... 04 00 00 00 4c 6f 63 00`

**Decoding:**
*   **DHEADER:** `1c` (28 bytes).
*   **Nested `coords`:**
    *   **Inner DHEADER:** `10` (16 bytes).
    *   `lat`: 8 bytes.
    *   `long`: 8 bytes.
*   **`location_name`:**
    *   **Length:** `04 00 00 00`. Size: 4.
    *   **Content:** `4c 6f 63 00` ("Loc\0"). Size: 4.
*   **Total Body:** 4 + 16 + 4 + 4 = 28 bytes.

**Analysis:**
Correct structure: [OuterDHeader] [InnerDHeader] [InnerData] [StrLen] [StrData].
Failure likely due to C# failing to handle Nested DHEADER or padding the string.

---

### 69. AtomicTests::OptionalStringTopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable
@topic
struct OptionalStringTopicAppendable {
    @key long id;
    @optional string<64> opt_string;
};
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 ff 08 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `05 00 00 00` (5 bytes).
*   **ID:** `ff 08 00 00` (2303). Size: 4.
*   **Optional Flag:** `00` (Absent). Size: 1.
*   **Total:** 5 bytes.

**Analysis:**
Optional string absent = 1 byte flag. No padding.

---

### 70. AtomicTests::SequenceFloat64TopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable
@topic
struct SequenceFloat64TopicAppendable {
    @key long id;
    sequence<double> values;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 38 08 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** 8 bytes.
*   **ID:** 4 bytes.
*   **Seq Length:** 4 bytes (0).
*   **Total:** 8 bytes.

**Analysis:**
Primitive sequence (`double`) is not wrapped in DHEADER.

---

### 71. AtomicTests::TwoKeyStringTopicAppendable [FAIL]

**IDL Definition:**
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
*   **`key1` (String):**
    *   Length: `03 00 00 00` (3).
    *   Content: `4b 31 00` ("K1\0").
    *   *Padding:* `00` (1 byte).
    *   *Why?* Next field `key2` length (long) needs 4-byte alignment. 3 bytes content + 1 byte pad = 4 bytes.
*   **`key2` (String):**
    *   Length: `03 00 00 00`.
    *   Content: `4b 32 00` ("K2\0").
    *   *Padding:* `00` (1 byte).
    *   *Why?* Next field `value` (double) needs 8-byte alignment.
    *   Offset check: 4(DH) + 4(Len) + 4(Str+Pad) + 4(Len) + 4(Str+Pad) = 20.
    *   20 is NOT 8-byte aligned?
    *   Wait, let's trace from Stream Start:
        *   EncHeader(4) + DHEADER(4) = 8.
        *   Key1 Len(4) -> 12.
        *   Key1 Dat(3) -> 15. Pad(1) -> 16. (Aligned).
        *   Key2 Len(4) -> 20.
        *   Key2 Dat(3) -> 23. Pad(1) -> 24. (Aligned).
        *   Value (double) -> 24. 24 is 8-byte aligned.
*   **`value` (Double):** `00...`. Size: 8.
*   **Total Body:** 8 (Key1) + 8 (Key2) + 8 (Val) = 24 bytes.

**Analysis:**
Here, XCDR2 **DID** pad the strings.
**Rule:** Padding is inserted **between** fields if the *next* field requires alignment. It is **not** inserted at the *end* of the struct (DHEADER scope) if there are no more fields.

---

### 72. AtomicTests::ComplexNestedTopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable struct ContainerAppendable { long count; Point3DAppendable center; double radius; };
@appendable @topic struct ComplexNestedTopicAppendable { @key long id; ContainerAppendable container; };
```

**Golden CDR Bytes:**
`00 09 00 00 30 00 00 00 9c 08 00 00 28 00 00 00 ...`

**Decoding:**
*   **Outer DHEADER:** `30` (48 bytes).
*   **ID:** `9c 08...` (4 bytes).
*   **`container` (Nested):**
    *   **Container DHEADER:** `28` (40 bytes).
    *   `count` (4 bytes).
    *   **`center` (Nested):**
        *   **Center DHEADER:** `18` (24 bytes).
        *   `x,y,z`: 24 bytes.
    *   `radius` (8 bytes).
    *   Container Total: 4 (DH) + 4 (cnt) + 4 (DH) + 24 (xyz) + 8 (rad) = 44 bytes?
    *   Wait, DHEADER says 40.
    *   Ah, DHEADER value *excludes* the DHEADER bytes themselves.
    *   Container Body: `count`(4) + `center`(4+24) + `radius`(8).
        *   4 + 28 + 8 = 40. **Matches.**
*   **Outer Total:** 4 (id) + 4 (ContDH) + 40 (ContBody) = 48 bytes. **Matches.**

**Analysis:**
Recursive DHEADER application. C# failure likely due to missing the inner `center` DHEADER or incorrect size calc.



Here is the detailed CDR stream analysis for the seventh batch of test cases.

This batch illuminates **Union Alignment in XCDR2**, **Recursive DHEADERs**, and confirms that **Bounded/Max Sequences** do not affect serialization size (they are treated like dynamic sequences).

### 73. AtomicTests::UnionBoolDiscTopicAppendable

**IDL Definition:**
```idl
@appendable
union BoolUnionAppendable switch(boolean) {
    case TRUE: long true_val;
    case FALSE: double false_val;
};

@appendable
@topic
struct UnionBoolDiscTopicAppendable {
    @key long id;
    BoolUnionAppendable data;
};
```

**Golden CDR Bytes:**
`00 09 00 00 14 00 00 00 d1 07 00 00 0c 00 00 00 00 00 00 00 00 00 00 00 00 73 a7 40`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **Outer DHEADER:** `14 00 00 00` (**20 bytes**).
*   **Member `id` (long):** `d1 07 00 00` (2001). Size: 4.
*   **Member `data` (Union):**
    *   **Union DHEADER:** `0c 00 00 00` (**12 bytes**).
    *   **Discriminator (boolean):** `00 00 00 00`
        *   Value: `FALSE` (0).
        *   **CRITICAL FINDING:** Even though `boolean` is 1 byte, XCDR2 encoded it here as **4 bytes** (or 1 byte + 3 padding).
        *   *Why?* DHEADER alignment logic often aligns the first member of the body.
    *   **Selected Member (`false_val` double):** `00 00 00 00 00 73 a7 40`
        *   Value: 3001.5. Size: 8 bytes.
        *   *Alignment Check:*
            *   Stream Start -> Offset 0.
            *   Header(4) + OuterDH(4) + ID(4) + UnionDH(4) = 16 bytes.
            *   Discriminator starts at 16. (Aligned).
            *   Discriminator + Pad consumes 4 bytes. Current Offset: 20.
            *   Double starts at 20.
            *   **20 is NOT 8-byte aligned.** (Multiples of 8: 16, 24).
        *   **CONCLUSION:** In XCDR2 Appendable Unions, strict alignment of members (like `double`) relative to the stream start is **NOT enforced** if the layout is tightly packed by the DHEADER structure. The double follows the 4-byte discriminator immediately.
    *   **Total Union Body:** 4 (Disc) + 8 (Double) = 12 bytes. Matches Union DHEADER.
*   **Total Body Size:** 4 (id) + 4 (UnionDH) + 12 (UnionBody) = 20 bytes. Matches Outer DHEADER.

---

### 74. AtomicTests::MaxLengthSequenceTopic [FAIL]

**IDL Definition:**
```idl
@final
@topic
struct MaxLengthSequenceTopic {
    @key long id;
    sequence<long, 10000> max_seq;
};
```

**Golden CDR Bytes:**
`00 01 00 00 ca 09 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 01 00 00` (XCDR1).
*   **Member `id` (long):** `ca 09 00 00` (2506). Size: 4.
*   **Member `max_seq` (sequence):**
    *   **Length:** `00 00 00 00` (0). Size: 4.
    *   **Data:** Empty.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Standard XCDR1. The generic bound (`10000`) does **not** cause pre-allocation of bytes in the stream. It behaves like a normal sequence. The C# serializer likely incorrectly padded it or validated the "max" constraint improperly.

---

### 75. AtomicTests::UnionWithOptionalTopic

**IDL Definition:**
```idl
@final
union UnionWithOptional switch(long) {
    case 1: long int_val;
    case 2: string<64> opt_str_val;
    ...
};
@final @topic struct UnionWithOptionalTopic { @key long id; UnionWithOptional data; };
```

**Golden CDR Bytes:**
`00 01 00 00 ce 09 00 00 01 00 00 00 ce 09 00 00`

**Decoding:**
*   **Header:** `00 01 00 00`.
*   **ID:** `ce 09...` (2510). 4 bytes.
*   **Union Disc:** `01 00...`. 4 bytes.
*   **Union Val (long):** `ce 09...`. 4 bytes.
*   **Total:** 12 bytes.

**Analysis:**
Standard XCDR1 union packing.

---

### 76. AtomicTests::DeepNestedStructTopicAppendable [FAIL]

**IDL Definition:**
```idl
// Nested hierarchy: Level1 -> Level2 -> Level3 -> Level4 -> Level5.
// All structs are @appendable.
// Each struct has one `long value` and the next nested struct.
```

**Golden CDR Bytes:**
`00 09 00 00 2c 00 00 00 cd 09 00 00 24 00 00 00 cd 09 00 00 1c 00 00 00 ... 14 00 00 00 ... 0c 00 00 00 ... 04 00 00 00 ...`

**Decoding:**
This is a perfect example of **Recursive DHEADERs**:

1.  **Outer DHEADER (L1):** `2c` (**44 bytes**).
    *   `id`: 4 bytes.
    *   **L1 DHEADER:** `24` (**36 bytes**).
        *   `val1`: 4 bytes.
        *   **L2 DHEADER:** `1c` (**28 bytes**).
            *   `val2`: 4 bytes.
            *   **L3 DHEADER:** `14` (**20 bytes**).
                *   `val3`: 4 bytes.
                *   **L4 DHEADER:** `0c` (**12 bytes**).
                    *   `val4`: 4 bytes.
                    *   **L5 DHEADER:** `04` (**4 bytes**).
                        *   `val5`: 4 bytes.

*   **Total Size Check:**
    *   L5: 4 + 4 = 8? No, DHEADER value *is* the body size.
    *   L5 Body = 4. L5 Total = 4(DH) + 4(Body) = 8.
    *   L4 Body = 4(Val) + 8(L5 Total) = 12. Matches DH=12.
    *   L3 Body = 4(Val) + 4(L4 DH) + 12(L4 Body) = 20. Matches DH=20.
    *   L2 Body = 4(Val) + 4(L3 DH) + 20(L3 Body) = 28. Matches DH=28.
    *   L1 Body = 4(Val) + 4(L2 DH) + 28(L2 Body) = 36. Matches DH=36.
    *   Outer Body = 4(ID) + 4(L1 DH) + 36(L1 Body) = 44. Matches DH=44.

**Analysis:**
The serialization is correct. The C# failure implies the recursive DHEADER calculation logic is flawed.

---

### 77. AtomicTests::TwoKeyInt32TopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable
@topic
struct TwoKeyInt32TopicAppendable {
    @key long key1;
    @key long key2;
    double value;
};
```

**Golden CDR Bytes:**
`00 09 00 00 10 00 00 00 61 09 00 00 00 00 00 00 00 00 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `10 00 00 00` (**16 bytes**).
*   **`key1` (long):** `61 09 00 00` (2401). Size: 4.
*   **`key2` (long):** `00 00 00 00` (Wait, trace says `00...`? IDL says keys. Golden bytes for TwoKeyInt32Topic (Test 45) had `40` and `41`. Here `61` is 2401. Key2 seems to be 0?)
    *   *Correction:* Bytes are `61 09 00 00` then `00 00 00 00`. Key2 is 0.
*   **`value` (double):** `00 00 00 00 00 00 00 00`. Size: 8.
*   **Total Body Size:** 4 + 4 + 8 = 16 bytes.

**Analysis:**
Tight packing. No padding needed as Double at offset 8 is aligned.

---

### 78. AtomicTests::SequenceStringTopicAppendable [FAIL]

**IDL Definition:**
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
*   **ID:** `3b 08...` (2107). 4 bytes.
*   **Sequence (Complex Type):**
    *   **Wrapper DHEADER:** `04 00 00 00` (**4 bytes**).
    *   **Length:** `00 00 00 00` (0).
*   **Total Body:** 4 + 4 + 4 = 12 bytes.

**Analysis:**
Confirmed: Sequence of strings (complex) gets a DHEADER wrapper.

---

### 79. AtomicTests::MaxLengthSequenceTopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable
@topic
struct MaxLengthSequenceTopicAppendable {
    @key long id;
    sequence<long, 10000> max_seq;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 cb 09 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** `08 00 00 00` (**8 bytes**).
*   **ID:** 4 bytes.
*   **Sequence (Primitive):**
    *   **Length:** `00 00 00 00` (0).
    *   *Note:* NO DHEADER wrapper for primitive sequence.
*   **Total Body:** 8 bytes.

**Analysis:**
Matches `SequenceInt32TopicAppendable`. Max bounds are ignored for serialization size.

---

### 80. AtomicTests::SequenceInt64TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct SequenceInt64TopicAppendable {
    @key long id;
    sequence<long long> values;
};
```

**Golden CDR Bytes:**
`00 09 00 00 10 00 00 00 35 08 00 00 01 00 00 00 40 b7 3a 7d 00 00 00 00`

**Decoding:**
*   **DHEADER:** `10` (16 bytes).
*   **ID:** 4 bytes.
*   **Sequence (Primitive):**
    *   **Length:** `01 00 00 00` (1). 4 bytes.
    *   **Value:** `40 b7...`. 8 bytes.
*   **Total Body:** 4 + 4 + 8 = 16 bytes.

**Analysis:**
Primitive sequence (int64) packed directly.

---

### 81. AtomicTests::DoublyNestedTopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable struct BoxAppendable { Point2DAppendable p1; Point2DAppendable p2; };
```

**Golden CDR Bytes:**
`00 09 00 00 30 00 00 00 9b 08 00 00 28 00 00 00 10 00 00 00 ...`

**Decoding:**
*   **Outer DH:** `30` (48).
*   **ID:** 4.
*   **Box:**
    *   **Box DH:** `28` (40).
    *   **P1:**
        *   **P1 DH:** `10` (16).
        *   Data: 16.
    *   **P2:**
        *   **P2 DH:** `10` (16).
        *   Data: 16.
    *   Box Body: 4(DH) + 16 + 4(DH) + 16 = 40.
*   Total: 4 + 4 + 40 = 48.

**Analysis:**
Standard recursive DHEADERs for nested appendable structs.

---

### 82. AtomicTests::UnionShortDiscTopicAppendable

**Golden CDR Bytes:**
`... 10 00 00 00 d3 07 00 00 08 00 00 00 04 00 00 00 00 60 7a 44`

**Decoding:**
*   **Outer DH:** 16.
*   **ID:** `d3 07...` (2003). 4 bytes.
*   **Union:**
    *   **Union DH:** `08 00 00 00` (8 bytes).
    *   **Disc (short):** `04 00`. 2 bytes.
        *   *Alignment Check:* Stream offset 16. Aligned.
    *   **Padding:** `00 00`. 2 bytes. (Align next float to 4).
    *   **Value (float):** `60 7a 44...`. 4 bytes.
    *   **Union Body:** 2 + 2 + 4 = 8. Matches Union DH.
*   **Total:** 4 + 4 + 8 = 16.

**Analysis:**
Union body is properly aligned internally.

---

### 83. AtomicTests::SequenceOctetTopicAppendable [FAIL]

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 3a 08 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** 8.
*   **ID:** 4.
*   **Seq Length:** 4 (0).
*   **Total:** 8.

**Analysis:**
Primitive sequence (octet) is not wrapped. Failure due to C# serializer wrapping it.



Here is the detailed CDR stream analysis for the seventh batch of test cases.

This batch highlights a **CRITICAL XCDR2 ALIGNMENT RULE**:
In XCDR2 (PL_CDR2), **8-byte primitives (double, long long) appear to only require 4-byte alignment** within the DHEADER scope, contrary to standard XCDR1 (which requires 8-byte alignment). This explains why padding is often smaller than expected (e.g., 2 bytes instead of 6).

### 84. AtomicTests::SequenceBooleanTopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable
@topic
struct SequenceBooleanTopicAppendable {
    @key long id;
    sequence<boolean> values;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 39 08 00 00 00 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **DHEADER:** `08 00 00 00` (8 bytes).
*   **Member `id` (long):** `39 08 00 00` (2105). Size: 4.
*   **Member `values` (sequence):**
    *   **Length:** `00 00 00 00` (0). Size: 4.
    *   **Content:** None.
*   **Total Body Size:** 8 bytes.

**Analysis:**
Primitive sequence = No Wrapper. The C# serializer likely wrapped it in a DHEADER, causing a size mismatch.

---

### 85. AtomicTests::MultiOptionalTopicAppendable [FAIL]

**IDL Definition:**
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
*   **Encapsulation Header:** `00 09 00 01` (PL_CDR2 LE).
*   **DHEADER:** `07 00 00 00` (7 bytes).
*   **Member `id` (long):** `02 09 00 00` (2306). Size: 4.
*   **Optional Flags:**
    *   `opt_int`: `00` (False).
    *   `opt_double`: `00` (False).
    *   `opt_string`: `00` (False).
*   **Total Body Size:** 4 + 1 + 1 + 1 = 7 bytes.

**Analysis:**
Multiple optional fields in an appendable struct are serialized as a contiguous block of boolean flags (bytes) immediately following the previous field. There is no padding between them.

---

### 86. AtomicTests::NestedStructTopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable struct Point2DAppendable { double x; double y; };
@appendable @topic struct NestedStructTopicAppendable { @key long id; Point2DAppendable point; };
```

**Golden CDR Bytes:**
`00 09 00 00 18 00 00 00 99 08 00 00 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00`

**Decoding:**
*   **Outer DHEADER:** `18 00 00 00` (**24 bytes**).
*   **Member `id` (long):** `99 08 00 00` (2201). Size: 4.
*   **Member `point` (Nested Appendable):**
    *   **Inner DHEADER:** `10 00 00 00` (**16 bytes**).
    *   **Content (2 doubles):** 16 bytes.
*   **Total Body Size:** 4 + 4 + 16 = 24 bytes.

**Analysis:**
Recursive DHEADER structure. C# likely missed the inner DHEADER.

---

### 87. AtomicTests::SequenceStructTopicAppendable [FAIL]

**IDL Definition:**
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
*   **DHEADER:** `0c 00 00 00` (**12 bytes**).
*   **Member `id` (long):** `3c 08 00 00` (2108). Size: 4.
*   **Member `points` (Sequence of Structs):**
    *   **Wrapper DHEADER:** `04 00 00 00` (**4 bytes**).
    *   **Length:** `00 00 00 00` (0).
*   **Total Body Size:** 4 + 4 + 4 = 12 bytes.

**Analysis:**
Sequences of **constructed types** (structs) MUST be wrapped in a DHEADER.

---

### 88. AtomicTests::NestedTripleKeyTopicAppendable [PASS (Verified Fail)]

*Note: Log indicated "CDR Verify FAILED: Received 16, Serialized 18".*

**IDL Definition:**
```idl
@appendable struct TripleKeyAppendable { @key long id1; @key long id2; @key long id3; };
@appendable @topic struct NestedTripleKeyTopicAppendable { @key TripleKeyAppendable keys; string<64> data; };
```

**Golden CDR Bytes:**
`00 09 00 02 16 00 00 00 0c 00 00 00 67 09 00 00 00 00 00 00 00 00 00 00 02 00 00 00 44 00 00 00`

**Decoding:**
*   **Outer DHEADER:** `16` (**22 bytes**).
*   **Member `keys` (Nested Appendable):**
    *   **Inner DHEADER:** `0c` (**12 bytes**).
    *   **Content (3 longs):** 12 bytes.
*   **Member `data` (string):**
    *   **Length:** `02 00 00 00` (2).
    *   **Content:** `44 00` ("D\0"). Size: 2.
*   **Total Body Size:** 4 (InnerDH) + 12 (InnerBody) + 4 (StrLen) + 2 (StrData) = 22 bytes.

**Analysis:**
Strict packing. String content (2 bytes) is not padded to 4. C# serialized 24 bytes (padded string).

---

### 89. AtomicTests::NestedKeyTopicAppendable

**IDL Definition:**
```idl
@appendable struct LocationAppendable { @key long building; @key short floor; };
@appendable @topic struct NestedKeyTopicAppendable { @key LocationAppendable loc; double temperature; };
```

**Golden CDR Bytes:**
`00 09 00 00 14 00 00 00 06 00 00 00 65 09 00 00 00 00 00 00 00 00 00 00 00 00 00 00`

**Decoding:**
*   **Outer DHEADER:** `14` (**20 bytes**).
*   **Member `loc` (Nested):**
    *   **Inner DHEADER:** `06` (**6 bytes**).
    *   **Content:** `building`(4) + `floor`(2) = 6 bytes.
*   **Padding:** `00 00` (2 bytes).
    *   *Why?* Next field `temperature` is `double`.
    *   Stream offsets:
        *   EncHdr(4) + OuterDH(4) + InnerDH(4) + InnerBody(6) = 18.
        *   18 + 2 (pad) = 20.
        *   Double starts at 20.
        *   **CRITICAL:** 20 is **4-byte aligned**, but **NOT 8-byte aligned**.
*   **Member `temperature` (double):** `00...00`. Size: 8.
*   **Total Body Size:** 4 (InnerDH) + 6 (InnerBody) + 2 (Pad) + 8 (Double) = 20 bytes.

**Analysis:**
This test confirms that in XCDR2 (or at least this implementation of it), **`double` only forces 4-byte alignment**, not 8-byte alignment. This explains why padding is 2 bytes (18 -> 20) instead of 6 bytes (18 -> 24).

---

### 90. AtomicTests::DeepNestedStructTopic [FAIL]

**IDL Definition:**
```idl
@final
struct DeepNestedStructTopic { @key long id; Level1 nested1; }; // XCDR1
```

**Golden CDR Bytes:**
`00 01 00 00 cc 09 00 00 cc 09 00 00 00 00 00 00 ...`

**Decoding:**
*   **Encapsulation:** `00 01` (XCDR1).
*   **Content:**
    *   `id`: 4 bytes.
    *   `nested1.value1`: 4 bytes.
    *   `nested1.nested2.value2`: 4 bytes.
    *   ... (5 levels).
*   **Total:** Flat sequence of 6 longs (4+4+4+4+4+4) = 24 bytes.

**Analysis:**
XCDR1 flattens nested structs if there are no alignment gaps. `long`s are perfectly packed.

---

### 91. AtomicTests::OptionalFloat64TopicAppendable [FAIL]

**IDL Definition:**
```idl
@appendable @topic struct OptionalFloat64TopicAppendable { @key long id; @optional double opt_value; };
```

**Golden CDR Bytes:**
`00 09 00 03 05 00 00 00 fe 08 00 00 00 00 00 00`

**Decoding:**
*   **DHEADER:** 5 bytes.
*   **ID:** 4 bytes.
*   **Flag:** 1 byte (`00` = Absent).
*   **Total:** 5 bytes.

**Analysis:**
Absent optional double is 1 byte. C# tried to read more and crashed.

---

### 92. AtomicTests::SequenceStructTopic

**IDL Definition:**
```idl
@final @topic struct SequenceStructTopic { @key long id; sequence<Point2D> points; };
```

**Golden CDR Bytes:**
`00 01 00 00 44 02 00 00 02 00 00 00 cd cc cc cc cc 20 82 40 ...`

**Decoding:**
*   **Encapsulation:** `00 01` (XCDR1).
*   **ID:** `44 02...` (580). 4 bytes.
*   **Length:** `02 00 00 00` (2). 4 bytes.
    *   *Note:* No padding between ID and Length.
*   **Points:**
    *   `Point[0]`: 16 bytes.
    *   `Point[1]`: 16 bytes.
*   **Total Body:** 4 + 4 + 16 + 16 = 40 bytes.

**Analysis:**
Standard XCDR1.

---

### 93. AtomicTests::SequenceFloat32Topic

**Golden CDR Bytes:**
`00 01 00 00 08 02 00 00 01 00 00 00 ...`

**Decoding:**
*   **ID:** 4 bytes.
*   **Length:** 4 bytes.
*   **Floats:** Contiguous.

**Analysis:**
Standard XCDR1.

---

### 94. AtomicTests::BoundedSequenceInt32Topic

**Golden CDR Bytes:**
`00 01 00 00 f9 01 00 00 06 00 00 00 ...`

**Decoding:**
*   **ID:** 4 bytes.
*   **Length:** 4 bytes (Value 6).
*   **Data:** 6 * 4 = 24 bytes.

**Analysis:**
Bounds do not affect XCDR1 serialization layout.

---

### 95. AtomicTests::SequenceStringTopic

**Golden CDR Bytes:**
`00 01 00 00 30 02 00 00 01 00 00 00 08 00 00 00 53 5f 35 36 30 5f 30 00`

**Decoding:**
*   **ID:** 4 bytes.
*   **Seq Length:** `01 00...`. 4 bytes.
*   **Str Length:** `08 00...`. 4 bytes.
*   **Str Data:** `53...00` (8 bytes).
*   **Total Body:** 4 + 4 + 4 + 8 = 20 bytes.

**Analysis:**
Standard XCDR1.


Here is the detailed CDR stream analysis for the eighth and final batch of test cases.

This batch provides a critical insight into **Sequence Encapsulation Rules in XCDR2**:
While `sequence<int32>` is raw (Length + Data), `sequence<enum>` is **WRAPPED** (DHeader + Length + Data). This distinction is vital for the serializer logic.

---

### 96. AtomicTests::SequenceUnionAppendableTopic

**IDL Definition:**
```idl
@appendable
@topic
struct SequenceUnionAppendableTopic {
    @key long id;
    sequence<SimpleUnionAppendable> unions;
};
```

**Golden CDR Bytes:**
`00 09 00 00 18 00 00 00 dc 05 00 00 10 00 00 00 01 00 00 00 08 00 00 00 01 00 00 00 98 3a 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **Outer DHEADER:** `18 00 00 00` (**24 bytes**).
*   **Member `id` (long):** `dc 05 00 00` (1500). Size: 4.
*   **Member `unions` (Sequence of Appendable Unions):**
    *   **Wrapper DHEADER:** `10 00 00 00` (**16 bytes**).
    *   **Sequence Length:** `01 00 00 00` (1 element). Size: 4.
    *   **Element[0] (Union):**
        *   **Union DHEADER:** `08 00 00 00` (8 bytes).
        *   **Discriminator:** `01 00 00 00` (1). Size: 4.
        *   **Value (long):** `98 3a 00 00`. Size: 4.
        *   Union Total: 4 + 4 = 8. (Matches Union DH).
    *   **Sequence Body Total:** 4 (Len) + 12 (Element) = 16 bytes. (Matches Wrapper DH).
*   **Outer Body Total:** 4 (ID) + 4 (WrapperDH) + 16 (SeqBody) = 24 bytes. (Matches Outer DH).

**Analysis:**
Sequences of complex types (Unions) are wrapped in DHEADERs. Inside the sequence, each appendable element (Union) also has its own DHEADER.

---

### 97. AtomicTests::SequenceInt32Topic

**IDL Definition:**
```idl
@final
@topic
struct SequenceInt32Topic {
    @key long id;
    sequence<long> values;
};
```

**Golden CDR Bytes:**
`00 01 00 00 f4 01 00 00 02 00 00 00 8c 3c 00 00 ab 3c 00 00`

**Decoding:**
*   **Header:** `00 01 00 00` (XCDR1).
*   **ID:** `f4 01 00 00` (500). 4 bytes.
*   **Length:** `02 00 00 00` (2). 4 bytes.
*   **Data:** 2 * 4 = 8 bytes.
*   **Total Body:** 16 bytes.

**Analysis:**
Standard XCDR1 primitive sequence.

---

### 98. AtomicTests::SequenceEnumAppendableTopic

**IDL Definition:**
```idl
@appendable
@topic
struct SequenceEnumAppendableTopic {
    @key long id;
    sequence<ColorEnum> colors;
};
```

**Golden CDR Bytes:**
`00 09 00 00 14 00 00 00 e6 05 00 00 0c 00 00 00 02 00 00 00 04 00 00 00 05 00 00 00`

**Decoding:**
*   **Encapsulation Header:** `00 09 00 00`
*   **Outer DHEADER:** `14 00 00 00` (**20 bytes**).
*   **Member `id` (long):** `e6 05 00 00` (1510). Size: 4.
*   **Member `colors` (Sequence of Enums):**
    *   **Wrapper DHEADER:** `0c 00 00 00` (**12 bytes**).
        *   **CRITICAL FINDING:** Unlike `sequence<int32>` (Test 99), `sequence<enum>` **HAS** a Wrapper DHEADER.
        *   This implies XCDR2 treats Enums as "constructed" or complex types in the context of sequence encapsulation, or simply distinct from strict "Primitive Numbers".
    *   **Length:** `02 00 00 00` (2). Size: 4.
    *   **Element[0]:** `04 00 00 00` (MAGENTA). Size: 4.
    *   **Element[1]:** `05 00 00 00` (CYAN). Size: 4.
    *   **Sequence Body Total:** 4 + 4 + 4 = 12 bytes. (Matches Wrapper DH).
*   **Outer Body Total:** 4 (ID) + 4 (WrapperDH) + 12 (SeqBody) = 20 bytes.

**Analysis:**
**Serializer Rule Update:** For XCDR2 sequences:
*   `sequence<primitive_int/float/bool>`: **No** Wrapper DHEADER.
*   `sequence<enum>`: **YES** Wrapper DHEADER.
*   `sequence<string/struct/union>`: **YES** Wrapper DHEADER.

---

### 99. AtomicTests::SequenceInt32TopicAppendable

**IDL Definition:**
```idl
@appendable
@topic
struct SequenceInt32TopicAppendable {
    @key long id;
    sequence<long> values;
};
```

**Golden CDR Bytes:**
`00 09 00 00 08 00 00 00 dc 05 00 00 00 00 00 00`

**Decoding:**
*   **Outer DHEADER:** `08 00 00 00` (8 bytes).
*   **ID:** 4 bytes.
*   **Member `values`:**
    *   **Length:** `00 00 00 00` (0).
    *   **Wrapper?** None. If there was a wrapper, we would see a DHEADER before the length.
*   **Total Body:** 8 bytes.

**Analysis:**
Confirmed distinct handling compared to SequenceEnum (Test 98).

---

### 100. AtomicTests::SequenceUnionTopic

**IDL Definition:**
```idl
@final @topic struct SequenceUnionTopic { @key long id; sequence<SimpleUnion> unions; };
```

**Golden CDR Bytes:**
`00 01 00 00 4e 02 00 00 01 00 00 00 03 00 00 00 08 00 00 00 55 5f ...`

**Decoding:**
*   **Header:** `00 01 00 00` (XCDR1).
*   **ID:** `4e 02...` (590). 4 bytes.
*   **Seq Length:** `01 00...`. 4 bytes.
*   **Union[0]:**
    *   **Disc:** `03 00...` (String case). 4 bytes.
    *   **String Len:** `08 00...`. 4 bytes.
    *   **String Data:** 8 bytes.
*   **Total:** 4 + 4 + (4+4+8) = 24 bytes.

**Analysis:**
Standard XCDR1.

---

### 101. AtomicTests::SequenceEnumTopic

**IDL Definition:**
```idl
@final @topic struct SequenceEnumTopic { @key long id; sequence<SimpleEnum> values; };
```

**Golden CDR Bytes:**
`00 01 00 00 3a 02 00 00 01 00 00 00 00 00 00 00`

**Decoding:**
*   **Header:** `00 01 00 00` (XCDR1).
*   **ID:** 4 bytes.
*   **Length:** 4 bytes.
*   **Value:** `00 00 00 00`. 4 bytes.
*   **Total:** 12 bytes.

**Analysis:**
Standard XCDR1.

---

### 102. AtomicTests::SequenceInt64Topic

**IDL Definition:**
```idl
@final @topic struct SequenceInt64Topic { @key long id; sequence<long long> values; };
```

**Golden CDR Bytes:**
`00 01 00 00 fe 01 00 00 01 00 00 00 30 c8 07 00 00 00 00 00`

**Decoding:**
*   **ID:** `fe 01...` (510). 4 bytes.
*   **Length:** `01 00...`. 4 bytes.
*   **Padding:** None?
    *   Stream offset: 4(Hdr)+4(ID)+4(Len) = 12.
    *   Next is `int64`. Needs 8-byte align.
    *   **Wait.** Bytes are `... 01 00 00 00 30 c8...`
    *   There is NO padding visible between Length (01) and Value (30).
    *   Offset 12 is NOT 8-byte aligned.
    *   **Observation:** The golden bytes dump might be truncating zeros, OR XCDR1 sequence buffers alignment is handled differently?
    *   Let's check the values. `30 c8 07 00...` = 510000.
    *   If bytes are `00 01 00 00` `fe 01 00 00` `01 00 00 00` `30 c8...` -> Total 20 bytes.
    *   4(Hdr) + 4(ID) + 4(Len) + 8(Val) = 20.
    *   It seems **XCDR1 did NOT pad** the int64 here.
    *   *Correction:* In XCDR1, the sequence length is 4 bytes. The sequence data alignment depends on the element type. If the sequence is valid XCDR1, it implies the buffer started 8-byte aligned, but the internal offset 12 isn't.
    *   *Possible Explanation:* The native writer determined that `sequence<long long>` elements don't strictly require 8-byte alignment relative to the *message* start, but relative to the *sequence* start? No, that's not standard.
    *   *Alternative:* Maybe the golden bytes are misleading or I'm miscounting.
    *   Actually, let's look at `SequenceFloat64Topic` (Test 103) below.

---

### 103. AtomicTests::SequenceFloat64Topic

**Golden CDR Bytes:**
`00 01 00 00 12 02 00 00 01 00 00 00 00 00 00 00 00 38 92 40`

**Decoding:**
*   **ID:** `12 02...` (530). 4 bytes.
*   **Length:** `01 00...`. 4 bytes.
*   **Padding:** `00 00 00 00`. (**4 bytes**).
*   **Value:** `00...40`. 8 bytes.
*   **Total:** 4 + 4 + 4 + 8 = 20 bytes.

**Analysis:**
Here, **Padding IS present** (Offset 12 -> 16).
Why did `SequenceInt64Topic` (Test 102) **NOT** have padding?
*   Test 102 Bytes: `00 01 00 00 fe 01 00 00 01 00 00 00 30 c8 07 00 00 00 00 00`
    *   Length: `01 00 00 00`
    *   Value: `30 c8 07 00 00 00 00 00`.
    *   Wait, is `30 c8 07 00` the padding? No, that's the value (510000).
    *   This is extremely strange. `Float64` got padding, `Int64` didn't.
    *   **Hypothesis:** The native implementation has a bug or specific optimization for `int64` vs `double` alignment in sequences, OR `Int64` alignment is 4 in this specific dialect of XCDR1? (Unlikely).
    *   **Actually**, looking closely at Test 102 dump: `... 30 c8 07 00 00 00 00 00`. That is 8 bytes.
    *   If there was padding, it would be `00 00 00 00` before `30...`.
    *   This inconsistency (Test 102 vs 103) is a red flag, but both passed. This implies the C# reader handles both padded and unpadded reads or happens to align correctly?
    *   *Correction:* Check Test 102 log output again.
    *   Received 20 bytes. 4(H)+4(ID)+4(Len)+8(Val)=20. **No Padding.**
    *   Test 103: Received 20 bytes. 4(H)+4(ID)+4(Len)+4(Pad)+8(Val) = 24?
    *   Log says received 20 bytes for Test 103 too.
    *   `00 01 00 00 12 02 00 00 01 00 00 00 00 00 00 00 00 38 92 40`.
    *   Count bytes: 4 (H) + 4 (ID) + 4 (Len) + 8 (Rest).
    *   The "Rest" is `00 00 00 00 00 38 92 40`.
    *   If value is ~1166, double representation is `... 40`.
    *   Is `00 00 00 00` padding or part of the double?
    *   1166.0 in double (LE): `00 00 00 00 00 38 92 40`.
    *   So the 8 bytes ARE the value. **There is NO padding in Test 103 either.**
    *   **Correction on Analysis:** In these specific XCDR1 tests, `int64` and `double` sequences were NOT padded to 8 bytes after the 4-byte length.
    *   *Why?* Because the sequence length is 4 bytes. If the sequence started 8-byte aligned, the Data starts at offset 4 relative to sequence start.
    *   Native DDS implementations sometimes treat Sequence Data as a separate block requiring only 4-byte alignment (for the length).

---

### 106. AtomicTests::UnionLongDiscTopicAppendable

**IDL Definition:**
```idl
@appendable
union SimpleUnionAppendable switch(long) { case 1: long int_val; ... };
@appendable @topic struct UnionLongDiscTopicAppendable { @key long id; SimpleUnionAppendable data; };
```

**Golden CDR Bytes:**
`00 09 00 00 14 00 00 00 40 06 00 00 0c 00 00 00 02 00 00 00 00 00 00 00 00 c0 a2 40`

**Decoding:**
*   **Outer DH:** `14` (20 bytes).
*   **ID:** `40 06...` (1600). 4 bytes.
*   **Union:**
    *   **Union DH:** `0c` (12 bytes).
    *   **Disc (long):** `02 00 00 00` (2 = Double case). 4 bytes.
    *   **Padding:** `00 00 00 00`. (Wait, let's count).
    *   Bytes remaining: `00 00 00 00 00 c0 a2 40`. (8 bytes).
    *   These 8 bytes are the Double `2400.0`.
    *   So there is **NO padding** between Disc (4) and Double (8).
    *   Union Body: 4 (Disc) + 8 (Val) = 12. Matches DH.
*   **Total:** 20 bytes.

**Analysis:**
Confirmed XCDR2 Appendable Union: **No alignment padding** between 4-byte discriminator and 8-byte value. Tight packing rules apply.

---

### 109. AtomicTests::UnionShortDiscTopic

**IDL Definition:**
```idl
@final union ShortUnion switch(short) { ... case 3: long long_val; };
```

**Golden CDR Bytes:**
`00 01 00 00 76 02 00 00 03 00 00 00 f0 9c 09 00`

**Decoding:**
*   **Header:** `00 01...`
*   **ID:** 4 bytes.
*   **Disc:** `03 00 00 00`.
    *   Discriminator is `short` (2 bytes).
    *   Next field is `long` (4 bytes).
    *   XCDR1 alignment: Short is at offset 4. Value must be at offset 8.
    *   So: `03 00` (Disc) + `00 00` (Pad) = 4 bytes.
*   **Value:** `f0 9c...`. 4 bytes.
*   **Total Body:** 4 (ID) + 2 (Disc) + 2 (Pad) + 4 (Val) = 12 bytes.

**Analysis:**
XCDR1 enforces internal union alignment.

---





