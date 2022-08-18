using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    partial class CollectionInternals
    {
        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                in this ReadOnlyArray<T> source
            )
            => source._array.AsSpan();
    }
}