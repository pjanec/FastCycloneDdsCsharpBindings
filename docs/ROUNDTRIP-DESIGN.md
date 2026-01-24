# CycloneDDS C# Bindings - Roundtrip Verification Framework

## Design Document v1.0

---

## 1. Executive Summary

This document describes the **Roundtrip Verification Framework** for the FastCycloneDDS C# Bindings library. The framework validates end-to-end interoperability between:

1. **C# → Native C**: C# serializer writes data, native C deserializer reads and validates.
2. **Native C → C#**: Native C writes data, C# deserializer reads and validates.

### Key Design Principles

- **Deterministic Seed-Based Data Generation**: Eliminates manual field-by-field verification code.
- **Low Maintenance**: Adding a new test type requires minimal code changes.
- **CI/CD Ready**: Clean console output, exit codes, and command-line operation.
- **Debug + Release**: Supports both build configurations for regression and deep debugging.
- **Type-Safe**: Leverages C# source generation and C macros for compile-time safety.

---

## 2. Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│                    Test Orchestrator (C#)                     │
│          CycloneDDS.Roundtrip.App.exe                        │
│                                                              │
│  • Loads Native DLL                                          │
│  • Controls test execution flow                              │
│  • Implements C# data generator                              │
│  • Verifies received data                                    │
│  • Reports results to console                                │
└────────┬─────────────────────────────────────────────┬───────┘
         │                                             │
         │ P/Invoke                          DDS Loop  │
         │                                             │
┌────────▼─────────────────────────┐     ┌─────────────▼───────┐
│  Native Test Library (C DLL)     │     │   DDS Domain 0      │
│  CycloneDDS.Roundtrip.Native.dll │◄───►│   (Loopback)        │
│                                  │     │                     │
│  • Type Registry                 │     │  Pub/Sub Messages   │
│  • C Data Generator              │     │                     │
│  • DDS Publishers/Subscribers    │     └─────────────────────┘
│  • Verification Engine           │
└──────────────────────────────────┘
```

### Component Breakdown

#### 2.1 Native Test Library (`CycloneDDS.Roundtrip.Native.dll`)

**Technology Stack:**
- Language: C (C11)
- Build System: CMake
- Dependencies: CycloneDDS C API (`ddsc.dll`)

**Core Responsibilities:**
1. **Type Registry**: Maps topic names to handler function tables.
2. **Data Generation**: Creates C structures deterministically from a seed value.
3. **Publishing**: Writes data to DDS topics via `dds_write()`.
4. **Subscribing**: Reads data via `dds_take()` and validates against expected seed.
5. **Lifecycle Management**: Creates/destroys DDS entities on demand.

**Exported C API:**
```c
// Initialization
EXPORT void Native_Init(uint32_t domain_id);
EXPORT void Native_Cleanup();

// Entity Management
EXPORT int Native_CreatePublisher(const char* topic_name);
EXPORT int Native_CreateSubscriber(const char* topic_name);

// Test Operations
EXPORT void Native_SendWithSeed(const char* topic_name, int seed);
EXPORT int Native_ExpectWithSeed(const char* topic_name, int seed, int timeout_ms);

// Diagnostics
EXPORT const char* Native_GetLastError();
```

#### 2.2 C# Test Orchestrator (`CycloneDDS.Roundtrip.App.exe`)

**Technology Stack:**
- Framework: .NET 8.0
- Dependencies: 
  - `CycloneDDS.Runtime`
  - `CycloneDDS.Schema`
  - Generated C# types from IDL

**Core Responsibilities:**
1. **Test Flow Control**: Executes test scenarios in sequence.
2. **C# Data Generation**: Mirrors the native generator using reflection/source generation.
3. **DDS Operations**: Uses `DdsWriter<T>` / `DdsReader<T>` APIs.
4. **Verification**: Compares received data against expected seed-generated data.
5. **Reporting**: Structured console output with pass/fail status.

---

## 3. The Seed-Based Verification Strategy

### 3.1 Core Concept

Instead of manually writing `assert(received.field == expected.field)` for hundreds of fields across dozens of types, we use a **deterministic pseudo-random generator**.

**Workflow:**

```
Seed (int)  →  Generator Function  →  Populated Data Structure
  42        →   fill_MyType(42)    →  { id: 42, name: "Str_42", ... }
