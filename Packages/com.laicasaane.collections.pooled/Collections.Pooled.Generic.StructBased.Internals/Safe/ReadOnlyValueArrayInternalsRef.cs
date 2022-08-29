using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    partial class ValueCollectionInternals
    {
        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                in this ReadOnlyValueArray<T> source
            )
            => source._array.AsReadOnlySpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ReadOnlyValueArray<T> source
                , int start
            )
            => source._array.AsReadOnlySpan(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ReadOnlyValueArray<T> source
                , int start, int length
            )
            => source._array.AsReadOnlySpan(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ReadOnlyValueArray<T> source
                , Index startIndex
            )
            => source._array.AsReadOnlySpan(startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ReadOnlyValueArray<T> source
                , Range range
            )
            => source._array.AsReadOnlySpan(range);
    }
}