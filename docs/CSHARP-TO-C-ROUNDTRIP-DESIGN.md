# C# to C Roundtrip Test Framework - Design Document

**Version:** 2.0  
**Date:** January 25, 2026  
**Status:** Design Phase

---

## 1. Executive Summary

This document describes a **comprehensive, minimalistic, and incremental test framework** for validating C# ↔ Native C DDS interoperability. Unlike the previous framework that tested complex types all at once, this new framework follows a **bottom-up, piece-by-piece** strategy.

### Key Design Principles

1. **Minimalistic Topics**: Each IDL topic tests exactly ONE feature (e.g., "single boolean field", "single sequence of int32")
2. **Incremental Complexity**: Start from the simplest primitives, gradually add complexity
3. **Wire Format Inspection**: Capture and analyze CDR byte streams for debugging
4. **Triple Verification**:
   - Native → C# (receive): Validate deserialization
   - Native → C# (CDR dump): Capture wire format for reference
   - C# → Native (send): Validate serialization against known CDR format
5. **JSON Validation**: All topics verified via IdlJson.Tests before runtime testing

### Testing Strategy

```
Phase 1: Receive Native Data → C# Deserialization + CDR Capture
Phase 2: Compare C# Serialization → Native CDR (byte-for-byte)
Phase 3: Send C# Data → Native Validation
```

---

## 2. Architecture Overview

### 2.1 Component Structure

```
tests/CsharpToC.Roundtrip.Tests/
├── idl/
│   └── atomic_tests.idl              # Minimalistic test topics
│
├── Native/
│   ├── CMakeLists.txt
│   ├── atomic_tests_native.c         # C implementation
│   └── test_registry.c               # Topic handler registry
│
├── App/
│   ├── CsharpToC.Roundtrip.App.csproj
│   ├── Program.cs                    # Test orchestrator
│   ├── TestRunner.cs                 # Per-topic test execution
│   ├── CdrDumper.cs                  # Hex dump utility
│   └── Validators/
│       ├── BooleanValidator.cs
│       ├── Int32Validator.cs
│       └── ... (one per basic type)
│
├── Output/
│   └── cdr_dumps/                    # CDR byte streams (.hex files)
│       ├── BooleanTopic_seed_42.hex
│       ├── Int32Topic_seed_100.hex
│       └── ...
│
├── README.md
└── INTEGRATION-GUIDE.md              # How to add new topics
```

---

## 3. Minimalistic Test Topics

### 3.1 Basic Primitives (Single Field)

Each topic tests **one** primitive type in isolation:

```idl
module AtomicTests {
    @topic
    struct BooleanTopic {
        @key long id;
        boolean value;
    };
    
    @topic
    struct CharTopic {
        @key long id;
        char value;
    };
    
    @topic
    struct OctetTopic {
        @key long id;
        octet value;
    };
    
    @topic
    struct Int16Topic {
        @key long id;
        short value;
    };
    
    @topic
    struct UInt16Topic {
        @key long id;
        unsigned short value;
    };
    
    @topic
    struct Int32Topic {
        @key long id;
        long value;
    };
    
    @topic
    struct UInt32Topic {
        @key long id;
        unsigned long value;
    };
    
    @topic
    struct Int64Topic {
        @key long id;
        long long value;
    };
    
    @topic
    struct UInt64Topic {
        @key long id;
        unsigned long long value;
    };
    
    @topic
    struct Float32Topic {
        @key long id;
        float value;
    };
    
    @topic
    struct Float64Topic {
        @key long id;
        double value;
    };
    
    @topic
    struct StringUnboundedTopic {
        @key long id;
        string value;
    };
    
    @topic
    struct StringBoundedTopic {
        @key long id;
        string<32> value;
    };
}
```

### 3.2 Enums

```idl
module AtomicTests {
    enum SimpleEnum { FIRST, SECOND, THIRD };
    
    @topic
    struct EnumTopic {
        @key long id;
        SimpleEnum value;
    };
}
```

