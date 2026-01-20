using CycloneDDS.Schema;

namespace CycloneDDS.Runtime.Tests
{
    [DdsTopic("KeyedTestTopic")]
    public partial struct KeyedTestMessage
    {
        [DdsKey, DdsId(0)]
        public int SensorId;   // KEY FIELD - Identifies instance
        
        [DdsId(1)]
        public int Value;      // Data field
    }
}
