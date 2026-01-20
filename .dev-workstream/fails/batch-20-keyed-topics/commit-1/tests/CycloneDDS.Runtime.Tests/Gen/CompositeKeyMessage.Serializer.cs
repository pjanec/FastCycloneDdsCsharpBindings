using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct CompositeKeyMessage
    {
        public int GetSerializedSize(int currentOffset)
        {
            var sizer = new CdrSizer(currentOffset);

            // Struct body
            sizer.Align(4); sizer.WriteInt32(0); // Part1
            sizer.Align(4); sizer.WriteInt32(0); // Part2
            sizer.Align(4); sizer.WriteString(this.Part3); // Part3
            sizer.Align(8); sizer.WriteDouble(0); // Value

            return sizer.GetSizeDelta(currentOffset);
        }

        public void Serialize(ref CdrWriter writer)
        {
            // Struct body
            writer.Align(4); writer.WriteInt32(this.Part1); // Part1
            writer.Align(4); writer.WriteInt32(this.Part2); // Part2
            writer.Align(4); writer.WriteString(this.Part3); // Part3
            writer.Align(8); writer.WriteDouble(this.Value); // Value
        }
    }
}
