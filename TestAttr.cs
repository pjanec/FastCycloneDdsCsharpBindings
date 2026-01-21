using System;
using System.Reflection;
using CycloneDDS.Schema;
using CycloneDDS.Runtime.Tests;

public class TestReflection
{
    public static void Main()
    {
        var type = typeof(KeyedTestMessage);
        var attr = type.GetCustomAttribute<DdsExtensibilityAttribute>();
        if (attr == null)
        {
            Console.WriteLine("No attribute found on KeyedTestMessage");
        }
        else
        {
            Console.WriteLine($"Found attribute: {attr.Kind}");
        }
    }
}
