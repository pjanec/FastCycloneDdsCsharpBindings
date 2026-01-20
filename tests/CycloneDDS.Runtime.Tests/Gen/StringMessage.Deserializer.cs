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
            int endPos = int.MaxValue;
                view.Id = reader.ReadInt32();
                reader.Align(4); view.Msg = reader.ReadString();
            return view;
        }
        public StringMessage ToOwned()
        {
            return this;
        }
    }
}