```

Both C and C# implement **identical** generator logic, so:

```c
// C side
MyType* data = create_MyType(seed=100);
// → data->id = 100, data->value = 314.0, etc.

// C# side
var data = new MyType();
DataGenerator.Fill(data, seed: 100);
// → data.Id = 100, data.Value = 314.0, etc.
```

### 3.2 Verification Pattern

**C# → Native Case:**
```
1. C# generates MyType(seed=100)
2. C# writes to topic
3. C# calls: Native_ExpectWithSeed("MyTopic", 100, timeout=5000)
4. Native reads message
5. Native generates local reference: MyType_ref(seed=100)
6. Native compares received vs reference
7. Returns: 0=match, -1=timeout, -2=mismatch
```

**Native → C# Case:**
```
1. C# calls: Native_SendWithSeed("MyTopic", 200)
2. Native generates MyType(seed=200) and writes
3. C# reads message via DdsReader<MyType>
4. C# generates local reference: MyType(seed=200)
5. C# compares received vs reference
6. Reports: pass/fail
```

### 3.3 Generator Rules (Pseudo-Code)

```
Function Fill(object, seed, offset=0):
    
    If type is int/long:
        return (seed + offset) * 31  // Prime multiplier
    
    If type is double/float:
        return (seed + offset) * 3.14159
    
    If type is string:
        return $"Str_{seed + offset}"
    
    If type is boolean:
        return ((seed + offset) % 2) == 0
    
    If type is enum:
        return EnumValues[(seed + offset) % NumCases]
    
    If type is struct:
        For each field (index i):
            Fill(field, seed, offset + i + 1)
    
    If type is sequence<T>:
        length = ((seed + offset) % 5) + 1  // 1-5 elements
        For i in 0..length-1:
            Fill(element[i], seed, offset + i + 10)
    
    If type is array<T, N>:
        For i in 0..N-1:
            Fill(element[i], seed, offset + i + 100)
    
    If type is union:
        discriminator = (seed + offset) % NumCases
        Fill(selected_branch, seed, offset + 1000)
```

**Key Properties:**
- **Deterministic**: Same seed → same output.
- **Diverse**: Different seeds → different values.
- **Collision-Resistant**: Offsets prevent accidental matches.

---

## 4. Project Structure

```
/tests
  /CycloneDDS.Roundtrip.Tests
    /idl
       roundtrip_test.idl          ← Complex test messages (all types)
    
    /Native
       CMakeLists.txt
       /src
          main_dll.c               ← Exported C API
          type_registry.c          ← Maps topic → handlers
          type_registry.h
          data_generator.c         ← C fill logic
          data_generator.h
          data_comparator.c        ← C compare logic
          data_comparator.h
          /handlers
             handler_primitives.c  ← Per-type implementations
             handler_sequences.c
             handler_unions.c
             ... (one per complex type)
       /generated                  ← idlc output (.c/.h)
    
    /App
       CycloneDDS.Roundtrip.App.csproj
       Program.cs                  ← Entry point
       NativeInterop.cs            ← P/Invoke declarations
       DataGenerator.cs            ← C# fill logic (reflection-based)
       TestScenarios.cs            ← Test case definitions
       ConsoleReporter.cs          ← Output formatting
       /Generated                  ← C# CodeGen output
```

---

## 5. Detailed Component Design

### 5.1 Native Type Registry

The registry is a compile-time table that maps topic names to function pointers.

**`type_registry.h`**
```c
typedef struct {
    const char* topic_name;
    const dds_topic_descriptor_t* (*get_descriptor)();
    void* (*alloc)();
    void  (*free)(void* sample);
    void  (*fill)(void* sample, int seed);
    bool  (*compare)(const void* a, const void* b);
} type_handler_t;

