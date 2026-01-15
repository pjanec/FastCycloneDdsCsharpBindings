using Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using CycloneDDS.CodeGen.Emitters;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System;
using CycloneDDS.CodeGen.Marshalling;

namespace CycloneDDS.CodeGen.Tests;

public class UnionMarshallerTests
{
    private TypeDeclarationSyntax ParseType(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        return tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(t => t.Identifier.Text == "TestUnion");
    }

    [Fact]
    public void Marshaller_MarshalUnion_GeneratesSwitch()
    {
        var csCode = @"
namespace Test
{
    using System;
    [AttributeUsage(AttributeTargets.Field)]
    public class DdsDiscriminatorAttribute : Attribute {}
    [AttributeUsage(AttributeTargets.Field)]
    public class DdsCaseAttribute : Attribute { public DdsCaseAttribute(int v) {} }

    [DdsUnion]
    public partial class TestUnion
    {
        [DdsDiscriminator]
        public int D;
        [DdsCase(1)]
        public float Value;
        [DdsCase(2)]
        public int Count;
    }
}";
        var type = ParseType(csCode);
        var emitter = new MarshallerEmitter();
        var marshallerCode = emitter.GenerateUnionMarshaller(type, "Test");

        // Verify switch on discriminator
        Assert.Contains("switch (managed.D)", marshallerCode);
        Assert.Contains("case 1:", marshallerCode);
        Assert.Contains("case 2:", marshallerCode);
        Assert.Contains("native.Value = managed.Value;", marshallerCode);
        Assert.Contains("native.Count = managed.Count;", marshallerCode);
    }

    [Fact]
    public void Marshaller_UnmarshalUnion_ReadsDiscriminator()
    {
        var csCode = @"
namespace Test
{
    using System;
    [AttributeUsage(AttributeTargets.Field)]
    public class DdsDiscriminatorAttribute : Attribute {}
    [AttributeUsage(AttributeTargets.Field)]
    public class DdsCaseAttribute : Attribute { public DdsCaseAttribute(int v) {} }

    [DdsUnion]
    public partial class TestUnion
    {
        [DdsDiscriminator]
        public int D;
        [DdsCase(1)]
        public float Value;
    }
}";
        var type = ParseType(csCode);
        var emitter = new MarshallerEmitter();
        var marshallerCode = emitter.GenerateUnionMarshaller(type, "Test");

        // Verify discriminator is read
        Assert.Contains("managed.D = native.D;", marshallerCode);
        Assert.Contains("switch (native.D)", marshallerCode);
    }

    [Fact]
    public void Marshaller_Union_ImplementsIMarshaller()
    {
        var csCode = @"
namespace Test
{
    using System;
    [AttributeUsage(AttributeTargets.Field)]
    public class DdsDiscriminatorAttribute : Attribute {}
    [AttributeUsage(AttributeTargets.Field)]
    public class DdsCaseAttribute : Attribute { public DdsCaseAttribute(int v) {} }

    [DdsUnion]
    public partial class TestUnion
    {
        [DdsDiscriminator]
        public int D;
        [DdsCase(1)]
        public float Value;
    }
}";
        var type = ParseType(csCode);
        var emitter = new MarshallerEmitter();
        var marshallerCode = emitter.GenerateUnionMarshaller(type, "Test");

        Assert.Contains("public class TestUnionMarshaller : IMarshaller<TestUnion, TestUnionNative>", marshallerCode);
    }
}
