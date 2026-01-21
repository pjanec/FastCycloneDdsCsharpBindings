using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;

namespace CycloneDDS.Runtime.Tests.KeyedMessages
{
    public partial struct ProcessAddress
    {
        public int GetSerializedSize(int currentOffset)
        {
            return GetSerializedSize(currentOffset, false);
        }

        public int GetSerializedSize(int currentOffset, bool isXcdr2 = false)
        {
            var sizer = new CdrSizer(currentOffset);

            // Struct body
            sizer.Align(4); sizer.WriteString(this.StationId, isXcdr2); // StationId
            sizer.Align(4); sizer.WriteString(this.ProcessId, isXcdr2); // ProcessId
            sizer.Align(4); sizer.WriteString(this.SomeOtherId, isXcdr2); // SomeOtherId

            return sizer.GetSizeDelta(currentOffset);
        }

        public void Serialize(ref CdrWriter writer)
        {
            // Struct body
            writer.Align(4); writer.WriteString(this.StationId, writer.IsXcdr2); // StationId
            writer.Align(4); writer.WriteString(this.ProcessId, writer.IsXcdr2); // ProcessId
            writer.Align(4); writer.WriteString(this.SomeOtherId, writer.IsXcdr2); // SomeOtherId
        }
    }
}
