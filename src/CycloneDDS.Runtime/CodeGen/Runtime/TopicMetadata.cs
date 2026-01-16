using System;
using CycloneDDS.Runtime.Descriptors;

namespace CycloneDDS.CodeGen.Runtime;

public class TopicMetadata
{
    public required string TopicName { get; init; }
    public string TypeName { get; init; } = "";
    public required Type NativeType { get; init; }
    public required Type ManagedType { get; init; }
    public Type? MarshallerType { get; init; }
    public int[] KeyFieldIndices { get; init; } = Array.Empty<int>();

    public string? Descriptor { get; init; } // IDL descriptor string (optional)
    public DescriptorData? TopicDescriptor { get; init; }

    public IntPtr BuiltinTopicHandle { get; init; } = IntPtr.Zero;
}