extern const type_handler_t* registry_lookup(const char* topic_name);
```

**Registration Macro (reduces boilerplate):**
```c
#define REGISTER_TYPE(CTYPE, TOPIC_NAME, DESC) \
    void* alloc_##CTYPE() { return dds_alloc(sizeof(CTYPE)); } \
    void free_##CTYPE(void* s) { dds_sample_free(s, &DESC, DDS_FREE_ALL); } \
    const dds_topic_descriptor_t* desc_##CTYPE() { return &DESC; } \
    void fill_##CTYPE(void* s, int seed); /* Implemented by user */ \
    bool compare_##CTYPE(const void* a, const void* b); /* Implemented by user */
```

### 5.2 Native Data Generator

**Example: Primitive Fields**
```c
void fill_AllPrimitives(Verification_AllPrimitives* sample, int seed) {
    sample->id = seed;
    sample->bool_field = ((seed + 1) % 2) != 0;
    sample->char_field = 'A' + ((seed + 2) % 26);
    sample->octet_field = (seed + 3) & 0xFF;
    sample->short_field = (seed + 4) * 31;
    sample->long_field = (seed + 5) * 997;
    sample->float_field = (float)(seed + 6) * 3.14f;
    sample->double_field = (double)(seed + 7) * 2.71828;
}
```

**Example: Sequences**
```c
void fill_SequenceTopic(Verification_SequenceTopic* sample, int seed) {
    sample->id = seed;
    
    // Dynamic sequence length: 1-5 elements
    uint32_t len = ((seed % 5) + 1);
    
    // Unbounded sequence
    dds_sequence_long_alloc(&sample->unbounded_seq, len);
    for (uint32_t i = 0; i < len; i++) {
        sample->unbounded_seq._buffer[i] = (seed + i + 10) * 31;
    }
    
    // Bounded sequence (max 10)
    len = (len > 10) ? 10 : len;
    dds_sequence_long_alloc(&sample->bounded_seq, len);
    for (uint32_t i = 0; i < len; i++) {
        sample->bounded_seq._buffer[i] = (seed + i + 20) * 31;
    }
}
```

**Example: Unions**
```c
void fill_UnionTopic(Verification_UnionTopic* sample, int seed) {
    sample->id = seed;
    
    // Discriminator cycles through cases
    int discriminator = (seed % 3);
    sample->shape._d = discriminator;
    
    switch (discriminator) {
        case 1:
            sample->shape._u.p2d.x = (seed + 100) * 3.14;
            sample->shape._u.p2d.y = (seed + 101) * 3.14;
            break;
        case 2:
            sample->shape._u.p3d.x = (seed + 200) * 3.14;
            sample->shape._u.p3d.y = (seed + 201) * 3.14;
            sample->shape._u.p3d.z = (seed + 202) * 3.14;
            break;
        case 3:
            sample->shape._u.radius = (seed + 300);
            break;
    }
}
```

### 5.3 C# Data Generator (Reflection-Based)

**`DataGenerator.cs`**
```csharp
public static class DataGenerator
{
    public static void Fill<T>(ref T target, int seed, int offset = 0) where T : struct
    {
        var type = typeof(T);
        
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            offset++;
            object boxed = target; // Box once
            
            if (field.FieldType == typeof(int))
                field.SetValue(boxed, (seed + offset) * 31);
            else if (field.FieldType == typeof(long))
                field.SetValue(boxed, (long)(seed + offset) * 997);
            else if (field.FieldType == typeof(double))
                field.SetValue(boxed, (seed + offset) * 3.14159);
            else if (field.FieldType == typeof(float))
                field.SetValue(boxed, (float)(seed + offset) * 3.14f);
            else if (field.FieldType == typeof(bool))
                field.SetValue(boxed, ((seed + offset) % 2) == 0);
            else if (field.FieldType == typeof(string))
                field.SetValue(boxed, $"Str_{seed + offset}");
            else if (field.FieldType.IsEnum)
            {
                var values = Enum.GetValues(field.FieldType);
                int index = (seed + offset) % values.Length;
                field.SetValue(boxed, values.GetValue(index));
            }
            else if (field.FieldType.IsGenericType && 
                     field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // Handle sequences
                var elemType = field.FieldType.GetGenericArguments()[0];
                int count = ((seed + offset) % 5) + 1;
                var list = (IList)Activator.CreateInstance(field.FieldType);
                
                for (int i = 0; i < count; i++)
                {
                    var elem = FillElement(elemType, seed, offset + i + 10);
                    list.Add(elem);
                }
                field.SetValue(boxed, list);
            }
            else if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive)
            {
                // Nested struct - recurse
                var nested = Activator.CreateInstance(field.FieldType);
                FillObject(ref nested, seed, offset + 100);
                field.SetValue(boxed, nested);
            }
            
