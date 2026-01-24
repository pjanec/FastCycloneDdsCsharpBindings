using System;
using System.Runtime.InteropServices;

namespace CycloneDDS.Roundtrip.App;

/// <summary>
/// P/Invoke interface to the native roundtrip test DLL.
/// </summary>
internal static class NativeInterop
{
    private const string DllName = "CycloneDDS.Roundtrip.Native.dll";

    #region Initialization

    /// <summary>
    /// Initialize the native test framework.
    /// </summary>
    /// <param name="domainId">DDS domain ID (0 for default)</param>
    /// <returns>0 on success, negative on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Native_Init(int domainId);

    /// <summary>
    /// Cleanup and shutdown the native framework.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Native_Cleanup();

    #endregion

    #region Entity Creation

    /// <summary>
    /// Create a native publisher for the specified topic.
    /// </summary>
    /// <param name="topicName">Name of the topic (must match IDL @topic annotation)</param>
    /// <returns>0 on success, negative on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int Native_CreatePublisher([MarshalAs(UnmanagedType.LPStr)] string topicName);

    /// <summary>
    /// Create a native subscriber for the specified topic.
    /// </summary>
    /// <param name="topicName">Name of the topic (must match IDL @topic annotation)</param>
    /// <returns>0 on success, negative on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int Native_CreateSubscriber([MarshalAs(UnmanagedType.LPStr)] string topicName);

    #endregion

    #region Data Operations

    /// <summary>
    /// Send a test sample with deterministic data generated from the seed.
    /// </summary>
    /// <param name="topicName">Topic to publish on</param>
    /// <param name="seed">Integer seed for data generation</param>
    /// <returns>0 on success, negative on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int Native_SendWithSeed(
        [MarshalAs(UnmanagedType.LPStr)] string topicName,
        int seed);

    /// <summary>
    /// Wait for a sample and verify it matches the expected data from the seed.
    /// </summary>
    /// <param name="topicName">Topic to subscribe on</param>
    /// <param name="seed">Integer seed for expected data generation</param>
    /// <param name="timeoutMs">Timeout in milliseconds (0 = infinite)</param>
    /// <returns>
    /// 0 = Match (received data equals expected)
    /// -1 = Timeout (no data received)
    /// -2 = Mismatch (received data differs from expected)
    /// -3 = Error (internal failure)
    /// </returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int Native_ExpectWithSeed(
        [MarshalAs(UnmanagedType.LPStr)] string topicName,
        int seed,
        int timeoutMs);

    #endregion

    #region Error Handling

    /// <summary>
    /// Get the last error message from the native DLL.
    /// </summary>
    /// <returns>Pointer to null-terminated string (do not free)</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr Native_GetLastError();

    /// <summary>
    /// Get the last error message as a managed string.
    /// </summary>
    public static string GetLastError()
    {
        IntPtr ptr = Native_GetLastError();
        if (ptr == IntPtr.Zero)
            return "No error information available";
        
        return Marshal.PtrToStringAnsi(ptr) ?? "Unknown error";
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if a native call succeeded and throw on failure.
    /// </summary>
    public static void ThrowIfFailed(int result, string operation)
    {
        if (result < 0)
        {
            string error = GetLastError();
            throw new Exception($"{operation} failed: {error}");
        }
    }

    /// <summary>
    /// Describe the result of Native_ExpectWithSeed.
    /// </summary>
    public static string DescribeExpectResult(int result)
    {
        return result switch
        {
            0 => "Match",
            -1 => "Timeout",
            -2 => "Mismatch",
            -3 => "Error",
            _ => $"Unknown ({result})"
        };
    }

    #endregion
}
