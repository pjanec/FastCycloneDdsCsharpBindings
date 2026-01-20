using System;
using CycloneDDS.Runtime;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct KeyedTestMessage
    {
        private static readonly uint[] _ops = new uint[] {
67108864, 16973825, 4, 16973856, 8, 0, 117440513, 1};

        public static uint[] GetDescriptorOps() => _ops;

        private static readonly DdsKeyDescriptor[] _keys = new DdsKeyDescriptor[] {
            new DdsKeyDescriptor { Name = "sensorId", Index = 6, Flags = 0 },
        };
        public static DdsKeyDescriptor[] GetDescriptorKeys() => _keys;

        public static uint GetDescriptorFlagset() => 2;
    }
}
