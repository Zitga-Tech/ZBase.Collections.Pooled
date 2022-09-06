using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ValueArrayHashSetInternalsRef<T>
    {
        [NonSerialized] public readonly int FreeEntryIndex;
        [NonSerialized] public readonly int Collisions;
        [NonSerialized] public readonly ulong FastModBucketsMultiplier;

        [NonSerialized] public readonly bool ClearEntries;

        [NonSerialized] public readonly ReadOnlySpan<ArrayEntry<T>> Entries;
        [NonSerialized] public readonly ReadOnlySpan<int> Buckets;

        [NonSerialized] public readonly ArrayPool<ArrayEntry<T>> EntryPool;
        [NonSerialized] public readonly ArrayPool<int> BucketPool;

        public ValueArrayHashSetInternalsRef(in ValueArrayHashSet<T> source)
        {
            FreeEntryIndex = source._freeEntryIndex;
            Collisions = source._collisions;
            FastModBucketsMultiplier = source._fastModBucketsMultiplier;

            ClearEntries = ValueArrayHashSet<T>.s_clearEntries;

            Entries = source._entries;
            Buckets = source._buckets;

            EntryPool = source._entryPool;
            BucketPool = source._bucketPool;
        }
    }

    partial class ValueCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayHashSetInternalsRef<T> GetRef<T>(
                in ValueArrayHashSet<T> source
            )
            => new ValueArrayHashSetInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal Keys and Values arrays as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<ArrayEntry<T>> AsReadOnlySpan<T>(
                in this ValueArrayHashSet<T> source
            )
            => source._entries.AsSpan(0, source.Count);

        /// <summary>
        /// Returns the internal Keys and Values arrays as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<ArrayEntry<T>> AsReadOnlyMemory<T>(
                in this ValueArrayHashSet<T> source
            )
            => source._entries.AsMemory(0, source.Count);
    }
}
