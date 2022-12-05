using System;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic.Internals
{
    public readonly ref struct TempArrayInternalsRef<T>
    {
        [NonSerialized] public readonly int Length;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly ReadOnlySpan<T> Array;

        public TempArrayInternalsRef(in TempArray<T> source)
        {
            Length = source._length;
            ClearArray = TempArray<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class TempCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArrayInternalsRef<T> GetRef<T>(
                in TempArray<T> source
            )
            => new TempArrayInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                in this TempArray<T> source
            )
            => source._array.AsSpan(0, source._length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this TempArray<T> source
                , int start
            )
            => source._array.AsSpan(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this TempArray<T> source
                , int start, int length
            )
            => source._array.AsSpan(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this TempArray<T> source
                , Index startIndex
            )
            => source._array.AsSpan(startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                  in this TempArray<T> source
                , Range range
            )
            => source._array.AsSpan(range);

        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(
                in this TempArray<T> source
            )
            => source._array.AsMemory(0, source._length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(
                  in this TempArray<T> source
                , int start
            )
            => source._array.AsMemory(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(
                  in this TempArray<T> source
                , int start, int length
            )
            => source._array.AsMemory(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(
                  in this TempArray<T> source
                , Index startIndex
            )
            => source._array.AsMemory(startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(
                  in this TempArray<T> source
                , Range range
            )
            => source._array.AsMemory(range);
    }
}
