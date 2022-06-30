using System.Collections.Generic;

namespace Collections.Pooled
{
    [System.Serializable]
    public readonly struct KVPair<TKey, TValue>
    {
        public readonly TKey Key;
        public readonly TValue Value;

        public KVPair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }

        public static implicit operator KeyValuePair<TKey, TValue>(in KVPair<TKey, TValue> kvp)
            => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);

        public static implicit operator KVPair<TKey, TValue>(in KeyValuePair<TKey, TValue> kvp)
            => new KVPair<TKey, TValue>(kvp.Key, kvp.Value);
    }
}
