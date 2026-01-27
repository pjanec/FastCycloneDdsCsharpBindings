using System;
using System.Threading.Tasks;
using Xunit;
using AtomicTests;

namespace CsharpToC.Roundtrip.Tests
{
    [Collection("Roundtrip Collection")]
    public class BasicPrimitivesTests : TestBase
    {
        public BasicPrimitivesTests(RoundtripFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestBoolean()
        {
            await RunRoundtrip<BooleanTopic>(
                "AtomicTests::BooleanTopic", 
                100,
                (s) => { 
                    var msg = new BooleanTopic(); 
                    msg.Id = s; 
                    msg.Value = (s % 2) != 0; 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == ((s % 2) != 0)
            );
        }

        [Fact]
        public async Task TestChar() => await RunRoundtrip<CharTopic>(
            "AtomicTests::CharTopic", 150, 
            s => new CharTopic { Id = s, Value = (byte)('A' + (s % 26)) },
            (d, s) => d.Id == s && d.Value == (byte)('A' + (s % 26)));

        [Fact]
        public async Task TestOctet() => await RunRoundtrip<OctetTopic>(
            "AtomicTests::OctetTopic", 200, 
            s => new OctetTopic { Id = s, Value = (byte)(s & 0xFF) },
            (d, s) => d.Id == s && d.Value == (byte)(s & 0xFF));

        [Fact]
        public async Task TestInt16() => await RunRoundtrip<Int16Topic>(
            "AtomicTests::Int16Topic", 300, 
            s => new Int16Topic { Id = s, Value = (short)(s * 31) },
            (d, s) => d.Id == s && d.Value == (short)(s * 31));

        [Fact]
        public async Task TestUInt16() => await RunRoundtrip<UInt16Topic>(
            "AtomicTests::UInt16Topic", 400, 
            s => new UInt16Topic { Id = s, Value = (ushort)(s * 31) },
            (d, s) => d.Id == s && d.Value == (ushort)(s * 31));

        [Fact]
        public async Task TestUInt32() => await RunRoundtrip<UInt32Topic>(
            "AtomicTests::UInt32Topic", 500, 
            s => new UInt32Topic { Id = s, Value = (uint)((s * 1664525L) + 1013904223L) },
            (d, s) => d.Id == s && d.Value == (uint)((s * 1664525L) + 1013904223L));

        [Fact]
        public async Task TestInt64() => await RunRoundtrip<Int64Topic>(
            "AtomicTests::Int64Topic", 600, 
            s => new Int64Topic { Id = s, Value = (long)s * 1000000L },
            (d, s) => d.Id == s && d.Value == (long)s * 1000000L);

        [Fact]
        public async Task TestUInt64() => await RunRoundtrip<UInt64Topic>(
            "AtomicTests::UInt64Topic", 700, 
            s => new UInt64Topic { Id = s, Value = (ulong)s * 1000000UL },
            (d, s) => d.Id == s && d.Value == (ulong)s * 1000000UL);

        [Fact]
        public async Task TestFloat32() => await RunRoundtrip<Float32Topic>(
            "AtomicTests::Float32Topic", 800, 
            s => new Float32Topic { Id = s, Value = (float)(s * 3.14159f) },
            (d, s) => d.Id == s && Math.Abs(d.Value - (float)(s * 3.14159f)) < 0.0001f);

        [Fact]
        public async Task TestFloat64() => await RunRoundtrip<Float64Topic>(
            "AtomicTests::Float64Topic", 900, 
            s => new Float64Topic { Id = s, Value = (double)(s * 3.14159265359) },
            (d, s) => d.Id == s && Math.Abs(d.Value - (double)(s * 3.14159265359)) < 0.000001);

        [Fact]
        public async Task TestInt32() 
        {
            await RunRoundtrip<Int32Topic>(
                "AtomicTests::Int32Topic", 
                200,
                (s) => { 
                    var msg = new Int32Topic(); 
                    msg.Id = s; 
                    msg.Value = (int)((s * 1664525L) + 1013904223L); 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == (int)((s * 1664525L) + 1013904223L)
            );
        }

        [Fact]
        public async Task TestStringBounded32()
        {
            await RunRoundtrip<StringBounded32Topic>(
                "AtomicTests::StringBounded32Topic", 
                300,
                (s) => { 
                    var msg = new StringBounded32Topic(); 
                    msg.Id = s; 
                    msg.Value = $"Str_{s}"; 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == $"Str_{s}"
            );
        }

        [Fact]
        public async Task TestStringUnbounded()
        {
            await RunRoundtrip<StringUnboundedTopic>(
                "AtomicTests::StringUnboundedTopic", 
                1100,
                (s) => { 
                    var msg = new StringUnboundedTopic(); 
                    msg.Id = s; 
                    msg.Value = $"StrUnbound_{s}"; 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == $"StrUnbound_{s}"
            );
        }

        [Fact]
        public async Task TestStringBounded256()
        {
            await RunRoundtrip<StringBounded256Topic>(
                "AtomicTests::StringBounded256Topic", 
                1200,
                (s) => { 
                    var msg = new StringBounded256Topic(); 
                    msg.Id = s; 
                    msg.Value = $"StrBound256_{s}"; 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == $"StrBound256_{s}"
            );
        }

        [Fact]
        public async Task TestEnum()
        {
            await RunRoundtrip<EnumTopic>(
                "AtomicTests::EnumTopic", 
                2300,
                (s) => { 
                    var msg = new EnumTopic(); 
                    msg.Id = s; 
                    msg.Value = (SimpleEnum)(s % 3); 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Value == (SimpleEnum)(s % 3)
            );
        }

        [Fact]
        public async Task TestColorEnum()
        {
            await RunRoundtrip<ColorEnumTopic>(
                "AtomicTests::ColorEnumTopic", 
                2400,
                (s) => { 
                    var msg = new ColorEnumTopic(); 
                    msg.Id = s; 
                    msg.Color = (ColorEnum)(s % 6); 
                    return msg; 
                },
                (msg, s) => msg.Id == s && msg.Color == (ColorEnum)(s % 6)
            );
        }
    }
}
