using System;
using CycloneDDS.Runtime;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct CompositeKeyMessage
    {
        private static readonly uint[] _ops = new uint[] {
67108864, 16973825, 4, 16973825, 8, 17104897, 12, 17039424, 20, 0, 117440513, 1, 117440513, 3, 117440513, 5};

        public static uint[] GetDescriptorOps() => _ops;

        private static readonly DdsKeyDescriptor[] _keys = new DdsKeyDescriptor[] {
            new DdsKeyDescriptor { Name = "part1", Index = 10, Flags = 0 },
            new DdsKeyDescriptor { Name = "part2", Index = 12, Flags = 1 },
            new DdsKeyDescriptor { Name = "part3", Index = 14, Flags = 2 },
        };
        public static DdsKeyDescriptor[] GetDescriptorKeys() => _keys;

        public static uint GetDescriptorFlagset() => 2;
    }
}
