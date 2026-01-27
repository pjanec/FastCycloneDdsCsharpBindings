using System;
using System.Threading.Tasks;
using Xunit;
using AtomicTests;

namespace CsharpToC.Roundtrip.Tests
{
    [Collection("Roundtrip Collection")]
    public class BasicPrimitivesAppendableTests : TestBase
    {
        public BasicPrimitivesAppendableTests(RoundtripFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestBooleanAppendable()
        {
            await RunRoundtrip<BooleanTopicAppendable>(
                "AtomicTests::BooleanTopicAppendable", 
                1100,
                (s) => { 
                    var msg = new BooleanTopicAppendable(); 
                    msg.Id = s; 
                    msg.Value = (s % 2) != 0; 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == ((s % 2) != 0)
            );
        }

        [Fact]
        public async Task TestCharAppendable() => await RunRoundtrip<CharTopicAppendable>(
            "AtomicTests::CharTopicAppendable", 1100, 
            s => new CharTopicAppendable { Id = s, Value = (byte)('A' + (s % 26)) },
            (d, s) => d.Id == s && d.Value == (byte)('A' + (s % 26)));

        [Fact]
        public async Task TestOctetAppendable() => await RunRoundtrip<OctetTopicAppendable>(
            "AtomicTests::OctetTopicAppendable", 1200, 
            s => new OctetTopicAppendable { Id = s, Value = (byte)(s & 0xFF) },
            (d, s) => d.Id == s && d.Value == (byte)(s & 0xFF));
            
        [Fact]
        public async Task TestInt16Appendable() => await RunRoundtrip<Int16TopicAppendable>(
            "AtomicTests::Int16TopicAppendable", 1300, 
            s => new Int16TopicAppendable { Id = s, Value = (short)(s * 31) },
            (d, s) => d.Id == s && d.Value == (short)(s * 31));
            
        [Fact]
        public async Task TestUInt16Appendable() => await RunRoundtrip<UInt16TopicAppendable>(
            "AtomicTests::UInt16TopicAppendable", 1400, 
            s => new UInt16TopicAppendable { Id = s, Value = (ushort)(s * 31) },
            (d, s) => d.Id == s && d.Value == (ushort)(s * 31));
            
        [Fact]
        public async Task TestUInt32Appendable() => await RunRoundtrip<UInt32TopicAppendable>(
            "AtomicTests::UInt32TopicAppendable", 1500, 
            s => new UInt32TopicAppendable { Id = s, Value = (uint)((s * 1664525L) + 1013904223L) },
            (d, s) => d.Id == s && d.Value == (uint)((s * 1664525L) + 1013904223L));
            
        [Fact]
        public async Task TestInt64Appendable() => await RunRoundtrip<Int64TopicAppendable>(
            "AtomicTests::Int64TopicAppendable", 1600, 
            s => new Int64TopicAppendable { Id = s, Value = (long)s * 1000000L },
            (d, s) => d.Id == s && d.Value == (long)s * 1000000L);
            
        [Fact]
        public async Task TestUInt64Appendable() => await RunRoundtrip<UInt64TopicAppendable>(
            "AtomicTests::UInt64TopicAppendable", 1700, 
            s => new UInt64TopicAppendable { Id = s, Value = (ulong)s * 1000000UL },
            (d, s) => d.Id == s && d.Value == (ulong)s * 1000000UL);

        [Fact]
        public async Task TestFloat32Appendable() => await RunRoundtrip<Float32TopicAppendable>(
            "AtomicTests::Float32TopicAppendable", 1800, 
            s => new Float32TopicAppendable { Id = s, Value = (float)(s * 3.14159f) },
            (d, s) => d.Id == s && Math.Abs(d.Value - (float)(s * 3.14159f)) < 0.0001f);

        [Fact]
        public async Task TestFloat64Appendable() => await RunRoundtrip<Float64TopicAppendable>(
            "AtomicTests::Float64TopicAppendable", 1900, 
            s => new Float64TopicAppendable { Id = s, Value = (double)(s * 3.14159265359) },
            (d, s) => d.Id == s && Math.Abs(d.Value - (double)(s * 3.14159265359)) < 0.000001);

        [Fact]
        public async Task TestInt32Appendable()
        {
            await RunRoundtrip<Int32TopicAppendable>(
                "AtomicTests::Int32TopicAppendable", 
                1200,
                (s) => { 
                    var msg = new Int32TopicAppendable(); 
                    msg.Id = s; 
                    msg.Value = (int)((s * 1664525L) + 1013904223L); 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == (int)((s * 1664525L) + 1013904223L)
            );
        }

        [Fact]
        public async Task TestStringBounded32Appendable()
        {
            await RunRoundtrip<StringBounded32TopicAppendable>(
                "AtomicTests::StringBounded32TopicAppendable", 
                1300,
                (s) => { 
                    var msg = new StringBounded32TopicAppendable(); 
                    msg.Id = s; 
                    msg.Value = $"Str_{s}"; 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == $"Str_{s}"
            );
        }

        [Fact]
        public async Task TestStringUnboundedAppendable()
        {
            await RunRoundtrip<StringUnboundedTopicAppendable>(
                "AtomicTests::StringUnboundedTopicAppendable", 
                2100,
                (s) => { 
                    var msg = new StringUnboundedTopicAppendable(); 
                    msg.Id = s; 
                    msg.Value = $"StrUnbound_{s}"; 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == $"StrUnbound_{s}"
            );
        }

        [Fact]
        public async Task TestStringBounded256Appendable()
        {
            await RunRoundtrip<StringBounded256TopicAppendable>(
                "AtomicTests::StringBounded256TopicAppendable", 
                2200,
                (s) => { 
                    var msg = new StringBounded256TopicAppendable(); 
                    msg.Id = s; 
                    msg.Value = $"StrBound256_{s}"; 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == $"StrBound256_{s}"
            );
        }

        [Fact]
        public async Task TestEnumAppendable()
        {
            await RunRoundtrip<EnumTopicAppendable>(
                "AtomicTests::EnumTopicAppendable", 
                2500,
                (s) => { 
                    var msg = new EnumTopicAppendable(); 
                    msg.Id = s; 
                    msg.Value = (SimpleEnum)(s % 3); 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == (SimpleEnum)(s % 3)
            );
        }

        [Fact]
        public async Task TestColorEnumAppendable()
        {
            await RunRoundtrip<ColorEnumTopicAppendable>(
                "AtomicTests::ColorEnumTopicAppendable", 
                2600,
                (s) => { 
                    var msg = new ColorEnumTopicAppendable(); 
                    msg.Id = s; 
                    msg.Color = (ColorEnum)(s % 6); 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Color == (ColorEnum)(s % 6)
            );
        }
    }
}
