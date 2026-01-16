using System.Collections.Generic;
using Xunit;
using CycloneDDS.CodeGen;

namespace CycloneDDS.CodeGen.Tests
{
    public class SchemaValidatorTests
    {
        [Fact]
        public void ValidStruct_WithPrimitives_Passes()
        {
            var type = new TypeInfo
            {
                Name = "TestStruct",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "F1", TypeName = "int" },
                    new FieldInfo { Name = "F2", TypeName = "double" }
                }
            };

            var validator = new SchemaValidator();
            var result = validator.Validate(type);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Struct_WithInvalidFieldType_Fails()
        {
            var type = new TypeInfo
            {
                Name = "TestStruct",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "F1", TypeName = "System.Random" }
                }
            };

            var validator = new SchemaValidator();
            var result = validator.Validate(type);

            Assert.False(result.IsValid);
            Assert.Contains("Invalid field type", result.Errors[0]);
        }

        [Fact]
        public void CircularDependency_Detected()
        {
            var typeA = new TypeInfo { Name = "A", Namespace = "Test" };
            var typeB = new TypeInfo { Name = "B", Namespace = "Test" };

            typeA.Fields.Add(new FieldInfo { Name = "FieldB", TypeName = "B", Type = typeB });
            typeB.Fields.Add(new FieldInfo { Name = "FieldA", TypeName = "A", Type = typeA });

            var validator = new SchemaValidator();
            var result = validator.Validate(typeA);

            Assert.False(result.IsValid);
            Assert.Contains("Circular dependency", result.Errors[0]);
        }

        [Fact]
        public void Union_WithoutDiscriminator_Fails()
        {
            var type = new TypeInfo
            {
                Name = "TestUnion",
                Attributes = new List<AttributeInfo> { new AttributeInfo { Name = "DdsUnion" } },
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "Case1", TypeName = "int" }
                }
            };

            var validator = new SchemaValidator();
            var result = validator.Validate(type);

            Assert.False(result.IsValid);
            Assert.Contains("must have exactly one [DdsDiscriminator]", result.Errors[0]);
        }

        [Fact]
        public void Union_WithDuplicateCaseValues_Fails()
        {
            var type = new TypeInfo
            {
                Name = "TestUnion",
                Attributes = new List<AttributeInfo> { new AttributeInfo { Name = "DdsUnion" } },
                Fields = new List<FieldInfo>
                {
                    new FieldInfo 
                    { 
                        Name = "Disc", 
                        TypeName = "int", 
                        Attributes = new List<AttributeInfo> { new AttributeInfo { Name = "DdsDiscriminator" } } 
                    },
                    new FieldInfo 
                    { 
                        Name = "Case1", 
                        TypeName = "int",
                        Attributes = new List<AttributeInfo> 
                        { 
                            new AttributeInfo { Name = "DdsCase", Arguments = new List<object> { 1 } } 
                        } 
                    },
                    new FieldInfo 
                    { 
                        Name = "Case2", 
                        TypeName = "int",
                        Attributes = new List<AttributeInfo> 
                        { 
                            new AttributeInfo { Name = "DdsCase", Arguments = new List<object> { 1 } } 
                        } 
                    }
                }
            };

            var validator = new SchemaValidator();
            var result = validator.Validate(type);

            Assert.False(result.IsValid);
            Assert.Contains("Duplicate case value", result.Errors[0]);
        }

        [Fact]
        public void StringField_WithoutDdsManaged_Fails()
        {
            var type = new TypeInfo
            {
                Name = "TestStruct",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "S", TypeName = "string" }
                }
            };

            var validator = new SchemaValidator();
            var result = validator.Validate(type);

            Assert.False(result.IsValid);
            Assert.Contains("Invalid field type", result.Errors[0]);
        }

        [Fact]
        public void StringField_WithDdsManaged_Passes()
        {
            var type = new TypeInfo
            {
                Name = "TestStruct",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo 
                    { 
                        Name = "S", 
                        TypeName = "string",
                        Attributes = new List<AttributeInfo> { new AttributeInfo { Name = "DdsManaged" } }
                    }
                }
            };

            var validator = new SchemaValidator();
            var result = validator.Validate(type);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void FixedString_Passes()
        {
            var type = new TypeInfo
            {
                Name = "TestStruct",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "FS", TypeName = "CycloneDDS.Schema.FixedString32" }
                }
            };

            var validator = new SchemaValidator();
            var result = validator.Validate(type);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void BoundedSeq_Passes()
        {
            var type = new TypeInfo
            {
                Name = "TestStruct",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "Seq", TypeName = "CycloneDDS.Schema.BoundedSeq<int>" }
                }
            };

            var validator = new SchemaValidator();
            var result = validator.Validate(type);

            Assert.True(result.IsValid);
        }
    }
}
