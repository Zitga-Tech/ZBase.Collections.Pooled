#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct ArrayDictionaryInternalsRefUnsafe<TKey, TValue>
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

        public ArrayDictionaryInternalsRefUnsafe(ArrayDictionary<TKey, TValue> source)
        {
            FreeEntryIndex = source._freeEntryIndex;
            Collisions = source._collisions;
            FastModBucketsMultiplier = source._fastModBucketsMultiplier;

            ClearEntries = ArrayDictionary<TKey, TValue>.s_clearEntries;
            ClearValues = ArrayDictionary<TKey, TValue>.s_clearValues;

            Entries = source._entries;
            Values = source._values;
            Buckets = source._buckets;

            EntryPool = source._entryPool;
            ValuePool = source._valuePool;
            BucketPool = source._bucketPool;
        }
    }

    partial class CollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayDictionaryInternalsRefUnsafe<TKey, TValue> GetRef<TKey, TValue>(
                ArrayDictionary<TKey, TValue> source
            )
            => new ArrayDictionaryInternalsRefUnsafe<TKey, TValue>(source);

        /// <summary>
        /// Returns the internal Keys and Values arrays as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsSpan<TKey, TValue>(
            ArrayDictionary<TKey, TValue> source
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
                ArrayDictionary<TKey, TValue> source
            )
            => source._entries.AsSpan(0, source.Count);

        /// <summary>
        /// Returns the internal Values array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TValue> AsSpanValues<TKey, TValue>(
                ArrayDictionary<TKey, TValue> source
            )
            => source._values.AsSpan(0, source.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<TKey, TValue>(
            this ArrayDictionary<TKey, TValue> source
            , out ArrayEntry<TKey>[] entries
            , out TValue[] values
            , out int count
        )
        {
            entries = source._entries;
            values = source._values;
            count = source.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<TKey, TValue>(
            this ArrayDictionary<TKey, TValue> source
            , out ArrayEntry<TKey>[] entries
            , out int count
        )
        {
            entries = source._entries;
            count = source.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<TKey, TValue>(
            this ArrayDictionary<TKey, TValue> source
            , out TValue[] values
            , out int count
        )
        {
            values = source._values;
            count = source.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetDirectValueByRef<TKey, TValue>(
                ArrayDictionary<TKey, TValue> dictionary
                , int index
            )
            => ref dictionary.GetDirectValueByRef(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue>(
                ArrayDictionary<TKey, TValue> dictionary
                , TKey key
            )
            => ref dictionary.GetOrAdd(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue>(
                ArrayDictionary<TKey, TValue> dictionary
                , in TKey key
            )
            => ref dictionary.GetOrAdd(in key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue>(
                ArrayDictionary<TKey, TValue> dictionary
                , TKey key
                , Func<TValue> builder
            )
            => ref dictionary.GetOrAdd(key, builder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue>(
                ArrayDictionary<TKey, TValue> dictionary
                , in TKey key
                , Func<TValue> builder
            )
            => ref dictionary.GetOrAdd(in key, builder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue, W>(
                ArrayDictionary<TKey, TValue> dictionary
                , TKey key
                , FuncRef<W, TValue> builder
                , ref W parameter
            )
            => ref dictionary.GetOrAdd(key, builder, ref parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue, W>(
                ArrayDictionary<TKey, TValue> dictionary
                , in TKey key
                , FuncRef<W, TValue> builder
                , ref W parameter
            )
            => ref dictionary.GetOrAdd(in key, builder, ref parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetValueByRef<TKey, TValue>(
                ArrayDictionary<TKey, TValue> dictionary
                , TKey key
            )
            => ref dictionary.GetValueByRef(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetValueByRef<TKey, TValue>(
                ArrayDictionary<TKey, TValue> dictionary
                , in TKey key
            )
            => ref dictionary.GetValueByRef(in key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue RecycleOrAdd<TKey, TValue, TValueProxy>(
                ArrayDictionary<TKey, TValue> dictionary
                , TKey key
                , Func<TValueProxy> builder
                , ActionRef<TValueProxy> recycler
            )
                where TValueProxy : class, TValue
            => ref dictionary.RecycleOrAdd(key, builder, recycler);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue RecycleOrAdd<TKey, TValue, TValueProxy>(
                ArrayDictionary<TKey, TValue> dictionary
                , in TKey key
                , Func<TValueProxy> builder
                , ActionRef<TValueProxy> recycler
            )
                where TValueProxy : class, TValue
            => ref dictionary.RecycleOrAdd(in key, builder, recycler);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue RecycleOrAdd<TKey, TValue, TValueProxy, U>(
                ArrayDictionary<TKey, TValue> dictionary
                , TKey key
                , FuncRef<U, TValue> builder
                , ActionRef<TValueProxy, U> recycler
                , ref U parameter
            )
                where TValueProxy : class, TValue
            => ref dictionary.RecycleOrAdd(key, builder, recycler, ref parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue RecycleOrAdd<TKey, TValue, TValueProxy, U>(
                ArrayDictionary<TKey, TValue> dictionary
                , in TKey key
                , FuncRef<U, TValue> builder
                , ActionRef<TValueProxy, U> recycler
                , ref U parameter
            )
                where TValueProxy : class, TValue
            => ref dictionary.RecycleOrAdd(in key, builder, recycler, ref parameter);

    }
}