### 3.3 Nested Structures

```idl
module AtomicTests {
    struct Point2D {
        double x;
        double y;
    };
    
    @topic
    struct NestedStructTopic {
        @key long id;
        Point2D point;
    };
    
    struct Box {
        Point2D topLeft;
        Point2D bottomRight;
    };
    
    @topic
    struct DoublyNestedTopic {
        @key long id;
        Box box;
    };
}
```

### 3.4 Unions

```idl
module AtomicTests {
    // Union with long discriminator
    union SimpleUnion switch(long) {
        case 1: long int_value;
        case 2: double double_value;
        case 3: string<64> string_value;
    };
    
    @topic
    struct UnionTopic {
        @key long id;
        SimpleUnion data;
    };
    
    // Union with boolean discriminator
    union BoolUnion switch(boolean) {
        case TRUE: long true_val;
        case FALSE: double false_val;
    };
    
    @topic
    struct BoolUnionTopic {
        @key long id;
        BoolUnion data;
    };
    
    // Union with enum discriminator
    enum Color { RED, GREEN, BLUE };
    
    union ColorUnion switch(Color) {
        case RED: long red_data;
        case GREEN: double green_data;
        case BLUE: string<32> blue_data;
    };
    
    @topic
    struct EnumUnionTopic {
        @key long id;
        ColorUnion data;
    };
}
```

### 3.5 Optional Fields

```idl
module AtomicTests {
    @topic
    struct OptionalInt32Topic {
        @key long id;
        @optional long opt_value;
    };
    
    @topic
    struct OptionalStringTopic {
        @key long id;
        @optional string<64> opt_string;
    };
    
    struct OptionalNested {
        double x;
        double y;
    };
    
    @topic
    struct OptionalStructTopic {
        @key long id;
        @optional OptionalNested opt_nested;
    };
}
```

### 3.6 Simple Sequences

```idl
module AtomicTests {
    @topic
    struct SequenceInt32Topic {
        @key long id;
        sequence<long> values;
    };
    
    @topic
    struct BoundedSequenceInt32Topic {
        @key long id;
        sequence<long, 10> values;
    };
    
    @topic
    struct SequenceFloat64Topic {
        @key long id;
        sequence<double> values;
    };
    
    @topic
    struct SequenceStringTopic {
        @key long id;
        sequence<string<32>> values;
    };
    
    @topic
    struct SequenceEnumTopic {
        @key long id;
        sequence<SimpleEnum> values;
    };
    
    @topic
    struct SequenceStructTopic {
        @key long id;
        sequence<Point2D> points;
    };
    
    @topic
    struct SequenceUnionTopic {
        @key long id;
        sequence<SimpleUnion> unions;
    };
}
```

### 3.7 Arrays

```idl
module AtomicTests {
    @topic
    struct ArrayInt32Topic {
        @key long id;
        long values[5];
    };
    
    @topic
    struct Array2DInt32Topic {
        @key long id;
        long matrix[3][4];
    };
    
    @topic
    struct ArrayStringTopic {
        @key long id;
        string<16> names[3];
    };
}
```

### 3.8 Extensibility (Appendable vs Final)

```idl
module AtomicTests {
    @appendable
    @topic
    struct AppendableInt32Topic {
        @key long id;
        long value;
    };
    
    @final
    @topic
    struct FinalInt32Topic {
        @key long id;
        long value;
    };
    
    @mutable
    @topic
    struct MutableInt32Topic {
        @key long id;
        @id(100) long value;
    };
}
```

### 3.9 Composite Keys

```idl
module AtomicTests {
    @topic
    struct TwoKeyTopic {
        @key long key1;
        @key long key2;
        double value;
    };
    
    @topic
    struct ThreeKeyTopic {
        @key long key1;
        @key string<32> key2;
        @key short key3;
        double value;
    };
}
```

### 3.10 Nested Keys

