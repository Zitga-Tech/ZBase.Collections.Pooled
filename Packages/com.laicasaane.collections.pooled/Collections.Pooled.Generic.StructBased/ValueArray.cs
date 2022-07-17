using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Collections.Pooled.Generic
{
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Length}")]
    [Serializable]
    public partial struct ValueArray<T> : IEnumerable<T>, IDisposable, IDeserializationCallback
    {
        internal static readonly bool s_clearArray = SystemRuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private static readonly T[] s_emptyArray = new T[0];

        internal T[] _array; // Do not rename (binary serialization)
        internal int _length; // Do not rename (binary serialization)

        [NonSerialized]
        internal ArrayPool<T> _pool;

        internal ValueArray(int length, ArrayPool<T> pool)
        {
            _pool = pool ?? ArrayPool<T>.Shared;
            _length = length;
            _array = pool.Rent(length);
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        public int Capacity
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
            _length = 0;
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            // We can't serialize array pools, so deserialized PooledLists will
            // have to use the shared pool, even if they were using a custom pool
            // before serialization.
            _pool = ArrayPool<T>.Shared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T>.Enumerator GetEnumerator()
            => _array.AsSpan(0, _length).GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new Enumerator(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ValueArray<T> _array;
            private int _index;
            private T _current;

            internal Enumerator(ValueArray<T> array)
            {
                _array = array;
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (((uint)_index < (uint)_array.Length))
                {
                    _current = _array._array[_index];
                    _index++;
                    return true;
                }

                _index = _array.Length + 1;
                _current = default;
                return false;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current!;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _array.Length + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _index = 0;
                _current = default;
            }
        }
    }
}
