using System;

namespace CycloneDDS.Schema
{
    /// <summary>
    /// The extensibility kind of the DDS type.
    /// </summary>
    public enum DdsExtensibilityKind
    {
        /// <summary>
        /// Final extensibility.
        /// </summary>
        Final,
        /// <summary>
        /// Appendable extensibility.
        /// </summary>
        Appendable,
        /// <summary>
        /// Mutable extensibility.
        /// </summary>
        Mutable
    }

    /// <summary>
    /// Controls the XTypes extensibility kind.
    /// Defaults to Appendable if not specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DdsExtensibilityAttribute : Attribute
    {
        /// <summary>
        /// The extensibility kind.
        /// </summary>
        public DdsExtensibilityKind Kind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DdsExtensibilityAttribute"/> class.
        /// </summary>
        /// <param name="kind">The extensibility kind.</param>
        public DdsExtensibilityAttribute(DdsExtensibilityKind kind)
        {
            Kind = kind;
        }
    }
}
