using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct KeyedTestMessage
    {
        public static KeyedTestMessage Deserialize(ref CdrReader reader)
        {
            var view = new KeyedTestMessage();
            int endPos = int.MaxValue;
                reader.Align(4); view.SensorId = reader.ReadInt32();
                reader.Align(4); view.Value = reader.ReadInt32();
            return view;
        }
        public KeyedTestMessage ToOwned()
        {
            return this;
        }
    }
}
