# Nested Struct Support Design

**Feature:** Support for custom helper structs nested within DDS topics  
**Attribute:** `[DdsStruct]` for non-topic struct/class types  
**Priority:** HIGH (Required for complex data models)

---

## 1. Overview

### 1.1 Problem Statement

Users need to compose complex data models using custom struct types as fields within Topic structs, or within sequences/collections.

**Current Limitation:**
Only types marked with `[DdsTopic]` are discovered and generate serialization code. Using plain structs causes:
- Missing `Serialize()` methods (compile error in generated code)
- Silent failures (generator doesn't know about the type)

**Example Requirement:**
```csharp
// Helper struct (NOT a topic itself)
public partial struct Point3D
{
    public double X, Y, Z;
}

// Topic using the helper
[DdsTopic("RobotPath")]
public partial struct RobotPath
{
    [DdsKey] public int RobotId;
    public Point3D StartLocation;              // Nested struct
    public BoundedSeq<Point3D> Waypoints;      // Sequence of custom structs
}
```

**Problem:** Without marking `Point3D`, the generator won't create serialization code for it.

### 1.2 Solution

Introduce `[DdsStruct]` attribute to mark helper types that should generate serialization code but are NOT top-level topics.

---

## 2. Design

### 2.1 New Attribute

**File:** `src/CycloneDDS.Schema/Attributes/TypeLevel/DdsStructAttribute.cs`

```csharp
using System;

namespace CycloneDDS.Schema
{
    /// <summary>
    /// Marks a struct or class as a DDS data type that can be nested within Topics.
    /// Triggers code generation for serialization but does not define a Topic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DdsStructAttribute : Attribute
    {
    }
}
```

**Usage:**
```csharp
[DdsStruct]
public partial struct Point3D
{
    public double X;
    public double Y;
    public double Z;
}
```

### 2.2 Discovery Updates

**File:** `tools/CycloneDDS.CodeGen/SchemaDiscovery.cs`

Types to discover:
- `[DdsTopic]` → Topic structs (generate serialization + topic descriptor)
- `[DdsStruct]` → Helper structs (generate serialization only) **← NEW**
- `enum` → Enums (generate IDL enum)

**Implementation:**
```csharp
bool isTopic = HasAttribute(typeSymbol, "CycloneDDS.Schema.DdsTopicAttribute");
bool isStruct = HasAttribute(typeSymbol, "CycloneDDS.Schema.DdsStructAttribute"); // NEW
bool isEnum = typeSymbol.TypeKind == TypeKind.Enum;

if (isTopic || isStruct || isEnum)
{
    var typeInfo = new TypeInfo 
    { 
        Name = typeSymbol.Name,
        IsTopic = isTopic,  // Differentiate for IDL/descriptor generation
        IsStruct = isStruct,
        IsEnum = isEnum
        // ... extract fields ...
    };
    topics.Add(typeInfo);
}
```

### 2.3 Strict Type Validation

**Goal:** Emit clear errors when users forget `[DdsStruct]` on nested types.

**File:** `tools/CycloneDDS.CodeGen/SchemaValidator.cs`

**Strategy:**
1. Build a registry of all discovered types (`[DdsTopic]`, `[DdsStruct]`, `enum`)
2. For each field in each type, validate the field's type:
   - Is it a primitive? (int, double, etc.) → OK
   - Is it a built-in wrapper? (FixedString32, Guid, etc.) → OK
   - Is it a collection? (BoundedSeq<T>, List<T>) → Recursively validate T
   - Is it a custom type? → **Must be in the registry**, else ERROR

**Implementation:**
```csharp
public class SchemaValidator
{
    private readonly HashSet<string> _knownTypeNames;

    public SchemaValidator(IEnumerable<TypeInfo> discoveredTypes)
    {
        // Registry of all valid types
        _knownTypeNames = new HashSet<string>(
            discoveredTypes.Select(t => t.FullName)
        );
    }

    public ValidationResult Validate(TypeInfo type)
    {
        var errors = new List<string>();

        foreach (var field in type.Fields)
        {
            ValidateFieldType(field, type.Name, errors);
        }

        return new ValidationResult(errors);
    }

    private void ValidateFieldType(FieldInfo field, string containerName, List<string> errors)
    {
        string typeName = field.TypeName;

        // Handle Nullable
        if (typeName.EndsWith("?"))
            typeName = typeName.TrimEnd('?');

        // Built-in primitives
        if (TypeMapper.IsPrimitive(typeName)) return;

        // Special wrappers
        if (typeName.Contains("FixedString")) return;
        if (typeName == "Guid" || typeName == "DateTime") return;

        // Collections
        if (typeName.StartsWith("BoundedSeq<") || typeName.StartsWith("List<"))
        {
            string innerType = ExtractGenericArgument(typeName);
            
            // Recursively validate inner type
            if (!IsValidUserType(innerType) && !TypeMapper.IsPrimitive(innerType) && innerType != "string")
            {
                errors.Add($"Field '{containerName}.{field.Name}' uses collection of type '{innerType}', " +
                           $"which is not a valid DDS type. Mark '{innerType}' with [DdsStruct] or [DdsTopic].");
            }
            return;
        }

        // Managed strings
        if (typeName == "string")
        {
            if (!field.HasAttribute("DdsManaged"))
                errors.Add($"Field '{containerName}.{field.Name}' is a string but missing [DdsManaged].");
            return;
        }

        // User-defined types - THE STRICT CHECK
        if (!IsValidUserType(typeName))
        {
            errors.Add($"Field '{containerName}.{field.Name}' uses type '{typeName}', " +
                       $"which is not a valid DDS type. " +
                       $"Did you forget to add [DdsStruct] or [DdsTopic] to '{typeName}'?");
        }
    }

    private bool IsValidUserType(string typeName)
    {
        return _knownTypeNames.Contains(typeName);
    }

    private string ExtractGenericArgument(string typeName)
    {
        int start = typeName.IndexOf('<') + 1;
        int end = typeName.LastIndexOf('>');
        if (start > 0 && end > start)
        {
            return typeName.Substring(start, end - start).Trim();
        }
        return typeName;
    }
}
```

**Example Errors:**
```
Field 'RobotPath.StartLocation' uses type 'Point3D', which is not a valid DDS type. 
Did you forget to add [DdsStruct] or [DdsTopic] to 'Point3D'?

Field 'RobotPath.Waypoints' uses collection of type 'Point3D', which is not a valid DDS type. 
Mark 'Point3D' with [DdsStruct] or [DdsTopic].
```

---

## 3. Integration with CodeGenerator

**File:** `tools/CycloneDDS.CodeGen/CodeGenerator.cs`

```csharp
public void Generate(string sourceDir, string outputDir)
{
    // 1. Discover all types
    var types = _discovery.DiscoverTopics(sourceDir);
    
    // 2. Validate ALL types with strict checking
    var validator = new SchemaValidator(types);  // Pass all types to build registry
    
    foreach (var type in types)
    {
        var result = validator.Validate(type);
        if (!result.IsValid)
        {
            // Emit clear errors and stop generation for this type
            foreach (var err in result.Errors)
            {
                Console.Error.WriteLine($"ERROR: {err}");
            }
            continue; // Don't generate for invalid types
        }
        
        // 3. Generate serialization for both [DdsTopic] and [DdsStruct]
        _serializerEmitter.Emit(type, outputDir);
        
        // 4. Generate topic descriptor ONLY for [DdsTopic]
        if (type.IsTopic)
        {
            _descriptorEmitter.Emit(type, outputDir);
        }
    }
}
```

---

## 4. IDL Generation

**File:** `tools/CycloneDDS.CodeGen/IdlEmitter.cs`

**Behavior:**
- `[DdsTopic]` types → Generate as `struct` with topic-level annotations (`@appendable`, etc.)
- `[DdsStruct]` types → Generate as plain `struct` (no topic semantics)
- Both emit field attributes (`@key`, `@id`, etc.)

**Example Output:**
```idl
// Helper struct (from [DdsStruct])
@appendable
struct Point3D {
    @id(0) double X;
    @id(1) double Y;
    @id(2) double Z;
};

// Topic (from [DdsTopic])
@appendable
struct RobotPath {
    @key @id(0) long RobotId;
    @id(1) Point3D StartLocation;
    @id(2) sequence<Point3D> Waypoints;
};
```

---

## 5. Performance Implications

**Zero-Allocation Maintained:**
1. `Point3D.Serialize(ref CdrWriter)` is generated
2. `RobotPath.Serialize()` calls `this.StartLocation.Serialize(ref writer)`
3. JIT inlines struct method calls → tight loop
4. No boxing (struct stays on stack)

**Nested Depth:**
Supports arbitrary nesting:
```csharp
[DdsStruct] struct A { public B field; }
[DdsStruct] struct B { public C field; }
[DdsStruct] struct C { public int field; }
```

---

## 6. Testing Requirements

### 6.1 Unit Tests (Minimum 5)

**SchemaDiscovery Tests:**
1. **Discovery_DdsStruct_Found**
   - Define struct with `[DdsStruct]`
   - Success: Type discovered by generator

**SchemaValidator Tests:**
2. **Validation_UnknownStruct_EmitsError**
   - Struct A uses Struct B (no attribute on B)
   - Success: Error emitted with helpful message

3. **Validation_KnownStruct_Passes**
   - Struct A uses Struct B (B has `[DdsStruct]`)
   - Success: No errors

4. **Validation_NestedSequence_UnknownType_EmitsError**
   - Field type: `BoundedSeq<UnknownStruct>`
   - Success: Error emitted

5. **Validation_NestedSequence_KnownType_Passes**
   - Field type: `BoundedSeq<KnownStruct>` (marked with `[DdsStruct]`)
   - Success: No errors

### 6.2 Integration Tests (Minimum 2)

6. **CodeGen_NestedStruct_Compiles**
   - Define Topic with nested `[DdsStruct]` field
   - Generate code
   - Success: Generated code compiles without errors

7. **Roundtrip_NestedStruct_Preserves**
   - Topic with `Point3D` field
   - Write data with X=1.0, Y=2.0, Z=3.0
   - Read back
   - Success: Values match exactly

---

## 7. Task Definition

**Task ID:** FCDC-S023  
**Stage:** 2 (Code Generation) - **Enhancement**  
**Priority:** HIGH  
**Effort:** 2-3 days  
**Dependencies:** Stage 2 complete (existing generator infrastructure)

**Deliverables:**
- `src/CycloneDDS.Schema/Attributes/TypeLevel/DdsStructAttribute.cs` (NEW)
- Updated `tools/CycloneDDS.CodeGen/SchemaDiscovery.cs`
- Updated `tools/CycloneDDS.CodeGen/SchemaValidator.cs`
- Updated `tools/CycloneDDS.CodeGen/CodeGenerator.cs`
- 7 tests (5 unit + 2 integration)

---

**Status:** Ready for Implementation  
**Impact:** HIGH (enables complex data models)
