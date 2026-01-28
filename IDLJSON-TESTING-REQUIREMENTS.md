# IdlJson.Tests - Missing Topic Coverage

**Date:** January 28, 2026  
**Status:** Action Required  
**Purpose:** Ensure all roundtrip test topics pass IdlJson verification before implementing serialization support

---

## Overview

The following topics from [atomic_tests.idl](tests/CsharpToC.Roundtrip.Tests/idl/atomic_tests.idl) are **NOT yet present** in [verification.idl](tests/IdlJson.Tests/verification.idl) and need to be added to ensure they pass IdlJson.Tests before implementing roundtrip serialization support.

**Critical Path:** IdlJson validation → Roundtrip implementation

---

## Missing Topics by Category

### 1. Primitive Types (FINAL Variants)

**Already in verification.idl:** ✅ Most primitives covered in `AllPrimitives` topic

**Missing individual atomic topics:**
- `BooleanTopic` - single boolean field test
- `CharTopic` - single char field test
- `OctetTopic` - single octet field test
- `Int16Topic` - single short field test
- `UInt16Topic` - single unsigned short field test
- `Int32Topic` - single long field test
- `UInt32Topic` - single unsigned long field test
- `Int64Topic` - single long long field test
- `UInt64Topic` - single unsigned long long field test
- `Float32Topic` - single float field test
- `Float64Topic` - single double field test
- `StringUnboundedTopic` - unbounded string test
- `StringBounded32Topic` - 32-char bounded string test
- `StringBounded256Topic` - 256-char bounded string test

**Action:** Add these atomic topics to verification.idl for isolated primitive testing

---

### 2. Primitive Types (APPENDABLE Variants)

**All appendable variants missing:**
- `BooleanTopicAppendable`
- `CharTopicAppendable`
- `OctetTopicAppendable`
- `Int16TopicAppendable`
- `UInt16TopicAppendable`
- `Int32TopicAppendable`
- `UInt32TopicAppendable`
- `Int64TopicAppendable`
- `UInt64TopicAppendable`
- `Float32TopicAppendable`
- `Float64TopicAppendable`
- `StringUnboundedTopicAppendable`
- `StringBounded32TopicAppendable`
- `StringBounded256TopicAppendable`

**Action:** Add appendable extensibility variants

---

### 3. Enumerations

**Missing:**
- `EnumTopic` (FINAL) - SimpleEnum test
- `ColorEnumTopic` (FINAL) - ColorEnum test
- `EnumTopicAppendable` (APPENDABLE)
- `ColorEnumTopicAppendable` (APPENDABLE)

**Note:** verification.idl has enums defined but not dedicated enum topics

**Action:** Add enum-focused topics

---

### 4. Nested Structures

**Missing FINAL variants:**
- `NestedStructTopic` - Point2D nested
- `Nested3DTopic` - Point3D nested
- `DoublyNestedTopic` - Box with nested Point2D
- `ComplexNestedTopic` - Container with Point3D

**Missing APPENDABLE variants:**
- `NestedStructTopicAppendable`
- `Nested3DTopicAppendable`
- `DoublyNestedTopicAppendable`
- `ComplexNestedTopicAppendable`

**Action:** Add all nested structure topics

---

### 5. Unions

**Missing FINAL variants:**
- `UnionLongDiscTopic` - SimpleUnion with long discriminator
- `UnionBoolDiscTopic` - BoolUnion with boolean discriminator
- `UnionEnumDiscTopic` - ColorUnion with enum discriminator
- `UnionShortDiscTopic` - ShortUnion with short discriminator

**Missing APPENDABLE variants:**
- `UnionLongDiscTopicAppendable`
- `UnionBoolDiscTopicAppendable`
- `UnionEnumDiscTopicAppendable`
- `UnionShortDiscTopicAppendable`

**Action:** Add all union discriminator type variations

---

### 6. Optional Fields

**Missing FINAL variants:**
- `OptionalInt32Topic`
- `OptionalFloat64Topic`
- `OptionalStringTopic`
- `OptionalStructTopic`
- `OptionalEnumTopic`
- `MultiOptionalTopic`

