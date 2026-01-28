# Roundtrip Test Expansion - Task Tracker

**Project:** C# to C Roundtrip Test Framework  
**Current Status:** 28/150+ topics implemented (19% coverage)  
**Target:** 120+ topics (comprehensive coverage with FINAL and APPENDABLE variants)  
**See:** [ROUNDTRIP-IMPLEMENTATION-GUIDE.md](ROUNDTRIP-IMPLEMENTATION-GUIDE.md) for detailed implementation instructions

---

## Progress Overview

**Current:** 28 topics ✅ | **Remaining:** 122+ topics ⏸️ | **Completion:** 19%

```
██▒▒▒▒▒▒▒▒ 19%
```

**Note:** Topic count increased significantly due to:
- Addition of comprehensive APPENDABLE variants for all topics
- New edge case topics (DeepNestedStructTopic, UnionWithOptionalTopic, MaxSizeStringTopic)
- Explicit extensibility testing (Mutable, Final variants)
- All topics now defined in [atomic_tests.idl](idl/atomic_tests.idl)

---

## Phase 1: Basic Primitives (Priority: HIGH)

**Goal:** Complete all primitive type tests (single field topics)  
**Status:** 14/14 complete  
**Estimated Effort:** 16-20 hours

- [x] **RT-P01** BooleanTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P02** Int32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P03** CharTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P04** OctetTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P05** Int16Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P06** UInt16Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P07** UInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P08** Int64Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P09** UInt64Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P10** Float32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#example-adding-float32topic)
- [x] **RT-P11** Float64Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [x] **RT-P12** StringBounded32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings)
- [x] **RT-P13** StringUnboundedTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings)
- [x] **RT-P14** StringBounded256Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings)

---

## Phase 2: Enumerations (Priority: HIGH)

**Goal:** Verify enum serialization/deserialization  
**Status:** 0/4 complete  
**Estimated Effort:** 5-6 hours

### FINAL Variants
- [ ] **RT-E01** EnumTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#55-enumerations) | IDL: `AtomicTests::EnumTopic`
- [ ] **RT-E02** ColorEnumTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#55-enumerations) | IDL: `AtomicTests::ColorEnumTopic`

### APPENDABLE Variants
- [ ] **RT-E01-A** EnumTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#55-enumerations) | IDL: `AtomicTests::EnumTopicAppendable`
- [ ] **RT-E02-A** ColorEnumTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#55-enumerations) | IDL: `AtomicTests::ColorEnumTopicAppendable`

---

## Phase 3: Arrays (Priority: HIGH)

**Goal:** Fix array handling in C# binding  
**Status:** 6/6 complete  
**Estimated Effort:** 8-12 hours

- [x] **RT-A01** ArrayInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#54-arrays)
- [x] **RT-A02** ArrayFloat64Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#54-arrays)
- [x] **RT-A03** ArrayStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#54-arrays)
- [x] **RT-A04** Array2DInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#54-arrays)
- [x] **RT-A05** Array3DInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#54-arrays)
- [x] **RT-A06** ArrayStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#54-arrays)

---

## Phase 4: Nested Structures (Priority: MEDIUM)

**Goal:** Verify nested struct serialization  
**Status:** 0/8 complete  
**Estimated Effort:** 10-14 hours

### FINAL Variants
- [ ] **RT-N01** NestedStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::NestedStructTopic`
- [ ] **RT-N02** Nested3DTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::Nested3DTopic`
- [ ] **RT-N03** DoublyNestedTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::DoublyNestedTopic`
- [ ] **RT-N04** ComplexNestedTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::ComplexNestedTopic`

