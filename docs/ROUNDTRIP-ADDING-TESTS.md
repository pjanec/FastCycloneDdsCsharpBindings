# Adding New Test Messages to the Roundtrip Framework

## Quick Reference Guide

This guide explains step-by-step how to add a new IDL message type to the roundtrip verification framework.

---

## Prerequisites

- Basic understanding of OMG IDL syntax
- Familiarity with CycloneDDS C API
- C# programming knowledge
- CMake basics

---

## Step-by-Step Process

### Step 1: Define Your IDL Type

**File:** `tests/CycloneDDS.Roundtrip.Tests/idl/roundtrip_test.idl`

Add your new type definition to the IDL file:

```idl
module RoundtripTests {
    
    // Example: Multi-dimensional array test
    @topic
    struct MatrixData {
        @key long matrix_id;
        double values[3][3];  // 3x3 matrix
        string<64> description;
    };
    
    // Example: Complex nested structure
    struct Address {
        string<128> street;
        long zip_code;
    };
    
    @topic
    struct Person {
        @key long person_id;
        string<64> name;
        Address home_address;
        sequence<Address> previous_addresses;
    };
}
```

**Important Notes:**
- Use `@topic` annotation for top-level publishable types
- Use `@key` for key fields
- Bounded strings (`string<N>`) are recommended for deterministic memory usage
- Keep module names consistent

---

### Step 2: Implement Native C Handler

#### 2.1 Create Handler Source File

**File:** `tests/CycloneDDS.Roundtrip.Tests/Native/src/handlers/handler_matrix.c`

```c
#include "type_registry.h"
#include "roundtrip_test.h"  // Generated header
#include <string.h>

// ====================
// Allocation & Cleanup
// ====================

void* alloc_MatrixData() {
    return dds_alloc(sizeof(RoundtripTests_MatrixData));
}

void free_MatrixData(void* sample) {
    dds_sample_free(sample, &RoundtripTests_MatrixData_desc, DDS_FREE_ALL);
}

const dds_topic_descriptor_t* desc_MatrixData() {
    return &RoundtripTests_MatrixData_desc;
}

// ====================
// Data Generation
// ====================

void fill_MatrixData(void* sample, int seed) {
    RoundtripTests_MatrixData* data = (RoundtripTests_MatrixData*)sample;
    
    // Key field
    data->matrix_id = seed;
    
    // Multi-dimensional array
    for (int row = 0; row < 3; row++) {
        for (int col = 0; col < 3; col++) {
            int offset = (row * 3 + col) + 100;
            data->values[row][col] = (double)(seed + offset) * 3.14159;
        }
    }
    
    // Bounded string
    snprintf(data->description, sizeof(data->description), 
             "Matrix_%d", seed);
}

// ====================
// Comparison
// ====================

bool compare_MatrixData(const void* a, const void* b) {
    const RoundtripTests_MatrixData* x = (const RoundtripTests_MatrixData*)a;
    const RoundtripTests_MatrixData* y = (const RoundtripTests_MatrixData*)b;
    
    // Compare key
    if (x->matrix_id != y->matrix_id) {
        printf("[COMPARE] matrix_id mismatch: %ld != %ld\n", 
               x->matrix_id, y->matrix_id);
        return false;
    }
    
    // Compare array elements
    for (int row = 0; row < 3; row++) {
        for (int col = 0; col < 3; col++) {
            double diff = fabs(x->values[row][col] - y->values[row][col]);
            if (diff > 0.0001) {  // Floating-point tolerance
                printf("[COMPARE] values[%d][%d] mismatch: %.6f != %.6f\n",
                       row, col, x->values[row][col], y->values[row][col]);
                return false;
            }
        }
    }
    
    // Compare string
    if (strcmp(x->description, y->description) != 0) {
        printf("[COMPARE] description mismatch: '%s' != '%s'\n",
               x->description, y->description);
        return false;
    }
    
    return true;
}
```

