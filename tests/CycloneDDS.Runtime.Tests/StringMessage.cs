using CycloneDDS.Schema;

namespace CycloneDDS.Runtime.Tests
{
    [DdsTopic("StringMessageTopic")]
    [DdsExtensibility(DdsExtensibilityKind.Appendable)]
    public partial struct StringMessage
    {
        public int Id;
        [DdsManaged]
        public string Msg;
    }
}