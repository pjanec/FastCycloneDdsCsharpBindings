using System.Runtime.CompilerServices;

namespace CycloneDDS.Core
{
    /// <summary>
    /// Single source of truth for XCDR2 alignment calculations.
    /// </summary>
    public static class AlignmentMath
    {
        /// <summary>
        /// Calculate next aligned position for given alignment.
        /// </summary>
        /// <param name="currentPosition">Current absolute position in stream</param>
        /// <param name="alignment">Required alignment (must be power of 2: 1, 2, 4, 8)</param>
        /// <returns>Next position aligned to boundary</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Align(int currentPosition, int alignment)
        {
            int mask = alignment - 1;
            int padding = (alignment - (currentPosition & mask)) & mask;
            return currentPosition + padding;
        }
    }
}