### APPENDABLE Variants
- [ ] **RT-N01-A** NestedStructTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::NestedStructTopicAppendable`
- [ ] **RT-N02-A** Nested3DTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::Nested3DTopicAppendable`
- [ ] **RT-N03-A** DoublyNestedTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::DoublyNestedTopicAppendable`
- [ ] **RT-N048 complete  
**Estimated Effort:** 10-14 hours

### FINAL Variants
- [x] **RT-U01** UnionLongDiscTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionLongDiscTopic`
- [ ] **RT-U02** UnionBoolDiscTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionBoolDiscTopic`
- [ ] **RT-U03** UnionEnumDiscTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionEnumDiscTopic`
- [ ] **RT-U04** UnionShortDiscTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionShortDiscTopic`

### APPENDABLE Variants
- [x] **RT-U01-A** UnionLongDiscTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionLongDiscTopicAppendable`
- [ ] **RT-U02-A** UnionBoolDiscTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionBoolDiscTopicAppendable`
- [ ] **RT-U03-A** UnionEnumDiscTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionEnumDiscTopicAppendable`
- [ ] **RT-U04-A** UnionShortDiscTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionShortDiscTopicAppendable`
**Status:** 1/4 complete  
**Estimated Effort:** 5-7 hours

- [x] **RT-U01** UnionLongDiscTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions)
- [ ] **RT-U02** UnionBoolDiscTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions)
- [ ] **RT-U03** UnionEnumDiscTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions)
- [ ] **RT-U0422 complete  
**Estimated Effort:** 28-36 hours

### FINAL Variants
- [x] **RT-S01** SequenceInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceInt32Topic`
- [ ] **RT-S02** BoundedSequenceInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::BoundedSequenceInt32Topic`
- [ ] **RT-S03** SequenceInt64Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceInt64Topic`
- [ ] **RT-S04** SequenceFloat32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceFloat32Topic`
- [ ] **RT-S05** SequenceFloat64Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceFloat64Topic`
- [ ] **RT-S06** SequenceBooleanTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceBooleanTopic`
- [ ] **RT-S07** SequenceOctetTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceOctetTopic`
- [ ] **RT-S08** SequenceStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceStringTopic`
- [ ] **RT-S09** SequenceEnumTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceEnumTopic`
- [ ] **RT-S10** SequenceStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceStructTopic`
- [ ] **RT-S11** SequenceUnionTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceUnionTopic`

### APPENDABLE Variants
- [x] **RT-S01-A** SequenceInt32TopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceInt32TopicAppendable`
- [ ] **RT-S02-A** BoundedSequenceInt32TopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::BoundedSequenceInt32TopicAppendable`
- [ ] **RT-S03-A** SequenceInt64TopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceInt64TopicAppendable`
- [ ] **RT-S04-A** SequenceFloat32TopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceFloat32TopicAppendable`
- [ ] **RT-S05-A** SequenceFloat64TopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceFloat64TopicAppendable`
- [ ] **RT-S06-A** SequenceBooleanTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceBooleanTopicAppendable`
- [ ] **RT-S07-A** SequenceOctetTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceOctetTopicAppendable`
- [ ] **RT-S08-A** SequenceStringTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceStringTopicAppendable`
- [ ] **RT-S09-A** SequenceEnumAppendableTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceEnumAppendableTopic`
- [ ] **RT-S10-A** SequenceStructTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::SequenceStructTopicAppendable`
- [ ] **RT-S1112 complete  
**Estimated Effort:** 16-20 hours

### FINAL Variants
- [ ] **RT-O01** OptionalInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::OptionalInt32Topic`
- [ ] **RT-O02** OptionalFloat64Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::OptionalFloat64Topic`
- [ ] **RT-O03** OptionalStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::OptionalStringTopic`
- [ ] **RT-O04** OptionalStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::OptionalStructTopic`
- [ ] **RT-O05** OptionalEnumTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::OptionalEnumTopic`
- [ ] **RT-O06** MultiOptionalTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::MultiOptionalTopic`

### APPENDABLE Variants
- [ ] **RT-O01-A** OptionalInt32TopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::OptionalInt32TopicAppendable`
- [ ] **RT-O02-A** OptionalFloat64TopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::OptionalFloat64TopicAppendable`
- [ ] **RT-O03-A** OptionalStringTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields) | IDL: `AtomicTests::OptionalStringTopicAppendable`
- [ ] **RT-O2/6 complete  
**Estimated Effort:** 6-8 hours

