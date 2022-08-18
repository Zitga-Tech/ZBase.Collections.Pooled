using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public struct ReadOnlyArray<T> : IReadOnlyList<T>
    {
        private static readonly T[] s_emptyArray = new T[0];

        private readonly T[] _array;

        internal ReadOnlyArray(T[] array)
        {
            _array = array ?? s_emptyArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyArray<T> Empty()
            => new ReadOnlyArray<T>(s_emptyArray);

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array[index];
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int index)
            => _array.CopyTo(array, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(_array);

        int IReadOnlyCollection<T>.Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new Enumerator(_array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(_array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyArray<T>(T[] array)
            => new ReadOnlyArray<T>(array);

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly T[] _array;
            private readonly int _length;
            private int _index;
            private T _current;

            public Enumerator(T[] array)
            {
                _array = array ?? s_emptyArray;
                _length = _array.Length;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if ((uint)_index < (uint)_length)
                {
                    _current = _array[_index];
                    _index++;
                    return true;
                }

                _index = _length + 1;
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
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current!;
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }
        }
    }
}
