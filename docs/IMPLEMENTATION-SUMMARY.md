# C# to C Roundtrip Testing Framework - Implementation Summary

**Version:** 2.0  
**Date:** January 25, 2026  
**Status:** Design Complete - Ready for Implementation

---

## What Has Been Created

This document summarizes the complete minimalistic roundtrip testing framework designed to debug C# ↔ Native C DDS interoperability issues piece by piece.

---

## 📋 Documentation Created

### 1. Core Design Document
**File**: [docs/CSHARP-TO-C-ROUNDTRIP-DESIGN.md](CSHARP-TO-C-ROUNDTRIP-DESIGN.md)

**Contents**:
- Complete architecture overview
- 72 minimalistic test topic definitions
- Testing workflow (3-phase validation)
- CDR dump format specification
- Native implementation patterns
- C# implementation patterns
- Success criteria

**Use this for**: Understanding the overall framework design and philosophy.

---

### 2. IdlJson Integration Guide
**File**: [docs/IDLJSON-INTEGRATION-GUIDE.md](IDLJSON-INTEGRATION-GUIDE.md)

**Contents**:
- Step-by-step guide for adding topics to IdlJson.Tests
- Macro definitions for verification
- Troubleshooting common issues
- Quick reference commands

**Use this for**: Adding any new topic to IdlJson verification before roundtrip testing.

---

### 3. Atomic Tests IdlJson Integration
**File**: [docs/ATOMIC-TESTS-IDLJSON-INTEGRATION.md](ATOMIC-TESTS-IDLJSON-INTEGRATION.md)

**Contents**:
- Batch-by-batch integration strategy (10 batches)
- Complete code snippets for all 72 topics
- Verification checklist
- Shell script for automated verification

**Use this for**: Systematically adding all atomic test topics to IdlJson.Tests.

---

### 4. Test Framework README
**File**: [tests/CsharpToC.Roundtrip.Tests/README.md](../tests/CsharpToC.Roundtrip.Tests/README.md)

**Contents**:
- Project structure
- Topic catalog (organized by category)
- Testing workflow explanation
- Usage examples
- Debugging with CDR dumps

**Use this for**: Day-to-day reference while working with the test framework.

---

### 5. Quick Start Guide
**File**: [tests/CsharpToC.Roundtrip.Tests/QUICKSTART.md](../tests/CsharpToC.Roundtrip.Tests/QUICKSTART.md)

**Contents**:
- Prerequisites checklist
- 5-step getting started guide
- First test run (BooleanTopic)
- Troubleshooting guide
- Testing strategy timeline

**Use this for**: Getting the framework up and running for the first time.

---

## 📁 Project Structure Created

```
tests/CsharpToC.Roundtrip.Tests/
├── idl/
│   └── atomic_tests.idl          ✅ Created - 72 minimalistic topics
│
├── Native/                        ⚠️ To be implemented
│   ├── CMakeLists.txt
│   ├── atomic_tests_native.c
│   └── test_registry.c
│
├── App/                           ⚠️ To be implemented
│   ├── CsharpToC.Roundtrip.App.csproj
│   ├── Program.cs
│   ├── TestRunner.cs
│   ├── CdrDumper.cs
│   └── Validators/
│
├── Output/                        ⚠️ Auto-created on first run
│   └── cdr_dumps/
│
├── README.md                      ✅ Created
└── QUICKSTART.md                  ✅ Created
```

---

## 🎯 Testing Topics Breakdown

### Implemented in IDL (72 total)

| Category | Count | Topics |
|----------|-------|--------|
| **Basic Primitives** | 14 | Boolean, Char, Octet, Int16, UInt16, Int32, UInt32, Int64, UInt64, Float32, Float64, String variants |
| **Enumerations** | 2 | SimpleEnum, ColorEnum |
| **Nested Structures** | 4 | Point2D, Point3D, Box, Container |
| **Unions** | 4 | Long/Bool/Enum/Short discriminators |
| **Optional Fields** | 6 | Primitives, structs, enums, multi-optional |
| **Sequences** | 11 | Primitives, bounded, structs, unions, strings, enums |
| **Arrays** | 6 | 1D, 2D, 3D, struct arrays |
| **Extensibility** | 6 | Appendable, Final, Mutable variants |
| **Composite Keys** | 4 | 2-key, 3-key, 4-key, mixed types |
| **Nested Keys** | 3 | Location, Coordinates, TripleKey |
| **Advanced Combos** | 7 | Nested sequences, optional sequences, etc. |
| **Edge Cases** | 5 | Empty, large, long strings, unbounded, all-primitives |

---

## 🔄 Testing Workflow

