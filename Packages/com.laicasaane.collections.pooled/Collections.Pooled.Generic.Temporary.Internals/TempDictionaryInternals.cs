#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct TempDictionaryInternals<TKey, TValue> : IDisposable
    {
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
        [NonSerialized] public readonly ulong FastModMultiplier;
#endif

        [NonSerialized] public readonly int Count;
        [NonSerialized] public readonly int FreeList;
        [NonSerialized] public readonly int FreeCount;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearEntries;

        [NonSerialized] public readonly int[] Buckets;
        [NonSerialized] public readonly Entry<TKey, TValue>[] Entries;
        [NonSerialized] public readonly IEqualityComparer<TKey> Comparer;

        [NonSerialized] public readonly ArrayPool<int> BucketPool;
        [NonSerialized] public readonly ArrayPool<Entry<TKey, TValue>> EntryPool;

        public TempDictionaryInternals(in TempDictionary<TKey, TValue> source)
        {
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
            FastModMultiplier = source._fastModMultiplier;
#endif

            Count = source._count;
            FreeList = source._freeList;
            FreeCount = source._freeCount;
            Version = source._version;
            ClearEntries = TempDictionary<TKey, TValue>.s_clearEntries;
            Buckets = source._buckets;
            Entries = source._entries;
            Comparer = source._comparer;
            BucketPool = source._bucketPool;
            EntryPool = source._entryPool;
        }

        public void Dispose()
        {
            if (Buckets != null && Buckets.Length > 0)
            {
                try
                {
                    BucketPool?.Return(Buckets);
                }
                catch { }
            }

            if (Entries != null && Entries.Length > 0)
            {
                try
                {
                    EntryPool?.Return(Entries, ClearEntries);
                }
                catch { }
            }
        }
    }

    public static partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static TempDictionaryInternals<TKey, TValue> TakeOwnership<TKey, TValue>(
                TempDictionary<TKey, TValue> source
            )
        {
            var internals = new TempDictionaryInternals<TKey, TValue>(source);

            source._buckets = null;
            source._entries = null;
            source.Dispose();

            return internals;
        }

        /// <summary>
        /// Gets either a ref to a <typeparamref name="TValue"/> in the <see cref="TempDictionary{TKey, TValue}"/> or a ref null if it does not exist in the <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary to get the ref to <typeparamref name="TValue"/> from.</param>
        /// <param name="key">The key used for lookup.</param>
        /// <remarks>
        /// Items should not be added or removed from the <see cref="TempDictionary{TKey, TValue}"/> while the ref <typeparamref name="TValue"/> is in use.
        /// The ref null can be detected using System.Runtime.CompilerServices.Unsafe.IsNullRef
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetValueRefOrNullRef<TKey, TValue>(
                TempDictionary<TKey, TValue> dictionary, TKey key
            ) where TKey : notnull
            => ref dictionary.FindValue(key);

        /// <summary>
        /// Gets a ref to a <typeparamref name="TValue"/> in the <see cref="TempDictionary{TKey, TValue}"/>, adding a new entry with a default value if it does not exist in the <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary to get the ref to <typeparamref name="TValue"/> from.</param>
        /// <param name="key">The key used for lookup.</param>
        /// <param name="exists">Whether or not a new entry for the given key was added to the dictionary.</param>
        /// <remarks>Items should not be added to or removed from the <see cref="TempDictionary{TKey, TValue}"/> while the ref <typeparamref name="TValue"/> is in use.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue? GetValueRefOrAddDefault<TKey, TValue>(
                TempDictionary<TKey, TValue> dictionary, TKey key, out bool exists
            ) where TKey : notnull
            => ref TempDictionary<TKey, TValue>.CollectionsMarshalHelper.GetValueRefOrAddDefault(dictionary, key, out exists);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryInsert<TKey, TValue>(
                TempDictionary<TKey, TValue> dictionary, TKey key, TValue value, InsertionBehavior behavior
            )
            => dictionary.TryInsert(key, value, behavior);
    }
}
