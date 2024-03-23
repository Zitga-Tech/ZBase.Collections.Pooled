using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic
{
    public readonly ref struct TempKVPairDictionary<TKey, TValue>
    {
        private readonly TempDictionary<TKey, TValue> _dictionary;

        public TempKVPairDictionary(in TempDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dictionary.Count;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dictionary.IsValid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(_dictionary);

        public ref struct Enumerator
        {
            private readonly TempDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private KVPair<TKey, TValue> _current;

            public Enumerator(in TempDictionary<TKey, TValue> dictionary)
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
        }
    }

    public static partial class TempDictionaryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempKVPairDictionary<TKey, TValue> ToTempKVPairDictionary<TKey, TValue>(
                in this TempDictionary<TKey, TValue> dictionary
            )
            => new TempKVPairDictionary<TKey, TValue>(dictionary);
    }
}