```
┌─────────────────────────────────────────────────────────────┐
│ PHASE 0: IdlJson Verification (MANDATORY FIRST STEP)       │
├─────────────────────────────────────────────────────────────┤
│ 1. Add topic to tests/IdlJson.Tests/verification.idl       │
│ 2. Run: idlc verification.idl                              │
│ 3. Run: idlc -l json verification.idl                      │
│ 4. Update verifier.c with VERIFY_ATOMIC_TOPIC()            │
│ 5. Build and run verifier                                  │
│ 6. Ensure: [PASS] All opcodes and sizes match              │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 1: Native → C# (Receive & Capture CDR)               │
├─────────────────────────────────────────────────────────────┤
│ 1. Native generates data from seed                         │
│ 2. Native publishes via dds_write()                        │
│ 3. C# receives and captures raw CDR bytes                  │
│ 4. C# saves: TopicName_seed_N_native.hex                   │
│ 5. C# deserializes to C# object                            │
│ 6. C# validates against expected seed data                 │
│                                                             │
│ Result: ✓ PASS = Deserialization works                     │
│         ✗ FAIL = C# deserializer bug                       │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 2: C# Serialization Verification                     │
├─────────────────────────────────────────────────────────────┤
│ 1. C# generates same data from same seed                   │
│ 2. C# serializes using C# serializer                       │
│ 3. C# saves: TopicName_seed_N_csharp.hex                   │
│ 4. Compare byte-for-byte with native hex dump              │
│                                                             │
│ Result: ✓ PASS = Serialization matches native              │
│         ✗ FAIL = C# serializer bug (shows offset)          │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 3: C# → Native (Send & Validate)                     │
├─────────────────────────────────────────────────────────────┤
│ 1. C# generates and publishes data                         │
│ 2. Native receives via dds_take()                          │
│ 3. Native validates against expected seed data             │
│                                                             │
│ Result: ✓ PASS = End-to-end roundtrip works                │
│         ✗ FAIL = Native interpretation issue               │
└─────────────────────────────────────────────────────────────┘
```

---

## 📊 Topic Testing Priority

### Week 1: Foundation (Must Pass First)
1. ✅ IdlJson verification for all primitives
2. ✅ BooleanTopic
3. ✅ Int32Topic
4. ✅ Float64Topic
5. ✅ StringBounded32Topic

**Goal**: Prove basic primitives work end-to-end.

### Week 2: Building Blocks
6. ✅ All remaining primitive topics (Char, Octet, Int16, UInt16, etc.)
7. ✅ EnumTopic
8. ✅ NestedStructTopic (Point2D)

**Goal**: Complete all basic types.

### Week 3: Critical Focus - Sequences
9. ⚠️ SequenceInt32Topic (BLOCKER - FOCUS HERE)
10. ⚠️ SequenceFloat64Topic
11. ⚠️ SequenceStringTopic
12. ⚠️ SequenceStructTopic

**Goal**: Understand and fix sequence serialization issues.

### Week 4-6: Advanced Features
- Arrays, Optionals, Unions
- Extensibility variants
- Composite and nested keys
- Advanced combinations

---

## 🐛 Known Issues to Address

### Current Blocker: Sequences

From `docs/Fixing-csharp-to-native-issues.md`:

1. **C# vs C Layout Mismatch**: C uses inline char arrays for `string<N>`, C# uses pointer layout
2. **Key Hashing**: Native expects data at specific offsets, C# has different layout
3. **Opcode Interpretation**: C# generates ops correctly (IdlJson verified), but native crashes on C# layout

**Strategy**: Isolate the problem by testing:
- `SequenceInt32Topic` (no strings, no structs - simplest possible)
- Capture both CDR dumps
- Compare byte-for-byte
- Identify exact divergence point

---

## 🚀 Next Steps for Implementation

### Step 1: IdlJson Integration (HIGHEST PRIORITY)

```bash
# Add all 72 topics to IdlJson.Tests in batches
# Start with Batch 1 (primitives)

cd tests/IdlJson.Tests

# Follow: docs/ATOMIC-TESTS-IDLJSON-INTEGRATION.md
# Systematically add each batch, verify, proceed
```

**Exit Criteria**: `./verifier verification.json` shows 0 errors for all batches.

---

### Step 2: Native Implementation

Create:
- `tests/CsharpToC.Roundtrip.Tests/Native/CMakeLists.txt`
- `tests/CsharpToC.Roundtrip.Tests/Native/atomic_tests_native.c`
- `tests/CsharpToC.Roundtrip.Tests/Native/test_registry.c`

