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
            // Adjust for XCDR stream offset (Header is 4 bytes)
            // Alignment is relative to the body, not the absolute stream start in this context
            int effectivePos = currentPosition - 4;
            int mask = alignment - 1;
            int padding = (alignment - (effectivePos & mask)) & mask;
            return currentPosition + padding;
        }
    }
}
