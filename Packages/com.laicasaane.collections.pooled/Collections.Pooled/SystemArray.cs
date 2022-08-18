using System.Runtime.CompilerServices;

namespace Collections.Pooled
{
    public static class SystemArray
    {
        /// <summary>Gets the maximum number of elements that may be contained in an array.</summary>
        /// <returns>The maximum count of elements allowed in any array.</returns>
        /// <remarks>
        /// <para>This property represents a runtime limitation, the maximum number of elements (not bytes)
        /// the runtime will allow in an array. There is no guarantee that an allocation under this length
        /// will succeed, but all attempts to allocate a larger array will fail.</para>
        /// <para>This property only applies to single-dimension, zero-bound (SZ) arrays.
        /// <see cref="Length"/> property may return larger value than this property for multi-dimensional arrays.</para>
        /// </remarks>
        public const int MaxLength =
            // Keep in sync with `inline SIZE_T MaxArrayLength()` from gchelpers and HashHelpers.MaxPrimeArrayLength.
            0X7FFFFFC7;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this T[] array)
            => array == null || array.Length == 0;
    }
}
