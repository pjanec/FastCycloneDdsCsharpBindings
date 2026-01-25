using System;
using Xunit;
using CsharpToC.Roundtrip.Tests;
using AtomicTests;
using CycloneDDS.Core;

namespace CsharpToC.Roundtrip.UnitTests
{
    public class SerializerHelperTests
    {
        [Fact]
        public void Serialize_BooleanTopic_GeneratesCorrectBytes()
        {
            // Arrange
            var msg = new BooleanTopic { Id = 1, Value = true };
            byte encodingKind = 0x09; // XCDR2 LE

            // Act
            // This relies on the Source Generator having run for BooleanTopic
            byte[] bytes = SerializerHelper.Serialize(msg, encodingKind);

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length >= 4); // At least header
            
            // Check Header: 00 09 00 00
            Assert.Equal(0x00, bytes[0]);
            Assert.Equal(0x09, bytes[1]);
            Assert.Equal(0x00, bytes[2]);
            Assert.Equal(0x00, bytes[3]);
            
            // BooleanTopic: Id (4 bytes), Value (1 byte)
            // Layout depends on alignment, but we expect some data.
            // Just verifying it runs without exception and produces output is a good start.
        }
    }
}
