using System;
using System.Threading.Tasks;
using Xunit;
using AtomicTests;

namespace CsharpToC.Roundtrip.Tests
{
    [Collection("Roundtrip Collection")]
    public class CompositeKeyTests : TestBase
    {
        public CompositeKeyTests(RoundtripFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestTwoKeyInt32() => await RunRoundtrip<TwoKeyInt32Topic>(
            "AtomicTests::TwoKeyInt32Topic", 
            1600,
            s => new TwoKeyInt32Topic { Key1 = s, Key2 = s + 1, Value = (double)s * 1.5 },
            (d, s) => d.Key1 == s && d.Key2 == s + 1 && Math.Abs(d.Value - (double)s * 1.5) < 0.0001);

        [Fact]
        public async Task TestTwoKeyString() => await RunRoundtrip<TwoKeyStringTopic>(
            "AtomicTests::TwoKeyStringTopic", 
            1610,
            s => new TwoKeyStringTopic { Key1 = $"k1_{s}", Key2 = $"k2_{s}", Value = (double)s * 2.5 },
            (d, s) => d.Key1 == $"k1_{s}" && d.Key2 == $"k2_{s}" && Math.Abs(d.Value - (double)s * 2.5) < 0.0001);

        [Fact]
        public async Task TestThreeKey() => await RunRoundtrip<ThreeKeyTopic>(
            "AtomicTests::ThreeKeyTopic",
            1620,
            s => new ThreeKeyTopic { Key1 = s, Key2 = $"k2_{s}", Key3 = (short)(s % 100), Value = (double)s * 3.5 },
            (d, s) => d.Key1 == s && d.Key2 == $"k2_{s}" && d.Key3 == (short)(s % 100) && Math.Abs(d.Value - (double)s * 3.5) < 0.0001);

        [Fact]
        public async Task TestFourKey() => await RunRoundtrip<FourKeyTopic>(
            "AtomicTests::FourKeyTopic",
            1630,
            s => new FourKeyTopic { Key1 = s, Key2 = s + 1, Key3 = s + 2, Key4 = s + 3, Description = $"Desc_{s}" },
            (d, s) => d.Key1 == s && d.Key2 == s + 1 && d.Key3 == s + 2 && d.Key4 == s + 3 && d.Description == $"Desc_{s}");
    }
}
