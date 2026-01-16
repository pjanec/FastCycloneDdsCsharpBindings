using System;
using System.Collections.Generic;
using System.Linq;

namespace CycloneDDS.CodeGen
{
    public class SchemaValidator
    {
        public ValidationResult Validate(TypeInfo type)
        {
            var errors = new List<string>();
            
            // 1. Check for circular dependencies
            if (HasCircularDependency(type))
                errors.Add($"Circular dependency detected in {type.FullName}");
            
            // 2. Validate field types
            foreach (var field in type.Fields)
            {
                if (!IsValidFieldType(field))
                    errors.Add($"Invalid field type: {field.TypeName} in {type.FullName}.{field.Name}");
            }
            
            // 3. Check union structure (if [DdsUnion])
            if (type.HasAttribute("DdsUnion"))
            {
                ValidateUnion(type, errors);
            }
            
            return new ValidationResult(errors);
        }

        private bool IsValidFieldType(FieldInfo field)
        {
            // If it's a resolved nested type, it's valid (assuming the nested type itself is valid, which is checked separately)
            if (field.Type != null) return true;

            var typeName = field.TypeName;
            
            // Primitives
            if (IsPrimitive(typeName)) return true;
            
            // String (must be managed)
            if (typeName == "string" || typeName == "System.String")
            {
                return field.HasAttribute("DdsManaged");
            }

            // Fixed Strings
            if (typeName.Contains("FixedString32") || 
                typeName.Contains("FixedString64") ||
                typeName.Contains("FixedString128") ||
                typeName.Contains("FixedString256")) return true;

            // BoundedSeq
            if (typeName.Contains("BoundedSeq<")) return true;

            // Managed types
            if (field.HasAttribute("DdsManaged"))
            {
                if (typeName == "string" || typeName == "System.String") return true;
                if (typeName.Contains("List<")) return true;
            }

            // Enums or other user types (not system types)
            if (!typeName.StartsWith("System.") && !typeName.StartsWith("List<")) return true;

            return false;
        }

        private bool IsPrimitive(string typeName)
        {
            return typeName switch
            {
                "byte" or "System.Byte" => true,
                "sbyte" or "System.SByte" => true,
                "short" or "System.Int16" => true,
                "ushort" or "System.UInt16" => true,
                "int" or "System.Int32" => true,
                "uint" or "System.UInt32" => true,
                "long" or "System.Int64" => true,
                "ulong" or "System.UInt64" => true,
                "float" or "System.Single" => true,
                "double" or "System.Double" => true,
                "bool" or "System.Boolean" => true,
                "char" or "System.Char" => true,
                _ => false
            };
        }
        
        private void ValidateUnion(TypeInfo type, List<string> errors)
        {
            // Must have exactly one [DdsDiscriminator]
            var discriminators = type.Fields.Where(f => f.HasAttribute("DdsDiscriminator")).ToList();
            if (discriminators.Count != 1)
                errors.Add($"Union {type.FullName} must have exactly one [DdsDiscriminator]");
            
            // All [DdsCase] values must be unique
            var caseValues = new HashSet<int>();
            foreach (var field in type.Fields.Where(f => f.HasAttribute("DdsCase")))
            {
                var attr = field.GetAttribute("DdsCase");
                if (attr != null)
                {
                    foreach (var c in attr.CaseValues)
                    {
                        if (!caseValues.Add(c))
                            errors.Add($"Duplicate case value {c} in union {type.FullName}");
                    }
                }
            }
        }

        private bool HasCircularDependency(TypeInfo type, HashSet<string>? visited = null)
        {
            visited ??= new HashSet<string>();
            
            if (!visited.Add(type.FullName))
                return true; // Cycle detected
            
            foreach (var field in type.Fields)
            {
                if (field.Type != null)
                {
                    if (HasCircularDependency(field.Type, new HashSet<string>(visited)))
                        return true;
                }
            }
            
            return false;
        }
    }
}
