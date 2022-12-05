#pragma warning disable CS8632

using System;
using System.Buffers;

namespace ZBase.Collections.Pooled.Generic.Internals
{
    public readonly struct TempArrayHashSetInternals<T> : IDisposable
    {
        [NonSerialized] public readonly int FreeEntryIndex;
        [NonSerialized] public readonly int Collisions;
        [NonSerialized] public readonly ulong FastModBucketsMultiplier;

        [NonSerialized] public readonly bool ClearEntries;

        [NonSerialized] public readonly ArrayEntry<T>[] Entries;
        [NonSerialized] public readonly int[] Buckets;

        [NonSerialized] public readonly ArrayPool<ArrayEntry<T>> EntryPool;
        [NonSerialized] public readonly ArrayPool<int> BucketPool;

        public TempArrayHashSetInternals(in TempArrayHashSet<T> source)
        {
            FreeEntryIndex = source._freeEntryIndex;
            Collisions = source._collisions;
            FastModBucketsMultiplier = source._fastModBucketsMultiplier;

            ClearEntries = TempArrayHashSet<T>.s_clearEntries;

            Entries = source._entries;
            Buckets = source._buckets;

            EntryPool = source._entryPool;
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
        }
    }

    partial class TempCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static TempArrayHashSetInternals<T> TakeOwnership<T>(
                ref TempArrayHashSet<T> source
            )
        {
            var internals = new TempArrayHashSetInternals<T>(source);

            source._buckets = null;
            source._entries = null;
            source.Dispose();

            return internals;
        }
    }
}
