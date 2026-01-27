using System;
using System.Threading.Tasks;
using Xunit;
using AtomicTests;

namespace CsharpToC.Roundtrip.Tests
{
    [Collection("Roundtrip Collection")]
    public class UnionTests : TestBase
    {
        public UnionTests(RoundtripFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestUnionLongDisc()
        {
            await RunRoundtrip<UnionLongDiscTopic>(
                "AtomicTests::UnionLongDiscTopic", 
                600,
                (s) => { 
                    var msg = new UnionLongDiscTopic();
                    msg.Id = s; 
                    int disc = (s % 3) + 1;
                    
                    var u = new SimpleUnion();
                    u._d = disc; 
                    
                    if (disc == 1) { 
                        u.Int_value = s * 100;
                    } else if (disc == 2) {
                        u.Double_value = s * 1.5;
                    } else if (disc == 3) {
                        u.String_value = $"Union_{s}";
                    }
                    msg.Data = u;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int disc = (s % 3) + 1;
                    if (msg.Data._d != disc) return false; 
                    
                    if (disc == 1) return msg.Data.Int_value == s * 100;
                    if (disc == 2) return Math.Abs(msg.Data.Double_value - (s * 1.5)) < 0.0001;
                    if (disc == 3) return msg.Data.String_value == $"Union_{s}";
                    return false;
                }
            );
        }

        [Fact]
        public async Task TestUnionBoolDisc()
        {
            await RunRoundtrip<UnionBoolDiscTopic>(
                "AtomicTests::UnionBoolDiscTopic",
                610,
                (s) => {
                    var msg = new UnionBoolDiscTopic();
                    msg.Id = s;
                    bool disc = (s % 2) == 0;

                    var u = new BoolUnion();
                    u._d = disc;

                    if (disc) u.True_val = s * 50;
                    else u.False_val = s * 1.5;

                    msg.Data = u;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    bool disc = (s % 2) == 0;
                    if (msg.Data._d != disc) return false;

                    if (disc) return msg.Data.True_val == s * 50;
                    else return Math.Abs(msg.Data.False_val - (s * 1.5)) < 0.0001;
                }
            );
        }

        [Fact]
        public async Task TestUnionEnumDisc()
        {
            await RunRoundtrip<UnionEnumDiscTopic>(
                "AtomicTests::UnionEnumDiscTopic",
                620,
                (s) => {
                    var msg = new UnionEnumDiscTopic();
                    msg.Id = s;
                    var disc = (ColorEnum)(s % 4);

                    var u = new ColorUnion();
                    u._d = disc;

                    switch (disc)
                    {
                        case ColorEnum.RED: u.Red_data = s * 20; break;
                        case ColorEnum.GREEN: u.Green_data = s * 2.5; break;
                        case ColorEnum.BLUE: u.Blue_data = $"Blue_{s}"; break;
                        case ColorEnum.YELLOW: u.Yellow_point = new Point2D { X = s * 1.1, Y = s * 2.2 }; break;
                    }
                    msg.Data = u;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    var disc = (ColorEnum)(s % 4);
                    if (msg.Data._d != disc) return false;

                    switch (disc)
                    {
                        case ColorEnum.RED: return msg.Data.Red_data == s * 20;
                        case ColorEnum.GREEN: return Math.Abs(msg.Data.Green_data - (s * 2.5)) < 0.0001;
                        case ColorEnum.BLUE: return msg.Data.Blue_data == $"Blue_{s}";
                        case ColorEnum.YELLOW: return Math.Abs(msg.Data.Yellow_point.X - (s * 1.1)) < 0.0001 && Math.Abs(msg.Data.Yellow_point.Y - (s * 2.2)) < 0.0001;
                        default: return false;
                    }
                }
            );
        }

        [Fact]
        public async Task TestUnionShortDisc()
        {
            await RunRoundtrip<UnionShortDiscTopic>(
                "AtomicTests::UnionShortDiscTopic",
                630,
                (s) => {
                    var msg = new UnionShortDiscTopic();
                    msg.Id = s;
                    short disc = (short)((s % 4) + 1);

                    var u = new ShortUnion();
                    u._d = disc;

                    switch (disc)
                    {
                        case 1: u.Byte_val = (byte)(s % 255); break;
                        case 2: u.Short_val = (short)(s * 10); break;
                        case 3: u.Long_val = s * 1000; break;
                        case 4: u.Float_val = (float)(s * 3.14); break;
                    }
                    msg.Data = u;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    short disc = (short)((s % 4) + 1);
                    if (msg.Data._d != disc) return false;

                    switch (disc)
                    {
                        case 1: return msg.Data.Byte_val == (byte)(s % 255);
                        case 2: return msg.Data.Short_val == (short)(s * 10);
                        case 3: return msg.Data.Long_val == s * 1000;
                        case 4: return Math.Abs(msg.Data.Float_val - (float)(s * 3.14)) < 0.001;
                        default: return false;
                    }
                }
            );
        }

        [Fact]
        public async Task TestUnionLongDiscAppendable()
        {
            await RunRoundtrip<UnionLongDiscTopicAppendable>(
                "AtomicTests::UnionLongDiscTopicAppendable", 
                1600,
                (s) => { 
                    var msg = new UnionLongDiscTopicAppendable();
                    msg.Id = s; 
                    int disc = (s % 3) + 1;
                    
                    var u = new SimpleUnionAppendable();
                    u._d = disc; 
                    
                    if (disc == 1) { 
                        u.Int_value = s * 100;
                    } else if (disc == 2) {
                        u.Double_value = s * 1.5;
                    } else if (disc == 3) {
                        u.String_value = $"Union_{s}";
                    }
                    msg.Data = u;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int disc = (s % 3) + 1;
                    if (msg.Data._d != disc) return false; 
                    
                    if (disc == 1) return msg.Data.Int_value == s * 100;
                    if (disc == 2) return Math.Abs(msg.Data.Double_value - (s * 1.5)) < 0.0001;
                    if (disc == 3) return msg.Data.String_value == $"Union_{s}";
                    return false;
                }
            );
        }
    }
}