#### 2.2 Register the Handler

**File:** `tests/CycloneDDS.Roundtrip.Tests/Native/src/type_registry.c`

Add your handler to the registry:

```c
// Forward declarations
extern void* alloc_MatrixData();
extern void free_MatrixData(void* sample);
extern const dds_topic_descriptor_t* desc_MatrixData();
extern void fill_MatrixData(void* sample, int seed);
extern bool compare_MatrixData(const void* a, const void* b);

// Registry table
static const type_handler_t registry[] = {
    // ... existing handlers ...
    
    {
        .topic_name = "MatrixData",
        .alloc_fn = alloc_MatrixData,
        .free_fn = free_MatrixData,
        .desc_fn = desc_MatrixData,
        .fill_fn = fill_MatrixData,
        .compare_fn = compare_MatrixData
    },
    
    // Sentinel
    { .topic_name = NULL }
};
```

---

### Step 3: Implement C# Data Generator

The C# side uses **reflection-based generation**, so most types work automatically. However, you may need to add custom logic for complex cases.

**File:** `tests/CycloneDDS.Roundtrip.Tests/App/DataGenerator.cs`

#### 3.1 Automatic Handling (No Code Needed)

For simple structs with primitives, strings, and sequences, the reflection-based generator handles it:

```csharp
// This works automatically:
var person = new RoundtripTests.Person();
DataGenerator.Fill(ref person, seed: 42);
```

#### 3.2 Custom Handling (If Needed)

For complex types (e.g., multi-dimensional arrays, unions), add a specialized filler:

```csharp
public static partial class DataGenerator
{
    // Specialized handler for MatrixData
    public static void FillMatrixData(ref RoundtripTests.MatrixData data, int seed)
    {
        data.matrix_id = seed;
        
        // Multi-dimensional array
        data.values = new double[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int offset = (row * 3 + col) + 100;
                data.values[row, col] = (seed + offset) * 3.14159;
            }
        }
        
        data.description = $"Matrix_{seed}";
    }
    
    // Register custom filler
    static DataGenerator()
    {
        RegisterCustomFiller<RoundtripTests.MatrixData>(FillMatrixData);
    }
}
```

---

### Step 4: Add Test Scenario

**File:** `tests/CycloneDDS.Roundtrip.Tests/App/TestScenarios.cs`

Add a new test method:

```csharp
public class RoundtripTests
{
    // ... existing setup code ...
    
    public void TestMatrixData()
    {
        const string topicName = "MatrixData";
        
        ConsoleReporter.StartTest(topicName);
        
        try
        {
            // Setup DDS entities
            var writer = GetOrCreateWriter<RoundtripTests.MatrixData>(topicName);
            var reader = GetOrCreateReader<RoundtripTests.MatrixData>(topicName);
            
            NativeInterop.Native_CreatePublisher(topicName);
            NativeInterop.Native_CreateSubscriber(topicName);
            
            Thread.Sleep(500); // Allow discovery
            
            // ========================================
            // Test 1: C# → Native
            // ========================================
            ConsoleReporter.StartDirection("C# → Native");
            
            const int sendSeed = 42;
            var sendData = new RoundtripTests.MatrixData();
            DataGenerator.Fill(ref sendData, sendSeed);
            
            writer.Write(sendData);
            
            int nativeResult = NativeInterop.Native_ExpectWithSeed(
                topicName, 
                sendSeed, 
                timeoutMs: 5000
            );
            
            if (nativeResult == 0)
            {
                ConsoleReporter.DirectionPass();
            }
            else if (nativeResult == -1)
            {
                ConsoleReporter.DirectionFail("Native timeout");
                return;
            }
            else
            {
                ConsoleReporter.DirectionFail("Native verification failed");
                return;
            }
            
            // ========================================
            // Test 2: Native → C#
            // ========================================
            ConsoleReporter.StartDirection("Native → C#");
            
            const int receiveSeed = 99;
            NativeInterop.Native_SendWithSeed(topicName, receiveSeed);
            
            bool dataAvailable = reader.WaitDataAsync(TimeSpan.FromSeconds(5)).Result;
            
            if (!dataAvailable)
            {
                ConsoleReporter.DirectionFail("C# timeout");
                return;
            }
            
            using var scope = reader.Take(maxSamples: 1);
            
            if (scope.Count == 0)
            {
                ConsoleReporter.DirectionFail("No samples received");
                return;
            }
            
            var receivedData = scope[0];
            var expectedData = new RoundtripTests.MatrixData();
            DataGenerator.Fill(ref expectedData, receiveSeed);
            
            bool match = DataGenerator.AreEqual(expectedData, receivedData);
            
            if (match)
            {
                ConsoleReporter.DirectionPass();
            }
            else
            {
                ConsoleReporter.DirectionFail("Data mismatch");
                return;
            }
            
            ConsoleReporter.TestPass();
        }
        catch (Exception ex)
        {
            ConsoleReporter.TestFail($"Exception: {ex.Message}");
        }
    }
}
```