**Missing APPENDABLE variants:**
- `OptionalInt32TopicAppendable`
- `OptionalFloat64TopicAppendable`
- `OptionalStringTopicAppendable`
- `OptionalStructTopicAppendable`
- `OptionalEnumTopicAppendable`
- `MultiOptionalTopicAppendable`

**Action:** Add optional field topics (XTypes feature)

---

### 7. Sequences

**Missing FINAL variants:**
- `SequenceInt32Topic` - unbounded sequence
- `BoundedSequenceInt32Topic` - bounded sequence<long, 10>
- `SequenceInt64Topic`
- `SequenceFloat32Topic`
- `SequenceFloat64Topic`
- `SequenceBooleanTopic`
- `SequenceOctetTopic`
- `SequenceStringTopic`
- `SequenceEnumTopic`
- `SequenceStructTopic`
- `SequenceUnionTopic`

**Missing APPENDABLE variants:**
- `SequenceInt32TopicAppendable`
- `BoundedSequenceInt32TopicAppendable`
- `SequenceInt64TopicAppendable`
- `SequenceFloat32TopicAppendable`
- `SequenceFloat64TopicAppendable`
- `SequenceBooleanTopicAppendable`
- `SequenceOctetTopicAppendable`
- `SequenceStringTopicAppendable`
- `SequenceEnumAppendableTopic`
- `SequenceStructTopicAppendable`
- `SequenceUnionAppendableTopic`

**Note:** verification.idl has some sequence topics but not comprehensive coverage

**Action:** Add all sequence type variations

---

### 8. Arrays

**Missing FINAL variants:**
- `ArrayInt32Topic` - 1D array
- `ArrayFloat64Topic` - 1D array
- `ArrayStringTopic` - array of strings
- `Array2DInt32Topic` - 2D array
- `Array3DInt32Topic` - 3D array
- `ArrayStructTopic` - array of Point2D

**Missing APPENDABLE variants:**
- `ArrayInt32TopicAppendable`
- `ArrayFloat64TopicAppendable`
- `ArrayStringTopicAppendable`
- `Array2DInt32TopicAppendable`
- `Array3DInt32TopicAppendable`
- `ArrayStructTopicAppendable`

**Note:** verification.idl has `ArrayTopic` but not atomic array topics

**Action:** Add all array dimension and type variations

---

### 9. Extensibility Variants

**Missing:**
- `AppendableInt32Topic` - @appendable annotated
- `AppendableStructTopic` - @appendable with Point2D
- `FinalInt32Topic` - @final annotated
- `FinalStructTopic` - @final with Point2D
- `MutableInt32Topic` - @mutable with @id annotations
- `MutableStructTopic` - @mutable with @id annotations

**Action:** Add explicit extensibility test topics

---

### 10. Composite Keys

**Missing FINAL variants:**
- `TwoKeyInt32Topic` - two long keys
- `TwoKeyStringTopic` - two string keys
- `ThreeKeyTopic` - mixed key types
- `FourKeyTopic` - four long keys

**Missing APPENDABLE variants:**
- `TwoKeyInt32TopicAppendable`
- `TwoKeyStringTopicAppendable`
- `ThreeKeyTopicAppendable`
- `FourKeyTopicAppendable`

**Note:** verification.idl has `CompositeKey` but limited coverage

**Action:** Add comprehensive multi-key topics

---

### 11. Nested Keys

**Missing FINAL variants:**
- `NestedKeyTopic` - Location struct key
- `NestedKeyGeoTopic` - Coordinates struct key
- `NestedTripleKeyTopic` - TripleKey struct key

**Missing APPENDABLE variants:**
- `NestedKeyTopicAppendable`
- `NestedKeyGeoTopicAppendable`
- `NestedTripleKeyTopicAppendable`

**Note:** verification.idl has `NestedKeyTopic` in different module

**Action:** Add AtomicTests module nested key topics

---

### 12. Edge Cases