Implement:
1. Data generators for each topic (seed → data)
2. Validators for each topic (data vs seed)
3. DDS publishers/subscribers
4. Exported C API (see design doc section 7.1-7.3)

**Start with**: BooleanTopic, Int32Topic, Float64Topic

---

### Step 3: C# Implementation

Create:
- `tests/CsharpToC.Roundtrip.Tests/App/CsharpToC.Roundtrip.App.csproj`
- `tests/CsharpToC.Roundtrip.Tests/App/Program.cs`
- `tests/CsharpToC.Roundtrip.Tests/App/TestRunner.cs`
- `tests/CsharpToC.Roundtrip.Tests/App/CdrDumper.cs`

Implement:
1. Test orchestration (see design doc section 8.1)
2. Data generator (C# mirror of native - see design doc section 8.2)
3. CDR dumper (see design doc section 5.3)
4. Per-topic validators

**Start with**: BooleanTopic, Int32Topic, Float64Topic

---

### Step 4: First Test Run

```bash
cd tests/CsharpToC.Roundtrip.Tests/App
dotnet run -- BooleanTopic
```

**Expected**: All 3 phases pass, hex dumps generated.

---

### Step 5: Scale Up Incrementally

Once BooleanTopic passes:
1. Test all primitives (14 topics)
2. Test enums (2 topics)
3. Test nested structs (4 topics)
4. **CRITICAL**: Test sequences (11 topics) - expect failures here
5. Debug sequence issues using CDR dumps
6. Fix C# serializer/deserializer
7. Continue with remaining categories

---

## 📖 Documentation Reference

| Document | Use Case |
|----------|----------|
| [CSHARP-TO-C-ROUNDTRIP-DESIGN.md](CSHARP-TO-C-ROUNDTRIP-DESIGN.md) | Understanding architecture, seeing examples |
| [IDLJSON-INTEGRATION-GUIDE.md](IDLJSON-INTEGRATION-GUIDE.md) | Adding any topic to IdlJson verification |
| [ATOMIC-TESTS-IDLJSON-INTEGRATION.md](ATOMIC-TESTS-IDLJSON-INTEGRATION.md) | Systematically verifying all 72 topics |
| [tests/CsharpToC.Roundtrip.Tests/README.md](../tests/CsharpToC.Roundtrip.Tests/README.md) | Day-to-day reference, topic catalog |
| [tests/CsharpToC.Roundtrip.Tests/QUICKSTART.md](../tests/CsharpToC.Roundtrip.Tests/QUICKSTART.md) | Getting started for first time |

---

## ✅ Success Metrics

### Short-term (Week 1-2)
- [ ] All 72 topics pass IdlJson verification
- [ ] BooleanTopic passes all 3 phases
- [ ] All 14 primitive topics pass all 3 phases
- [ ] CDR dumps successfully captured and compared

### Medium-term (Week 3-4)
- [ ] SequenceInt32Topic passes (breakthrough moment)
- [ ] All sequence topics pass
- [ ] Enums, structs, arrays pass

### Long-term (Week 5-6)
- [ ] All 72 topics pass
- [ ] Framework documented
- [ ] CI/CD integration
- [ ] Confidence in C# serialization

---

## 🎯 The Goal

**Replace speculation with verification.**

Instead of guessing why sequences fail, we now:
1. Test the simplest possible sequence topic
2. Capture exact byte streams from both sides
3. Compare and identify the divergence
4. Fix the root cause
5. Verify the fix with all sequence variants
6. Move forward with confidence

**This framework makes debugging systematic, not random.**

---

## 🆘 When You Get Stuck

1. **Check IdlJson first**: `./verifier verification.json` must show 0 errors
2. **Look at hex dumps**: `cat Output/cdr_dumps/*.hex`
3. **Compare working vs failing**: What's different between Int32Topic (works) and SequenceInt32Topic (fails)?
4. **Test simpler variant**: If SequenceStructTopic fails, test SequenceInt32Topic first
5. **Review design docs**: The answer is probably documented
6. **Document the issue**: Add findings to `docs/Fixing-csharp-to-native-issues.md`

---

## 📝 Final Notes

This framework is designed to:
- ✅ Isolate problems (one feature per topic)
- ✅ Provide transparency (CDR hex dumps)
- ✅ Enable systematic debugging (clear phases)
- ✅ Build incrementally (primitives → sequences → combinations)
- ✅ Prevent regression (test suite for all features)

**Start with IdlJson verification. Everything else builds on that foundation.**

---

**Status**: Design complete. Implementation can now proceed systematically.

**Next Action**: Begin Batch 1 IdlJson integration (14 primitive topics).