- [x] **RT-X01** AppendableInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration) | IDL: `AtomicTests::AppendableInt32Topic`
- [x] **RT-X02** AppendableStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration) | IDL: `AtomicTests::AppendableStructTopic`
- [ ] **RT-X03** FinalInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration) | IDL: `AtomicTests::FinalInt32Topic`
- [ ] **RT-X04** FinalStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration) | IDL: `AtomicTests::FinalStructTopic`
- [ ] **RT-X05** MutableInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration) | IDL: `AtomicTests::MutableInt32Topic`
- [ ] **RT-X06** MutableStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration) | IDL: `AtomicTests::MutableStructTopic`

- [ ] **RT-O01** OptionalInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields)
- [ ] **RT-O02** OptionalFloat64Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields)
- [ ] **RT-O03** OptionalStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields)
- [ ] **RT-O04** OptionalStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields)
- [ ] **RT-O05** OptionalEnumTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields)
- [ ] **RT-O06** MultiOptionalTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields)

---

## Phase 8: Extensibility Variants (Priority: HIGH)

**Goal:** Complete extensibility matrix  
**Status:** 6/7 complete (Appendable variants)  
**Estimated Effort:** 2-3 hours

- [x] **RT-EXT-01** Explicit Final Attributes [NEW]
- [x] **RT-X01** AppendableInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration)
- [x] **RT-X02** AppendableStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration)
- [ ] **RT-X03** FinalInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration)
- [ ] **RT-X04** FinalStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#41-why-dual-configuration)
- [ ] **RT-X058 complete  
**Estimated Effort:** 12-16 hours

### FINAL Variants
- [ ] **RT-K01** TwoKeyInt32Topic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::TwoKeyInt32Topic`
- [ ] **RT-K02** TwoKeyStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::TwoKeyStringTopic`
- [ ] **RT-K03** ThreeKeyTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::ThreeKeyTopic`
- [ ] **RT-K04** FourKeyTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::FourKeyTopic`

### APPENDABLE Variants
- [ ] **RT-K01-A** TwoKeyInt32TopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::TwoKeyInt32TopicAppendable`
- [ ] **RT-K02-A** TwoKeyStringTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::TwoKeyStringTopicAppendable`
- [ ] **RT-K03-A** ThreeKeyTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::ThreeKeyTopicAppendable`
- [ ] **RT-K046 complete  
**Estimated Effort:** 10-14 hours

### FINAL Variants
- [ ] **RT-NK01** NestedKeyTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::NestedKeyTopic`
- [ ] **RT-NK02** NestedKeyGeoTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::NestedKeyGeoTopic`
- [ ] **RT-NK03** NestedTripleKeyTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::NestedTripleKeyTopic`

### APPENDABLE Variants
- [ ] **RT-NK01-A** NestedKeyTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::NestedKeyTopicAppendable`
- [ ] **RT-NK02-A** NestedKeyGeoTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::NestedKeyGeoTopicAppendable`
- [ ] **RT-NK03-A** NestedTripleKeyTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics) | IDL: `AtomicTests::NestedTripleKeyTopicAppendable`
- [ ] **RT-K02** TwoKeyStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics)
- [ ] **RT-K03** ThreeKeyTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics)
- [ ] **RT-K04** FourKeyTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics)

---

## Phase 10: Nested Keys (Priority: LOW)

**Goal:** Verify nested struct keys  
**Status:** 0/36 complete (6 topics excluded per requirements)  
**Estimated Effort:** 20-28 hours

