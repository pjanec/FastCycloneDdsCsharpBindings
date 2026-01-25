# C# to C Roundtrip Tests

**Minimalistic, Incremental DDS Interoperability Testing**

---

## 📚 Quick Navigation

**NEW: Comprehensive Analysis & Implementation Guides Available**

| Document | Purpose | When to Read |
|----------|---------|--------------|
| **[ROUNDTRIP-ANALYSIS-SUMMARY.md](ROUNDTRIP-ANALYSIS-SUMMARY.md)** | Executive overview | Start here for high-level understanding |
| **[ROUNDTRIP-STATUS-ANALYSIS.md](ROUNDTRIP-STATUS-ANALYSIS.md)** | Detailed verification | Understanding what's working and gaps |
| **[ROUNDTRIP-IMPLEMENTATION-GUIDE.md](ROUNDTRIP-IMPLEMENTATION-GUIDE.md)** | Step-by-step guide | Implementing new topics |
| **[ROUNDTRIP-TASK-TRACKER.md](ROUNDTRIP-TASK-TRACKER.md)** | Task list & progress | Planning work and tracking completion |
| **[QUICKSTART.md](QUICKSTART.md)** | Getting started | First-time setup |
| **This README** | Project overview | Understanding structure & topics |

---

## Overview

This test framework validates C# ↔ Native C DDS communication by testing **one feature at a time**. Unlike complex integration tests, each topic here is designed to be as simple as possible, making debugging straightforward.

### Current Status

- **Implemented:** 10/77 topics (13% coverage)
- **Test Quality:** HIGH (triple validation with CDR byte comparison)
- **Status:** ✅ Basic tests confirmed working (see [ROUNDTRIP-STATUS-ANALYSIS.md](ROUNDTRIP-STATUS-ANALYSIS.md))
- **Next Steps:** See [ROUNDTRIP-TASK-TRACKER.md](ROUNDTRIP-TASK-TRACKER.md)

### Key Features

- ✅ **77 Minimalistic Topics**: Each tests exactly ONE feature (10 implemented, 67 remaining)
- ✅ **Wire Format Inspection**: Captures CDR byte streams as hex dumps
- ✅ **Triple Validation**: Receive, Serialize, Send
- ✅ **Genuine C# ↔ C Communication**: Independent DDS implementations (verified)
- ✅ **Deterministic**: Seed-based data generation for reproducibility

---

## Project Structure

```
CsharpToC.Roundtrip.Tests/
├── idl/
│   └── atomic_tests.idl          # 80+ minimalistic test topics
│
├── Native/                        # C implementation (DLL)
│   ├── CMakeLists.txt
│   ├── atomic_tests_native.c     # Native handlers
│   └── test_registry.c           # Topic registry
│
├── App/                           # C# test orchestrator
│   ├── CsharpToC.Roundtrip.App.csproj
│   ├── Program.cs
│   ├── TestRunner.cs
│   ├── CdrDumper.cs
│   └── Validators/               # Per-topic validators
│
├── Output/
│   └── cdr_dumps/                # CDR byte stream captures
│       ├── BooleanTopic_seed_42_native.hex
│       ├── BooleanTopic_seed_42_csharp.hex
│       └── ...
│
└── README.md (this file)
```

---

## Topics Organized by Complexity

### 1. Basic Primitives (14 topics)
- `BooleanTopic`, `CharTopic`, `OctetTopic`
- `Int16Topic`, `UInt16Topic`, `Int32Topic`, `UInt32Topic`
- `Int64Topic`, `UInt64Topic`
- `Float32Topic`, `Float64Topic`
- `StringUnboundedTopic`, `StringBounded32Topic`, `StringBounded256Topic`

### 2. Enumerations (2 topics)
- `EnumTopic`, `ColorEnumTopic`

### 3. Nested Structures (4 topics)
- `NestedStructTopic` (single nesting)
- `Nested3DTopic`, `DoublyNestedTopic`, `ComplexNestedTopic`

### 4. Unions (4 topics)
- `UnionLongDiscTopic`, `UnionBoolDiscTopic`
- `UnionEnumDiscTopic`, `UnionShortDiscTopic`

### 5. Optional Fields (6 topics)
- `OptionalInt32Topic`, `OptionalFloat64Topic`, `OptionalStringTopic`
- `OptionalStructTopic`, `OptionalEnumTopic`, `MultiOptionalTopic`

