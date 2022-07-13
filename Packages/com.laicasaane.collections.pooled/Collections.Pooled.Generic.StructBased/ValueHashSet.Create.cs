#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    partial struct ValueHashSet<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create()
            => new(0, null, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(int capacity)
            => new(capacity, null, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(IEnumerable<T> collection)
            => new(collection, null, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(IEqualityComparer<T>? comparer)
            => new(comparer, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
            => new(collection, comparer, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(int capacity, IEqualityComparer<T>? comparer)
            => new(capacity, comparer, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(IEqualityComparer<T>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<T>> entryPool)
            => new(comparer, bucketPool, entryPool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(IEnumerable<T> collection, IEqualityComparer<T>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<T>> entryPool)
            => new(collection, comparer, bucketPool, entryPool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(int capacity, IEqualityComparer<T>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<T>> entryPool)
            => new(capacity, comparer, bucketPool, entryPool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(T[] items)
            => new(items.AsSpan(), null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(T[] items, IEqualityComparer<T>? comparer)
            => new(items.AsSpan(), comparer, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(T[] items, IEqualityComparer<T>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<T>> entryPool)
            => new(items.AsSpan(), comparer, bucketPool, entryPool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(in ReadOnlySpan<T> span)
            => new(span, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(in ReadOnlySpan<T> span, IEqualityComparer<T>? comparer)
            => new(span, comparer, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueHashSet<T> Create(in ReadOnlySpan<T> span, IEqualityComparer<T>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<T>> entryPool)
            => new(span, comparer, bucketPool, entryPool);
    }
}