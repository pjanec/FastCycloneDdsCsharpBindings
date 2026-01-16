using Xunit;
using CycloneDDS.Core;

namespace CycloneDDS.Core.Tests
{
    public class AlignmentMathTests
    {
        [Fact]
        public void Align_0_4_Returns_0()
        {
            Assert.Equal(0, AlignmentMath.Align(0, 4));
        }

        [Fact]
        public void Align_1_4_Returns_4()
        {
            Assert.Equal(4, AlignmentMath.Align(1, 4));
        }

        [Fact]
        public void Align_2_4_Returns_4()
        {
            Assert.Equal(4, AlignmentMath.Align(2, 4));
        }

        [Fact]
        public void Align_3_4_Returns_4()
        {
            Assert.Equal(4, AlignmentMath.Align(3, 4));
        }

        [Fact]
        public void Align_5_8_Returns_8()
        {
            Assert.Equal(8, AlignmentMath.Align(5, 8));
        }

        [Fact]
        public void Align_7_2_Returns_8()
        {
            Assert.Equal(8, AlignmentMath.Align(7, 2));
        }

        [Fact]
        public void Align_100_1_Returns_100()
        {
            Assert.Equal(100, AlignmentMath.Align(100, 1));
        }

        [Fact]
        public void Align_0_8_Returns_0()
        {
            Assert.Equal(0, AlignmentMath.Align(0, 8));
        }
    }
}
