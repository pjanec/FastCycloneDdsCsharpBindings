# Cyclone DDS Serdata Integration Guide

This document outlines the integration of Cyclone DDS `serdata` APIs into the C# bindings to achieve high-performance, zero-allocation serialization and lazy deserialization.

## 1. Overview

The `serdata` (Serialized Data) API in Cyclone DDS allows applications to interact directly with the serialized representation of data. This enables:

*   **Zero-Allocation Writes:** Constructing a CDR blob directly in a pooled buffer and passing it to DDS without intermediate copies.
*   **Lazy Deserialization:** Receiving a handle to the serialized data (`serdata`) and deserializing it only when accessed, potentially skipping unused samples.

## 2. Native API Extensions

To support this in C#, we use the `dds_take_with_collector` API, which provides a callback mechanism to receive `serdata` pointers directly, bypassing the standard `dds_take` marshalling which can be problematic for `serdata`.

### 2.1. `dds_take_with_collector`

This function allows us to provide a callback that is invoked for each sample taken from the reader.

```c
typedef dds_return_t (*dds_read_with_collector_fn_t) (
    void *arg, 
    const dds_sample_info_t *si, 
    const struct ddsi_sertype *st, 
    struct ddsi_serdata *sd);

DDS_EXPORT dds_return_t dds_take_with_collector (
    dds_entity_t reader_or_condition, 
    uint32_t maxs, 
    dds_instance_handle_t handle, 
    uint32_t mask, 
    dds_read_with_collector_fn_t collect_sample, 
    void *collect_sample_arg);
```

### 2.2. `ddsi_serdata` Operations

We also expose functions to manipulate `serdata` objects:

*   `ddsi_serdata_ref`: Increments the reference count.
*   `ddsi_serdata_unref`: Decrements the reference count.
*   `ddsi_serdata_size`: Gets the size of the serialized data.
*   `ddsi_serdata_to_ser`: Copies the serialized data to a buffer.

## 3. C# Implementation

### 3.1. `DdsApi` P/Invoke Definitions

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int DdsReadWithCollectorDelegate(
    IntPtr arg,
    IntPtr sampleInfo, // const dds_sample_info_t *
    IntPtr sertype,    // const struct ddsi_sertype *
    IntPtr serdata);   // struct ddsi_serdata *

[DllImport(DLL_NAME)]
public static extern int dds_take_with_collector(
    int reader,
    uint maxs,
    long handle, // dds_instance_handle_t
    uint mask,
    DdsReadWithCollectorDelegate collect_sample,
    IntPtr collect_sample_arg);

[DllImport(DLL_NAME)]
public static extern unsafe IntPtr ddsi_serdata_ref(IntPtr serdata);

[DllImport(DLL_NAME)]
public static extern void ddsi_serdata_unref(IntPtr serdata);
```

### 3.2. `DdsReader` Implementation

The `DdsReader` uses `dds_take_with_collector` to populate a `ViewScope`.

```csharp
private static int CollectorCallback(IntPtr arg, IntPtr sampleInfo, IntPtr sertype, IntPtr serdata)
{
    var handle = GCHandle.FromIntPtr(arg);
    var context = (CollectorContext)handle.Target;
    
    if (context.Count >= context.MaxSamples) return 0;

    // Marshal SampleInfo
    context.Infos[context.Count] = Marshal.PtrToStructure<DdsApi.DdsSampleInfo>(sampleInfo);
    
    // Handle Serdata
    if (serdata != IntPtr.Zero)
    {
        // Increment ref count because we are keeping it beyond the callback
        unsafe 
        {
            context.Samples[context.Count] = DdsApi.ddsi_serdata_ref(serdata);
        }
    }
    else
    {
        context.Samples[context.Count] = IntPtr.Zero;
    }
    
    context.Count++;
    return 0; // DDS_RETCODE_OK
}
```

### 3.3. `ViewScope` Implementation

The `ViewScope` manages the lifecycle of the `serdata` objects. When accessing a sample, it performs lazy deserialization.

```csharp
public TView this[int index]
{
    get
    {
        // ... validation ...
        IntPtr serdata = _samples[index];
        
        // Extract CDR and Deserialize
        uint size = DdsApi.ddsi_serdata_size(serdata);
        byte[] buffer = Arena.Rent((int)size);
        try
        {
            DdsApi.ddsi_serdata_to_ser(serdata, UIntPtr.Zero, (UIntPtr)size, buffer);
            // Deserialize from buffer...
        }
        finally
        {
            Arena.Return(buffer);
        }
    }
}

public void Dispose()
{
    // Release serdata references
    foreach (var serdata in _samples)
    {
        if (serdata != IntPtr.Zero) DdsApi.ddsi_serdata_unref(serdata);
    }
    // ...
}
```

## 4. Troubleshooting

*   **Crashes in `dds_take`:** If `dds_take` crashes, it's often due to incorrect P/Invoke signatures or struct layouts. Using `dds_take_with_collector` avoids complex array marshalling.
*   **`BadParameter`:** Ensure `maxs` is not 0 and `mask` is valid.
*   **Data Corruption:** Ensure `DdsSampleInfo` struct layout matches the native `dds_sample_info_t` exactly, including padding.

## 5. Status

*   **Write Path:** Implemented using `dds_create_serdata_from_cdr` and `dds_writecdr`.
*   **Read Path:** Implemented using `dds_take_with_collector` and lazy deserialization.
*   **Validation:** `FullRoundtrip_SimpleMessage_DataMatches` test verifies end-to-end functionality.
