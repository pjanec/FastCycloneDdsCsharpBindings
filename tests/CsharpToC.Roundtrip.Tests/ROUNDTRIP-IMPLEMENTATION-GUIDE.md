# C# to C Roundtrip Testing - Implementation Guide

**Date:** January 25, 2026  
**Purpose:** Systematic guide for expanding roundtrip test coverage  
**Current Status:** 10/77 topics implemented (13% coverage)  
**Target:** 80%+ coverage (62+ topics)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture Recap](#2-architecture-recap)
3. [Adding a New Topic - Step by Step](#3-adding-a-new-topic---step-by-step)
4. [Dual Topic Pattern (Final + Appendable)](#4-dual-topic-pattern-final--appendable)
5. [Implementation Patterns by Type](#5-implementation-patterns-by-type)
6. [Testing Strategy](#6-testing-strategy)
7. [Common Pitfalls](#7-common-pitfalls)
8. [Quality Standards](#8-quality-standards)

---

## 1. Overview

### 1.1 Purpose

This guide enables you to **systematically add new test topics** to the C# ↔ C roundtrip test framework. Each topic verifies C# and Native C DDS implementations produce byte-identical CDR serialization and can exchange data bidirectionally.

### 1.2 Key Principles

1. **One Feature Per Topic** - Each topic tests exactly ONE DDS feature
2. **Dual Configuration** - Each feature tested with BOTH Final and Appendable extensibility
3. **Triple Validation** - Every topic must pass: Receive, Serialize, Send
4. **Deterministic Data** - Seed-based generation ensures reproducibility
5. **Wire Format Verification** - CDR bytes must match byte-for-byte

### 1.3 File Locations

**All paths relative to:** `D:\Work\FastCycloneDdsCsharpBindings\tests\CsharpToC.Roundtrip.Tests\`

```
CsharpToC.Roundtrip.Tests/
├── idl/
│   └── atomic_tests.idl          # IDL topic definitions
│
├── Native/
│   ├── atomic_tests_native.c     # Native data generators/validators
│   └── test_registry.c           # Topic handler registry
│
├── AtomicTestsTypes.cs           # C# type definitions
├── Program.cs                    # Test orchestrator
└── SerializerHelper.cs           # CDR serialization helper
```

---

## 2. Architecture Recap

### 2.1 Test Flow

```
┌────────────────────────────────────────────────────────────────┐
│                    PHASE 1: Native → C#                        │
├────────────────────────────────────────────────────────────────┤
│ 1. C# calls Native_SendWithSeed(topic, seed)                   │
│ 2. Native C generates data (seed-based)                        │
│ 3. Native C serializes to CDR (Cyclone serializer)             │
│ 4. Native C sends via DDS                                      │
│ 5. C# receives via DDS                                         │
│ 6. C# deserializes from CDR (custom deserializer)              │
│ 7. C# validates data (seed-based validator)                    │
│ ✅ Proves: C serialization → C# deserialization works          │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│              PHASE 2: CDR Byte Verification                     │
├────────────────────────────────────────────────────────────────┤
│ 1. Capture CDR bytes from Native send (Phase 1)                │
│ 2. C# serializes same data (custom serializer)                 │
│ 3. Compare byte-for-byte (with alignment padding)              │
│ ✅ Proves: C# serializer produces identical CDR as C           │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                    PHASE 3: C# → Native                        │
├────────────────────────────────────────────────────────────────┤
│ 1. C# generates data (seed-based)                              │
│ 2. C# serializes to CDR (custom serializer)                    │
│ 3. C# sends via DDS                                            │
│ 4. Native C receives via DDS                                   │
│ 5. Native C deserializes from CDR (Cyclone deserializer)       │
│ 6. Native C validates data (seed-based validator)              │
│ ✅ Proves: C# serialization → C deserialization works          │
└────────────────────────────────────────────────────────────────┘
```

### 2.2 Seed-Based Validation

**Critical Concept:** Both C and C# must use **identical seed-based algorithms** to generate/validate data.

**Example:**
```c
// Native C (atomic_tests_native.c)
void generate_Int32Topic(void* data, int seed) {
    AtomicTests_Int32Topic* msg = (AtomicTests_Int32Topic*)data;
    msg->id = seed;
    msg->value = (int32_t)((seed * 1664525L) + 1013904223L);
}

int validate_Int32Topic(void* data, int seed) {
    AtomicTests_Int32Topic* msg = (AtomicTests_Int32Topic*)data;
    if (msg->id != seed) return -1;
    int32_t expected = (int32_t)((seed * 1664525L) + 1013904223L);
    if (msg->value != expected) return -1;
    return 0;
}
```

```csharp
// C# (Program.cs)
await RunRoundtrip<Int32Topic>(
    "AtomicTests::Int32Topic", 
    200, // Base seed
    (s) => { // Generator (must match C)
        var msg = new Int32Topic(); 
        msg.Id = s; 
        msg.Value = (int)((s * 1664525L) + 1013904223L); 
        return msg; 
    },
    (msg, s) => // Validator (must match C)
        msg.Id == s && msg.Value == (int)((s * 1664525L) + 1013904223L)
);
```

**Why this matters:** If data doesn't match, test fails. This proves serialization/deserialization preserved values correctly.

---

## 3. Adding a New Topic - Step by Step

### Example: Adding `Float32Topic`

#### Step 1: Define in IDL

**File:** `idl/atomic_tests.idl`

```idl
@topic
struct Float32Topic {
    @key long id;
    float value;
};

// Appendable variant
@appendable
@topic
struct Float32TopicAppendable {
    @key long id;
    float value;
};
```

**Rules:**
- Use `@topic` annotation
- Always include `@key long id` for consistency
- One data field per topic (minimalistic principle)
- Create both Final (default) and Appendable variants

#### Step 2: Define C# Types

**File:** `AtomicTestsTypes.cs`

```csharp
[DdsTopic("Float32Topic")]
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct Float32Topic
{
    [DdsKey]
    public int Id { get; set; }
    public float Value { get; set; }
}

[DdsTopic("Float32TopicAppendable")]
[DdsExtensibility(DdsExtensibilityKind.Appendable)]
public partial struct Float32TopicAppendable
{
    [DdsKey]
    public int Id { get; set; }
    public float Value { get; set; }
}
```

**Rules:**
- Topic name must match IDL exactly
- `DdsExtensibility` must match IDL (`Final` by default, `Appendable` if `@appendable`)
- Use `partial struct` (required for code generation)
- Property names follow C# conventions (PascalCase)
- Add `[DdsManaged]` if topic contains strings, sequences, or other managed types

#### Step 3: Implement Native Handlers

**File:** `Native/atomic_tests_native.c`

```c
// --- Float32Topic ---
static void generate_Float32Topic(void* data, int seed) {
    AtomicTests_Float32Topic* msg = (AtomicTests_Float32Topic*)data;
    msg->id = seed;
    // Use deterministic algorithm
    msg->value = (float)(seed * 3.14159f);
}

static int validate_Float32Topic(void* data, int seed) {
    AtomicTests_Float32Topic* msg = (AtomicTests_Float32Topic*)data;
    if (msg->id != seed) return -1;
    float expected = (float)(seed * 3.14159f);
    // Floating point comparison with epsilon
    if (fabsf(msg->value - expected) > 0.0001f) {
        fprintf(stderr, "Float32Topic mismatch: expected %f, got %f\n", 
                expected, msg->value);
        return -1;
    }
    return 0;
}
DEFINE_HANDLER(Float32Topic, float32_topic);

// --- Float32TopicAppendable ---
static void generate_Float32TopicAppendable(void* data, int seed) {
    AtomicTests_Float32TopicAppendable* msg = (AtomicTests_Float32TopicAppendable*)data;
    msg->id = seed;
    msg->value = (float)(seed * 3.14159f);
}

static int validate_Float32TopicAppendable(void* data, int seed) {
    AtomicTests_Float32TopicAppendable* msg = (AtomicTests_Float32TopicAppendable*)data;
    if (msg->id != seed) return -1;
    float expected = (float)(seed * 3.14159f);
    if (fabsf(msg->value - expected) > 0.0001f) return -1;
    return 0;
}
DEFINE_HANDLER(Float32TopicAppendable, float32_topic_appendable);
```

**Rules:**
- Generator and validator must use **identical algorithms**
- Use `DEFINE_HANDLER` macro to create handler structs
- Include error logging in validator for debugging
- For floating point, use epsilon comparison (`fabsf`, `fabs`)

#### Step 4: Register Handlers

**File:** `Native/test_registry.c`

Add extern declarations:
```c
// Around line 13-20
extern const topic_handler_t float32_topic_handler;
extern const topic_handler_t float32_topic_appendable_handler;
```

Add to registry array:
```c
// Around line 30-40
static const topic_handler_t* handlers[] = {
    &boolean_topic_handler,
    &int32_topic_handler,
    // ... existing handlers ...
    &float32_topic_handler,              // ADD THIS
    &float32_topic_appendable_handler,   // ADD THIS
    NULL  // Sentinel
};
```

#### Step 5: Implement C# Test Functions

**File:** `Program.cs`

```csharp
// Around line 240 (after existing test functions)

static async Task TestFloat32()
{
    await RunRoundtrip<Float32Topic>(
        "AtomicTests::Float32Topic", 
        700, // Unique base seed
        (s) => { 
            var msg = new Float32Topic(); 
            msg.Id = s; 
            msg.Value = (float)(s * 3.14159f); // Must match C algorithm
            return msg; 
        },
        (msg, s) => {
            if (msg.Id != s) return false;
            float expected = (float)(s * 3.14159f);
            return Math.Abs(msg.Value - expected) < 0.0001f;
        }
    );
}

static async Task TestFloat32Appendable()
{
    await RunRoundtrip<Float32TopicAppendable>(
        "AtomicTests::Float32TopicAppendable", 
        1700, // Appendable seed (1000 + base seed)
        (s) => { 
            var msg = new Float32TopicAppendable(); 
            msg.Id = s; 
            msg.Value = (float)(s * 3.14159f);
            return msg; 
        },
        (msg, s) => {
            if (msg.Id != s) return false;
            float expected = (float)(s * 3.14159f);
            return Math.Abs(msg.Value - expected) < 0.0001f;
        }
    );
}
```

**Rules:**
- Generator lambda must match C `generate_*` function
- Validator lambda must match C `validate_*` function
- Use unique base seeds (avoid collisions)
- Appendable tests use base seed + 1000

#### Step 6: Add Test Invocations

**File:** `Program.cs` (in `Main` function)

```csharp
// Around line 60-70
await TestBoolean();
await TestInt32();
await TestStringBounded32();
await TestSequenceInt32();
await TestUnionLongDisc();
await TestFloat32();  // ADD THIS

// Appendable Tests
await TestBooleanAppendable();
await TestInt32Appendable();
await TestStringBounded32Appendable();
await TestSequenceInt32Appendable();
await TestUnionLongDiscAppendable();
await TestFloat32Appendable();  // ADD THIS
```

#### Step 7: Build & Test

**From PowerShell:**

```powershell
# Navigate to project root
cd D:\Work\FastCycloneDdsCsharpBindings

# Rebuild native DLL
cd tests\CsharpToC.Roundtrip.Tests\Native\build
cmake ..
cmake --build . --config Debug

# Rebuild C# project
cd ..\..
dotnet build

# Run tests
.\bin\Debug\net8.0\CsharpToC.Roundtrip.Tests.exe
```

**Expected output:**
```
Testing AtomicTests::Float32Topic...
   [C -> C#] Requesting Native Send...
   [C -> C#] Success
   [CDR Verify] Success (Byte-for-Byte match)
   [C# -> C] Sending...
   [C# -> C] Success
Testing AtomicTests::Float32TopicAppendable...
   [C -> C#] Success
   [CDR Verify] Success (Byte-for-Byte match)
   [C# -> C] Success
```

---

## 4. Dual Topic Pattern (Final + Appendable)

### 4.1 Why Dual Configuration?

**Reason 1: Different Wire Formats**
- Final → XCDR1 encoding (compact, no headers)
- Appendable → XCDR2 encoding (includes DHEADER for extensibility)

**Reason 2: QoS Negotiation**
- Final types request `DDS_DATA_REPRESENTATION_XCDR1`
- Appendable types request `DDS_DATA_REPRESENTATION_XCDR2`
- Mismatch causes discovery failure

**Reason 3: Schema Evolution**
- Appendable supports adding fields (forward/backward compatibility)
- Final does not support evolution
- Need to test both behaviors

### 4.2 Naming Convention

**IDL:**
```idl
struct MyTopic { ... };              // Final (default)
@appendable struct MyTopicAppendable { ... };
```

**C#:**
```csharp
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct MyTopic { ... }

[DdsExtensibility(DdsExtensibilityKind.Appendable)]
public partial struct MyTopicAppendable { ... }
```

**Native C:**
```c
const topic_handler_t my_topic_handler = { ... };
const topic_handler_t my_topic_appendable_handler = { ... };
```

**Test Functions:**
```csharp
await TestMyTopic();           // Final variant
await TestMyTopicAppendable(); // Appendable variant
```

### 4.3 Seed Allocation

**Convention:**
- Final tests: Base seed (100, 200, 300, ...)
- Appendable tests: Base seed + 1000 (1100, 1200, 1300, ...)

**Example:**
```csharp
await RunRoundtrip<BooleanTopic>("...", 100, ...);          // Final
await RunRoundtrip<BooleanTopicAppendable>("...", 1100, ...); // Appendable
```

**Why:** Avoids seed collisions if tests run in parallel.

---

## 5. Implementation Patterns by Type

### 5.1 Primitive Types

**Example: Int64Topic**

**IDL:**
```idl
@topic
struct Int64Topic {
    @key long id;
    long long value;
};
```

**C# Type:**
```csharp
[DdsTopic("Int64Topic")]
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct Int64Topic
{
    [DdsKey]
    public int Id { get; set; }
    public long Value { get; set; }  // long long → long in C#
}
```

**Native Generator:**
```c
static void generate_Int64Topic(void* data, int seed) {
    AtomicTests_Int64Topic* msg = (AtomicTests_Int64Topic*)data;
    msg->id = seed;
    msg->value = (int64_t)(seed * 1000000LL);
}
```

**C# Generator:**
```csharp
(s) => new Int64Topic { 
    Id = s, 
    Value = (long)(s * 1000000L) 
}
```

### 5.2 Strings

**Example: StringUnboundedTopic**

**IDL:**
```idl
@topic
struct StringUnboundedTopic {
    @key long id;
    string value;  // Unbounded
};
```

**C# Type:**
```csharp
[DdsTopic("StringUnboundedTopic")]
[DdsManaged]  // REQUIRED for strings
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct StringUnboundedTopic
{
    [DdsKey]
    public int Id { get; set; }
    public string Value { get; set; }
}
```

**Native Generator:**
```c
static void generate_StringUnboundedTopic(void* data, int seed) {
    AtomicTests_StringUnboundedTopic* msg = (AtomicTests_StringUnboundedTopic*)data;
    msg->id = seed;
    char buffer[128];
    snprintf(buffer, 128, "String_%d", seed);
    msg->value = dds_string_dup(buffer);  // Allocate string
}
```

**Native Validator:**
```c
static int validate_StringUnboundedTopic(void* data, int seed) {
    AtomicTests_StringUnboundedTopic* msg = (AtomicTests_StringUnboundedTopic*)data;
    if (msg->id != seed) return -1;
    char buffer[128];
    snprintf(buffer, 128, "String_%d", seed);
    if (strcmp(msg->value, buffer) != 0) return -1;
    return 0;
}
```

**C# Generator:**
```csharp
(s) => new StringUnboundedTopic { 
    Id = s, 
    Value = $"String_{s}" 
}
```

### 5.3 Sequences

**Example: SequenceFloat64Topic**

**IDL:**
```idl
@topic
struct SequenceFloat64Topic {
    @key long id;
    sequence<double> values;
};
```

**C# Type:**
```csharp
[DdsTopic("SequenceFloat64Topic")]
[DdsManaged]  // REQUIRED for sequences
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct SequenceFloat64Topic
{
    [DdsKey]
    public int Id { get; set; }
    public List<double> Values { get; set; }
}
```

**Native Generator:**
```c
static void generate_SequenceFloat64Topic(void* data, int seed) {
    AtomicTests_SequenceFloat64Topic* msg = (AtomicTests_SequenceFloat64Topic*)data;
    msg->id = seed;
    
    uint32_t len = (seed % 8);  // Variable length based on seed
    msg->values._maximum = len;
    msg->values._length = len;
    msg->values._release = true;
    
    if (len > 0) {
        msg->values._buffer = dds_alloc(sizeof(double) * len);
        for (uint32_t i = 0; i < len; i++) {
            msg->values._buffer[i] = (double)((seed + i) * 2.71828);
        }
    } else {
        msg->values._buffer = NULL;
    }
}
```

**Native Validator:**
```c
static int validate_SequenceFloat64Topic(void* data, int seed) {
    AtomicTests_SequenceFloat64Topic* msg = (AtomicTests_SequenceFloat64Topic*)data;
    if (msg->id != seed) return -1;
    
    uint32_t expected_len = (seed % 8);
    if (msg->values._length != expected_len) return -1;
    
    for (uint32_t i = 0; i < expected_len; i++) {
        double expected = (double)((seed + i) * 2.71828);
        if (fabs(msg->values._buffer[i] - expected) > 0.0001) return -1;
    }
    return 0;
}
```

**C# Generator:**
```csharp
(s) => {
    var msg = new SequenceFloat64Topic();
    msg.Id = s;
    int len = s % 8;
    var list = new List<double>();
    for (int i = 0; i < len; i++) {
        list.Add((double)((s + i) * 2.71828));
    }
    msg.Values = list;
    return msg;
}
```

### 5.4 Arrays

**Example: ArrayFloat64Topic**

**IDL:**
```idl
@topic
struct ArrayFloat64Topic {
    @key long id;
    double values[5];  // Fixed size
};
```

**C# Type:**
```csharp
[DdsTopic("ArrayFloat64Topic")]
[DdsManaged]  // REQUIRED for arrays
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct ArrayFloat64Topic
{
    [DdsKey]
    public int Id { get; set; }
    
    [ArrayLength(5)]
    public double[] Values { get; set; }
}
```

**Native Generator:**
```c
static void generate_ArrayFloat64Topic(void* data, int seed) {
    AtomicTests_ArrayFloat64Topic* msg = (AtomicTests_ArrayFloat64Topic*)data;
    msg->id = seed;
    for (int i = 0; i < 5; i++) {
        msg->values[i] = (double)((seed + i) * 1.618);
    }
}
```

**C# Generator:**
```csharp
(s) => {
    var msg = new ArrayFloat64Topic();
    msg.Id = s;
    msg.Values = new double[5];
    for (int i = 0; i < 5; i++) {
        msg.Values[i] = (double)((s + i) * 1.618);
    }
    return msg;
}
```

### 5.5 Enumerations

**Example: EnumTopic**

**IDL:**
```idl
enum SimpleEnum { 
    FIRST, 
    SECOND, 
    THIRD 
};

@topic
struct EnumTopic {
    @key long id;
    SimpleEnum value;
};
```

**C# Type:**
```csharp
public enum SimpleEnum
{
    FIRST = 0,
    SECOND = 1,
    THIRD = 2
}

[DdsTopic("EnumTopic")]
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct EnumTopic
{
    [DdsKey]
    public int Id { get; set; }
    public SimpleEnum Value { get; set; }
}
```

**Native Generator:**
```c
static void generate_EnumTopic(void* data, int seed) {
    AtomicTests_EnumTopic* msg = (AtomicTests_EnumTopic*)data;
    msg->id = seed;
    msg->value = (AtomicTests_SimpleEnum)(seed % 3);  // 0, 1, or 2
}
```

**C# Generator:**
```csharp
(s) => new EnumTopic { 
    Id = s, 
    Value = (SimpleEnum)(s % 3) 
}
```

### 5.6 Nested Structures

**Example: NestedStructTopic**

**IDL:**
```idl
struct Point2D {
    double x;
    double y;
};

@topic
struct NestedStructTopic {
    @key long id;
    Point2D point;
};
```

**C# Type:**
```csharp
[DdsStruct]
public partial struct Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
}

[DdsTopic("NestedStructTopic")]
[DdsManaged]  // REQUIRED for nested structs
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct NestedStructTopic
{
    [DdsKey]
    public int Id { get; set; }
    public Point2D Point { get; set; }
}
```

**Native Generator:**
```c
static void generate_NestedStructTopic(void* data, int seed) {
    AtomicTests_NestedStructTopic* msg = (AtomicTests_NestedStructTopic*)data;
    msg->id = seed;
    msg->point.x = (double)(seed * 1.0);
    msg->point.y = (double)(seed * 2.0);
}
```

**C# Generator:**
```csharp
(s) => new NestedStructTopic { 
    Id = s, 
    Point = new Point2D { 
        X = (double)(s * 1.0), 
        Y = (double)(s * 2.0) 
    } 
}
```

### 5.7 Unions

**Example: UnionBoolDiscTopic**

**IDL:**
```idl
union BoolUnion switch(boolean) {
    case TRUE: long true_val;
    case FALSE: double false_val;
};

@topic
struct UnionBoolDiscTopic {
    @key long id;
    BoolUnion data;
};
```

**C# Type:**
```csharp
[DdsUnion]
[DdsStruct]  // Required workaround
[DdsManaged]
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct BoolUnion
{
    [DdsDiscriminator]
    public bool _d { get; set; }

    [DdsCase(true)]
    public int True_val { get; set; }

    [DdsCase(false)]
    public double False_val { get; set; }
}

[DdsTopic("UnionBoolDiscTopic")]
[DdsManaged]
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct UnionBoolDiscTopic
{
    [DdsKey]
    public int Id { get; set; }
    public BoolUnion Data { get; set; }
}
```

**Native Generator:**
```c
static void generate_UnionBoolDiscTopic(void* data, int seed) {
    AtomicTests_UnionBoolDiscTopic* msg = (AtomicTests_UnionBoolDiscTopic*)data;
    msg->id = seed;
    msg->data._d = (seed % 2) == 0;  // Discriminator
    
    if (msg->data._d) {  // TRUE case
        msg->data._u.true_val = seed * 100;
    } else {  // FALSE case
        msg->data._u.false_val = seed * 1.5;
    }
}
```

**C# Generator:**
```csharp
(s) => {
    var msg = new UnionBoolDiscTopic();
    msg.Id = s;
    var u = new BoolUnion();
    u._d = (s % 2) == 0;
    
    if (u._d) {
        u.True_val = s * 100;
    } else {
        u.False_val = s * 1.5;
    }
    msg.Data = u;
    return msg;
}
```

### 5.8 Optional Fields

**Example: OptionalInt32Topic**

**IDL:**
```idl
@topic
struct OptionalInt32Topic {
    @key long id;
    @optional long opt_value;
};
```

**C# Type:**
```csharp
[DdsTopic("OptionalInt32Topic")]
[DdsManaged]  // REQUIRED for optional fields
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct OptionalInt32Topic
{
    [DdsKey]
    public int Id { get; set; }
    
    [DdsOptional]
    public int? Opt_value { get; set; }  // Nullable type
}
```

**Native Generator:**
```c
static void generate_OptionalInt32Topic(void* data, int seed) {
    AtomicTests_OptionalInt32Topic* msg = (AtomicTests_OptionalInt32Topic*)data;
    msg->id = seed;
    
    if ((seed % 2) == 0) {
        // Set optional field
        msg->opt_value = dds_alloc(sizeof(int32_t));
        *msg->opt_value = seed * 10;
    } else {
        // Leave optional field unset
        msg->opt_value = NULL;
    }
}
```

**C# Generator:**
```csharp
(s) => {
    var msg = new OptionalInt32Topic();
    msg.Id = s;
    
    if ((s % 2) == 0) {
        msg.Opt_value = s * 10;  // Set optional
    } else {
        msg.Opt_value = null;     // Leave unset
    }
    return msg;
}
```

### 5.9 Multi-Key Topics

**Example: TwoKeyInt32Topic**

**IDL:**
```idl
@topic
struct TwoKeyInt32Topic {
    @key long key1;
    @key long key2;
    double value;
};
```

**C# Type:**
```csharp
[DdsTopic("TwoKeyInt32Topic")]
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct TwoKeyInt32Topic
{
    [DdsKey]
    public int Key1 { get; set; }
    
    [DdsKey]
    public int Key2 { get; set; }
    
    public double Value { get; set; }
}
```

**Native Generator:**
```c
static void generate_TwoKeyInt32Topic(void* data, int seed) {
    AtomicTests_TwoKeyInt32Topic* msg = (AtomicTests_TwoKeyInt32Topic*)data;
    msg->key1 = seed;
    msg->key2 = seed + 1;
    msg->value = (double)(seed * 0.5);
}
```

**C# Generator:**
```csharp
(s) => new TwoKeyInt32Topic { 
    Key1 = s, 
    Key2 = s + 1, 
    Value = (double)(s * 0.5) 
}
```

---

## 6. Testing Strategy

### 6.1 Incremental Implementation Order

**Recommended order (easiest to hardest):**

1. **Primitives (12 topics)**
   - CharTopic, OctetTopic
   - Int16Topic, UInt16Topic, UInt32Topic
   - Int64Topic, UInt64Topic
   - Float32Topic, Float64Topic
   - StringUnboundedTopic, StringBounded256Topic

2. **Enumerations (2 topics)**
   - EnumTopic, ColorEnumTopic

3. **Arrays (6 topics)**
   - ArrayInt32Topic (fix existing)
   - ArrayFloat64Topic, ArrayStringTopic
   - Array2DInt32Topic, Array3DInt32Topic, ArrayStructTopic

4. **Nested Structures (4 topics)**
   - NestedStructTopic, Nested3DTopic
   - DoublyNestedTopic, ComplexNestedTopic

5. **Remaining Unions (3 topics)**
   - UnionBoolDiscTopic, UnionEnumDiscTopic, UnionShortDiscTopic

6. **Remaining Sequences (10 topics)**
   - BoundedSequenceInt32Topic, SequenceInt64Topic
   - SequenceFloat32Topic, SequenceFloat64Topic
   - SequenceBooleanTopic, SequenceOctetTopic
   - SequenceStringTopic, SequenceEnumTopic
   - SequenceStructTopic, SequenceUnionTopic

7. **Optional Fields (6 topics)**
   - OptionalInt32Topic, OptionalFloat64Topic, OptionalStringTopic
   - OptionalStructTopic, OptionalEnumTopic, MultiOptionalTopic

8. **Composite Keys (4 topics)**
   - TwoKeyInt32Topic, TwoKeyStringTopic
   - ThreeKeyTopic, FourKeyTopic

9. **Nested Keys (3 topics)**
   - NestedKeyTopic, NestedKeyGeoTopic, NestedTripleKeyTopic

10. **Edge Cases (10 topics)**
    - EmptySequenceTopic, LargeSequenceTopic
    - LongStringTopic, UnboundedStringTopic
    - AllPrimitivesAtomicTopic

### 6.2 Batch Size

**Recommended batch size:** 4-6 topics per implementation session

**Why:**
- Small enough to maintain focus
- Large enough to build momentum
- Allows for thorough testing before moving on

**Example Batch:**
```
Batch 1: CharTopic, OctetTopic, Int16Topic, UInt16Topic
Batch 2: Int64Topic, UInt64Topic, Float32Topic, Float64Topic
Batch 3: EnumTopic, ColorEnumTopic, ArrayInt32Topic, ArrayFloat64Topic
```

### 6.3 Testing Workflow

**For each topic:**

1. **Implement** (IDL → C# Type → Native Handler → Test Function)
2. **Build** (CMake + dotnet build)
3. **Run Single Test** (verify it passes)
4. **Run All Tests** (ensure no regressions)
5. **Commit** (one commit per batch of 4-6 topics)

**Example test run:**
```powershell
# Build
cd D:\Work\FastCycloneDdsCsharpBindings\tests\CsharpToC.Roundtrip.Tests\Native\build
cmake --build . --config Debug

cd ..\..
dotnet build

# Run
.\bin\Debug\net8.0\CsharpToC.Roundtrip.Tests.exe

# Look for:
# - "Testing AtomicTests::YourTopic..."
# - "[C -> C#] Success"
# - "[CDR Verify] Success"
# - "[C# -> C] Success"
# - "ALL TESTS PASSED"
```

---

## 7. Common Pitfalls

### 7.1 Seed Mismatch Between C and C#

**Problem:** Generator algorithms differ between C and C#

**Example:**
```c
// Native C
msg->value = seed * 2;
```

```csharp
// C# (WRONG)
msg.Value = seed * 3;  // Different algorithm!
```

**Result:** Validator fails because values don't match

**Fix:** Always use identical algorithms

### 7.2 Missing `[DdsManaged]` Attribute

**Problem:** Forgot `[DdsManaged]` for types with strings/sequences/arrays

**Symptom:** Compilation errors or runtime crashes

**Fix:**
```csharp
[DdsTopic("StringTopic")]
[DdsManaged]  // REQUIRED
[DdsExtensibility(DdsExtensibilityKind.Final)]
public partial struct StringTopic { ... }
```

### 7.3 Incorrect Extensibility

**Problem:** C# extensibility doesn't match IDL

**Example:**
```idl
// IDL
@appendable
struct MyTopic { ... };
```

```csharp
// C# (WRONG)
[DdsExtensibility(DdsExtensibilityKind.Final)]  // Should be Appendable!
public partial struct MyTopic { ... }
```

**Result:** QoS mismatch, discovery fails, "Did not receive data"

**Fix:** Always match extensibility between IDL and C#

### 7.4 Handler Not Registered

**Problem:** Forgot to add handler to `test_registry.c`

**Symptom:** "Topic not found: AtomicTests::MyTopic"

**Fix:**
1. Add `extern const topic_handler_t my_topic_handler;`
2. Add `&my_topic_handler,` to handlers array
3. Rebuild native DLL

### 7.5 Floating Point Comparison

**Problem:** Direct equality check for floats/doubles

**Example:**
```csharp
// WRONG
(msg, s) => msg.Value == (float)(s * 3.14)
```

**Result:** Random failures due to floating point precision

**Fix:**
```csharp
// CORRECT
(msg, s) => Math.Abs(msg.Value - (float)(s * 3.14)) < 0.0001f
```

### 7.6 Sequence Memory Management

**Problem:** Native sequence not properly allocated/freed

**Example:**
```c
// WRONG - memory leak
msg->values._buffer = malloc(sizeof(int32_t) * len);  // Should use dds_alloc
```

**Fix:**
```c
// CORRECT
msg->values._buffer = dds_alloc(sizeof(int32_t) * len);
msg->values._release = true;  // Tell DDS to free on cleanup
```

### 7.7 String Memory Management

**Problem:** Static strings in C instead of allocated

**Example:**
```c
// WRONG - stack address
char buffer[32];
snprintf(buffer, 32, "Str_%d", seed);
msg->value = buffer;  // DANGER: buffer goes out of scope
```

**Fix:**
```c
// CORRECT
char buffer[32];
snprintf(buffer, 32, "Str_%d", seed);
msg->value = dds_string_dup(buffer);  // Allocate on heap
```

---

## 8. Quality Standards

### 8.1 Code Quality

**Every implementation must:**
- ✅ Follow existing naming conventions
- ✅ Include error logging in Native validators
- ✅ Use deterministic seed-based algorithms
- ✅ Match algorithms exactly between C and C#
- ✅ Handle memory correctly (dds_alloc, dds_string_dup)
- ✅ Use epsilon comparison for floating point

### 8.2 Test Quality

**Every test must:**
- ✅ Pass all 3 phases (Receive, Serialize, Send)
- ✅ Use unique base seed (no collisions)
- ✅ Test both Final and Appendable variants
- ✅ Include proper error messages
- ✅ Not hang or timeout (discovery within 1500ms)

### 8.3 Documentation Quality

**When adding topics, update:**
- ✅ This implementation guide (if new pattern discovered)
- ✅ README.md (update counts, status)
- ✅ Task tracker (mark tasks complete)

---

## 9. Quick Reference

### 9.1 File Modification Checklist

For each new topic:
- [ ] `idl/atomic_tests.idl` - Add IDL definitions (Final + Appendable)
- [ ] `AtomicTestsTypes.cs` - Add C# types (Final + Appendable)
- [ ] `Native/atomic_tests_native.c` - Add generators/validators (Final + Appendable)
- [ ] `Native/test_registry.c` - Register handlers (extern + array)
- [ ] `Program.cs` - Add test functions (Final + Appendable)
- [ ] `Program.cs` - Add test invocations in Main()
- [ ] **Rebuild Native** (CMake)
- [ ] **Rebuild C#** (dotnet build)
- [ ] **Run Tests** (verify pass)

### 9.2 Seed Allocation Reference

| Topic Category | Final Seed Range | Appendable Seed Range |
|----------------|------------------|------------------------|
| Boolean | 100 | 1100 |
| Int32 | 200 | 1200 |
| String | 300 | 1300 |
| Array | 400 | 1400 |
| Sequence | 500 | 1500 |
| Union | 600 | 1600 |
| Char | 700 | 1700 |
| Octet | 800 | 1800 |
| Int16 | 900 | 1900 |
| ... | ... | ... |

**Rule:** Increment by 100 for each new topic type

### 9.3 Build Commands

**Rebuild everything:**
```powershell
cd D:\Work\FastCycloneDdsCsharpBindings\tests\CsharpToC.Roundtrip.Tests

# Clean
Remove-Item -Recurse -Force Native\build
Remove-Item -Recurse -Force bin
Remove-Item -Recurse -Force obj

# Build Native
mkdir Native\build
cd Native\build
cmake ..
cmake --build . --config Debug

# Build C#
cd ..\..
dotnet build

# Run
.\bin\Debug\net8.0\CsharpToC.Roundtrip.Tests.exe
```

---

**Document Status:** ✅ Complete  
**Next Steps:** See companion task tracker document  
**Questions?** Refer to [ROUNDTRIP-STATUS-ANALYSIS.md](ROUNDTRIP-STATUS-ANALYSIS.md) for detailed architecture analysis
