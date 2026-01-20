using System;
using CycloneDDS.Runtime;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct KeyedTestMessage
    {
        private static readonly uint[] _ops = new uint[] {
16973825, 0, 16973856, 4, 0, 117440513, 0};

        public static uint[] GetDescriptorOps() => _ops;

        private static readonly DdsKeyDescriptor[] _keys = new DdsKeyDescriptor[] {
            new DdsKeyDescriptor { Name = "sensorId", Index = 5, Flags = 0 },
        };
        public static DdsKeyDescriptor[] GetDescriptorKeys() => _keys;

        public static uint GetDescriptorFlagset() => 3;
    }
}