### FINAL Variants
- [ ] **RT-EC01** EmptySequenceTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::EmptySequenceTopic`
- [ ] **RT-EC04** UnboundedStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings) | IDL: `AtomicTests::UnboundedStringTopic`
- [ ] **RT-EC05** AllPrimitivesAtomicTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types) | IDL: `AtomicTests::AllPrimitivesAtomicTopic`
- [ ] **RT-EC06** MaxSizeStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings) | IDL: `AtomicTests::MaxSizeStringTopic` **[NEW]**
- [ ] **RT-EC08** DeepNestedStructTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::DeepNestedStructTopic` **[NEW - 5 levels]**
- [ ] **RT-EC09** UnionWithOptionalTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionWithOptionalTopic` **[NEW]**

### APPENDABLE Variants
- [ ] **RT-EC01-A** EmptySequenceTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences) | IDL: `AtomicTests::EmptySequenceTopicAppendable`
- [ ] **RT-EC04-A** UnboundedStringTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings) | IDL: `AtomicTests::UnboundedStringTopicAppendable`
- [ ] **RT-EC05-A** AllPrimitivesAtomicTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types) | IDL: `AtomicTests::AllPrimitivesAtomicTopicAppendable`
- [ ] **RT-EC06-A** MaxSizeStringTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings) | IDL: `AtomicTests::MaxSizeStringTopicAppendable` **[NEW]**
- [ ] **RT-EC08-A** DeepNestedStructTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures) | IDL: `AtomicTests::DeepNestedStructTopicAppendable` **[NEW]**
- [ ] **RT-EC09-A** UnionWithOptionalTopicAppendable → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions) | IDL: `AtomicTests::UnionWithOptionalTopicAppendable` **[NEW]**

### EXCLUDED (per requirements - large/max size tests)
- ~~**RT-EC02** LargeSequenceTopic~~ → *Deferred*
- ~~**RT-EC03** LongStringTopic~~ → *Deferred*
- ~~**RT-EC07** MaxLengthSequenceTopic~~ → *Deferred*
- ~~**RT-EC10** SequenceOfOptionalTopic~~ → *Advanced combination - Phase 12
**Estimated Effort:** 12-16 hours

- [ ] **RT-EC01** EmptySequenceTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences)
- [ ] **RT-EC02** LargeSequenceTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences)
- [ ] **RT-EC03** LongStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings)
- [ ] **RT-EC04** UnboundedStringTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#52-strings)
- [ ] **RT-EC05** AllPrimitivesAtomicTopic → [guide](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)
- [ ] **RT-EC06** MaxSizeStringTopic *[define in IDL]*
- [ ] **RT-EC07** MaxLengthSequenceTopic *[define in IDL]*
- [ ] **RT-EC08** DeepNestedStructTopic *[define in IDL]*
- [ ] **RT-EC09** UnionWithOptionalTopic *[define in IDL]*
- [ ] **RT-EC10** SequenceOfOptionalTopic *[commented in IDL - enable]*

---

## Phase 12: Advanced Combinations (Priority: DEFERRED)

**Goal:** Complex type compositions (currently commented out in IDL)  
**Status:** 0/0 complete  
**Estimated Effort:** TBD

**Note:** All advanced combination topics remain commented out in atomic_tests.idl pending completion of basic phases.

- ~~**RT-AC01** SequenceOfOptionalTopic~~ → *Requires optional sequence element support*
- ~~**RT-AC02** OptionalSequenceTopic~~ → *Requires optional sequence field support*
- ~~**RT-AC03** NestedSequenceTopic~~ → *Sequence of sequences - complex*
- ~~**RT-AC04** SequenceOfStructWithSequenceTopic~~ → *Struct containing sequence in sequence*
- ~~**RT-AC05** AppendableWithSequenceTopic~~ → *Covered by sequence appendable variants*
- ~~**RT-AC06** ArrayOfSequenceTopic~~ → *Array element is sequence*
- ~~**RT-AC07** ComplexKeyTopic~~ → *Point2D as key field*

**Decision:** Defer until all basic phases (1-11) complete successfully.

---

## Summary Statistics

**Total Topics Defined:** 150+ topics in atomic_tests.idl  
**Completed:** 28 topics (19%)  
**Remaining:** 122+ topics (81%)

**Breakdown by Phase:**
- Phase 1: Primitives - 14/14 FINAL ✅, 0/14 APPENDABLE ⏸️
- Phase 2: Enums - 0/4 ⏸️
- Phase 3: Arrays - 6/6 FINAL ✅, 0/6 APPENDABLE ⏸️
- Phase 4: Nested Structures - 0/8 ⏸️
- Phase 5: Unions - 1/4 FINAL ✅, 1/4 APPENDABLE ✅, 6 remaining ⏸️
- Phase 6: Sequences - 1/11 FINAL ✅, 1/11 APPENDABLE ✅, 20 remaining ⏸️
- Phase 7: Optional Fields - 0/12 ⏸️
- Phase 8: Extensibility - 2/6 ✅
- Phase 9: Composite Keys - 0/8 ⏸️
- Phase 10: Nested Keys - 0/6 ⏸️
- Phase 11: Edge Cases - 0/16 ⏸️
- Phase 12: Advanced - 0/0 (deferred)

---

## Quality Gates

**Before marking a task complete:**
- ✅ Both FINAL and APPENDABLE variants implemented (where applicable)
- ✅ Native generators/validators added in `Native/atomic_tests_native.c`
- ✅ C# types defined with correct attributes in `AtomicTestsTypes.cs`
- ✅ Test functions added and invoked in `Program.cs`
- ✅ All 3 roundtrip phases pass
- ✅ CDR byte verification succeeds
- ✅ No regressions in existing tests
- ✅ Code follows existing patterns
- ✅ Topic exists in `idl/atomic_tests.idl`

---

## Implementation Priorities

### Sprint 1 (HIGH): Enums
**Goal:** Complete all enum tests  
**Effort:** 5-6 hours  
**Topics:** RT-E01, RT-E02 + appendable variants

### Sprint 2 (HIGH): Remaining Sequences
**Goal:** Complete sequence type matrix  
**Effort:** 24-30 hours  
**Topics:** RT-S02 through RT-S11 + appendable variants (20 topics)

### Sprint 3 (MEDIUM): Nested Structures
**Goal:** Verify nested serialization  
**Effort:** 10-14 hours  
**Topics:** RT-N01 through RT-N04 + appendable variants (8 topics)

### Sprint 4 (MEDIUM): Remaining Unions
**Goal:** Complete union discriminator types  
**Effort:** 8-12 hours  
**Topics:** RT-U02 through RT-U04 + appendable variants (6 topics)

### Sprint 5 (LOW): Optional Fields
**Goal:** XTypes optional handling  
**Effort:** 16-20 hours  
**Topics:** RT-O01 through RT-O06 + appendable variants (12 topics)

### Sprint 6 (LOW): Keys
**Goal:** Multi-key and nested key support  
**Effort:** 22-30 hours  
**Topics:** RT-K01 through RT-K04, RT-NK01 through RT-NK03 + appendable variants (14 topics)

### Sprint 7 (LOW): Extensibility
**Goal:** Final and Mutable variants  
**Effort:** 6-8 hours  
**Topics:** RT-X03 through RT-X06 (4 topics)

### Sprint 8 (LOW): Edge Cases & Polish
**Goal:** Boundary conditions, deep nesting  
**Effort:** 20-28 hours  
**Topics:** RT-EC01, RT-EC04 through RT-EC09 + appendable variants (12 topics)

---

## Critical Path

**Before implementing any roundtrip tests:**

1. ✅ **IdlJson.Tests Verification** - See [IDLJSON-TESTING-REQUIREMENTS.md](../../IDLJSON-TESTING-REQUIREMENTS.md)
   - All topics must successfully parse in IdlJson.Tests
   - JSON descriptors must generate correctly
   - Code generator must produce valid C# types
   - **Status:** Document created listing all 135+ topics needing verification

2. ⏸️ **Code Generator Fixes** - Fix any issues discovered during IdlJson testing
   - Array handling
   - Optional field support
   - Union with optional members
   - Deep nesting support

3. ⏸️ **Roundtrip Implementation** - Follow this task tracker systematically
   - Start with high-priority phases
   - Complete both FINAL and APPENDABLE variants together
   - Verify no regressions after each topic

---

## References

- **Implementation Guide:** [ROUNDTRIP-IMPLEMENTATION-GUIDE.md](ROUNDTRIP-IMPLEMENTATION-GUIDE.md)
- **IdlJson Requirements:** [IDLJSON-TESTING-REQUIREMENTS.md](../../IDLJSON-TESTING-REQUIREMENTS.md) **[NEW]**
- **IDL Definitions:** [idl/atomic_tests.idl](idl/atomic_tests.idl)
- **Design Document:** [../../docs/CSHARP-TO-C-ROUNDTRIP-DESIGN.md](../../docs/CSHARP-TO-C-ROUNDTRIP-DESIGN.md)
- **Topic Catalog:** [README.md](README.md)

---

**Last Updated:** January 28, 2026  
**Maintained By:** Development Lead  
**Status:** Updated with comprehensive APPENDABLE variants and new edge case topics

### RT-P03: CharTopic

**Deliverable:** Add support for IDL `char` type  
**Files to Modify:**
- `idl/atomic_tests.idl` - Already defined (line ~39)
- `AtomicTestsTypes.cs` - Add `CharTopic` and `CharTopicAppendable`
- `Native/atomic_tests_native.c` - Add generators/validators
- `Native/test_registry.c` - Register handlers
- `Program.cs` - Add test functions

**Implementation Pattern:** See [Primitive Types](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)

**Seed-Based Algorithm:**
```c
// Native
msg->value = (char)('A' + (seed % 26));
```
```csharp
// C#
msg.Value = (char)('A' + (s % 26));
```

**Acceptance Criteria:**
- ✅ Native → C# roundtrip passes
- ✅ CDR byte verification passes
- ✅ C# → Native roundtrip passes
- ✅ Both Final and Appendable variants work

---

### RT-P04: OctetTopic

**Deliverable:** Add support for IDL `octet` type (unsigned 8-bit)  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Primitive Types](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)

**Seed-Based Algorithm:**
```c
// Native
msg->value = (uint8_t)(seed & 0xFF);
```
```csharp
// C#
msg.Value = (byte)(s & 0xFF);
```

**C# Type Mapping:** `octet` → `byte`

---

### RT-P05: Int16Topic

**Deliverable:** Add support for IDL `short` type  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Primitive Types](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#51-primitive-types)

**Seed-Based Algorithm:**
```c
// Native
msg->value = (int16_t)(seed * 31);
```
```csharp
// C#
msg.Value = (short)(s * 31);
```

**C# Type Mapping:** `short` → `short`

---

### RT-P10: Float32Topic

**Deliverable:** Add support for IDL `float` type  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Example: Adding Float32Topic](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#example-adding-float32topic)

**Seed-Based Algorithm:**
```c
// Native
msg->value = (float)(seed * 3.14159f);
```
```csharp
// C#
msg.Value = (float)(s * 3.14159f);
```

**Special Considerations:**
- Use epsilon comparison for validation: `fabsf(a - b) < 0.0001f`
- Include `<math.h>` in native code

---

### RT-E01: EnumTopic

**Deliverable:** Verify enum type serialization  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Enumerations](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#55-enumerations)

**IDL Definition:** Already exists (line ~117)
```idl
enum SimpleEnum { FIRST, SECOND, THIRD };
struct EnumTopic { @key long id; SimpleEnum value; };
```

**Seed-Based Algorithm:**
```c
// Native
msg->value = (AtomicTests_SimpleEnum)(seed % 3);
```
```csharp
// C#
msg.Value = (SimpleEnum)(s % 3);
```

**C# Type Definition:**
```csharp
public enum SimpleEnum { FIRST = 0, SECOND = 1, THIRD = 2 }
```

---

### RT-A01: ArrayInt32Topic [FIX EXISTING]

**Deliverable:** Fix array handling in C# binding  
**Status:** IDL defined, Native handler exists, but C# test skipped  
**Current Issue:** Array handling incomplete in code generator

**Files to Modify:**
- `AtomicTestsTypes.cs` - Uncomment/fix `ArrayInt32Topic` definition
- `Program.cs` - Uncomment `TestArrayInt32()` function
- Possibly `src/CycloneDDS.CodeGen/` - Fix array code generation

**Implementation Pattern:** See [Arrays](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#54-arrays)

**IDL Definition:** Already exists (line ~365)
```idl
struct ArrayInt32Topic { @key long id; long values[5]; };
```

**C# Type:**
```csharp
[DdsTopic("ArrayInt32Topic")]
[DdsManaged]
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct ArrayInt32Topic
{
    [DdsKey]
    public int Id { get; set; }
    
    [ArrayLength(5)]
    public int[] Values { get; set; }
}
```

**Acceptance Criteria:**
- ✅ C# code generator produces correct array serialization code
- ✅ Fixed-size array (5 elements) works correctly
- ✅ All 3 roundtrip phases pass

---

### RT-N01: NestedStructTopic

**Deliverable:** Verify nested struct serialization  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Nested Structures](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#56-nested-structures)

**IDL Definition:** Already exists (line ~136)
```idl
struct Point2D { double x; double y; };
struct NestedStructTopic { @key long id; Point2D point; };
```

**C# Types:**
```csharp
[DdsStruct]
public partial struct Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
}

