using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct CompositeKeyMessage
    {
        public static CompositeKeyMessage Deserialize(ref CdrReader reader)
        {
            var view = new CompositeKeyMessage();
            int endPos = int.MaxValue;
                reader.Align(4); view.Part1 = reader.ReadInt32();
                reader.Align(4); view.Part2 = reader.ReadInt32();
                reader.Align(4); view.Part3 = Encoding.UTF8.GetString(reader.ReadStringBytes().ToArray());
                reader.Align(8); view.Value = reader.ReadDouble();
            return view;
        }
        public CompositeKeyMessage ToOwned()
        {
            return this;
        }
    }
}
