using System;
using System.Collections.Generic;
using CycloneDDS.Schema;

namespace AtomicTests
{
    [DdsTopic("BooleanTopic")]
    [DdsExtensibility(DdsExtensibilityKind.Final)]
    public partial struct BooleanTopic
    {
        [DdsKey]
        public int Id { get; set; }
        public bool Value { get; set; }
    }

    [DdsTopic("Int32Topic")]
    [DdsExtensibility(DdsExtensibilityKind.Final)]
    public partial struct Int32Topic
    {
        [DdsKey]
        public int Id { get; set; }
        public int Value { get; set; }
    }

    [DdsTopic("StringBounded32Topic")]
    [DdsManaged]
    [DdsExtensibility(DdsExtensibilityKind.Final)]
    public partial struct StringBounded32Topic
    {
        [DdsKey]
        public int Id { get; set; }
        
        [MaxLength(32)]
        public string Value { get; set; }
    }

    /*
    [DdsTopic("ArrayInt32Topic")]
    [DdsManaged]
    public partial struct ArrayInt32Topic
    {
        [DdsKey]
        public int Id { get; set; }
        
        [ArrayLength(5)]
        public int[] Values { get; set; }
    }
    */

    [DdsTopic("SequenceInt32Topic")]
    [DdsManaged]
    [DdsExtensibility(DdsExtensibilityKind.Final)]
    public partial struct SequenceInt32Topic
    {
        [DdsKey]
        public int Id { get; set; }
        
        // List is fine for managed
        public List<int> Values { get; set; }
    }

    // Union definition
    [DdsUnion]
    [DdsStruct] // Hack
    [DdsManaged]
    [DdsExtensibility(DdsExtensibilityKind.Final)]
    public partial struct SimpleUnion
    {
        [DdsDiscriminator]
        public int _d { get; set; }

        [DdsCase(1)]
        public int Int_value { get; set; }

        [DdsCase(2)]
        public double Double_value { get; set; }

        [DdsCase(3)]
        public string String_value { get; set; }
    }

    [DdsTopic("UnionLongDiscTopic")]
    [DdsManaged]
    [DdsExtensibility(DdsExtensibilityKind.Final)]
    public partial struct UnionLongDiscTopic
    {
        [DdsKey]
        public int Id { get; set; }
        
        public SimpleUnion Data { get; set; }
    }
}
