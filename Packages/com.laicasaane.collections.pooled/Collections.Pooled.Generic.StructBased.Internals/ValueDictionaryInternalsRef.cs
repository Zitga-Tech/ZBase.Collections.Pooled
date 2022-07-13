using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ValueDictionaryInternalsRef<TKey, TValue>
    {
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
        [NonSerialized] public readonly ulong FastModMultiplier;
#endif

        [NonSerialized] public readonly int Count;
        [NonSerialized] public readonly int FreeList;
        [NonSerialized] public readonly int FreeCount;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearEntries;

        [NonSerialized] public readonly ReadOnlySpan<int> Buckets;
        [NonSerialized] public readonly ReadOnlySpan<Entry<TKey, TValue>> Entries;
        [NonSerialized] public readonly IEqualityComparer<TKey> Comparer;

        public ValueDictionaryInternalsRef(in ValueDictionary<TKey, TValue> source)
        {
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
            FastModMultiplier = source._fastModMultiplier;
#endif

            Count = source._count;
            FreeList = source._freeList;
            FreeCount = source._freeCount;
            Version = source._version;
            ClearEntries = ValueDictionary<TKey, TValue>.s_clearEntries;
            Buckets = source._buckets;
            Entries = source._entries;
            Comparer = source._comparer;
        }
    }

    public static partial class ValueCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueDictionaryInternalsRef<TKey, TValue> GetRef<TKey, TValue>(
                ValueDictionary<TKey, TValue> source
            )
            => new ValueDictionaryInternalsRef<TKey, TValue>(source);

        /// <summary>
        /// Returns the internal <see cref="Entry{TKey, TValue}"/> array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<Entry<TKey, TValue>> AsReadOnlySpan<TKey, TValue>(
                ValueDictionary<TKey, TValue> source
            )
            => source._entries.AsSpan(0, source.Count);
    }
}
