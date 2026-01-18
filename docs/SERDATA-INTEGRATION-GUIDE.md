# Cyclone DDS Serdata Integration Guide

This document outlines the integration of Cyclone DDS `serdata` APIs into the C# bindings to achieve high-performance, zero-allocation serialization and lazy deserialization.

## 1. Overview

The `serdata` (Serialized Data) API in Cyclone DDS allows applications to interact directly with the serialized representation of data. This enables:

*   **Zero-Allocation Writes:** Constructing a CDR blob directly in a pooled buffer and passing it to DDS without intermediate copies.
*   **Lazy Deserialization:** Receiving a handle to the serialized data (`serdata`) and deserializing it only when accessed, potentially skipping unused samples.

## 2. Native API Extensions

To support this in C#, we have exposed several internal `serdata` functions from the native `ddsc` library. These functions are normally inline in C headers but need to be exported for P/Invoke.

### 2.1. Exported Functions

The following functions have been added to `dds_topic.c` and exported in `dds.h`:

*   `dds_get_topic_sertype`: Retrieves the `ddsi_sertype` for a topic.
*   `dds_serdata_ref`: Increments the reference count of a `serdata` object.
*   `dds_serdata_unref`: Decrements the reference count and frees if zero.
*   `dds_serdata_size`: Returns the size of the serialized data.
*   `dds_serdata_to_ser`: Copies the serialized data to a buffer.
*   `dds_serdata_from_ser_iov`: Creates a `serdata` object from an IO vector (used for zero-copy writes).

## 3. C# Implementation

### 3.1. `DdsApi` P/Invoke Definitions

We import the exported functions in `DdsApi.cs`:

```csharp
[DllImport(DLL_NAME)]
public static extern int dds_takecdr(
    int reader, 
    [In, Out] IntPtr[] samples, 
    uint maxs,
    [In, Out] DdsSampleInfo[] infos, 
    uint mask);

[DllImport(DLL_NAME, EntryPoint = "dds_serdata_ref")]
public static extern IntPtr ddsi_serdata_ref(IntPtr serdata);

[DllImport(DLL_NAME, EntryPoint = "dds_serdata_unref")]
public static extern void ddsi_serdata_unref(IntPtr serdata);

// ... other serdata imports ...
```

### 3.2. `DdsSampleInfo` Layout

A critical part of the integration is ensuring the `DdsSampleInfo` struct matches the native `dds_sample_info_t` layout exactly, including padding.

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct DdsSampleInfo
{
    public uint SampleState;
    public uint ViewState;
    public uint InstanceState;
    public byte ValidData; 
    private byte _pad1;
    private byte _pad2;
    private byte _pad3;
    public long SourceTimestamp;
    // ...
    private uint _pad4;
}
```

### 3.3. `DdsReader` Implementation

The `DdsReader` uses `dds_takecdr` to receive `serdata` handles:

1.  **Take:** Calls `dds_takecdr` to fill an array of `IntPtr` (serdata handles).
2.  **ViewScope:** Wraps the handles in a `ViewScope`.
3.  **Lazy Access:** When the user accesses a sample via `ViewScope[i]`:
    *   Checks if `serdata` is valid.
    *   Gets size via `ddsi_serdata_size`.
    *   Rents a buffer.
    *   Copies data via `ddsi_serdata_to_ser`.
    *   Deserializes using `CdrReader`.
4.  **Dispose:** `ViewScope.Dispose` calls `ddsi_serdata_unref` for all valid handles.

### 3.4. `DdsWriter` Implementation

The `DdsWriter` uses a zero-allocation approach:

1.  **Rent Buffer:** Rents a buffer from `Arena`.
2.  **Serialize:** Serializes into the buffer.
3.  **Create Serdata:** Calls `dds_create_serdata_from_cdr` (using `dds_get_topic_sertype` and `ddsi_serdata_from_ser_iov`).
4.  **Write:** Calls `dds_writecdr`.

## 4. Troubleshooting

*   **Crashes in `dds_takecdr`:** Usually due to `DdsSampleInfo` layout mismatch. Verify padding.
*   **`BadParameter`:** Check P/Invoke signatures (e.g., `uint` vs `int`, `bool` vs `byte`).
*   **Linker Errors:** Ensure `ddsc.dll` exports the required `serdata` functions.

## 5. Status

*   **Write Path:** Working (Zero-allocation).
*   **Read Path:** Working (Lazy deserialization via `dds_takecdr`).
*   **Validation:** Verified with `FullRoundtrip_SimpleMessage_DataMatches` test.
