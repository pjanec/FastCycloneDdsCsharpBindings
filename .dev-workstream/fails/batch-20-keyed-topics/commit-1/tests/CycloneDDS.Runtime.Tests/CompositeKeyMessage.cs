using CycloneDDS.Schema;

namespace CycloneDDS.Runtime.Tests
{
    [DdsTopic("CompositeKeyTopic")]
    public partial struct CompositeKeyMessage
    {
        [DdsKey, DdsId(0)]
        public int Part1;

        [DdsKey, DdsId(1)]
        public int Part2;

        [DdsKey, DdsId(2)]
        public string Part3;

        [DdsId(3)]
        public double Value;
    }
}
