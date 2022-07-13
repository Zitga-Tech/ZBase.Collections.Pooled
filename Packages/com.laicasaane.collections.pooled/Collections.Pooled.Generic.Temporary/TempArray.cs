using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public ref struct TempArray<T>
    {
        internal static readonly bool s_clearArray = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private static readonly T[] s_emptyArray = new T[0];

        internal T[] _array; // Do not rename (binary serialization)

        [NonSerialized]
        internal ArrayPool<T> _pool;

        public TempArray(int length)
            : this(length, ArrayPool<T>.Shared)
        { }

        public TempArray(int length, ArrayPool<T> pool)
        {
            _pool = pool ?? ArrayPool<T>.Shared;
            _array = pool.Rent(length);
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.Length;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[index];
        }

        private void ReturnArray(T[] replaceWith)
        {
            if (_array?.Length > 0)
            {
                try
                {
                    _pool.Return(_array, s_clearArray);
                }
                catch { }
            }

            _array = replaceWith ?? s_emptyArray;
        }

        public void Dispose()
        {
            ReturnArray(s_emptyArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T>.Enumerator GetEnumerator()
            => _array.AsSpan().GetEnumerator();
    }
}
