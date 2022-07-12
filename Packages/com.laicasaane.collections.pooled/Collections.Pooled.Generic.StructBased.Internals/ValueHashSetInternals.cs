using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct ValueHashSetInternals<T> : IDisposable
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
        [NonSerialized] public readonly Entry<T>[] Entries;
        [NonSerialized] public readonly IEqualityComparer<T> Comparer;

        [NonSerialized] public readonly ArrayPool<int> BucketPool;
        [NonSerialized] public readonly ArrayPool<Entry<T>> EntryPool;

        public ValueHashSetInternals(in ValueHashSet<T> source)
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
        public static ValueHashSetInternals<T> TakeOwnership<T>(
                ValueHashSet<T> source
            )
        {
            var internals = new ValueHashSetInternals<T>(source);

            source._buckets = null;
            source._entries = null;
            source.Dispose();

            return internals;
        }

        /// <summary>
        /// Gets either a ref to a <typeparamref name="T"/> in the <see cref="ValueHashSet{T}"/> or a ref null if it does not exist in the <paramref name="set"/>.
        /// </summary>
        /// <param name="set">The set to get the ref to <typeparamref name="T"/> from.</param>
        /// <param name="equalValue">The value to search for.</param>
        /// <remarks>
        /// Items should not be added or removed from the <see cref="ValueHashSet{T}"/> while the ref <typeparamref name="T"/> is in use.
        /// The ref null can be detected using System.Runtime.CompilerServices.Unsafe.IsNullRef
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetValueRefOrNullRef<T>(
                ValueHashSet<T> set, T equalValue
            ) where T : notnull
            => ref set.FindValue(equalValue);

        /// <summary>Adds the specified element to the set if it's not already contained.</summary>
        /// <param name="value">The element to add to the set.</param>
        /// <param name="location">The index into <see cref="_entries"/> of the element.</param>
        /// <returns>
        /// true if the element is added to the <see cref="ValueHashSet{T}"/> object; false if the element is already present.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddIfNotPresent<T>(
                ValueHashSet<T> set, T value, out int location
            )
            => set.AddIfNotPresent(value, out location);
    }
}