            target = (T)boxed; // Unbox
        }
    }
    
    // Deep equality check
    public static bool AreEqual<T>(T a, T b) where T : struct
    {
        var type = typeof(T);
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var valA = field.GetValue(a);
            var valB = field.GetValue(b);
            
            if (!DeepEquals(valA, valB))
            {
                Console.WriteLine($"[Mismatch] Field '{field.Name}': {valA} != {valB}");
                return false;
            }
        }
        return true;
    }
}
```

### 5.4 Test Scenario Example

**`TestScenarios.cs`**
```csharp
public class RoundtripTests
{
    private DdsParticipant _participant;
    private Dictionary<string, object> _writers = new();
    private Dictionary<string, object> _readers = new();
    
    public void RunAllTests()
    {
        _participant = new DdsParticipant(domainId: 0);
        NativeInterop.Native_Init(domainId: 0);
        
        try
        {
            TestCase("AllPrimitives", TestAllPrimitives);
            TestCase("SequenceTopic", TestSequences);
            TestCase("UnionTopic", TestUnions);
            TestCase("NestedKeyTopic", TestNestedKeys);
            // ... more tests
        }
        finally
        {
            NativeInterop.Native_Cleanup();
            _participant.Dispose();
        }
    }
    
    private void TestAllPrimitives()
    {
        const string topic = "AllPrimitives";
        
        // Setup
        var writer = GetWriter<Verification.AllPrimitives>(topic);
        var reader = GetReader<Verification.AllPrimitives>(topic);
        NativeInterop.Native_CreateSubscriber(topic);
        NativeInterop.Native_CreatePublisher(topic);
        Thread.Sleep(500); // Discovery time
        
        // Test 1: C# → Native
        var sendData = new Verification.AllPrimitives();
        DataGenerator.Fill(ref sendData, seed: 42);
        writer.Write(sendData);
        
        int result = NativeInterop.Native_ExpectWithSeed(topic, seed: 42, timeout: 5000);
        Assert.AreEqual(0, result, "Native failed to receive or verify C# data");
        
        // Test 2: Native → C#
        NativeInterop.Native_SendWithSeed(topic, seed: 99);
        
        bool received = reader.WaitDataAsync(TimeSpan.FromSeconds(5)).Result;
        Assert.IsTrue(received, "C# did not receive data from Native");
        
        using var scope = reader.Take(1);
        Assert.AreEqual(1, scope.Count, "Expected 1 sample");
        
        var receivedData = scope[0];
        var expectedData = new Verification.AllPrimitives();
        DataGenerator.Fill(ref expectedData, seed: 99);
        
        bool match = DataGenerator.AreEqual(expectedData, receivedData);
        Assert.IsTrue(match, "C# data mismatch");
    }
}
```

---

## 6. Build System Integration

### 6.1 CMake Configuration (`Native/CMakeLists.txt`)

```cmake
cmake_minimum_required(VERSION 3.16)
project(CycloneDDS.Roundtrip.Native C)

