using System;
using System.Threading.Tasks;
using Xunit;
using AtomicTests;

namespace CsharpToC.Roundtrip.Tests
{
    [Collection("Roundtrip Collection")]
    public class NestedStructTests : TestBase
    {
        public NestedStructTests(RoundtripFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestNestedStruct()
        {
            await RunRoundtrip<NestedStructTopic>(
                "AtomicTests::NestedStructTopic",
                600,
                (s) => {
                    var msg = new NestedStructTopic();
                    msg.Id = s;
                    msg.Point = new Point2D { X = s * 1.1, Y = s * 2.2 };
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    if (Math.Abs(msg.Point.X - (s * 1.1)) > 0.0001) return false;
                    if (Math.Abs(msg.Point.Y - (s * 2.2)) > 0.0001) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestNested3D()
        {
            await RunRoundtrip<Nested3DTopic>(
                "AtomicTests::Nested3DTopic",
                610,
                (s) => {
                    var msg = new Nested3DTopic();
                    msg.Id = s;
                    msg.Point = new Point3D { X = s + 1.0, Y = s + 2.0, Z = s + 3.0 };
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    if (Math.Abs(msg.Point.X - (s + 1.0)) > 0.0001) return false;
                    if (Math.Abs(msg.Point.Y - (s + 2.0)) > 0.0001) return false;
                    if (Math.Abs(msg.Point.Z - (s + 3.0)) > 0.0001) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestDoublyNested()
        {
            await RunRoundtrip<DoublyNestedTopic>(
                "AtomicTests::DoublyNestedTopic",
                620,
                (s) => {
                    var msg = new DoublyNestedTopic();
                    msg.Id = s;
                    msg.Box = new Box {
                        TopLeft = new Point2D { X = s, Y = s + 1.0 },
                        BottomRight = new Point2D { X = s + 10.0, Y = s + 11.0 }
                    };
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    if (Math.Abs(msg.Box.TopLeft.X - s) > 0.0001) return false;
                    if (Math.Abs(msg.Box.TopLeft.Y - (s + 1.0)) > 0.0001) return false;
                    if (Math.Abs(msg.Box.BottomRight.X - (s + 10.0)) > 0.0001) return false;
                    if (Math.Abs(msg.Box.BottomRight.Y - (s + 11.0)) > 0.0001) return false;
                    return true;
                }
            );
        }

        [Fact]
        public async Task TestComplexNested()
        {
            await RunRoundtrip<ComplexNestedTopic>(
                "AtomicTests::ComplexNestedTopic",
                630,
                (s) => {
                    var msg = new ComplexNestedTopic();
                    msg.Id = s;
                    msg.Container = new Container {
                        Count = s,
                        Radius = s * 0.5,
                        Center = new Point3D { X = s + 0.1, Y = s + 0.2, Z = s + 0.3 }
                    };
                    return msg;
                },
                (msg, s) => {
                    if (msg.Id != s) return false;
                    if (msg.Container.Count != s) return false;
                    if (Math.Abs(msg.Container.Radius - (s * 0.5)) > 0.0001) return false;
                    if (Math.Abs(msg.Container.Center.X - (s + 0.1)) > 0.0001) return false;
                    if (Math.Abs(msg.Container.Center.Y - (s + 0.2)) > 0.0001) return false;
                    if (Math.Abs(msg.Container.Center.Z - (s + 0.3)) > 0.0001) return false;
                    return true;
                }
            );
        }
    }
}
