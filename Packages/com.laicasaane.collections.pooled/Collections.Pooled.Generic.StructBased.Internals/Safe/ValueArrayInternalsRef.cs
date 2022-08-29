using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ValueArrayInternalsRef<T>
    {
        [NonSerialized] public readonly int Length;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly ReadOnlySpan<T> Array;

        public ValueArrayInternalsRef(in ValueArray<T> source)
        {
            Length = source._length;
            ClearArray = ValueArray<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class ValueCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayInternalsRef<T> GetRef<T>(
                in ValueArray<T> source
            )
            => new ValueArrayInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                in this ValueArray<T> source
            )
            => source._array.AsSpan(0, source._length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ValueArray<T> source
                , int start
            )
            => source._array.AsSpan(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ValueArray<T> source
                , int start, int length
            )
            => source._array.AsSpan(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ValueArray<T> source
                , Index startIndex
            )
            => source._array.AsSpan(startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this ValueArray<T> source
                , Range range
            )
            => source._array.AsSpan(range);
    }
}
