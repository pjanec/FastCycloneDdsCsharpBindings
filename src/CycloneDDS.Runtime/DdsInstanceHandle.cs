using System;

namespace CycloneDDS.Runtime
{
    /// <summary>
    /// Represents a handle to a specific instance of a keyed topic.
    /// </summary>
    public readonly struct DdsInstanceHandle : IEquatable<DdsInstanceHandle>
    {
        public readonly long Value;

        public static readonly DdsInstanceHandle Nil = new DdsInstanceHandle(0);

        public DdsInstanceHandle(long value) { Value = value; }

        public bool IsNil => Value == 0;
        
        public bool Equals(DdsInstanceHandle other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is DdsInstanceHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"InstanceHandle(0x{Value:x})";

        public static bool operator ==(DdsInstanceHandle left, DdsInstanceHandle right) => left.Equals(right);
        public static bool operator !=(DdsInstanceHandle left, DdsInstanceHandle right) => !left.Equals(right);

        public static implicit operator long(DdsInstanceHandle handle) => handle.Value;
        public static implicit operator DdsInstanceHandle(long value) => new DdsInstanceHandle(value);
    }
}
