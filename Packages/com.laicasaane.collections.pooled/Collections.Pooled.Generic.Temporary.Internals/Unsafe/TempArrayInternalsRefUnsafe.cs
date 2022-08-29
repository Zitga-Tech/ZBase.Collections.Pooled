using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct TempArrayInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Length;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;

        public TempArrayInternalsRefUnsafe(in TempArray<T> source)
        {
            Length = source._length;
            ClearArray = TempArray<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class TempCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArrayInternalsRefUnsafe<T> GetRef<T>(
                in TempArray<T> source
            )
            => new TempArrayInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in this TempArray<T> source
            )
            => source._array.AsSpan(0, source._length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                  in this TempArray<T> source
                , int start
            )
            => source._array.AsSpan(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                  in this TempArray<T> source
                , int start, int length
            )
            => source._array.AsSpan(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                  in this TempArray<T> source
                , Index startIndex
            )
            => source._array.AsSpan(startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                  in this TempArray<T> source
                , Range range
            )
            => source._array.AsSpan(range);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<T>(
                in this TempArray<T> source
                , out T[] array
                , out int length
            )
        {
            array = source._array;
            length = source._length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create<T>(T[] array, int length, ArrayPool<T> pool)
            => TempArray<T>.Create(array, length, pool);
    }
}
