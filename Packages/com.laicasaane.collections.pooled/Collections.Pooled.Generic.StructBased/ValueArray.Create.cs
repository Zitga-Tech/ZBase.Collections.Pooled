using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    partial struct ValueArray<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Create(int minLength)
            => new(minLength, ArrayPool<T>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArray<T> Create(int minLength, ArrayPool<T> pool)
            => new(minLength, pool);
    }
}