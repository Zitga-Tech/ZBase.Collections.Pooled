using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ValueHashSetInternalsRef<T>
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
        [NonSerialized] public readonly ReadOnlySpan<Entry<T>> Entries;
        [NonSerialized] public readonly IEqualityComparer<T> Comparer;

        public ValueHashSetInternalsRef(in ValueHashSet<T> source)
        {
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
            FastModMultiplier = source._fastModMultiplier;
#endif

            Count = source._count;
            FreeList = source._freeList;
            FreeCount = source._freeCount;
            Version = source._version;
            ClearEntries = ValueHashSet<T>.s_clearEntries;
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
        public static ValueHashSetInternalsRef<T> GetRef<T>(
                ValueHashSet<T> source
            )
            => new ValueHashSetInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal <see cref="Entry{T}"/> array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<Entry<T>> AsReadOnlySpan<T>(
                ValueHashSet<T> source
            )
            => source._entries.AsSpan(0, source.Count);
    }
}