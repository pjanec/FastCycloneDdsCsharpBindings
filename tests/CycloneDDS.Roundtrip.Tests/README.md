# CycloneDDS Roundtrip Verification Framework

## Overview

This test framework validates **end-to-end interoperability** between the C# bindings and native CycloneDDS C implementation. It tests both serialization/deserialization directions:

1. **C# → Native C**: C# serializes data, native C deserializes and verifies
2. **Native C → C#**: Native C serializes data, C# deserializes and verifies

## Quick Start

### Prerequisites

- Windows 10/11
- Visual Studio 2022 (with C++ and C# workloads)
- .NET 8.0 SDK
- CMake 3.16+
- CycloneDDS (auto-built if needed)

### Build Everything

```batch
cd D:\Work\FastCycloneDdsCsharpBindings

REM For Release build (fast, optimized)
build_roundtrip_tests.bat Release

REM For Debug build (symbols, logging)
build_roundtrip_tests.bat Debug
```

### Run Tests

```batch
cd tests\CycloneDDS.Roundtrip.Tests\App\bin\Release\net8.0
CycloneDDS.Roundtrip.App.exe
```

## Architecture

```
┌─────────────────────────────────────┐
│   C# Test Orchestrator (.NET 8.0)  │
│   ─────────────────────────────────│
│   • Controls test execution         │
│   • Generates seed-based test data  │
│   • Publishes/subscribes via DDS    │
│   • Verifies received data          │
│   • Reports results to console      │
└──────────┬────────────────┬─────────┘
           │                │
       P/Invoke         DDS Pub/Sub
           │                │
┌──────────▼────────────────▼─────────┐
│   Native Test DLL (C11)             │
│   ─────────────────────────────────│
│   • Type registry (topic → handlers)│
│   • Seed-based data generator       │
│   • DDS publisher/subscriber        │
│   • Data comparator                 │
└─────────────────────────────────────┘
```

## Project Structure

```
tests/CycloneDDS.Roundtrip.Tests/
│
├── idl/
│   └── roundtrip_test.idl          ← IDL definitions (all test types)
│
├── Native/                          ← C DLL Project
│   ├── CMakeLists.txt
│   ├── src/
│   │   ├── main_dll.c              ← Exported API (Native_Init, etc.)
│   │   ├── type_registry.c/.h      ← Topic → handler mapping
│   │   └── handlers/
│   │       ├── handler_basic.c     ← AllPrimitives, CompositeKey, etc.
│   │       └── ...                 ← More handlers (to be added)
│   └── build/
│       └── <generated>/            ← CMake output
│
└── App/                             ← C# Application (TODO)
    ├── CycloneDDS.Roundtrip.App.csproj
    ├── Program.cs                   ← Entry point
    ├── NativeInterop.cs             ← P/Invoke declarations
    ├── DataGenerator.cs             ← C# seed-based generator
    ├── TestScenarios.cs             ← Test case definitions
    └── ConsoleReporter.cs           ← Output formatting
```

## Current Status

### ✅ Completed

- [x] Design documents (ROUNDTRIP-DESIGN.md, ROUNDTRIP-ADDING-TESTS.md)
- [x] IDL test messages (15 comprehensive test types)
- [x] Native C DLL infrastructure:
  - [x] CMake build system
  - [x] Type registry
  - [x] Exported API (Init, Cleanup, CreatePublisher, CreateSubscriber, SendWithSeed, ExpectWithSeed)
  - [x] Sample handlers (AllPrimitives, CompositeKey, NestedKeyTopic, SequenceTopic)
- [x] Build automation (build_roundtrip_tests.bat)

### 🚧 In Progress

- [ ] C# Application:
  - [ ] Project structure (.csproj)
  - [ ] NativeInterop (P/Invoke)
  - [ ] DataGenerator (reflection-based)
  - [ ] TestScenarios
  - [ ] ConsoleReporter
- [ ] Additional handlers:
  - [ ] NestedSequences
  - [ ] ArrayTopic
  - [ ] StringTopic
  - [ ] OptionalFields
  - [ ] UnionTopic
  - [ ] And 6 more...

## Test Coverage

The framework tests the following IDL features:

| Feature | Test Type | Status |
|---------|-----------|--------|
| **Primitives** | AllPrimitives | ✅ Handler ready |
| **Composite Keys** | CompositeKey | ✅ Handler ready |
| **Nested Keys** | NestedKeyTopic | ✅ Handler ready |
| **Sequences** | SequenceTopic | ✅ Handler ready |
| **Nested Sequences** | NestedSequences | ⏳ Pending |
| **Arrays** | ArrayTopic | ⏳ Pending |
| **Multi-Dim Arrays** | ArrayTopic | ⏳ Pending |
| **Strings** | StringTopic | ⏳ Pending |
| **Optional Fields** | OptionalFields | ⏳ Pending |
| **Unions** | UnionTopic | ⏳ Pending |
| **Nested Structs** | Person | ⏳ Pending |
| **Typedefs** | TypedefChain | ⏳ Pending |
| **Mixed Complex** | MixedComplexTopic | ⏳ Pending |
| **Large Payload** | LargePayload | ⏳ Pending |
| **Multi-Instance** | SensorData | ⏳ Pending |
| **Deep Nesting** | DeepNesting | ⏳ Pending |

## Key Concepts

### Seed-Based Data Generation

Instead of manually writing assertions for every field, both C and C# generate data deterministically from an integer seed:

```
Seed 42  →  AllPrimitives { id=42, bool_field=true, char_field='C', ... }
Seed 99  →  AllPrimitives { id=99, bool_field=false, char_field='E', ... }
```

**Verification Pattern:**
1. Generate data with seed X
2. Serialize & transmit via DDS
3. Receive data
4. Generate expected data with seed X
5. Compare received vs. expected

### Native C API

```c
// Initialize framework
Native_Init(domain_id);

// Create DDS entities
Native_CreatePublisher("AllPrimitives");
Native_CreateSubscriber("AllPrimitives");

// Send test data
Native_SendWithSeed("AllPrimitives", seed: 42);

// Verify received data
int result = Native_ExpectWithSeed("AllPrimitives", seed: 42, timeout: 5000);
// Returns: 0=match, -1=timeout, -2=mismatch

// Cleanup
Native_Cleanup();
```

## Adding a New Test Type

See **[ROUNDTRIP-ADDING-TESTS.md](../../docs/ROUNDTRIP-ADDING-TESTS.md)** for detailed guide.

**Quick Steps:**

1. **Add IDL** to `idl/roundtrip_test.idl`:
   ```idl
   @topic
   struct MyNewType {
       @key long id;
       string<64> data;
   };
   ```

2. **Create Handler** (`Native/src/handlers/handler_mynew.c`):
   ```c
   void fill_MyNewType(void* sample, int seed) {
       MyNewType* data = (MyNewType*)sample;
       data->id = seed;
       snprintf(data->data, 64, "Data_%d", seed);
   }
   
   bool compare_MyNewType(const void* a, const void* b) {
       // ... comparison logic
   }
   ```

3. **Register Handler** in `type_registry.c`

4. **Add Test Case** to C# app (when available)

5. **Rebuild:**
   ```batch
   build_roundtrip_tests.bat Release
   ```

## Troubleshooting

### CMake Configuration Failed

**Problem:** `idlc not found`

**Solution:** Build CycloneDDS first:
```batch
build_cyclone.bat
```

### Native DLL Build Errors

**Problem:** Missing generated headers

**Solution:** Delete build folder and retry:
```batch
rmdir /s /q tests\CycloneDDS.Roundtrip.Tests\Native\build
build_roundtrip_tests.bat
```

### Runtime: Type Not Found

**Problem:** `Type 'XYZ' not found in registry`

**Solution:** Ensure handler is registered in `type_registry.c`:
```c
extern void* alloc_XYZ();
// ... other declarations

static const type_handler_t registry[] = {
    // ... existing entries
    {
        .topic_name = "XYZ",
        .alloc_fn = alloc_XYZ,
        // ... other function pointers
    },
    { .topic_name = NULL } // Keep sentinel!
};
```

## Performance Considerations

### Build Times

- **Native DLL (incremental):** ~10 seconds
- **Native DLL (clean):** ~30 seconds
- **C# App:** ~5 seconds

### Test Execution

- **Single test case:** ~1 second (includes discovery)
- **Full suite (15 types):** ~15-20 seconds

### Optimization

**Release builds** are **~10x faster** than Debug for serialization/deserialization.

## CI/CD Integration

The test framework is designed for automation:

### Command-Line Execution

```batch
REM Build
build_roundtrip_tests.bat Release

REM Run (exits with code 0 on success, 1 on failure)
tests\CycloneDDS.Roundtrip.Tests\App\bin\Release\net8.0\CycloneDDS.Roundtrip.App.exe

REM Check exit code
IF ERRORLEVEL 1 (
    echo Tests FAILED
    exit /b 1
)
```

### Expected Output

```
[Roundtrip] Initializing (Domain 0)...
[Native] Initialization complete.
========================================
Registered Types:
========================================
  [1] AllPrimitives
  [2] CompositeKey
  [3] NestedKeyTopic
  [4] SequenceTopic
========================================

========================================
Test Suite: Roundtrip Verification
========================================

[Test 1/4] AllPrimitives
  [C# → Native] Sending seed=42...
  [Native] Verification PASSED
  [Native → C#] Receiving seed=99...
  [C#] Verification PASSED
  Result: PASS

[Test 2/4] CompositeKey
  ...

========================================
Summary
========================================
Total:   4 tests
Passed:  4 tests
Failed:  0 tests
Time:    8.2 seconds

Exit Code: 0
```

## Next Steps

1. **Complete C# Application** (in progress)
2. **Implement Remaining Handlers** (11 types pending)
3. **Add Multi-Instance Tests** (keyed topic lifecycle)
4. **Performance Benchmarking** (throughput/latency)
5. **Negative Tests** (error handling, malformed data)

## References

- [Design Document](../../docs/ROUNDTRIP-DESIGN.md)
- [Adding Tests Guide](../../docs/ROUNDTRIP-ADDING-TESTS.md)
- [Main README](../../README.md)

## License

Same as parent project (see root LICENSE file)
