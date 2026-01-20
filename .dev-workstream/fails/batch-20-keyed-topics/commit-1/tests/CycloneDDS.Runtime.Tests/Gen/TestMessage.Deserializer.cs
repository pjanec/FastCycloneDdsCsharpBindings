using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct TestMessage
    {
        public static TestMessage Deserialize(ref CdrReader reader)
        {
            var view = new TestMessage();
            int endPos = int.MaxValue;
                reader.Align(4); view.Id = reader.ReadInt32();
                reader.Align(4); view.Value = reader.ReadInt32();
            return view;
        }
        public TestMessage ToOwned()
        {
            return this;
        }
    }
}
