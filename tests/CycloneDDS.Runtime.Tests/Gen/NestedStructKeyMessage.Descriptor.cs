using System;
using CycloneDDS.Runtime;

namespace CycloneDDS.Runtime.Tests.KeyedMessages
{
    public partial struct NestedStructKeyMessage
    {
        private static readonly uint[] _ops = new uint[] {67108864, 16973825, 0, 17629185, 8, 196614, 17039368, 32, 0, 17104897, 0, 17104897, 8, 17104896, 16, 0, 117440513, 1, 117440514, 3, 0, 117440514, 3, 2};

        public static uint[] GetDescriptorOps() => _ops;

        private static readonly DdsKeyDescriptor[] _keys = new DdsKeyDescriptor[]
        {
            new DdsKeyDescriptor { Name = "FrameId", Offset = 16, Index = 0 },
            new DdsKeyDescriptor { Name = "processAddr.stationId", Offset = 18, Index = 1 },
            new DdsKeyDescriptor { Name = "processAddr.processId", Offset = 21, Index = 2 },
        };
        public static DdsKeyDescriptor[] GetKeyDescriptors() => _keys;
    }
}