### 6. Sequences (11 topics)
- `SequenceInt32Topic`, `BoundedSequenceInt32Topic`
- `SequenceInt64Topic`, `SequenceFloat32Topic`, `SequenceFloat64Topic`
- `SequenceBooleanTopic`, `SequenceOctetTopic`
- `SequenceStringTopic`, `SequenceEnumTopic`
- `SequenceStructTopic`, `SequenceUnionTopic`

### 7. Arrays (6 topics)
- `ArrayInt32Topic`, `ArrayFloat64Topic`, `ArrayStringTopic`
- `Array2DInt32Topic`, `Array3DInt32Topic`, `ArrayStructTopic`

### 8. Extensibility (6 topics)
- `AppendableInt32Topic`, `AppendableStructTopic`
- `FinalInt32Topic`, `FinalStructTopic`
- `MutableInt32Topic`, `MutableStructTopic`

### 9. Composite Keys (4 topics)
- `TwoKeyInt32Topic`, `TwoKeyStringTopic`
- `ThreeKeyTopic`, `FourKeyTopic`

### 10. Nested Keys (3 topics)
- `NestedKeyTopic`, `NestedKeyGeoTopic`, `NestedTripleKeyTopic`

### 11. Advanced Combinations (7 topics)
- `SequenceOfOptionalTopic`, `OptionalSequenceTopic`
- `NestedSequenceTopic`, `SequenceOfStructWithSequenceTopic`
- `AppendableWithSequenceTopic`, `ArrayOfSequenceTopic`
- `ComplexKeyTopic`

### 12. Edge Cases (5 topics)
- `EmptySequenceTopic`, `LargeSequenceTopic`
- `LongStringTopic`, `UnboundedStringTopic`
- `AllPrimitivesAtomicTopic`

**Total: 72 Topics** (organized for incremental testing)

---

## Testing Workflow

### Phase 1: Native → C# (Receive & Capture)

1. Native generates data from seed: `generate_BooleanTopic(seed=42)`
2. Native publishes: `dds_write(pub, data)`
3. C# receives: `reader.TakeAsync()`
4. C# captures raw CDR bytes: `sample.RawCdrData`
5. C# saves hex dump: `BooleanTopic_seed_42_native.hex`
6. C# deserializes and validates against expected seed-42 data

### Phase 2: C# Serialization Verification

1. C# generates data: `DataGenerator.Generate<BooleanTopic>(42)`
2. C# serializes: `CdrSerializer.Serialize(data)`
3. C# saves hex dump: `BooleanTopic_seed_42_csharp.hex`
4. Compare byte-for-byte with native hex dump
5. Report mismatch position if any

### Phase 3: C# → Native (Send)

1. C# generates and publishes data
2. Native receives: `dds_take(sub, &sample, &info, 1, 1)`
3. Native validates against expected seed-42 data
4. Returns: `0=success`, `-1=timeout`, `-2=data mismatch`

---

## Prerequisites

Before running these tests:

1. **IdlJson Verification**: All topics must pass `tests/IdlJson.Tests` first
   ```bash
   cd tests/IdlJson.Tests
   idlc atomic_tests.idl
   idlc -l json atomic_tests.idl
   # Update verifier.c with new topics
   cd build && cmake --build . && ./verifier ../atomic_tests.json
   ```

2. **Build Native Library**:
   ```bash
   cd tests/CsharpToC.Roundtrip.Tests/Native/build
   cmake ..
   cmake --build .
   ```

3. **Generate C# Types**:
   ```bash
   cd tests/CsharpToC.Roundtrip.Tests/App
   dotnet build  # Triggers code generation from IDL
   ```

---

## Running Tests

### Run All Tests

```bash
cd tests/CsharpToC.Roundtrip.Tests/App
dotnet run
```

### Run Specific Topic

```bash
dotnet run -- BooleanTopic
```

### Run Multiple Topics

```bash
dotnet run -- BooleanTopic Int32Topic Float64Topic
```

### Run by Category

```bash
dotnet run -- --category primitives
dotnet run -- --category sequences
dotnet run -- --category keys
```

---

## Understanding Test Output

### Successful Test

