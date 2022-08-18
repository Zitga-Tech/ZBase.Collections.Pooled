#pragma warning disable CS8632

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public readonly struct ValueKVPairDictionary<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
    {
        private readonly ValueDictionary<TKey, TValue> _dictionary;

        public ValueKVPairDictionary(in ValueDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(_dictionary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
            => new Enumerator(_dictionary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(_dictionary);

        public struct Enumerator : IEnumerator<KVPair<TKey, TValue>>
        {
            private readonly ValueDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private KVPair<TKey, TValue> _current;

            public Enumerator(in ValueDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                while ((uint)_index < (uint)_dictionary._count)
                {
                    ref Entry<TKey, TValue> entry = ref _dictionary._entries![_index++];

                    if (entry.Next >= -1)
                    {
                        _current = new KVPair<TKey, TValue>(entry.Key, entry.Value);
                        return true;
                    }
                }

                _index = _dictionary._count + 1;
                _current = default;
                return false;
            }

            public KVPair<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            public void Dispose() { }

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return new KVPair<TKey, TValue>(_current.Key, _current.Value);
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _dictionary._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _index = 0;
                _current = default;
            }
        }
    }

    public static partial class ValueDictionaryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueKVPairDictionary<TKey, TValue> ToValueKVPairDictionary<TKey, TValue>(
                in this ValueDictionary<TKey, TValue> dictionary
            )
            => new ValueKVPairDictionary<TKey, TValue>(dictionary);
    }
}