# Configuration
if(NOT DEFINED CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

if(NOT DEFINED CYCLONE_INSTALL_DIR)
    get_filename_component(REPO_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../../.." ABSOLUTE)
    set(CYCLONE_INSTALL_DIR "${REPO_ROOT}/cyclone-compiled")
endif()

set(IDLC_EXE "${CYCLONE_INSTALL_DIR}/bin/idlc.exe")

# Find CycloneDDS
include_directories("${CYCLONE_INSTALL_DIR}/include")
link_directories("${CYCLONE_INSTALL_DIR}/lib")

# IDL Generation
file(GLOB IDL_FILES "${CMAKE_CURRENT_SOURCE_DIR}/../idl/*.idl")

foreach(IDL_FILE ${IDL_FILES})
    get_filename_component(IDL_NAME ${IDL_FILE} NAME_WE)
    set(GEN_C "${CMAKE_CURRENT_BINARY_DIR}/generated/${IDL_NAME}.c")
    set(GEN_H "${CMAKE_CURRENT_BINARY_DIR}/generated/${IDL_NAME}.h")
    
    add_custom_command(
        OUTPUT ${GEN_C} ${GEN_H}
        COMMAND ${CMAKE_COMMAND} -E make_directory "${CMAKE_CURRENT_BINARY_DIR}/generated"
        COMMAND ${IDLC_EXE} -l c -o "${CMAKE_CURRENT_BINARY_DIR}/generated" ${IDL_FILE}
        DEPENDS ${IDL_FILE}
        COMMENT "Generating C code for ${IDL_NAME}.idl"
    )
    list(APPEND GENERATED_SOURCES ${GEN_C})
endforeach()

# Sources
file(GLOB NATIVE_SOURCES 
    "src/*.c" 
    "src/handlers/*.c"
)

# Build DLL
add_library(CycloneDDS.Roundtrip.Native SHARED 
    ${NATIVE_SOURCES}
    ${GENERATED_SOURCES}
)

target_include_directories(CycloneDDS.Roundtrip.Native PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/src"
    "${CMAKE_CURRENT_BINARY_DIR}/generated"
)

target_link_libraries(CycloneDDS.Roundtrip.Native PRIVATE ddsc)

# Windows: Export all symbols
if(WIN32)
    set_target_properties(CycloneDDS.Roundtrip.Native PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
endif()

# Installation
install(TARGETS CycloneDDS.Roundtrip.Native
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
)
```

### 6.2 Build Batch File

**`build_roundtrip_tests.bat`**
```bat
@echo off
SETLOCAL

:: Parse build type
SET BUILD_TYPE=%1
IF "%BUILD_TYPE%"=="" SET BUILD_TYPE=Release

SET ROOT=%~dp0
SET NATIVE_DIR=%ROOT%tests\CycloneDDS.Roundtrip.Tests\Native
SET APP_DIR=%ROOT%tests\CycloneDDS.Roundtrip.Tests\App
SET BUILD_DIR=%NATIVE_DIR%\build

echo ========================================================
echo   Building Roundtrip Tests (%BUILD_TYPE%)
echo ========================================================

:: Step 1: Build Cyclone (if not already built)
IF NOT EXIST "%ROOT%cyclone-compiled\bin\ddsc.dll" (
    echo [Cyclone] Not found. Building Cyclone first...
    IF /I "%BUILD_TYPE%"=="Debug" (
        call "%ROOT%build_cyclone_debug.bat"
    ) ELSE (
        call "%ROOT%build_cyclone.bat"
    )
)

:: Step 2: Configure Native CMake
echo.
echo [Native] Configuring CMake...
IF NOT EXIST "%BUILD_DIR%" mkdir "%BUILD_DIR%"
pushd "%BUILD_DIR%"

cmake -G "Visual Studio 17 2022" -A x64 ^
    -DCMAKE_BUILD_TYPE=%BUILD_TYPE% ^
    -DCYCLONE_INSTALL_DIR="%ROOT%cyclone-compiled" ^
    ..

IF ERRORLEVEL 1 (
    echo [ERROR] CMake configuration failed
    popd
    exit /b 1
)

:: Step 3: Build Native DLL
echo.
echo [Native] Building DLL...
cmake --build . --config %BUILD_TYPE% --parallel

IF ERRORLEVEL 1 (
    echo [ERROR] Native build failed
    popd
    exit /b 1
)

SET NATIVE_DLL=%BUILD_DIR%\%BUILD_TYPE%\CycloneDDS.Roundtrip.Native.dll
popd

:: Step 4: Build C# App
echo.
echo [C#] Building App...
dotnet build "%APP_DIR%\CycloneDDS.Roundtrip.App.csproj" -c %BUILD_TYPE%

IF ERRORLEVEL 1 (
    echo [ERROR] C# build failed
    exit /b 1
)

:: Step 5: Deploy Native DLL to C# output
echo.
echo [Deploy] Copying Native DLL to C# output...
SET CSHARP_OUT=%APP_DIR%\bin\%BUILD_TYPE%\net8.0
copy /Y "%NATIVE_DLL%" "%CSHARP_OUT%\"
copy /Y "%ROOT%cyclone-compiled\bin\ddsc.dll" "%CSHARP_OUT%\"

echo.
echo ========================================================
echo   Build Complete!
echo   Run: %CSHARP_OUT%\CycloneDDS.Roundtrip.App.exe
echo ========================================================

ENDLOCAL
```

---

## 7. Console Output Format

The test runner should produce structured, parseable output:

```
[Roundtrip] Initializing...
[Roundtrip] Domain ID: 0
[Roundtrip] Native DLL: Loaded
[Roundtrip] Discovery Wait: 500ms

========================================
Test Suite: Roundtrip Verification
========================================

[Test 1/10] AllPrimitives
  [C# → Native] Sending seed=42...
  [C# → Native] Native verified: OK
  [Native → C#] Receiving seed=99...
  [Native → C#] C# verified: OK
  Result: PASS

[Test 2/10] SequenceTopic
  [C# → Native] Sending seed=100...
  [C# → Native] Native verified: OK
  [Native → C#] Receiving seed=200...
  [Native → C#] ERROR: Field 'unbounded_seq[2]' mismatch
                Expected: 6570, Received: 0
  Result: FAIL

...

========================================
Summary
========================================
Total:   10 tests
Passed:  9 tests
Failed:  1 test
Time:    12.3 seconds

Exit Code: 1 (failure detected)
```

**Exit Codes:**
- `0`: All tests passed
- `1`: At least one test failed
- `2`: Fatal error (crash, timeout, etc.)

---

## 8. Maintenance Considerations

### 8.1 Adding a New Test Type

**Effort:** ~30 minutes per complex type

1. Add IDL definition to `idl/roundtrip_test.idl`
2. Implement C handler:
   - `fill_TypeName()` function
   - `compare_TypeName()` function
   - Register in `type_registry.c`
3. Add test case to `TestScenarios.cs`
4. Rebuild (batch file handles code generation)

### 8.2 Common Pitfalls

**Sequence Allocation:**
- C requires manual `dds_sequence_*_alloc()`. Don't forget to free.
- C# uses `List<T>` - automatic memory management.

**Union Discriminators:**
- Ensure both sides set `_d` field identically.
- Discriminator value determines which branch is active.

**String Bounds:**
- IDL `string<N>` maps to bounded strings in C. Don't exceed limits.

**Alignment:**
- C structs may have padding. Use `offsetof()` carefully.

---

## 9. Future Enhancements

### 9.1 Performance Benchmarking
Add throughput and latency tests alongside correctness tests.

### 9.2 Multi-Instance Testing
Use keyed topics with multiple instances to test instance management.

### 9.3 QoS Variations
Test Reliable vs. Best-Effort, Transient-Local durability, etc.

### 9.4 Fragmentation Testing
Send messages >64KB to test message fragmentation.

### 9.5 Negative Tests
Intentionally corrupt data to ensure error detection works.

---

## 10. Conclusion

This framework provides a **robust, maintainable, and scalable** approach to verifying the C# bindings against the native CycloneDDS implementation. The seed-based generation strategy eliminates the need for hundreds of manual assertions while ensuring comprehensive coverage of all IDL types and features.

**Next Steps:**
1. Review this design with stakeholders
2. Implement the framework skeleton
3. Migrate test_20.idl as the first test suite
4. Add CI/CD integration