```idl
module AtomicTests {
    struct Location {
        @key long building;
        @key short floor;
    };
    
    @topic
    struct NestedKeyTopic {
        @key Location loc;
        double temperature;
    };
}
```

### 3.11 Advanced Combinations (Phase 3)

Only after all basics pass:

```idl
module AtomicTests {
    @topic
    struct SequenceOfOptionalTopic {
        @key long id;
        sequence<@optional long> opt_values;
    };
    
    @topic
    struct NestedSequenceTopic {
        @key long id;
        sequence<sequence<long>> matrix;
    };
    
    @appendable
    @topic
    struct AppendableWithSequenceTopic {
        @key long id;
        sequence<Point2D> points;
        @optional string<64> description;
    };
}
```

---

## 4. Test Execution Flow

### 4.1 Phase 1: Native → C# (Receive & Capture)

For each topic:

1. **Native Sends**: `Native_SendWithSeed("BooleanTopic", 42)`
   - Native generates deterministic data from seed
   - Publishes via `dds_write()`

2. **C# Receives**:
   - Subscribes to topic
   - Reads message via `DdsReader<BooleanTopic>`
   - **Captures raw CDR bytes** before deserialization
   - Saves to `Output/cdr_dumps/BooleanTopic_seed_42.hex`

3. **C# Validates**:
   - Deserializes to C# object
   - Compares against locally generated seed-42 data
   - Reports: `[PASS/FAIL] BooleanTopic deserialization`

### 4.2 Phase 2: C# Serialization Verification

1. **C# Generates**: `BooleanTopic(seed=42)`
2. **C# Serializes**: Convert to CDR bytes using C# serializer
3. **Compare**: Byte-for-byte with `BooleanTopic_seed_42.hex` from Phase 1
4. **Report**: `[PASS/FAIL] BooleanTopic serialization matches native`

### 4.3 Phase 3: C# → Native (Send)

1. **C# Sends**: Serialize and publish `BooleanTopic(seed=42)`
2. **Native Receives**: `Native_ExpectWithSeed("BooleanTopic", 42, timeout=5000)`
   - Native reads message
   - Compares against locally generated seed-42 data
3. **Report**: `[PASS/FAIL] BooleanTopic C#→Native roundtrip`

---

## 5. CDR Dump Format

### 5.1 File Naming Convention

```
{TopicName}_seed_{Seed}_{Variant}.hex
```

Examples:
- `BooleanTopic_seed_42_native.hex`
- `BooleanTopic_seed_42_csharp.hex`
- `SequenceInt32Topic_seed_100_native.hex`

### 5.2 Hex File Format

```
# Topic: BooleanTopic
# Seed: 42
# Direction: Native → C#
# Timestamp: 2026-01-25T14:30:00Z
# CDR Encoding: XCDR2
# Size: 16 bytes

00 01 00 00  # XCDR2 header
00 00 00 2A  # key: id = 42
01 00 00 00  # value: true (aligned to 4 bytes)
```

### 5.3 CdrDumper.cs Implementation

```csharp
public class CdrDumper
{
    public static void SaveHexDump(string topicName, int seed, byte[] cdrData, string direction)
    {
        var filename = $"{topicName}_seed_{seed}_{direction}.hex";
        var path = Path.Combine("Output", "cdr_dumps", filename);
        
        using var writer = new StreamWriter(path);
        writer.WriteLine($"# Topic: {topicName}");
        writer.WriteLine($"# Seed: {seed}");
        writer.WriteLine($"# Direction: {direction}");
        writer.WriteLine($"# Timestamp: {DateTime.UtcNow:O}");
        writer.WriteLine($"# Size: {cdrData.Length} bytes");
        writer.WriteLine();
        
        for (int i = 0; i < cdrData.Length; i += 4)
        {
            var chunk = cdrData.Skip(i).Take(4).ToArray();
            var hex = string.Join(" ", chunk.Select(b => b.ToString("X2")));
            writer.WriteLine($"{hex,-11}  # Offset {i}");
        }
    }
    
    public static byte[] LoadHexDump(string filename)
    {
        var path = Path.Combine("Output", "cdr_dumps", filename);
        var bytes = new List<byte>();
        
        foreach (var line in File.ReadLines(path))
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue;
                
            var parts = line.Split('#')[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                bytes.Add(Convert.ToByte(part, 16));
            }
        }
        
        return bytes.ToArray();
    }
}
```