#### 4.1 Register Test in Suite

**File:** `tests/CycloneDDS.Roundtrip.Tests/App/Program.cs`

```csharp
static void Main(string[] args)
{
    var tests = new RoundtripTests();
    
    tests.Initialize();
    
    // ... existing tests ...
    tests.TestMatrixData();  // Add your test here
    
    tests.Cleanup();
    tests.PrintSummary();
    
    Environment.Exit(tests.FailedCount > 0 ? 1 : 0);
}
```

---

### Step 5: Build and Run

#### 5.1 Rebuild Everything

```batch
cd D:\Work\FastCycloneDdsCsharpBindings
build_roundtrip_tests.bat Release
```

This will:
1. Generate C code from IDL using `idlc`
2. Compile Native DLL
3. Generate C# code (via your CodeGen)
4. Build C# app
5. Copy DLLs to output folder

#### 5.2 Run Tests

```batch
cd tests\CycloneDDS.Roundtrip.Tests\App\bin\Release\net8.0
CycloneDDS.Roundtrip.App.exe
```

**Expected Output:**
```
[Roundtrip] Initializing...
========================================
[Test] MatrixData
  [C# → Native] Sending seed=42...
  [C# → Native] PASS
  [Native → C#] Receiving seed=99...
  [Native → C#] PASS
  Result: PASS
========================================
```

---

## Advanced Patterns

### Pattern 1: Sequences of Complex Types

**IDL:**
```idl
struct SensorReading {
    double temperature;
    double pressure;
};

@topic
struct SensorBatch {
    @key long batch_id;
    sequence<SensorReading> readings;
};
```

**C Handler:**
```c
void fill_SensorBatch(void* sample, int seed) {
    RoundtripTests_SensorBatch* data = (RoundtripTests_SensorBatch*)sample;
    
    data->batch_id = seed;
    
    uint32_t count = ((seed % 5) + 1);  // 1-5 elements
    dds_sequence_SensorReading_alloc(&data->readings, count);
    
    for (uint32_t i = 0; i < count; i++) {
        data->readings._buffer[i].temperature = (seed + i + 10) * 3.14;
        data->readings._buffer[i].pressure = (seed + i + 20) * 2.71;
    }
}
```

**C# Generator (automatic via reflection)**

### Pattern 2: Unions

**IDL:**
```idl
union DataValue switch(short) {
    case 1: long int_val;
    case 2: double float_val;
    case 3: string<32> str_val;
};

@topic
struct VariantData {
    @key long id;
    DataValue value;
};
```

