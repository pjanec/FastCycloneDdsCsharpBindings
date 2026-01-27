using System;
using System.Threading.Tasks;
using Xunit;
using AtomicTests;

namespace CsharpToC.Roundtrip.Tests
{
    [Collection("Roundtrip Collection")]
    public class NestedKeyTests : TestBase
    {
        public NestedKeyTests(RoundtripFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestNestedKey() => await RunRoundtrip<NestedKeyTopic>(
            "AtomicTests::NestedKeyTopic",
            1700,
            s => new NestedKeyTopic { Loc = new Location { Building = s, Floor = (short)(s % 10) }, Temperature = 20.0 + s },
            (d, s) => d.Loc.Building == s && d.Loc.Floor == (short)(s % 10) && Math.Abs(d.Temperature - (20.0 + s)) < 0.0001);

        [Fact]
        public async Task TestNestedKeyGeo() => await RunRoundtrip<NestedKeyGeoTopic>(
            "AtomicTests::NestedKeyGeoTopic",
            1710,
            s => new NestedKeyGeoTopic { Coords = new Coordinates { Latitude = s * 0.1, Longitude = s * 0.2 }, Location_name = $"Loc_{s}" },
            (d, s) => Math.Abs(d.Coords.Latitude - s * 0.1) < 0.0001 && Math.Abs(d.Coords.Longitude - s * 0.2) < 0.0001 && d.Location_name == $"Loc_{s}");

        [Fact]
        public async Task TestNestedTripleKey() => await RunRoundtrip<NestedTripleKeyTopic>(
            "AtomicTests::NestedTripleKeyTopic",
            1720,
            s => new NestedTripleKeyTopic { Keys = new TripleKey { Id1 = s, Id2 = s+1, Id3 = s+2 }, Data = $"Data_{s}" },
            (d, s) => d.Keys.Id1 == s && d.Keys.Id2 == s+1 && d.Keys.Id3 == s+2 && d.Data == $"Data_{s}");
    }
}
