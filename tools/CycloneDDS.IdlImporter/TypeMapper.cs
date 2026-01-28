using System;
using System.Collections.Generic;
using System.Linq;

namespace CycloneDDS.IdlImporter;

/// <summary>
/// Maps IDL types to C# types with appropriate attributes.
/// Handles primitives, collections, and user-defined types.
/// </summary>
/// <remarks>
/// Implementation planned in IDLIMP-003: Type Mapper Implementation
/// See: tools/CycloneDDS.IdlImporter/IDLImport-TASK-DETAILS.md#idlimp-003
/// </remarks>
public class TypeMapper
{
    /// <summary>
    /// Maps an IDL primitive type name to its C# equivalent.
    /// </summary>
    /// <param name="idlType">IDL type name (e.g., "long", "double", "string")</param>
    /// <returns>C# type name (e.g., "int", "double", "string")</returns>
    public string MapPrimitive(string idlType)
    {
        // TODO: Implement in IDLIMP-003
        // See design document: Type Mapping Rules - Primitive Types
        return idlType switch
        {
            "boolean" => "bool",
            "char" => "byte",
            "octet" => "byte",
            "short" => "short",
            "unsigned short" => "ushort",
            "long" => "int",
            "unsigned long" => "uint",
            "long long" => "long",
            "unsigned long long" => "ulong",
            "float" => "float",
            "double" => "double",
            "string" => "string",
            _ => throw new NotImplementedException($"Type mapping not yet implemented for: {idlType} (IDLIMP-003)")
        };
    }

    /// <summary>
    /// Maps an IDL member (field) to its C# representation with metadata.
    /// </summary>
    /// <param name="member">JSON member definition from idlc output</param>
    /// <returns>Tuple of (C# type, requires [DdsManaged], array length, bound)</returns>
    public (string CsType, bool IsManaged, int ArrayLen, int Bound) MapMember(object member)
    {
        // TODO: Implement in IDLIMP-003
        // Handle:
        // - Primitives
        // - Sequences (bounded/unbounded) → List<T>
        // - Arrays (fixed/dynamic) → T[]
        // - Bounded strings → string with MaxLength
        // - User-defined types
        
        throw new NotImplementedException("Member mapping not yet implemented (IDLIMP-003)");
    }

    /// <summary>
    /// Converts an IDL module path to a C# namespace.
    /// </summary>
    /// <param name="idlModulePath">IDL module path (e.g., "Module::SubModule")</param>
    /// <returns>C# namespace (e.g., "Module.SubModule")</returns>
    public string GetCSharpNamespace(string idlModulePath)
    {
        if (string.IsNullOrEmpty(idlModulePath)) return string.Empty;
        return idlModulePath.Replace("::", ".");
    }

    /// <summary>
    /// Determines if a type requires the [DdsManaged] attribute.
    /// </summary>
    /// <param name="csType">C# type name</param>
    /// <param name="isCollection">Whether the field is a collection</param>
    /// <returns>True if [DdsManaged] should be applied</returns>
    public bool RequiresManagedAttribute(string csType, bool isCollection)
    {
        // Always managed:
        // - string
        // - List<T> (sequences)
        // - T[] (arrays)
        
        if (csType == "string") return true;
        if (csType.StartsWith("List<")) return true;
        if (csType.EndsWith("[]")) return true;
        
        return false;
    }
}
