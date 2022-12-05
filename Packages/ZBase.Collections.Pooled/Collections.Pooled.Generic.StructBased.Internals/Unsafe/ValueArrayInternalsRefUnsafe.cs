using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct ValueArrayInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Length;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;

        public ValueArrayInternalsRefUnsafe(in ValueArray<T> source)
        {
            Length = source._length;
            ClearArray = ValueArray<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class ValueCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayInternalsRefUnsafe<T> GetRef<T>(
                in ValueArray<T> source
            )
            => new ValueArrayInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in this ValueArray<T> source
            )
            => source._array.AsSpan(0, source._length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                  in this ValueArray<T> source
                , int start
            )
            => source._array.AsSpan(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                  in this ValueArray<T> source
                , int start, int length
            )
            => source._array.AsSpan(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                  in this ValueArray<T> source
                , Index startIndex
            )
            => source._array.AsSpan(startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                  in this ValueArray<T> source
                , Range range
            )
            => source._array.AsSpan(range);

        /// <summary>
        /// Returns the internal array as a <see cref="Memory{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> AsMemory<T>(
                in this ValueArray<T> source
            )
            => source._array.AsMemory(0, source._length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> AsMemory<T>(
                  in this ValueArray<T> source
                , int start
            )
            => source._array.AsMemory(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> AsMemory<T>(
                  in this ValueArray<T> source
                , int start, int length
            )
            => source._array.AsMemory(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> AsMemory<T>(
                  in this ValueArray<T> source
                , Index startIndex
            )
            => source._array.AsMemory(startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> AsMemory<T>(
                  in this ValueArray<T> source
                , Range range
            )
            => source._array.AsMemory(range);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<T>(
                in this ValueArray<T> source
                , out T[] array
                , out int length
            )
        {
            array = source._array;
            length = source._length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Create<T>(T[] array, int length, ArrayPool<T> pool)
            => ValueArray<T>.Create(array, length, pool);
    }
}
