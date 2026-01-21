using CycloneDDS.Schema;

namespace CycloneDDS.Runtime.Tests.KeyedMessages
{
    [DdsTopic("ProcessAddress")] // Optional, but usually structs don't need topics unless likely to be top level.
    [DdsIdlFile("NestedKeys")]
    [DdsExtensibility(DdsExtensibilityKind.Final)]
    public partial struct ProcessAddress
    {
        [DdsManaged]
        [DdsKey]
        public string StationId { get; set; }

        [DdsManaged]
        [DdsKey]
        public string ProcessId { get; set; }

        [DdsManaged]
        public string SomeOtherId { get; set; }
    }
}
