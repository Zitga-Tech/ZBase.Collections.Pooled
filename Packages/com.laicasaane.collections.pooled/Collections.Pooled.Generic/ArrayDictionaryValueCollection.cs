using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public readonly struct ArrayDictionaryValueCollection<TKey, TValue> : ICollection<TValue>
    {
        private readonly ArrayDictionary<TKey, TValue> _dictionary;

        internal ArrayDictionaryValueCollection(ArrayDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dictionary.Count;
        }

        public bool IsReadOnly => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(TValue item)
            => _dictionary.ContainsValue(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(TValue[] dest, int destIndex)
            => _dictionary._values.AsSpan(0, _dictionary.Count).CopyTo(dest.AsSpan(destIndex));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(_dictionary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            => new Enumerator(_dictionary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(_dictionary);

        void ICollection<TValue>.Add(TValue item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
        }

        void ICollection<TValue>.Clear()
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
        }

        bool ICollection<TValue>.Remove(TValue item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
            return false;
        }

        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly ArrayDictionary<TKey, TValue> _dictionary;
            private readonly int _count;

            private int _index;

            internal Enumerator(ArrayDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _index = -1;
                _count = dictionary.Count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
#if DEBUG
                if (_count != _dictionary.Count)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
#endif
                if (_index < _count - 1)
                {
                    ++_index;
                    return true;
                }

                return false;
            }

            public TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _dictionary._values[_index];
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose() { }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _dictionary._values[_index];
            }
        }
    }
}