---

## 6. Integration with IdlJson.Tests

### 6.1 Workflow

Before any runtime testing, all topics must pass IdlJson validation:

1. Add topic to `tests/IdlJson.Tests/verification.idl`
2. Generate JSON: `idlc -l json verification.idl`
3. Add verification to `verifier.c`
4. Build and run: `./verifier verification.json`
5. Ensure all ops, sizes, and offsets match

### 6.2 Adding a New Topic to IdlJson.Tests

**Step 1**: Add to `verification.idl`

```idl
// In tests/IdlJson.Tests/verification.idl
module AtomicTests {
    @topic
    struct BooleanTopic {
        @key long id;
        boolean value;
    };
}
```

**Step 2**: Regenerate C header and JSON

```bash
cd tests/IdlJson.Tests
idlc -l c verification.idl           # Generates verification.h
idlc -l json verification.idl   # Generates verification.json
```

**Step 3**: Add verification to `verifier.c`

```c
// In main() function, add:
VERIFY_TOPIC("BooleanTopic", BooleanTopic);
```

The `VERIFY_TOPIC` macro will:
- Check `sizeof(AtomicTests_BooleanTopic)` matches JSON
- Validate all opcodes in topic descriptor
- Verify key offsets

**Step 4**: Build and run

```bash
cd build
cmake ..
cmake --build .
./verifier ../verification.json
```

Expected output:
```
[PASS] sizeof(BooleanTopic): 8
[PASS] All 12 Opcodes match.
```

---

## 7. Native Implementation

### 7.1 Test Registry Pattern

```c
// test_registry.h
typedef struct {
    const char* name;
    const dds_topic_descriptor_t* descriptor;
    void (*generate)(void* data, int seed);
    int (*validate)(void* data, int seed);
    size_t size;
} topic_handler_t;

extern const topic_handler_t* find_handler(const char* topic_name);
```

### 7.2 Example Handler

```c
// boolean_topic_handler.c
#include "atomic_tests.h"

static void generate_boolean_topic(void* data, int seed) {
    AtomicTests_BooleanTopic* msg = (AtomicTests_BooleanTopic*)data;
    msg->id = seed;
    msg->value = (seed % 2) == 0;
}

static int validate_boolean_topic(void* data, int seed) {
    AtomicTests_BooleanTopic* msg = (AtomicTests_BooleanTopic*)data;
    
    if (msg->id != seed) {
        fprintf(stderr, "Key mismatch: expected %d, got %d\n", seed, msg->id);
        return -1;
    }
    
    bool expected = (seed % 2) == 0;
    if (msg->value != expected) {
        fprintf(stderr, "Value mismatch: expected %d, got %d\n", expected, msg->value);
        return -1;
    }
    
    return 0;
}

const topic_handler_t boolean_topic_handler = {
    .name = "BooleanTopic",
    .descriptor = &AtomicTests_BooleanTopic_desc,
    .generate = generate_boolean_topic,
    .validate = validate_boolean_topic,
    .size = sizeof(AtomicTests_BooleanTopic)
};
```

### 7.3 Registry Implementation

```c
// test_registry.c
#include "test_registry.h"

extern const topic_handler_t boolean_topic_handler;
extern const topic_handler_t int32_topic_handler;
extern const topic_handler_t sequence_int32_handler;
// ... declare all handlers

static const topic_handler_t* handlers[] = {
    &boolean_topic_handler,
    &int32_topic_handler,
    &sequence_int32_handler,
    NULL  // Sentinel
};

const topic_handler_t* find_handler(const char* topic_name) {
    for (int i = 0; handlers[i] != NULL; i++) {
        if (strcmp(handlers[i]->name, topic_name) == 0) {
            return handlers[i];
        }
    }
    return NULL;
}
```

