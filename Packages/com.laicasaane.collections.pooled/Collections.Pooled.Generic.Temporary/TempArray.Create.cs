using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    partial struct TempArray<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create(int minLength)
            => new TempArray<T>(minLength, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Create(int minLength, ArrayPool<T> pool)
            => new TempArray<T>(minLength, pool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Empty()
            => Create(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArray<T> Empty(ArrayPool<T> pool)
            => Create(0, pool);
    }
}