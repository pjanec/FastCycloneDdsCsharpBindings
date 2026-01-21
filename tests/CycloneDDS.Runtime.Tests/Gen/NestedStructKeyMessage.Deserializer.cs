using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace CycloneDDS.Runtime.Tests.KeyedMessages
{
    public partial struct NestedStructKeyMessage
    {
        public static NestedStructKeyMessage Deserialize(ref CdrReader reader)
        {
            var view = new NestedStructKeyMessage();
            // DHEADER
            reader.Align(4);
            uint dheader = reader.ReadUInt32();
            int endPos = reader.Position + (int)dheader;
            if (reader.Position < endPos)
            {
                reader.Align(4); view.FrameId = reader.ReadUInt32();
            }
            if (reader.Position < endPos)
            {
                view.ProcessAddr = CycloneDDS.Runtime.Tests.KeyedMessages.ProcessAddress.Deserialize(ref reader);
            }
            if (reader.Position < endPos)
            {
                reader.Align(8); view.TimeStamp = reader.ReadDouble();
            }

            if (reader.Position < endPos)
            {
                reader.Seek(endPos);
            }
            return view;
        }
        public NestedStructKeyMessage ToOwned()
        {
            return this;
        }
    }
}
