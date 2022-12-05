using System.Collections.Generic;

namespace ZBase.Collections.Pooled
{
    public interface IArrayDictionary<TKey, TValue>
        : IDictionary<TKey, TValue>
        , IReadOnlyArrayDictionary<TKey, TValue>
        , ICollection<ArrayKVPair<TKey, TValue>>
        , ICollection<KVPair<TKey, TValue>>
    {
        void Add(in TKey key, in TValue value);

        void Add(in TKey key, TValue value);

        void Add(TKey key, in TValue value);

        void EnsureCapacity(int capacity);

        void IncreaseCapacityBy(int capacity);

        bool Remove(in TKey key);

        bool Remove(in TKey key, out int index, out TValue value);

        bool Remove(TKey key, out int index, out TValue value);

        void Set(in TKey key, in TValue value);

        void Set(in TKey key, TValue value);

        void Set(TKey key, in TValue value);

        void Set(TKey key, TValue value);

        bool TryAdd(in TKey key, in TValue value, out int index);

        bool TryAdd(in TKey key, TValue value, out int index);

        bool TryAdd(TKey key, in TValue value, out int index);

        bool TryAdd(TKey key, TValue value, out int index);
    }
}