**Missing FINAL variants:**
- `EmptySequenceTopic` - sequence with zero elements
- `LargeSequenceTopic` - sequence with 1000+ elements (skip for now per requirements)
- `LongStringTopic` - string<4096>
- `UnboundedStringTopic` - unbounded string with varying sizes
- `AllPrimitivesAtomicTopic` - all 12 primitives in one struct
- `MaxSizeStringTopic` - string<8192> **[NEW]**
- `MaxLengthSequenceTopic` - sequence<long, 10000> (skip for now)
- `DeepNestedStructTopic` - 5-level nesting **[NEW]**
- `UnionWithOptionalTopic` - union with @optional member **[NEW]**

**Missing APPENDABLE variants:**
- `EmptySequenceTopicAppendable`
- `UnboundedStringTopicAppendable`
- `AllPrimitivesAtomicTopicAppendable`
- `MaxSizeStringTopicAppendable` **[NEW]**
- `DeepNestedStructTopicAppendable` **[NEW]**
- `UnionWithOptionalTopicAppendable` **[NEW]**

**Action:** Add edge case topics (excluding large/max size per requirements)

---

## Recommended Implementation Order

### Phase 1: Core Primitives (High Priority)
1. Add all 14 primitive FINAL topics
2. Add all 14 primitive APPENDABLE topics
3. Verify code generation works correctly

### Phase 2: Enums and Basic Structures (High Priority)
1. Add enum topics (4 topics)
2. Add nested structure topics (8 topics)
3. Verify nested serialization

### Phase 3: Collections (Medium Priority)
1. Add array topics (12 topics)
2. Add sequence topics (22 topics)
3. Verify collection handling

### Phase 4: Advanced Types (Medium Priority)
1. Add union topics (8 topics)
2. Add optional field topics (12 topics)
3. Verify XTypes features

### Phase 5: Keys and Extensibility (Low Priority)
1. Add extensibility topics (6 topics)
2. Add composite key topics (8 topics)
3. Add nested key topics (6 topics)

### Phase 6: Edge Cases (Low Priority)
1. Add edge case topics (12 topics, excluding large/max)
2. Verify boundary conditions

---

## Summary Statistics

**Total Topics in atomic_tests.idl:** ~150+ topics  
**Topics in verification.idl:** ~15 topics (different module structure)  
**Topics to Add:** ~135+ topics

**Breakdown:**
- Primitives: 28 topics (14 FINAL + 14 APPENDABLE)
- Enums: 4 topics
- Nested Structures: 8 topics
- Unions: 8 topics
- Optional Fields: 12 topics
- Sequences: 22 topics
- Arrays: 12 topics
- Extensibility: 6 topics
- Composite Keys: 8 topics
- Nested Keys: 6 topics
- Edge Cases: 12 topics (excluding large size tests)

---

## Next Steps

1. **Developer Action Required:**
   - Open `tests/IdlJson.Tests/verification.idl`
   - Add topics from atomic_tests.idl module (copy relevant definitions)
   - Update `IdlJson.Tests` test code to verify all new topics
   - Run `IdlJson.Tests` to ensure all pass
   - Fix any code generation issues discovered

2. **Acceptance Criteria:**
   - ✅ All topics from atomic_tests.idl successfully parse in IdlJson.Tests
   - ✅ JSON descriptors generated correctly
   - ✅ No parsing errors or warnings
   - ✅ Code generator produces valid C# types

3. **Dependencies:**
   - IdlJson.Tests must pass before implementing roundtrip serialization
   - Any failures indicate code generator issues that must be fixed first

---

**Owner:** Development Team  
**Status:** Pending Implementation  
**Priority:** HIGH - Blocks roundtrip test implementation

---

## Notes

- **Exclude for now:** LargeSequenceTopic, LongStringTopic, MaxLengthSequenceTopic (per requirements)
- **Focus on:** Comprehensive type coverage with both FINAL and APPENDABLE variants
- **Rationale:** Appendable vs Final uses different CDR serialization, deserves separate testing
- **Integration:** Topics from CycloneDDS.Roundtrip.Tests will be merged into atomic_tests.idl
