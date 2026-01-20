using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct KeyedTestMessage
    {
        public int GetSerializedSize(int currentOffset)
        {
            var sizer = new CdrSizer(currentOffset);

            // Struct body
            sizer.Align(4); sizer.WriteInt32(0); // SensorId
            sizer.Align(4); sizer.WriteInt32(0); // Value

            return sizer.GetSizeDelta(currentOffset);
        }

        public void Serialize(ref CdrWriter writer)
        {
            // Struct body
            writer.Align(4); writer.WriteInt32(this.SensorId); // SensorId
            writer.Align(4); writer.WriteInt32(this.Value); // Value
        }
    }
}