[DdsTopic("NestedStructTopic")]
[DdsManaged]
public partial struct NestedStructTopic
{
    [DdsKey]
    public int Id { get; set; }
    public Point2D Point { get; set; }
}
```

**Seed-Based Algorithm:**
```c
// Native
msg->point.x = (double)(seed * 1.0);
msg->point.y = (double)(seed * 2.0);
```
```csharp
// C#
msg.Point = new Point2D { X = (double)(s * 1.0), Y = (double)(s * 2.0) };
```

---

### RT-U02: UnionBoolDiscTopic

**Deliverable:** Union with boolean discriminator  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Unions](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#57-unions)

**IDL Definition:** Already exists (line ~214)
```idl
union BoolUnion switch(boolean) {
    case TRUE: long true_val;
    case FALSE: double false_val;
};
struct UnionBoolDiscTopic { @key long id; BoolUnion data; };
```

**Seed-Based Algorithm:**
```c
// Native
msg->data._d = (seed % 2) == 0;
if (msg->data._d) {
    msg->data._u.true_val = seed * 100;
} else {
    msg->data._u.false_val = seed * 1.5;
}
```

**Special Considerations:**
- Boolean discriminator uses `TRUE`/`FALSE` constants
- C# discriminator type is `bool`

---

### RT-S02: BoundedSequenceInt32Topic

**Deliverable:** Sequence with maximum length constraint  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Sequences](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#53-sequences)

**IDL Definition:** Already exists (line ~307)
```idl
struct BoundedSequenceInt32Topic {
    @key long id;
    sequence<long, 10> values;  // Max 10 elements
};
```

**C# Type:**
```csharp
[DdsTopic("BoundedSequenceInt32Topic")]
[DdsManaged]
public partial struct BoundedSequenceInt32Topic
{
    [DdsKey]
    public int Id { get; set; }
    
