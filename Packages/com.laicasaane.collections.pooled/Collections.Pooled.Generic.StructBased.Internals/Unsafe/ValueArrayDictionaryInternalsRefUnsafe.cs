#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct ValueArrayDictionaryInternalsRefUnsafe<TKey, TValue>
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

        public ValueArrayDictionaryInternalsRefUnsafe(in ValueArrayDictionary<TKey, TValue> source)
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
    }

    partial class ValueCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayDictionaryInternalsRefUnsafe<TKey, TValue> GetRef<TKey, TValue>(
                in ValueArrayDictionary<TKey, TValue> source
            )
            => new ValueArrayDictionaryInternalsRefUnsafe<TKey, TValue>(source);

        /// <summary>
        /// Returns the internal Keys and Values arrays as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsSpan<TKey, TValue>(
            in ValueArrayDictionary<TKey, TValue> source
            , out ReadOnlySpan<ArrayEntry<TKey>> keys
            , out ReadOnlySpan<TValue> values
        )
        {
            keys = source._entries.AsSpan(0, source.Count);
            values = source._values.AsSpan(0, source.Count);
        }

        /// <summary>
        /// Returns the internal Keys array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<ArrayEntry<TKey>> AsSpanKeys<TKey, TValue>(
                in ValueArrayDictionary<TKey, TValue> source
            )
            => source._entries.AsSpan(0, source.Count);

        /// <summary>
        /// Returns the internal Values array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TValue> AsSpanValues<TKey, TValue>(
                in ValueArrayDictionary<TKey, TValue> source
            )
            => source._values.AsSpan(0, source.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<TKey, TValue>(
            this in ValueArrayDictionary<TKey, TValue> source
            , out ArrayEntry<TKey>[] keys
            , out TValue[] values
            , out int count
        )
        {
            keys = source._entries;
            values = source._values;
            count = source.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafeKeys<TKey, TValue>(
            this in ValueArrayDictionary<TKey, TValue> source
            , out ArrayEntry<TKey>[] keys
            , out int count
        )
        {
            keys = source._entries;
            count = source.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafeValues<TKey, TValue>(
            this in ValueArrayDictionary<TKey, TValue> source
            , out TValue[] values
            , out int count
        )
        {
            values = source._values;
            count = source.Count;
        }
    }
}