---

## 8. C# Implementation

### 8.1 Test Runner

```csharp
public class TestRunner
{
    private readonly NativeApi _native;
    private readonly string _outputDir;
    
    public TestRunner(NativeApi native, string outputDir)
    {
        _native = native;
        _outputDir = outputDir;
        Directory.CreateDirectory(Path.Combine(outputDir, "cdr_dumps"));
    }
    
    public async Task<TestResult> RunTest<T>(string topicName, int seed) 
        where T : class, new()
    {
        var result = new TestResult { TopicName = topicName, Seed = seed };
        
        // Phase 1: Receive from native
        result.ReceiveTest = await TestReceive<T>(topicName, seed);
        
        // Phase 2: Compare serialization
        result.SerializationTest = await TestSerialization<T>(topicName, seed);
        
        // Phase 3: Send to native
        result.SendTest = await TestSend<T>(topicName, seed);
        
        return result;
    }
    
    private async Task<PhaseResult> TestReceive<T>(string topicName, int seed)
    {
        // Create C# subscriber
        var reader = CreateReader<T>(topicName);
        
        // Tell native to send
        _native.SendWithSeed(topicName, seed);
        
        // Wait for message
        var sample = await reader.TakeAsync(timeout: TimeSpan.FromSeconds(5));
        if (sample == null)
            return PhaseResult.Timeout("No message received");
        
        // Capture CDR bytes
        var cdrBytes = sample.RawCdrData; // Assumes reader exposes this
        CdrDumper.SaveHexDump(topicName, seed, cdrBytes, "native");
        
        // Validate deserialized data
        var expected = DataGenerator.Generate<T>(seed);
        bool valid = DataValidator.Compare(sample.Data, expected);
        
        return valid 
            ? PhaseResult.Pass("Deserialization successful")
            : PhaseResult.Fail("Data mismatch");
    }
    
    private async Task<PhaseResult> TestSerialization<T>(string topicName, int seed)
    {
        // Generate data in C#
        var data = DataGenerator.Generate<T>(seed);
        
        // Serialize using C# serializer
        var cdrBytes = CdrSerializer.Serialize(data);
        CdrDumper.SaveHexDump(topicName, seed, cdrBytes, "csharp");
        
        // Load native reference
        var nativeBytes = CdrDumper.LoadHexDump($"{topicName}_seed_{seed}_native.hex");
        
        // Compare byte-for-byte
        bool match = cdrBytes.SequenceEqual(nativeBytes);
        
        return match
            ? PhaseResult.Pass("Serialization matches native")
            : PhaseResult.Fail($"Byte mismatch at offset {FindFirstMismatch(cdrBytes, nativeBytes)}");
    }
    
    private async Task<PhaseResult> TestSend<T>(string topicName, int seed)
    {
        // Generate and send
        var data = DataGenerator.Generate<T>(seed);
        var writer = CreateWriter<T>(topicName);
        await writer.WriteAsync(data);
        
        // Tell native to expect
        int result = _native.ExpectWithSeed(topicName, seed, timeout: 5000);
        
        return result == 0
            ? PhaseResult.Pass("Native received and validated")
            : PhaseResult.Fail($"Native validation failed: error {result}");
    }
}
```

### 8.2 Data Generator (C# Mirror of Native)

