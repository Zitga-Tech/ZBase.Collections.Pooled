#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct ValueArrayHashSetInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int FreeEntryIndex;
        [NonSerialized] public readonly int Collisions;
        [NonSerialized] public readonly ulong FastModBucketsMultiplier;

        [NonSerialized] public readonly bool ClearEntries;

        [NonSerialized] public readonly ArrayEntry<T>[] Entries;
        [NonSerialized] public readonly int[] Buckets;

        [NonSerialized] public readonly ArrayPool<ArrayEntry<T>> EntryPool;
        [NonSerialized] public readonly ArrayPool<int> BucketPool;

        public ValueArrayHashSetInternalsRefUnsafe(in ValueArrayHashSet<T> source)
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

    partial class ValueCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayHashSetInternalsRefUnsafe<T> GetRef<T>(
                in ValueArrayHashSet<T> source
            )
            => new ValueArrayHashSetInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal Keys and Values arrays as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<ArrayEntry<T>> AsSpan<T>(
                in this ValueArrayHashSet<T> source
            )
            => source._entries.AsSpan(0, source.Count);

        /// <summary>
        /// Returns the internal Keys and Values arrays as a <see cref="Memory{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<ArrayEntry<T>> AsMemory<T>(
                in this ValueArrayHashSet<T> source
            )
            => source._entries.AsMemory(0, source.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<T>(
            in this ValueArrayHashSet<T> source
            , out ArrayEntry<T>[] entries
            , out int count
        )
        {
            entries = source._entries;
            count = source.Count;
        }
    }
}
