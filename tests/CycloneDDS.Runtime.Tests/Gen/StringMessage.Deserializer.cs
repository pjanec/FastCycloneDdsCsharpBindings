using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct StringMessage
    {
        public static StringMessage Deserialize(ref CdrReader reader)
        {
            var view = new StringMessage();
            // DHEADER
            reader.Align(4);
            uint dheader = reader.ReadUInt32();
            int endPos = reader.Position + (int)dheader;
            if (reader.Position < endPos)
            {
                reader.Align(4); view.Id = reader.ReadInt32();
            }
            if (reader.Position < endPos)
            {
                reader.Align(4); view.Msg = reader.ReadString();
            }

            if (reader.Position < endPos)
            {
                reader.Seek(endPos);
            }
            return view;
        }
        public StringMessage ToOwned()
        {
            return this;
        }
    }
}
