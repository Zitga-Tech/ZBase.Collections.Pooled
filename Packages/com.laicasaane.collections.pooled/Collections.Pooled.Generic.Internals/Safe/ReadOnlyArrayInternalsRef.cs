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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ReadOnlyArray<T> source
                , int start
            )
            => source._array.AsSpan(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ReadOnlyArray<T> source
                , int start, int length
            )
            => source._array.AsSpan(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ReadOnlyArray<T> source
                , Index startIndex
            )
            => source._array.AsSpan(startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ReadOnlyArray<T> source
                , Range range
            )
            => source._array.AsSpan(range);

    }
}