```csharp
public static class DataGenerator
{
    public static T Generate<T>(int seed) where T : class, new()
    {
        var obj = new T();
        Fill(obj, seed, offset: 0);
        return obj;
    }
    
    private static void Fill(object obj, int seed, int offset)
    {
        var type = obj.GetType();
        
        foreach (var prop in type.GetProperties())
        {
            var propType = prop.PropertyType;
            offset++;
            
            if (propType == typeof(int))
                prop.SetValue(obj, (seed + offset) * 31);
            else if (propType == typeof(long))
                prop.SetValue(obj, (long)(seed + offset) * 31);
            else if (propType == typeof(double))
                prop.SetValue(obj, (seed + offset) * 3.14159);
            else if (propType == typeof(bool))
                prop.SetValue(obj, ((seed + offset) % 2) == 0);
            else if (propType == typeof(string))
                prop.SetValue(obj, $"Str_{seed + offset}");
            else if (propType.IsEnum)
            {
                var values = Enum.GetValues(propType);
                var index = (seed + offset) % values.Length;
                prop.SetValue(obj, values.GetValue(index));
            }
            else if (propType.IsClass)
            {
                var nested = Activator.CreateInstance(propType);
                Fill(nested, seed, offset + 100);
                prop.SetValue(obj, nested);
            }
            else if (propType.IsGenericType && 
                     propType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elemType = propType.GetGenericArguments()[0];
                var list = (System.Collections.IList)Activator.CreateInstance(propType);
                int count = ((seed + offset) % 5) + 1;
                
                for (int i = 0; i < count; i++)
                {
                    var elem = Activator.CreateInstance(elemType);
                    Fill(elem, seed, offset + i + 10);
                    list.Add(elem);
                }
                
                prop.SetValue(obj, list);
            }
        }
    }
}
```

---

## 9. Success Criteria

### 9.1 Per-Topic Success

A topic test is considered **PASSED** when:

1. ✅ IdlJson verification passes (sizes, ops, keys match)
2. ✅ Phase 1 (Native→C#): Deserialization successful, data matches seed
3. ✅ Phase 2 (Serialization): C# CDR bytes match Native CDR bytes
4. ✅ Phase 3 (C#→Native): Native validates received data

### 9.2 Overall Success

The framework is considered **READY** when:

- All basic primitive topics pass (11 topics)
- All enum topics pass (1 topic)
- All nested struct topics pass (2 topics)
- All union topics pass (3 topics)
- All optional topics pass (3 topics)
- All sequence topics pass (6 topics)
- All array topics pass (3 topics)
- All key topics pass (3 topics)

**Total: ~35 minimalistic topics**

---

## 10. Debugging Workflow

When a test fails:

1. **Check IdlJson first**: Ensure ops/sizes are correct
2. **Inspect CDR dumps**: Compare `.hex` files side-by-side
3. **Isolate the problem**: 
   - If Phase 1 fails: C# deserialization bug
   - If Phase 2 fails: C# serialization bug
   - If Phase 3 fails: Native interpretation issue
4. **Use minimal reproducer**: Focus on the single failing topic
5. **Verify encoding**: Check if XCDR1 vs XCDR2 mismatch

---

## 11. Future Extensions

After basic topics pass:

1. **Performance Testing**: Measure serialization/deserialization speed
2. **Fuzzing**: Generate random seeds, test thousands of samples
3. **Interoperability**: Test against other DDS implementations (RTI, FastDDS)
4. **Schema Evolution**: Test version compatibility (add/remove fields)
5. **Large Data**: Test MB-sized sequences, strings

---

## 12. Migration from Old Framework

The old `CycloneDDS.Roundtrip.Tests` can remain for integration testing of complex types. The new `CsharpToC.Roundtrip.Tests` focuses on **building blocks**.

Once all atomics pass, we can confidently combine them into complex scenarios knowing each component is validated individually.

---

## 13. Summary

This framework enables:

- ✅ **Systematic debugging**: Know exactly which primitive/feature fails
- ✅ **Wire format transparency**: See exactly what bytes are exchanged
- ✅ **Confidence building**: Start small, prove correctness, scale up
- ✅ **Maintenance**: Easy to add new topics, clear structure

**Next Steps**: Implement the IDL, build the infrastructure, run the first test (BooleanTopic).
