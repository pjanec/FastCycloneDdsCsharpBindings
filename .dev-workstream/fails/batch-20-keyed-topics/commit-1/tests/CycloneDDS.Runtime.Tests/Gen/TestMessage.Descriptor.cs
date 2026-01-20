using System;
using CycloneDDS.Runtime;

namespace CycloneDDS.Runtime.Tests
{
    public partial struct TestMessage
    {
        private static readonly uint[] _ops = new uint[] {
16973856, 0, 16973856, 4, 0};

        public static uint[] GetDescriptorOps() => _ops;

        private static readonly DdsKeyDescriptor[] _keys = new DdsKeyDescriptor[] {
        };
        public static DdsKeyDescriptor[] GetDescriptorKeys() => _keys;

        public static uint GetDescriptorFlagset() => 3;
    }
}
