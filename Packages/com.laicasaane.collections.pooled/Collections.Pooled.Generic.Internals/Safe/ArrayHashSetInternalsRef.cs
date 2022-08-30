using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ArrayHashSetInternalsRef<T>
    {
        [NonSerialized] public readonly int FreeEntryIndex;
        [NonSerialized] public readonly int Collisions;
        [NonSerialized] public readonly ulong FastModBucketsMultiplier;

        [NonSerialized] public readonly bool ClearEntries;

        [NonSerialized] public readonly ReadOnlySpan<ArrayEntry<T>> Entries;
        [NonSerialized] public readonly ReadOnlySpan<int> Buckets;

        [NonSerialized] public readonly ArrayPool<ArrayEntry<T>> EntryPool;
        [NonSerialized] public readonly ArrayPool<int> BucketPool;

        public ArrayHashSetInternalsRef(ArrayHashSet<T> source)
        {
            FreeEntryIndex = source._freeEntryIndex;
            Collisions = source._collisions;
            FastModBucketsMultiplier = source._fastModBucketsMultiplier;

            ClearEntries = ArrayHashSet<T>.s_clearEntries;

            Entries = source._entries;
            Buckets = source._buckets;

            EntryPool = source._entryPool;
            BucketPool = source._bucketPool;
        }
    }

    partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayHashSetInternalsRef<T> GetRef<T>(
                ArrayHashSet<T> source
            )
            => new ArrayHashSetInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal Keys and Values arrays as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<ArrayEntry<T>> AsReadOnlySpan<T>(
                this ArrayHashSet<T> source
            )
            => source._entries.AsSpan(0, source.Count);
    }
}
