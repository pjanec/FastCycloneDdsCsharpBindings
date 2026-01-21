using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;

namespace CycloneDDS.Runtime.Tests.KeyedMessages
{
    public partial struct NestedStructKeyMessage
    {
        public int GetSerializedSize(int currentOffset)
        {
            return GetSerializedSize(currentOffset, false);
        }

        public int GetSerializedSize(int currentOffset, bool isXcdr2 = false)
        {
            var sizer = new CdrSizer(currentOffset);

            // DHEADER
            sizer.Align(4);
            sizer.WriteUInt32(0);

            // Struct body
            sizer.Align(4); sizer.WriteUInt32(0); // FrameId
            sizer.Skip(this.ProcessAddr.GetSerializedSize(sizer.Position, isXcdr2)); // ProcessAddr
            sizer.Align(8); sizer.WriteDouble(0); // TimeStamp

            return sizer.GetSizeDelta(currentOffset);
        }

        public void Serialize(ref CdrWriter writer)
        {
            // DHEADER
            writer.Align(4);
            int dheaderPos = writer.Position;
            writer.WriteUInt32(0);
            int bodyStart = writer.Position;
            // Struct body
            writer.Align(4); writer.WriteUInt32(this.FrameId); // FrameId
            this.ProcessAddr.Serialize(ref writer); // ProcessAddr
            writer.Align(8); writer.WriteDouble(this.TimeStamp); // TimeStamp
            int bodyLen = writer.Position - bodyStart;
            writer.WriteUInt32At(dheaderPos, (uint)bodyLen);
        }
    }
}
