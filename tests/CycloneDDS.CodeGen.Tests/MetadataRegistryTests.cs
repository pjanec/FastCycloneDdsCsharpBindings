using Xunit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;
using CycloneDDS.CodeGen.Emitters;

namespace CycloneDDS.CodeGen.Tests;

public class MetadataRegistryTests
{
    private TypeDeclarationSyntax ParseType(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        return tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();
    }

    [Fact]
    public void MetadataRegistry_ContainsAllTopics()
    {
        var topic1Code = @"
namespace Test
{
    [DdsTopic(""Topic1"")]
    public partial class TestTopic1
    {
        public int A;
    }
}";
        var topic2Code = @"
namespace Test
{
    [DdsTopic(""Topic2"")]
    public partial class TestTopic2
    {
        public int B;
    }
}";
        var type1 = ParseType(topic1Code);
        var type2 = ParseType(topic2Code);
        
        var emitter = new MetadataRegistryEmitter();
        var topics = new List<(TypeDeclarationSyntax, string)>
        {
            (type1, "Topic1"),
            (type2, "Topic2")
        };
        var registryCode = emitter.GenerateRegistry(topics, "Test");

        Assert.Contains("{ \"Topic1\", new TopicMetadata", registryCode);
        Assert.Contains("{ \"Topic2\", new TopicMetadata", registryCode);
    }

    [Fact]
    public void MetadataRegistry_GetMetadata_HasMethod()
    {
        var topicCode = @"
namespace Test
{
    [DdsTopic(""Topic1"")]
    public partial class TestTopic1
    {
        public int A;
    }
}";
        var type = ParseType(topicCode);
        var emitter = new MetadataRegistryEmitter();
        var topics = new List<(TypeDeclarationSyntax, string)> { (type, "Topic1") };
        var registryCode = emitter.GenerateRegistry(topics, "Test");

        Assert.Contains("public static TopicMetadata GetMetadata(string topicName)", registryCode);
    }

    [Fact]
    public void MetadataRegistry_KeyFieldIndices_Empty()
    {
        var topicCode = @"
namespace Test
{
    [DdsTopic(""Topic1"")]
    public partial class TestTopic1
    {
        public int A;
    }
}";
        var type = ParseType(topicCode);
        var emitter = new MetadataRegistryEmitter();
        var topics = new List<(TypeDeclarationSyntax, string)> { (type, "Topic1") };
        var registryCode = emitter.GenerateRegistry(topics, "Test");

        Assert.Contains("KeyFieldIndices = Array.Empty<int>()", registryCode);
    }

    [Fact]
    public void MetadataRegistry_KeyFieldIndices_Correct()
    {
        var topicCode = @"
namespace Test
{
    using System;
    [AttributeUsage(AttributeTargets.Field)]
    public class DdsKeyAttribute : Attribute {}

    [DdsTopic(""Topic1"")]
    public partial class TestTopic1
    {
        [DdsKey]
        public int Id;
        public string Name;
        [DdsKey]
        public int GroupId;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(topicCode);
        var type = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(t => t.Identifier.Text == "TestTopic1");
        var emitter = new MetadataRegistryEmitter();
        var topics = new List<(TypeDeclarationSyntax, string)> { (type, "Topic1") };
        var registryCode = emitter.GenerateRegistry(topics, "Test");

        // Id is field 0, GroupId is field 2
        Assert.Contains("KeyFieldIndices = new[] { 0, 2 }", registryCode);
    }

    [Fact]
    public void MetadataRegistry_TryGetMetadata_HasMethod()
    {
        var topicCode = @"
namespace Test
{
    [DdsTopic(""Topic1"")]
    public partial class TestTopic1
    {
        public int A;
    }
}";
        var type = ParseType(topicCode);
        var emitter = new MetadataRegistryEmitter();
        var topics = new List<(TypeDeclarationSyntax, string)> { (type, "Topic1") };
        var registryCode = emitter.GenerateRegistry(topics, "Test");

        Assert.Contains("public static bool TryGetMetadata(string topicName, out TopicMetadata? metadata)", registryCode);
    }

    [Fact]
    public void MetadataRegistry_GetAllTopics_HasMethod()
    {
        var topicCode = @"
namespace Test
{
    [DdsTopic(""Topic1"")]
    public partial class TestTopic1
    {
        public int A;
    }
}";
        var type = ParseType(topicCode);
        var emitter = new MetadataRegistryEmitter();
        var topics = new List<(TypeDeclarationSyntax, string)> { (type, "Topic1") };
        var registryCode = emitter.GenerateRegistry(topics, "Test");

        Assert.Contains("public static IEnumerable<TopicMetadata> GetAllTopics()", registryCode);
    }
}
