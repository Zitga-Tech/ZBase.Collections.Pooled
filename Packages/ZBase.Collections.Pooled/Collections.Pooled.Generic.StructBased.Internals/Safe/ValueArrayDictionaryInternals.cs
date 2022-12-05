#pragma warning disable CS8632

using System;
using System.Buffers;

namespace ZBase.Collections.Pooled.Generic.Internals
{
    public readonly struct ValueArrayDictionaryInternals<TKey, TValue> : IDisposable
    {
        [NonSerialized] public readonly int FreeEntryIndex;
        [NonSerialized] public readonly int Collisions;
        [NonSerialized] public readonly ulong FastModBucketsMultiplier;

        [NonSerialized] public readonly bool ClearEntries;
        [NonSerialized] public readonly bool ClearValues;

        [NonSerialized] public readonly ArrayEntry<TKey>[] Entries;
        [NonSerialized] public readonly TValue[] Values;
        [NonSerialized] public readonly int[] Buckets;

        [NonSerialized] public readonly ArrayPool<ArrayEntry<TKey>> EntryPool;
        [NonSerialized] public readonly ArrayPool<TValue> ValuePool;
        [NonSerialized] public readonly ArrayPool<int> BucketPool;

        public ValueArrayDictionaryInternals(in ValueArrayDictionary<TKey, TValue> source)
        {
            FreeEntryIndex = source._freeEntryIndex;
            Collisions = source._collisions;
            FastModBucketsMultiplier = source._fastModBucketsMultiplier;

            ClearEntries = ValueArrayDictionary<TKey, TValue>.s_clearEntries;
            ClearValues = ValueArrayDictionary<TKey, TValue>.s_clearValues;

            Entries = source._entries;
            Values = source._values;
            Buckets = source._buckets;

            EntryPool = source._entryPool;
            ValuePool = source._valuePool;
            BucketPool = source._bucketPool;
        }

        public void Dispose()
        {
            if (Buckets.IsNullOrEmpty() == false)
            {
                try
                {
                    BucketPool?.Return(Buckets);
                }
                catch { }
            }

            if (Entries.IsNullOrEmpty() == false)
            {
                try
                {
                    EntryPool?.Return(Entries, ClearEntries);
                }
                catch { }
            }

            if (Values.IsNullOrEmpty() == false)
            {
                try
                {
                    ValuePool?.Return(Values, ClearValues);
                }
                catch { }
            }
        }
    }

    partial class ValueCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static ValueArrayDictionaryInternals<TKey, TValue> TakeOwnership<TKey, TValue>(
                ref ValueArrayDictionary<TKey, TValue> source
            )
        {
            var internals = new ValueArrayDictionaryInternals<TKey, TValue>(source);

            source._buckets = null;
            source._entries = null;
            source._values = null;
            source.Dispose();

            return internals;
        }
    }
}
