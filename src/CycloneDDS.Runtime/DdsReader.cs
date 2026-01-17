using System;
using System.Buffers;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using CycloneDDS.Core;
using CycloneDDS.Runtime.Interop;
using CycloneDDS.Runtime.Memory;

namespace CycloneDDS.Runtime
{
    public delegate void DeserializeDelegate<TView>(ref CdrReader reader, out TView view);

    public sealed class DdsReader<T, TView> : IDisposable 
        where TView : struct
    {
        private DdsEntityHandle? _readerHandle;
        private DdsEntityHandle? _topicHandle;
        private DdsParticipant? _participant;
        private readonly IntPtr _topicDescriptor;
        
        private static readonly DeserializeDelegate<TView>? _deserializer;
        
        static DdsReader()
        {
            try { 
                _deserializer = CreateDeserializerDelegate(); 
                
                // Verify Struct Size
                uint nativeSize = DdsApi.dds_sample_info_size();
                int managedSize = Marshal.SizeOf<DdsApi.DdsSampleInfo>();
                if (nativeSize != managedSize)
                {
                    Console.WriteLine($"[DdsReader] CRITICAL: DdsSampleInfo size mismatch. Native: {nativeSize}, Managed: {managedSize}");
                    // throw new InvalidOperationException($"DdsSampleInfo size mismatch. Native: {nativeSize}, Managed: {managedSize}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"[DdsReader] Initialization failed: {ex}"); throw; }
        }

        public DdsReader(DdsParticipant participant, string topicName, IntPtr topicDescriptor)
        {
            if (_deserializer == null) 
                 throw new InvalidOperationException($"Type {typeof(T).Name} missing Deserialize method.");

            _participant = participant;
            _topicDescriptor = topicDescriptor;

            // Create Topic
            var topic = DdsApi.dds_create_topic(participant.NativeEntity, topicDescriptor, topicName, IntPtr.Zero, IntPtr.Zero);
            if (!topic.IsValid)
            {
                 int err = topic.Handle;
                 DdsApi.DdsReturnCode rc = (DdsApi.DdsReturnCode)err;
                 throw new DdsException(rc, $"Failed to create topic '{topicName}'");
            }
            _topicHandle = new DdsEntityHandle(topic);

            // Create Reader (Default QoS)
             var reader = DdsApi.dds_create_reader(participant.NativeEntity, topic, IntPtr.Zero, IntPtr.Zero);
             if (!reader.IsValid)
             {
                  int err = reader.Handle;
                  DdsApi.DdsReturnCode rc = (DdsApi.DdsReturnCode)err;
                  throw new DdsException(rc, $"Failed to create reader for '{topicName}'");
             }
             _readerHandle = new DdsEntityHandle(reader);
        }

        public ViewScope<TView> Take(int maxSamples = 32)
        {
             if (_readerHandle == null) throw new ObjectDisposedException(nameof(DdsReader<T, TView>));
             
             var samples = ArrayPool<IntPtr>.Shared.Rent(maxSamples);
             var infos = ArrayPool<DdsApi.DdsSampleInfo>.Shared.Rent(maxSamples);
             
             Array.Clear(samples, 0, maxSamples);
             Array.Clear(infos, 0, maxSamples); 
             
             int count = DdsApi.dds_takecdr(
                 _readerHandle.NativeHandle.Handle,
                 samples,
                 (uint)maxSamples,
                 infos,
                 0xFFFFFFFF); // DDS_ANY_STATE

             if (count < 0)
             {
                 ArrayPool<IntPtr>.Shared.Return(samples);
                 ArrayPool<DdsApi.DdsSampleInfo>.Shared.Return(infos);
                 
                 if (count == (int)DdsApi.DdsReturnCode.NoData)
                 {
                     return new ViewScope<TView>(_readerHandle.NativeHandle, null, null, 0, null);
                 }
                 throw new DdsException((DdsApi.DdsReturnCode)count, $"dds_takecdr failed: {count}");
             }
             
             return new ViewScope<TView>(_readerHandle.NativeHandle, samples, infos, count, _deserializer);
        }

        public void Dispose()
        {
            _readerHandle?.Dispose();
            _readerHandle = null;
            _topicHandle?.Dispose();
            _topicHandle = null;
            _participant = null;
        }
        
        private static DeserializeDelegate<TView> CreateDeserializerDelegate()
        {
             var method = typeof(T).GetMethod("Deserialize", new[] { typeof(CdrReader).MakeByRefType() });
             if (method == null) throw new MissingMethodException(typeof(T).Name, "Deserialize");
             
             var dm = new DynamicMethod("DeserializeThunk", typeof(void), new[] { typeof(CdrReader).MakeByRefType(), typeof(TView).MakeByRefType() }, typeof(DdsReader<T,TView>).Module);
             var il = dm.GetILGenerator();
             // IL Stack: [out view], [ref reader] -> call -> [out view], [result] -> stobj -> []
             il.Emit(OpCodes.Ldarg_1); // out view
             il.Emit(OpCodes.Ldarg_0); // ref reader
             il.Emit(OpCodes.Call, method); // returns TView (stack)
             il.Emit(OpCodes.Stobj, typeof(TView));
             il.Emit(OpCodes.Ret);
             
             return (DeserializeDelegate<TView>)dm.CreateDelegate(typeof(DeserializeDelegate<TView>));
        }
    }

    public ref struct ViewScope<TView> where TView : struct
    {
        private DdsApi.DdsEntity _reader;
        private IntPtr[]? _samples;
        private DdsApi.DdsSampleInfo[]? _infos;
        private int _count;
        private DeserializeDelegate<TView>? _deserializer;
        
        public ReadOnlySpan<DdsApi.DdsSampleInfo> Infos => _infos != null ? _infos.AsSpan(0, _count) : ReadOnlySpan<DdsApi.DdsSampleInfo>.Empty;

        internal ViewScope(DdsApi.DdsEntity reader, IntPtr[]? samples, DdsApi.DdsSampleInfo[]? infos, int count, DeserializeDelegate<TView>? deserializer)
        {
            _reader = reader;
            _samples = samples;
            _infos = infos;
            _count = count;
            _deserializer = deserializer;
        }
        
        public int Count => _count;

        public TView this[int index]
        {
            get
            {
                if (index < 0 || index >= _count) throw new IndexOutOfRangeException();
                if (_infos == null || _samples == null) throw new ObjectDisposedException("ViewScope");
                
                // If invalid data, return default
                if (_infos[index].ValidData == 0) return default;
                
                IntPtr serdata = _samples[index];
                if (serdata == IntPtr.Zero) return default;

                // Lazy Deserialization from Serdata
                uint size = DdsApi.ddsi_serdata_size(serdata);
                
                if (size == 0) return default;
                
                byte[] buffer = Arena.Rent((int)size);
                
                try
                {
                    unsafe
                    {
                        fixed (byte* p = buffer)
                        {
                            // Extract CDR from serdata
                            DdsApi.ddsi_serdata_to_ser(serdata, UIntPtr.Zero, (UIntPtr)size, (IntPtr)p);
                            
                            // Deserialize
                            var span = new ReadOnlySpan<byte>(p, (int)size);
                            var reader = new CdrReader(span);
                            
                            // Cyclone DDS provides the 4-byte encapsulation header in the serdata.
                            // We must skip it so that CdrReader is aligned to the start of the payload
                            // and reads the correct data.
                            if (reader.Remaining >= 4)
                            {
                                // TODO: Verify header if needed.
                                reader.ReadInt32(); // Advance 4 bytes
                            }
                            
                            _deserializer!(ref reader, out TView view);
                            return view;
                        }
                    }
                }
                finally
                {
                    Arena.Return(buffer);
                }
            }
        }
        
        public void Dispose()
        {
            // Release references to serdata
            if (_count > 0 && _samples != null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (_samples[i] != IntPtr.Zero)
                    {
                        DdsApi.ddsi_serdata_unref(_samples[i]);
                    }
                }
            }
            
            if (_samples != null) ArrayPool<IntPtr>.Shared.Return(_samples);
            if (_infos != null) ArrayPool<DdsApi.DdsSampleInfo>.Shared.Return(_infos);
            
            _count = 0;
            _samples = null;
            _infos = null;
            _deserializer = null;
        }
    }
}
