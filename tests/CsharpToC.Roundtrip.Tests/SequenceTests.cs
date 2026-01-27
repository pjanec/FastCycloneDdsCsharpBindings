using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using AtomicTests;

namespace CsharpToC.Roundtrip.Tests
{
    [Collection("Roundtrip Collection")]
    public class SequenceTests : TestBase
    {
        public SequenceTests(RoundtripFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestSequenceInt32()
        {
            await RunRoundtrip<SequenceInt32Topic>(
                "AtomicTests::SequenceInt32Topic", 
                500,
                (s) => { 
                    var msg = new SequenceInt32Topic();
                    msg.Id = s; 
                    int len = s % 6;
                    var list = new System.Collections.Generic.List<int>();
                    for(int i=0; i<len; i++) list.Add((int)((s + i) * 31));
                    msg.Values = list; // Assign List directly
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = s % 6;
                    if (msg.Values == null || msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (msg.Values[i] != (int)((s + i) * 31)) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestBoundedSequenceInt32()
        {
            await RunRoundtrip<BoundedSequenceInt32Topic>(
                "AtomicTests::BoundedSequenceInt32Topic",
                505,
                (s) => {
                    var msg = new BoundedSequenceInt32Topic();
                    msg.Id = s;
                    int len = (s % 10) + 1;
                    var list = new List<int>();
                    for(int i=0; i<len; i++) list.Add((int)(s + i));
                    msg.Values = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 10) + 1;
                    if (msg.Values == null || msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (msg.Values[i] != (int)(s + i)) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceInt64()
        {
            await RunRoundtrip<SequenceInt64Topic>(
                "AtomicTests::SequenceInt64Topic",
                510,
                (s) => {
                    var msg = new SequenceInt64Topic();
                    msg.Id = s;
                    int len = (s % 5) + 1;
                    var list = new List<long>();
                    for(int i=0; i<len; i++) list.Add((long)((s + i) * 1000L));
                    msg.Values = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 5) + 1;
                    if (msg.Values == null || msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (msg.Values[i] != (long)((s + i) * 1000L)) return false;
                    return true;
                }
            );
        }
        
        [Fact]
        public async Task TestSequenceFloat32()
        {
            await RunRoundtrip<SequenceFloat32Topic>(
                "AtomicTests::SequenceFloat32Topic",
                520,
                (s) => {
                    var msg = new SequenceFloat32Topic();
                    msg.Id = s;
                    int len = (s % 5) + 1;
                    var list = new List<float>();
                    for(int i=0; i<len; i++) list.Add((float)((s + i) * 1.1f));
                    msg.Values = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 5) + 1;
                    if (msg.Values == null || msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (Math.Abs(msg.Values[i] - (float)((s + i) * 1.1f)) > 0.001) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceFloat64()
        {
            await RunRoundtrip<SequenceFloat64Topic>(
                "AtomicTests::SequenceFloat64Topic",
                530,
                (s) => {
                    var msg = new SequenceFloat64Topic();
                    msg.Id = s;
                    int len = (s % 5) + 1;
                    var list = new List<double>();
                    for(int i=0; i<len; i++) list.Add((double)((s + i) * 2.2));
                    msg.Values = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 5) + 1;
                    if (msg.Values == null || msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (Math.Abs(msg.Values[i] - (double)((s + i) * 2.2)) > 0.0001) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceBoolean()
        {
            await RunRoundtrip<SequenceBooleanTopic>(
                "AtomicTests::SequenceBooleanTopic",
                540,
                (s) => {
                    var msg = new SequenceBooleanTopic();
                    msg.Id = s;
                    int len = (s % 5) + 1;
                    var list = new List<bool>();
                    for(int i=0; i<len; i++) list.Add(((s + i) % 2) == 0);
                    msg.Values = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 5) + 1;
                    if (msg.Values == null || msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (msg.Values[i] != (((s + i) % 2) == 0)) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceOctet()
        {
            await RunRoundtrip<SequenceOctetTopic>(
                "AtomicTests::SequenceOctetTopic",
                550,
                (s) => {
                    var msg = new SequenceOctetTopic();
                    msg.Id = s;
                    int len = (s % 5) + 1;
                    var list = new List<byte>();
                    for(int i=0; i<len; i++) list.Add((byte)((s + i) % 255));
                    msg.Bytes = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 5) + 1;
                    if (msg.Bytes == null || msg.Bytes.Count != len) return false;
                    for(int i=0; i<len; i++) if (msg.Bytes[i] != (byte)((s + i) % 255)) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceString()
        {
            await RunRoundtrip<SequenceStringTopic>(
                "AtomicTests::SequenceStringTopic",
                560,
                (s) => {
                    var msg = new SequenceStringTopic();
                    msg.Id = s;
                    int len = (s % 5) + 1;
                    var list = new List<string>();
                    for(int i=0; i<len; i++) list.Add($"S_{s}_{i}");
                    msg.Values = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 5) + 1;
                    if (msg.Values == null || msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (msg.Values[i] != $"S_{s}_{i}") return false;
                    return true;
                }
            );
        }
        
        [Fact]
        public async Task TestSequenceEnum()
        {
            await RunRoundtrip<SequenceEnumTopic>(
                "AtomicTests::SequenceEnumTopic",
                570,
                (s) => {
                    var msg = new SequenceEnumTopic();
                    msg.Id = s;
                    int len = (s % 3) + 1;
                    var list = new List<SimpleEnum>();
                    for(int i=0; i<len; i++) list.Add((SimpleEnum)((s + i) % 3));
                    msg.Values = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 3) + 1;
                    if (msg.Values == null || msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (msg.Values[i] != (SimpleEnum)((s + i) % 3)) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceStruct()
        {
            await RunRoundtrip<SequenceStructTopic>(
                "AtomicTests::SequenceStructTopic",
                580,
                (s) => {
                    var msg = new SequenceStructTopic();
                    msg.Id = s;
                    int len = (s % 3) + 1;
                    var list = new List<Point2D>();
                    for(int i=0; i<len; i++) list.Add(new Point2D { X = (double)((s + i) + 0.1), Y = (double)((s + i) + 0.2) });
                    msg.Points = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 3) + 1;
                    if (msg.Points == null || msg.Points.Count != len) return false;
                    for(int i=0; i<len; i++) {
                        if (Math.Abs(msg.Points[i].X - ((s + i) + 0.1)) > 0.0001) return false;
                        if (Math.Abs(msg.Points[i].Y - ((s + i) + 0.2)) > 0.0001) return false;
                    }
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceUnion()
        {
            await RunRoundtrip<SequenceUnionTopic>(
                "AtomicTests::SequenceUnionTopic",
                590,
                (s) => {
                    var msg = new SequenceUnionTopic();
                    msg.Id = s;
                    int len = (s % 2) + 1;
                    var list = new List<SimpleUnion>();
                    for(int i=0; i<len; i++) {
                        var u = new SimpleUnion();
                        int disc = ((s + i) % 3) + 1;
                        u._d = disc;
                        if (disc == 1) u.Int_value = (s + i) * 10;
                        else if (disc == 2) u.Double_value = (s + i) * 2.5;
                        else if (disc == 3) u.String_value = $"U_{s}_{i}";
                        list.Add(u);
                    }
                    msg.Unions = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 2) + 1;
                    if (msg.Unions == null || msg.Unions.Count != len) return false;
                    for(int i=0; i<len; i++) {
                        int disc = ((s + i) % 3) + 1;
                        if (msg.Unions[i]._d != disc) return false;
                        if (disc == 1) { if (msg.Unions[i].Int_value != (s + i) * 10) return false; }
                        else if (disc == 2) { if (Math.Abs(msg.Unions[i].Double_value - ((s + i) * 2.5)) > 0.0001) return false; }
                        else if (disc == 3) { if (msg.Unions[i].String_value != $"U_{s}_{i}") return false; }
                    }
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceUnionAppendable()
        {
            await RunRoundtrip<SequenceUnionAppendableTopic>(
                "AtomicTests::SequenceUnionAppendableTopic",
                1500,
                (s) => {
                    var msg = new SequenceUnionAppendableTopic();
                    msg.Id = s;
                    int len = (s % 2) + 1;
                    var list = new List<SimpleUnionAppendable>();
                    for(int i=0; i<len; i++) {
                        var u = new SimpleUnionAppendable();
                        int disc = ((s + i) % 3) + 1;
                        u._d = disc;
                        if (disc == 1) u.Int_value = (s + i) * 10;
                        else if (disc == 2) u.Double_value = (s + i) * 2.5;
                        else if (disc == 3) u.String_value = $"U_{s}_{i}";
                        list.Add(u);
                    }
                    msg.Unions = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 2) + 1;
                    if (msg.Unions == null || msg.Unions.Count != len) return false;
                    for(int i=0; i<len; i++) {
                        int disc = ((s + i) % 3) + 1;
                        if (msg.Unions[i]._d != disc) return false;
                        if (disc == 1) { if (msg.Unions[i].Int_value != (s + i) * 10) return false; }
                        else if (disc == 2) { if (Math.Abs(msg.Unions[i].Double_value - ((s + i) * 2.5)) > 0.0001) return false; }
                        else if (disc == 3) { if (msg.Unions[i].String_value != $"U_{s}_{i}") return false; }
                    }
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceEnumAppendable()
        {
            await RunRoundtrip<SequenceEnumAppendableTopic>(
                "AtomicTests::SequenceEnumAppendableTopic",
                1510,
                (s) => {
                    var msg = new SequenceEnumAppendableTopic();
                    msg.Id = s;
                    int len = (s % 3) + 1;
                    var list = new List<ColorEnum>();
                    for(int i=0; i<len; i++) list.Add((ColorEnum)((s + i) % 6));
                    msg.Colors = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = (s % 3) + 1;
                    if (msg.Colors == null || msg.Colors.Count != len) return false;
                    for(int i=0; i<len; i++) {
                         if (msg.Colors[i] != (ColorEnum)((s + i) % 6)) return false;
                    }
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestSequenceInt32Appendable()
        {
            await RunRoundtrip<SequenceInt32TopicAppendable>(
                "AtomicTests::SequenceInt32TopicAppendable", 
                1500,
                (s) => { 
                    var msg = new SequenceInt32TopicAppendable();
                    msg.Id = s; 
                    int len = s % 6;
                    var list = new System.Collections.Generic.List<int>();
                    for(int i=0; i<len; i++) list.Add((int)((s + i) * 31));
                    msg.Values = list;
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    int len = s % 6;
                    if ((msg.Values == null) && (len == 0)) return true;
                    if (msg.Values == null) return false;
                    if (msg.Values.Count != len) return false;
                    for(int i=0; i<len; i++) if (msg.Values[i] != (int)((s + i) * 31)) return false;
                    return true;
                }
            );
        }
    }
}
