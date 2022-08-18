using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    partial struct ValueArray<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Create(int minLength)
            => new ValueArray<T>(minLength, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Create(int minLength, ArrayPool<T> pool)
            => new ValueArray<T>(minLength, pool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Empty()
            => Create(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Empty(ArrayPool<T> pool)
            => Create(0, pool);
    }
}