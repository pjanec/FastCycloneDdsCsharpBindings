using CycloneDDS.Core;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace CycloneDDS.Runtime.Tests.KeyedMessages
{
    public partial struct ProcessAddress
    {
        public static ProcessAddress Deserialize(ref CdrReader reader)
        {
            var view = new ProcessAddress();
            int endPos = int.MaxValue;
                reader.Align(4); view.StationId = reader.ReadString();
                reader.Align(4); view.ProcessId = reader.ReadString();
                reader.Align(4); view.SomeOtherId = reader.ReadString();
            return view;
        }
        public ProcessAddress ToOwned()
        {
            return this;
        }
    }
}
