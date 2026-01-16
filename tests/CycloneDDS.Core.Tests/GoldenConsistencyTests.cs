using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using CycloneDDS.Core;

namespace CycloneDDS.Core.Tests
{
    public class GoldenConsistencyTests
    {
        // Hex strings from golden_data_generator.c output
        private const string Expected_SimplePrimitive = "15CD5B0777BE9F1A2FDD5E40";
        private const string Expected_NestedStruct = "AB000000B168DE3AAC1C5A643BDD8E40";
        private const string Expected_FixedString = "4669786564537472696E67313233000000000000000000000000000000000000";
        private const string Expected_UnboundedString = "76B2010014000000556E626F756E646564537472696E674461746100";
        private const string Expected_PrimitiveSequence = "050000000A000000140000001E0000002800000032000000";
        private const string Expected_StringSequence = "1E00000003000000040000004F6E65000400000054776F0006000000546872656500";
        private const string Expected_MixedStruct = "FF000000D5FDFFFFF168E388B5F8E43E0C0000004D69786564537472696E6700";
        private const string Expected_AppendableStruct = "13000000E70300000B000000417070656E6461626C6500";

        private string ToHex(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "");
        }

        [Fact]
        public void SimplePrimitive_MatchesGolden()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new CdrWriter(buffer);
            
            // struct SimplePrimitive { long id; double value; };
            writer.WriteInt32(123456789);
            writer.WriteDouble(123.456);
            
            writer.Complete();
            Assert.Equal(Expected_SimplePrimitive, ToHex(buffer.WrittenSpan.ToArray()));
        }

        [Fact]
        public void NestedStruct_MatchesGolden()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new CdrWriter(buffer);

            // struct NestedStruct { octet byte_field; Nested nested; };
            // struct Nested { long a; double b; };
            writer.WriteByte(0xAB);
            writer.Align(4); // Observed alignment in golden data
            
            // Nested.a
            writer.WriteInt32(987654321);
            // Nested.b (double) - Align(8)
            writer.Align(8);
            writer.WriteDouble(987.654);

            writer.Complete();
            Assert.Equal(Expected_NestedStruct, ToHex(buffer.WrittenSpan.ToArray()));
        }

        [Fact]
        public void FixedString_MatchesGolden()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new CdrWriter(buffer);

            // struct FixedString { char message[32]; };
            var str = "FixedString123";
            var utf8 = Encoding.UTF8.GetBytes(str);
            writer.WriteFixedString(utf8, 32);

            writer.Complete();
            Assert.Equal(Expected_FixedString, ToHex(buffer.WrittenSpan.ToArray()));
        }

        [Fact]
        public void UnboundedString_MatchesGolden()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new CdrWriter(buffer);

            // struct UnboundedString { long id; string message; };
            writer.WriteInt32(111222);
            writer.WriteString("UnboundedStringData");

            writer.Complete();
            Assert.Equal(Expected_UnboundedString, ToHex(buffer.WrittenSpan.ToArray()));
        }

        [Fact]
        public void PrimitiveSequence_MatchesGolden()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new CdrWriter(buffer);

            // struct PrimitiveSequence { sequence<long> values; };
            int[] values = { 10, 20, 30, 40, 50 };
            writer.WriteInt32(values.Length);
            foreach (var v in values) writer.WriteInt32(v);

            writer.Complete();
            Assert.Equal(Expected_PrimitiveSequence, ToHex(buffer.WrittenSpan.ToArray()));
        }

        [Fact]
        public void StringSequence_MatchesGolden()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new CdrWriter(buffer);

            // struct StringSequence { sequence<string> values; };
            // Has DHEADER
            
            // Calculate body size
            var sizer = new CdrSizer(0);
            string[] strings = { "One", "Two", "Three" };
            sizer.WriteInt32(strings.Length);
            foreach (var s in strings) sizer.WriteString(s);
            int bodySize = sizer.Position;

            // Write DHEADER
            writer.WriteUInt32((uint)bodySize);

            // Write Body
            writer.WriteInt32(strings.Length);
            foreach (var s in strings) writer.WriteString(s);

            writer.Complete();
            Assert.Equal(Expected_StringSequence, ToHex(buffer.WrittenSpan.ToArray()));
        }

        [Fact]
        public void MixedStruct_MatchesGolden()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new CdrWriter(buffer);

            // struct MixedStruct { octet b; long i; double d; string s; };
            writer.WriteByte(0xFF);
            writer.Align(4);
            writer.WriteInt32(-555);
            writer.Align(8);
            writer.WriteDouble(0.00001);
            writer.WriteString("MixedString");

            writer.Complete();
            Assert.Equal(Expected_MixedStruct, ToHex(buffer.WrittenSpan.ToArray()));
        }

        [Fact]
        public void AppendableStruct_MatchesGolden()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new CdrWriter(buffer);

            // @appendable struct AppendableStruct { long id; string message; };
            // Has DHEADER.
            
            // Calculate body size
            var sizer = new CdrSizer(0);
            sizer.WriteInt32(999);
            sizer.WriteString("Appendable");
            int bodySize = sizer.Position;

            // Write DHEADER
            writer.WriteUInt32((uint)bodySize);

            // Write Body
            writer.WriteInt32(999);
            writer.WriteString("Appendable");

            writer.Complete();
            Assert.Equal(Expected_AppendableStruct, ToHex(buffer.WrittenSpan.ToArray()));
        }
    }
}
