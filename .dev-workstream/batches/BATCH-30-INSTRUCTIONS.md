# BATCH-30: Collections and Variants (Unions & Sequences)

**Batch Number:** BATCH-30
**Parent Batch:** BATCH-29
**Estimated Effort:** 6-8 hours
**Priority:** HIGH

---

## 📋 Onboarding & Workflow

### Background
We have successfully implemented Primitives, Arrays, Nested Structures, and Keys.
The next major phase is completing **Unions** (Phase 5) and **Sequences** (Phase 6).
We also need to enable the `ColorEnumTopic` which was previously skipped.

**Goal:** Achieve 70%+ coverage of the Atomic Test Suite.

---

## 🎯 Objectives

1.  **Enable ColorEnumTopic:** Verify the second enum test case.
2.  **Implement Remaining Unions:** Boolean, Enum, and Short discriminators.
3.  **Implement Remaining Sequences:** Bounded, Int64, Float, Boolean, Octet, String, Enum, Struct, Union.

---

## ✅ Tasks

### Task 1: Enable ColorEnumTopic (RT-E02)

**Files to Modify:**
- `tests/CsharpToC.Roundtrip.Tests/Program.cs`

**Instructions:**
1.  Uncomment `await TestColorEnum();` in `Main`.
2.  Uncomment `await TestColorEnumAppendable();` in `Main` (if it exists/is implemented).
3.  Verify it passes.

---

### Task 2: Implement Remaining Unions (Phase 5)

**Topics:**
- `UnionBoolDiscTopic` (RT-U02)
- `UnionEnumDiscTopic` (RT-U03)
- `UnionShortDiscTopic` (RT-U04)

**Files to Modify:**
- `tests/CsharpToC.Roundtrip.Tests/Native/atomic_tests_native.c`
- `tests/CsharpToC.Roundtrip.Tests/Native/test_registry.c`
- `tests/CsharpToC.Roundtrip.Tests/Program.cs`

**Instructions:**
1.  **Native Handlers (`atomic_tests_native.c`):**
    -   Implement `generate_UnionBoolDiscTopic` / `validate_UnionBoolDiscTopic`.
        -   Logic: `msg->data._d = (seed % 2) == 0;` (TRUE/FALSE).
    -   Implement `generate_UnionEnumDiscTopic` / `validate_UnionEnumDiscTopic`.
        -   Logic: `msg->data._d = (AtomicTests_ColorEnum)(seed % 4);` (Switch on RED, GREEN, BLUE, YELLOW).
    -   Implement `generate_UnionShortDiscTopic` / `validate_UnionShortDiscTopic`.
        -   Logic: `msg->data._d = (seed % 4) + 1;`.
2.  **Registry (`test_registry.c`):**
    -   Register the new handlers.
3.  **C# Tests (`Program.cs`):**
    -   Implement `TestUnionBoolDisc()`.
    -   Implement `TestUnionEnumDisc()`.
    -   Implement `TestUnionShortDisc()`.
    -   Add to `Main()`.

---

### Task 3: Implement Remaining Sequences (Phase 6)

**Topics:**
- `BoundedSequenceInt32Topic` (RT-S02)
- `SequenceInt64Topic` (RT-S03)
- `SequenceFloat32Topic` (RT-S04)
- `SequenceFloat64Topic` (RT-S05)
- `SequenceBooleanTopic` (RT-S06)
- `SequenceOctetTopic` (RT-S07)
- `SequenceStringTopic` (RT-S08)
- `SequenceEnumTopic` (RT-S09)
- `SequenceStructTopic` (RT-S10)
- `SequenceUnionTopic` (RT-S11)

**Files to Modify:**
- `tests/CsharpToC.Roundtrip.Tests/Native/atomic_tests_native.c`
- `tests/CsharpToC.Roundtrip.Tests/Native/test_registry.c`
- `tests/CsharpToC.Roundtrip.Tests/Program.cs`

**Instructions:**
1.  **Native Handlers (`atomic_tests_native.c`):**
    -   Implement `generate` and `validate` functions for all sequence topics.
    -   **Pattern:**
        ```c
        uint32_t len = (seed % 5) + 1; // Vary length
        // Allocate buffer
        // Fill loop
        ```
    -   **Special Cases:**
        -   `BoundedSequence`: Ensure length <= 10.
        -   `SequenceString`: Use `dds_string_dup`.
        -   `SequenceStruct`: Fill nested struct fields.
        -   `SequenceUnion`: Fill nested union fields.
2.  **Registry (`test_registry.c`):**
    -   Register all new handlers.
3.  **C# Tests (`Program.cs`):**
    -   Implement corresponding `Test...` functions.
    -   Use `List<T>` for sequences.
    -   Add to `Main()`.

---

## 🧪 Testing Requirements

**Success Criteria:**
- [ ] All new Union tests PASS.
- [ ] All new Sequence tests PASS.
- [ ] `ColorEnumTopic` PASSES.
- [ ] No regressions in existing tests.

**Command:**
```powershell
build_roundtrip_tests.bat
tests\CsharpToC.Roundtrip.Tests\bin\Debug\net8.0\CsharpToC.Roundtrip.Tests.exe
```

---

## 📊 Report Requirements

**Report to:** `.dev-workstream/reports/BATCH-30-REPORT.md`

1.  List all implemented topics.
2.  Note any specific challenges with Sequences or Unions.
3.  Confirm full pass of the suite.
