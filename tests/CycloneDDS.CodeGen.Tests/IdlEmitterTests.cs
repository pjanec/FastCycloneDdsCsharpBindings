using System.Collections.Generic;
using Xunit;
using CycloneDDS.CodeGen;

namespace CycloneDDS.CodeGen.Tests
{
    public class IdlEmitterTests
    {
        [Fact]
        public void SimpleStruct_EmitsCorrectIdl()
        {
            var type = new TypeInfo
            {
                Name = "SensorData",
                Namespace = "MyApp",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "Id", TypeName = "int" },
                    new FieldInfo { Name = "Value", TypeName = "double" }
                }
            };
            
            var emitter = new IdlEmitter();
            string idl = emitter.EmitIdl(type);
            
            Assert.Contains("module MyApp", idl);
            Assert.Contains("@appendable", idl);
            Assert.Contains("struct SensorData", idl);
            Assert.Contains("int32 id;", idl);
            Assert.Contains("double value;", idl);
        }

        [Fact]
        public void Struct_WithKeyField_EmitsKeyAnnotation()
        {
            var type = new TypeInfo
            {
                Name = "KeyData",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo 
                    { 
                        Name = "Id", 
                        TypeName = "int",
                        Attributes = new List<AttributeInfo> { new AttributeInfo { Name = "DdsKey" } }
                    }
                }
            };
            
            var emitter = new IdlEmitter();
            string idl = emitter.EmitIdl(type);
            
            Assert.Contains("@key int32 id;", idl);
        }

        [Fact]
        public void FixedString_EmitsCharArray()
        {
            var type = new TypeInfo
            {
                Name = "StringData",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "Msg", TypeName = "CycloneDDS.Schema.FixedString32" }
                }
            };
            
            var emitter = new IdlEmitter();
            string idl = emitter.EmitIdl(type);
            
            Assert.Contains("char msg[32];", idl);
        }

        [Fact]
        public void BoundedSeq_EmitsSequence()
        {
            var type = new TypeInfo
            {
                Name = "SeqData",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "Values", TypeName = "CycloneDDS.Schema.BoundedSeq<int>" }
                }
            };
            
            var emitter = new IdlEmitter();
            string idl = emitter.EmitIdl(type);
            
            Assert.Contains("sequence<int32> values;", idl);
        }

        [Fact]
        public void Union_EmitsSwitchSyntax()
        {
            var type = new TypeInfo
            {
                Name = "Command",
                Attributes = new List<AttributeInfo> { new AttributeInfo { Name = "DdsUnion" } },
                Fields = new List<FieldInfo>
                {
                    new FieldInfo 
                    { 
                        Name = "Kind", 
                        TypeName = "int",
                        Attributes = new List<AttributeInfo> { new AttributeInfo { Name = "DdsDiscriminator" } }
                    },
                    new FieldInfo 
                    { 
                        Name = "Move", 
                        TypeName = "MoveData",
                        Attributes = new List<AttributeInfo> 
                        { 
                            new AttributeInfo { Name = "DdsCase", Arguments = new List<object> { 1 } } 
                        }
                    }
                }
            };
            
            var emitter = new IdlEmitter();
            string idl = emitter.EmitIdl(type);
            
            Assert.Contains("union Command switch (int32) {", idl);
            Assert.Contains("case 1:", idl);
            Assert.Contains("MoveData move;", idl);
        }

        [Fact]
        public void OptionalField_EmitsOptionalAnnotation()
        {
            var type = new TypeInfo
            {
                Name = "OptData",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo 
                    { 
                        Name = "Val", 
                        TypeName = "int",
                        Attributes = new List<AttributeInfo> { new AttributeInfo { Name = "DdsOptional" } }
                    }
                }
            };
            
            var emitter = new IdlEmitter();
            string idl = emitter.EmitIdl(type);
            
            Assert.Contains("@optional int32 val;", idl);
        }

        [Fact]
        public void Enum_EmitsCorrectIdl()
        {
            var type = new TypeInfo
            {
                Name = "Color",
                IsEnum = true,
                EnumMembers = new List<string> { "Red", "Green", "Blue" }
            };
            
            var emitter = new IdlEmitter();
            string idl = emitter.EmitIdl(type);
            
            Assert.Contains("enum Color {", idl);
            Assert.Contains("Red,", idl);
            Assert.Contains("Green,", idl);
            Assert.Contains("Blue", idl);
        }

        [Fact]
        public void Struct_WithDependency_EmitsInclude()
        {
            var depType = new TypeInfo { Name = "OtherStruct", IsTopic = true };
            var type = new TypeInfo
            {
                Name = "MyStruct",
                Fields = new List<FieldInfo>
                {
                    new FieldInfo { Name = "Other", TypeName = "OtherStruct", Type = depType }
                }
            };
            
            var emitter = new IdlEmitter();
            string idl = emitter.EmitIdl(type);
            
            Assert.Contains("#include \"OtherStruct.idl\"", idl);
        }
    }
}