**C Handler:**
```c
void fill_VariantData(void* sample, int seed) {
    RoundtripTests_VariantData* data = (RoundtripTests_VariantData*)sample;
    
    data->id = seed;
    
    int discriminator = (seed % 3) + 1;  // 1, 2, or 3
    data->value._d = discriminator;
    
    switch (discriminator) {
        case 1:
            data->value._u.int_val = (seed + 100);
            break;
        case 2:
            data->value._u.float_val = (seed + 200) * 3.14;
            break;
        case 3:
            snprintf(data->value._u.str_val, 32, "Union_%d", seed);
            break;
    }
}
```

### Pattern 3: Nested Keys

**IDL:**
```idl
struct LocationKey {
    @key long building;
    @key short floor;
};

@topic
struct RoomSensor {
    @key LocationKey location;
    double temperature;
};
```

**C Handler:**
```c
void fill_RoomSensor(void* sample, int seed) {
    RoundtripTests_RoomSensor* data = (RoundtripTests_RoomSensor*)sample;
    
    // Nested key
    data->location.building = seed;
    data->location.floor = (seed % 10) + 1;
    
    data->temperature = (seed + 50) * 1.5;
}
```

---

## Troubleshooting

### Issue: "Type not found in registry"

**Cause:** Handler not registered in `type_registry.c`

**Solution:** Add your handler to the `registry[]` array

### Issue: "Native comparison failed"

**Cause:** Fill logic differs between C and C#

**Solution:** 
1. Add debug prints to both fillers
2. Compare generated values step-by-step
3. Ensure same seed → same output

### Issue: "Timeout waiting for data"

**Cause:** Discovery not complete, QoS mismatch, or network issue

**Solution:**
1. Increase discovery wait time (default 500ms)
2. Check QoS settings match
3. Verify both entities created successfully

### Issue: "Floating-point comparison fails"

**Cause:** Precision differences

**Solution:** Use epsilon comparison in `compare_` function:
```c
if (fabs(a->value - b->value) > 0.0001) { /* mismatch */ }
```

---

## Best Practices

### 1. Use Deterministic Offsets

Ensure each field gets a unique offset to avoid accidental collisions:

```c
data->field1 = seed + 10;
data->field2 = seed + 20;
data->field3 = seed + 30;
```

### 2. Handle String Bounds

Always respect bounded string sizes:

```c
snprintf(data->name, sizeof(data->name), "Value_%d", seed);
```

### 3. Free Sequences

C requires manual cleanup:

```c
void free_MyType(void* sample) {
    dds_sample_free(sample, &MyType_desc, DDS_FREE_ALL);
}
```

### 4. Test Edge Cases

Use specific seed values to test:
- Empty sequences: `seed % (MAX+1) == 0` → length 0
- Maximum sequences: `seed % (MAX+1) == MAX` → length MAX
- Boundary values: INT_MAX, 0, negative numbers

### 5. Document Complex Logic

If your fill logic is non-trivial, add comments explaining the pattern:

```c
// Generate fibonacci-like sequence based on seed
// F(0) = seed, F(1) = seed+1, F(n) = F(n-1) + F(n-2)
```

---

## Checklist

Before committing your new test type:

- [ ] IDL compiles without errors (`idlc -l c mytype.idl`)
- [ ] C handler implemented (alloc, free, fill, compare, desc)
- [ ] Handler registered in `type_registry.c`
- [ ] C# test scenario added to `TestScenarios.cs`
- [ ] Test registered in `Program.cs`
- [ ] Build succeeds (`build_roundtrip_tests.bat`)
- [ ] Test passes both directions (C#→Native, Native→C#)
- [ ] Console output is clear and informative
- [ ] Code follows naming conventions
- [ ] Comments explain non-obvious logic

---

## Summary

Adding a new test type requires:

1. **~5 minutes:** Write IDL definition
2. **~15 minutes:** Implement C handler (fill + compare)
3. **~5 minutes:** Register handler
4. **~10 minutes:** Write C# test scenario
5. **~5 minutes:** Build and validate

**Total: ~40 minutes per complex type**

The seed-based approach eliminates the need to write hundreds of assertion lines, making this framework highly maintainable even with dozens of test types.

