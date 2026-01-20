using System;

namespace CycloneDDS.Schema
{
    public enum DdsExtensibilityKind
    {
        Final,
        Appendable,
        Mutable
    }

    /// <summary>
    /// Controls the XTypes extensibility kind.
    /// Defaults to Appendable if not specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DdsExtensibilityAttribute : Attribute
    {
        public DdsExtensibilityKind Kind { get; }

        public DdsExtensibilityAttribute(DdsExtensibilityKind kind)
        {
            Kind = kind;
        }
    }
}
