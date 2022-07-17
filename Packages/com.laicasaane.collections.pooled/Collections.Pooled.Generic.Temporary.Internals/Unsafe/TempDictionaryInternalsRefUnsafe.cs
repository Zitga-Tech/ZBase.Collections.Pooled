#pragma warning disable CS8632

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly ref struct TempDictionaryInternalsRefUnsafe<TKey, TValue>
    {
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
        [NonSerialized] public readonly ulong FastModMultiplier;
#endif

        [NonSerialized] public readonly int Count;
        [NonSerialized] public readonly int FreeList;
        [NonSerialized] public readonly int FreeCount;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool IsReferenceKey;
        [NonSerialized] public readonly bool IsReferenceValue;
        [NonSerialized] public readonly bool ClearEntries;

        [NonSerialized] public readonly Span<int> Buckets;
        [NonSerialized] public readonly Span<Entry<TKey, TValue>> Entries;
        [NonSerialized] public readonly IEqualityComparer<TKey> Comparer;

        public TempDictionaryInternalsRefUnsafe(in TempDictionary<TKey, TValue> source)
        {
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
            FastModMultiplier = source._fastModMultiplier;
#endif

            Count = source._count;
            FreeList = source._freeList;
            FreeCount = source._freeCount;
            Version = source._version;
            IsReferenceKey = TempDictionary<TKey, TValue>.s_isReferenceKey;
            IsReferenceValue = TempDictionary<TKey, TValue>.s_isReferenceValue;
            ClearEntries = TempDictionary<TKey, TValue>.s_clearEntries;
            Buckets = source._buckets;
            Entries = source._entries;
            Comparer = source._comparer;
        }
    }

    partial class TempCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempDictionaryInternalsRefUnsafe<TKey, TValue> GetRef<TKey, TValue>(
                in TempDictionary<TKey, TValue> source
            )
            => new TempDictionaryInternalsRefUnsafe<TKey, TValue>(source);

        /// <summary>
        /// Returns the internal <see cref="Entry{TKey, TValue}"/> array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<Entry<TKey, TValue>> AsSpan<TKey, TValue>(
                in TempDictionary<TKey, TValue> source
            )
            => source._entries.AsSpan(0, source._count);

        /// <summary>
        /// Gets a ref to a <typeparamref name="TValue"/> in the <see cref="TempDictionary{TKey, TValue}"/>, adding a new entry with a default value if it does not exist in the <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary to get the ref to <typeparamref name="TValue"/> from.</param>
        /// <param name="key">The key used for lookup.</param>
        /// <param name="exists">Whether or not a new entry for the given key was added to the dictionary.</param>
        /// <remarks>Items should not be added to or removed from the <see cref="TempDictionary{TKey, TValue}"/> while the ref <typeparamref name="TValue"/> is in use.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue? GetValueRefOrAddDefault<TKey, TValue>(
                ref TempDictionary<TKey, TValue> dictionary
                , TKey key
                , out bool exists
            ) where TKey : notnull
            => ref TempDictionary<TKey, TValue>.CollectionsMarshalHelper.GetValueRefOrAddDefault(dictionary, key, out exists);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryInsert<TKey, TValue>(
                ref TempDictionary<TKey, TValue> dictionary
                , TKey key, TValue value
                , InsertionBehavior behavior
            )
            => dictionary.TryInsert(key, value, behavior);
    }
}