```
[TEST] BooleanTopic (seed=42)
  [Phase 1] Native → C# Receive    ✓ PASS (12ms)
  [Phase 2] C# Serialization       ✓ PASS (bytes match)
  [Phase 3] C# → Native Send       ✓ PASS (validated)
  CDR Dump: Output/cdr_dumps/BooleanTopic_seed_42_native.hex
```

### Failed Test (Deserialization)

```
[TEST] SequenceInt32Topic (seed=100)
  [Phase 1] Native → C# Receive    ✗ FAIL
    Expected: [100, 131, 162, 193, 224]
    Received: [100, 131, 162, 193, 0]
  [Phase 2] SKIPPED (Phase 1 failed)
  [Phase 3] SKIPPED (Phase 1 failed)
```

### Failed Test (Serialization Mismatch)

```
[TEST] StringBounded32Topic (seed=42)
  [Phase 1] Native → C# Receive    ✓ PASS
  [Phase 2] C# Serialization       ✗ FAIL
    Byte mismatch at offset 16
    Native: 53 74 72 5F 34 32 00 00
    C#:     53 74 72 5F 34 32 00 01
  [Phase 3] SKIPPED (Phase 2 failed)
```

---

## Debugging with CDR Dumps

When a test fails, inspect the hex dumps:

```bash
# View native CDR bytes
cat Output/cdr_dumps/BooleanTopic_seed_42_native.hex

# View C# CDR bytes
cat Output/cdr_dumps/BooleanTopic_seed_42_csharp.hex

# Compare side-by-side
diff Output/cdr_dumps/BooleanTopic_seed_42_native.hex \
     Output/cdr_dumps/BooleanTopic_seed_42_csharp.hex
```

Each hex dump includes:
- Topic name and seed
- Timestamp
- CDR encoding (XCDR1/XCDR2)
- Total size
- Annotated byte sequences (4-byte chunks with offsets)

---

## Adding New Topics

See [IDLJSON-INTEGRATION-GUIDE.md](../../docs/IDLJSON-INTEGRATION-GUIDE.md) for detailed steps.

**Quick version:**

1. Add topic to `idl/atomic_tests.idl`
2. Add to `tests/IdlJson.Tests/verification.idl`
3. Verify: `idlc -l json verification.idl && ./verifier verification.json`
4. Implement native handler in `Native/atomic_tests_native.c`
5. Register in `Native/test_registry.c`
6. Run: `dotnet run -- YourNewTopic`

---

## Design Documents

- [CSHARP-TO-C-ROUNDTRIP-DESIGN.md](../../docs/CSHARP-TO-C-ROUNDTRIP-DESIGN.md) - Full framework design
- [IDLJSON-INTEGRATION-GUIDE.md](../../docs/IDLJSON-INTEGRATION-GUIDE.md) - IdlJson verification guide
- [ROUNDTRIP-DESIGN.md](../../docs/ROUNDTRIP-DESIGN.md) - Original roundtrip framework (complex topics)

---

## Success Criteria

A topic is considered **VALIDATED** when:

1. ✅ IdlJson verification passes (sizes, ops, keys match C compiler)
2. ✅ Phase 1 passes (C# deserializes native data correctly)
3. ✅ Phase 2 passes (C# serialization matches native byte-for-byte)
4. ✅ Phase 3 passes (Native validates C#-sent data)

The framework is considered **PRODUCTION READY** when:

- All 14 primitive topics pass
- All enum topics pass
- All nested struct topics pass
- All union topics pass
- All optional topics pass
- All sequence topics pass (this is currently the blocker)
- All array topics pass
- All extensibility topics pass
- All key topics pass

---

## Known Issues

(To be filled as testing progresses)

---

## Future Enhancements

1. **Performance Benchmarks**: Measure serialization/deserialization speed
2. **Fuzzing**: Generate random seeds, run thousands of iterations
3. **Schema Evolution**: Test forward/backward compatibility
4. **Large Data Stress Test**: Multi-megabyte sequences
5. **Concurrency**: Multiple writers/readers simultaneously

---

## Support

For issues or questions:
1. Check existing hex dumps in `Output/cdr_dumps/`
2. Review [CSHARP-TO-C-ROUNDTRIP-DESIGN.md](../../docs/CSHARP-TO-C-ROUNDTRIP-DESIGN.md)
3. Verify with IdlJson.Tests first
4. Compare working topics to failing ones

**Remember**: Start simple, validate incrementally, build confidence piece by piece.
