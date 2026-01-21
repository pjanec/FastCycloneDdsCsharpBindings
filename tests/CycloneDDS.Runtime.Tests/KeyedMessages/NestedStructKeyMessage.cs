using CycloneDDS.Schema;

namespace CycloneDDS.Runtime.Tests.KeyedMessages
{
    [DdsTopic("NestedStructKeyMessage")]
    [DdsIdlFile("NestedKeys")]
    public partial struct NestedStructKeyMessage
    {
        [DdsKey]
        public uint FrameId { get; set; }

        [DdsKey]
        public ProcessAddress ProcessAddr { get; set; }

        public double TimeStamp { get; set; }
    }
}
