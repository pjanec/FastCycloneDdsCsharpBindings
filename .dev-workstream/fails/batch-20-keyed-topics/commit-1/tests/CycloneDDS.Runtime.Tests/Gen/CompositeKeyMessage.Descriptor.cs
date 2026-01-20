using System;
using CycloneDDS.Runtime;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct CompositeKeyMessage
    {
        private static readonly uint[] _ops = new uint[] {
16973825, 0, 16973825, 4, 17104897, 8, 17039424, 16, 0, 117440513, 0, 117440513, 2, 117440513, 4};

        public static uint[] GetDescriptorOps() => _ops;

        private static readonly DdsKeyDescriptor[] _keys = new DdsKeyDescriptor[] {
            new DdsKeyDescriptor { Name = "part1", Index = 9, Flags = 0 },
            new DdsKeyDescriptor { Name = "part2", Index = 11, Flags = 1 },
            new DdsKeyDescriptor { Name = "part3", Index = 13, Flags = 2 },
        };
        public static DdsKeyDescriptor[] GetDescriptorKeys() => _keys;

        public static uint GetDescriptorFlagset() => 2;
    }
}
