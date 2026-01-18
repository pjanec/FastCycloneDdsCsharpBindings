using System;

namespace CycloneDDS.Schema
{
    /// <summary>
    /// Marks a struct or class as a DDS data type that can be nested within Topics.
    /// Triggers code generation for serialization but does not define a Topic.
    /// Use this for helper types like Point3D, Quaternion, etc.
    /// </summary>
    /// <example>
    /// <code>
    /// [DdsStruct]
    /// public partial struct Point3D
    /// {
    ///     public double X;
    ///     public double Y;
    ///     public double Z;
    /// }
    /// 
    /// [DdsTopic("Robot")]
    /// public partial struct RobotState
    /// {
    ///     [DdsKey] public int Id;
    ///     public Point3D Position;  // Uses the [DdsStruct] type
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DdsStructAttribute : Attribute
    {
    }
}
