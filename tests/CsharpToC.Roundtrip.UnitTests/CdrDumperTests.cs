using System;
using System.IO;
using Xunit;
using CsharpToC.Roundtrip.Tests;

namespace CsharpToC.Roundtrip.UnitTests
{
    public class CdrDumperTests
    {
        [Fact]
        public void SaveBin_CreatesFile()
        {
            // Arrange
            string topic = "TestTopic";
            int seed = 123;
            string suffix = "test";
            byte[] data = new byte[] { 0x01, 0x02, 0x03 };
            
            // Cleanup potential previous run
            string expectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "cdr_dumps", $"{topic}_{seed}_{suffix}.bin");
            if (File.Exists(expectedPath)) File.Delete(expectedPath);

            // Act
            CdrDumper.SaveBin(topic, seed, suffix, data);

            // Assert
            Assert.True(File.Exists(expectedPath), $"File should exist at {expectedPath}");
            byte[] readBack = File.ReadAllBytes(expectedPath);
            Assert.Equal(data, readBack);
        }

        [Fact]
        public void Compare_IdenticalArrays_ReturnsTrue()
        {
            byte[] a = new byte[] { 1, 2, 3 };
            byte[] b = new byte[] { 1, 2, 3 };
            
            bool result = CdrDumper.Compare(a, b, out string error);
            
            Assert.True(result);
            Assert.Empty(error);
        }

        [Fact]
        public void Compare_DifferentLength_ReturnsFalse()
        {
            byte[] a = new byte[] { 1, 2, 3 };
            byte[] b = new byte[] { 1, 2 };
            
            bool result = CdrDumper.Compare(a, b, out string error);
            
            Assert.False(result);
            Assert.Contains("Length mismatch", error);
        }

        [Fact]
        public void Compare_DifferentContent_ReturnsFalse()
        {
            byte[] a = new byte[] { 1, 2, 3 };
            byte[] b = new byte[] { 1, 2, 4 };
            
            bool result = CdrDumper.Compare(a, b, out string error);
            
            Assert.False(result);
            Assert.Contains("Byte mismatch at index 2", error);
        }
    }
}