    [MaxLength(10)]
    public List<int> Values { get; set; }
}
```

**Seed-Based Algorithm:**
```c
// Native
uint32_t len = (seed % 11);  // 0-10 elements (within bound)
if (len > 10) len = 10;
```

---

### RT-O01: OptionalInt32Topic

**Deliverable:** Optional field handling  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Optional Fields](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#58-optional-fields)

**IDL Definition:** Already exists (line ~264)
```idl
struct OptionalInt32Topic {
    @key long id;
    @optional long opt_value;
};
```

**C# Type:**
```csharp
[DdsTopic("OptionalInt32Topic")]
[DdsManaged]
public partial struct OptionalInt32Topic
{
    [DdsKey]
    public int Id { get; set; }
    
    [DdsOptional]
    public int? Opt_value { get; set; }
}
```

**Seed-Based Algorithm:**
```c
// Native
if ((seed % 2) == 0) {
    msg->opt_value = dds_alloc(sizeof(int32_t));
    *msg->opt_value = seed * 10;
} else {
    msg->opt_value = NULL;
}
```

**Special Considerations:**
- Native uses pointers (NULL = unset)
- C# uses Nullable types (`int?`)
- Memory management: `dds_alloc` for set values

---

### RT-K01: TwoKeyInt32Topic

**Deliverable:** Multi-key topic handling  
**Files to Modify:** Same as RT-P03  
**Implementation Pattern:** See [Multi-Key Topics](ROUNDTRIP-IMPLEMENTATION-GUIDE.md#59-multi-key-topics)

**IDL Definition:** Already exists (line ~437)
```idl
struct TwoKeyInt32Topic {
    @key long key1;
    @key long key2;
    double value;
};
```

**C# Type:**
```csharp
[DdsTopic("TwoKeyInt32Topic")]
public partial struct TwoKeyInt32Topic
{
    [DdsKey]
    public int Key1 { get; set; }
    
