using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic
{
    partial struct ValueArray<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Create(int length)
            => new ValueArray<T>(length, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Create(int length, ArrayPool<T> pool)
            => new ValueArray<T>(length, pool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Empty()
            => Create(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Empty(ArrayPool<T> pool)
            => Create(0, pool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ValueArray<T> Create(in ReadOnlySpan<T> array)
            => new ValueArray<T>(array, array.Length, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ValueArray<T> Create(in ReadOnlySpan<T> array, ArrayPool<T> pool)
            => new ValueArray<T>(array, array.Length, pool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ValueArray<T> Create(in ReadOnlySpan<T> array, int length)
            => new ValueArray<T>(array, length, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ValueArray<T> Create(in ReadOnlySpan<T> array, int length, ArrayPool<T> pool)
            => new ValueArray<T>(array, length, pool);
    }
}