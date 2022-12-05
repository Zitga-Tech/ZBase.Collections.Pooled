using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic
{
    public readonly struct ArrayDictionaryKeyCollection<TKey, TValue> : ICollection<TKey>
    {
        private readonly ArrayDictionary<TKey, TValue> _dictionary;

        internal ArrayDictionaryKeyCollection(ArrayDictionary<TKey, TValue> dictionary)
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
        public bool Contains(TKey item)
            => _dictionary.ContainsKey(item);

        public void CopyTo(TKey[] dest, int destIndex)
        {
            if (destIndex < 0 || destIndex > dest.Length)
            {
                ThrowHelper.ThrowDestIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (dest.Length - destIndex < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            Span<ArrayEntry<TKey>> keys = _dictionary._entries.AsSpan();

            if (keys.Length == 0)
                return;

            for (int i = 0, len = _dictionary.Count; i < len; i++)
            {
                dest[destIndex++] = keys[i].Key;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(_dictionary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            => new Enumerator(_dictionary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(_dictionary);

        void ICollection<TKey>.Add(TKey item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
        }

        void ICollection<TKey>.Clear()
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
        }

        bool ICollection<TKey>.Remove(TKey item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
            return false;
        }

        public struct Enumerator : IEnumerator<TKey>
        {
            private readonly ArrayDictionary<TKey, TValue> _dictionary;
            private readonly int _count;

            private int _index;

            public Enumerator(ArrayDictionary<TKey, TValue> dictionary)
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

            public TKey Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _dictionary._entries[_index].Key;
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose() { }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _dictionary._entries[_index].Key;
            }
        }
    }
}