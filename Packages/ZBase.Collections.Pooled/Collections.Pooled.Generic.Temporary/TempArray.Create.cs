using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic
{
    partial struct TempArray<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create(int length)
            => new TempArray<T>(length, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create(int length, ArrayPool<T> pool)
            => new TempArray<T>(length, pool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Empty()
            => Create(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Empty(ArrayPool<T> pool)
            => Create(0, pool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create(in ReadOnlySpan<T> array)
            => new TempArray<T>(array, array.Length, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create(in ReadOnlySpan<T> array, ArrayPool<T> pool)
            => new TempArray<T>(array, array.Length, pool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create(in ReadOnlySpan<T> array, int length)
            => new TempArray<T>(array, length, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create(in ReadOnlySpan<T> array, int length, ArrayPool<T> pool)
            => new TempArray<T>(array, length, pool);
    }
}