    [DdsKey]
    public int Key2 { get; set; }
    
    public double Value { get; set; }
}
```

**Seed-Based Algorithm:**
```c
// Native
msg->key1 = seed;
msg->key2 = seed + 1;
msg->value = (double)(seed * 0.5);
```

**Special Considerations:**
- Both keys must match for instance identity
- DDS uses composite key for filtering/querying

---

## Implementation Priorities

### Sprint 1 (Week 1-2): Basic Coverage
**Goal:** Complete primitives + enums + fix arrays  
**Tasks:** RT-P03 through RT-P14, RT-E01, RT-E02, RT-A01

### Sprint 2 (Week 3-4): Collections
**Goal:** Complete sequences and remaining arrays  
**Tasks:** RT-S02 through RT-S11, RT-A02 through RT-A06

### Sprint 3 (Week 5-6): Complex Types
**Goal:** Nested structs, remaining unions, extensibility  
**Tasks:** RT-N01 through RT-N04, RT-U02 through RT-U04, RT-X03 through RT-X06

### Sprint 4 (Week 7-8): Advanced Features
**Goal:** Optional fields, multi-keys, edge cases  
**Tasks:** RT-O01 through RT-O06, RT-K01 through RT-K04, RT-NK01 through RT-NK03

### Sprint 5 (Week 9-10): Edge Cases & Polish
**Goal:** Boundary conditions, stress tests, cleanup  
**Tasks:** RT-EC01 through RT-EC10

---

## Quality Gates

**Before marking a task complete:**
- ✅ Both Final and Appendable variants implemented
- ✅ Native generators/validators added
- ✅ C# types defined with correct attributes
- ✅ Test functions added and invoked in Main()
- ✅ All 3 roundtrip phases pass
- ✅ CDR byte verification succeeds
- ✅ No regressions in existing tests
- ✅ Code follows existing patterns

---

## References

- **Implementation Guide:** [ROUNDTRIP-IMPLEMENTATION-GUIDE.md](ROUNDTRIP-IMPLEMENTATION-GUIDE.md)
- **Status Analysis:** [ROUNDTRIP-STATUS-ANALYSIS.md](ROUNDTRIP-STATUS-ANALYSIS.md)
- **Design Document:** [../../docs/CSHARP-TO-C-ROUNDTRIP-DESIGN.md](../../docs/CSHARP-TO-C-ROUNDTRIP-DESIGN.md)
- **Topic Catalog:** [README.md](README.md)

---

**Last Updated:** January 28, 2026  
**Maintained By:** Development Lead  
**Status:** Updated with comprehensive APPENDABLE variants and new edge case topics

---

## Detailed Task Definitions

### Task Template (Example)

**Task ID:** RT-XXX  
**Topic Name:** [TopicName] | [TopicNameAppendable]  
**IDL Location:** `AtomicTests::[TopicName]`  
**Priority:** [HIGH/MEDIUM/LOW]  
**Effort:** [X-Y hours]

**Deliverable:** [Description]

**Files to Modify:**
- `idl/atomic_tests.idl` - ✅ Already defined
- `AtomicTestsTypes.cs` - Add C# type definition
- `Native/atomic_tests_native.c` - Add generator/validator
- `Native/test_registry.c` - Register handlers  
- `Program.cs` - Add test function

**Seed-Based Algorithm:**
```c
// Native generator
```
```csharp
// C# generator
```

**Acceptance Criteria:**
- ✅ Native → C# roundtrip passes
- ✅ CDR byte verification passes
- ✅ C# → Native roundtrip passes
- ✅ Both FINAL and APPENDABLE variants work

---

### Task Definitions
