using System;
using System.Collections.Generic;

namespace ZBase.Collections.Pooled
{
    public interface IReadOnlyArrayDictionary<TKey, TValue>
        : IReadOnlyDictionary<TKey, TValue>
        , IReadOnlyCollection<ArrayKVPair<TKey, TValue>>
        , IReadOnlyCollection<KVPair<TKey, TValue>>
    {
        TValue this[in TKey key] { get; set; }

        bool ContainsKey(in TKey key);

        bool ContainsValue(TValue value);

        bool ContainsValue(in TValue value);

        int GetIndex(in TKey key);

        int GetIndex(TKey key);

        bool TryFindIndex(in TKey key, out int findIndex);

        bool TryFindIndex(TKey key, out int findIndex);

        bool TryGetValue(in TKey key, out TValue result);

        void CopyTo(KVPair<TKey, TValue>[] dest);

        void CopyTo(KVPair<TKey, TValue>[] dest, int destIndex, int count);

        void CopyTo(in Span<KVPair<TKey, TValue>> dest);

        void CopyTo(in Span<KVPair<TKey, TValue>> dest, int destIndex);

        void CopyTo(in Span<KVPair<TKey, TValue>> dest, int destIndex, int count);
    }
}
