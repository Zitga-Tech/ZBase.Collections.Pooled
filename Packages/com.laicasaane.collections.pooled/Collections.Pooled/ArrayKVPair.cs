using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled
{
    public readonly struct ArrayKVPair<TKey, TValue>
    {
        private readonly TValue[] _values;
        private readonly TKey _key;
        private readonly int _index;

        public ArrayKVPair(TKey keys, TValue[] values, int index)
        {
            _values = values;
            _index = index;
            _key = keys;
        }

        public TKey Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _key;
        }

        public ref TValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _values[_index];
        }

        public void Deconstruct(out TKey key, out TValue value)
        {
            key = _key;
            value = _values[_index];
        }

        public static implicit operator KVPair<TKey, TValue>(in ArrayKVPair<TKey, TValue> kvp)
            => new KVPair<TKey, TValue>(kvp.Key, kvp.Value);

        public static implicit operator KeyValuePair<TKey, TValue>(in ArrayKVPair<TKey, TValue> kvp)
            => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);
    }
}
