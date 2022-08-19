// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Collections.Pooled.Generic
{
    public ref partial struct TempDictionary<TKey, TValue>
    {
        // constants for serialization
        private const string VersionName = "Version"; // Do not rename (binary serialization)
        private const string HashSizeName = "HashSize"; // Do not rename (binary serialization). Must save buckets.Length
        private const string KeyValuePairsName = "KeyValuePairs"; // Do not rename (binary serialization)
        private const string ComparerName = "Comparer"; // Do not rename (binary serialization)

        private static readonly int[] s_emptyBuckets = new int[0];
        private static readonly Entry<TKey, TValue>[] s_emptyEntries = new Entry<TKey, TValue>[0];

        internal int[]? _buckets;
        internal Entry<TKey, TValue>[]? _entries;

#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
        internal ulong _fastModMultiplier;
#endif

        internal int _count;
        internal int _freeList;
        internal int _freeCount;
        internal int _version;
        internal IEqualityComparer<TKey>? _comparer;

        [NonSerialized]
        internal ArrayPool<int> _bucketPool;

        [NonSerialized]
        internal ArrayPool<Entry<TKey, TValue>> _entryPool;

        internal static readonly bool s_isReferenceKey = SystemRuntimeHelpers.IsReferenceOrContainsReferences<TKey>();
        internal static readonly bool s_isReferenceValue = SystemRuntimeHelpers.IsReferenceOrContainsReferences<TValue>();
        internal static readonly bool s_clearEntries = s_isReferenceKey || s_isReferenceValue;

        private const int StartOfFreeList = -3;

        internal TempDictionary(int capacity, IEqualityComparer<TKey>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<TKey, TValue>> entryPool)
        {
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
            _fastModMultiplier = default;
#endif

            _count = default;
            _freeList = default;
            _freeCount = default;
            _version = default;
            _comparer = default;

            _bucketPool = bucketPool ?? ArrayPool<int>.Shared;
            _entryPool = entryPool ?? ArrayPool<Entry<TKey, TValue>>.Shared;

            _buckets = s_emptyBuckets;
            _entries = s_emptyEntries;

            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            if (capacity > 0)
            {
                Initialize(capacity);
            }
            else
            {
                _buckets = s_emptyBuckets;
                _entries = s_emptyEntries;
            }

            if (comparer is not null && comparer != EqualityComparer<TKey>.Default) // first check for null to avoid forcing default comparer instantiation unnecessarily
            {
                _comparer = comparer;
            }

            // Special-case EqualityComparer<string>.Default, StringComparer.Ordinal, and StringComparer.OrdinalIgnoreCase.
            // We use a non-randomized comparer for improved perf, falling back to a randomized comparer if the
            // hash buckets become unbalanced.
            if (typeof(TKey) == typeof(string))
            {
                IEqualityComparer<string>? stringComparer = NonRandomizedStringEqualityComparer.Default;
                if (stringComparer is not null)
                {
                    _comparer = (IEqualityComparer<TKey>?)stringComparer;
                }
            }
        }

        internal TempDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<TKey, TValue>> entryPool)
            : this(dictionary != null ? dictionary.Count : 0, comparer, bucketPool, entryPool)
        {
            if (dictionary == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
            }

            AddRange(dictionary);
        }

        internal TempDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<TKey, TValue>> entryPool)
            : this((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count ?? 0, comparer, bucketPool, entryPool)
        {
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            }

            AddRange(collection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            // Fallback path for IEnumerable that isn't a non-subclassed Dictionary<TKey,TValue>.
            foreach (KeyValuePair<TKey, TValue> pair in collection)
            {
                Add(pair.Key, pair.Value);
            }
        }

        public void AddRange(TempDictionary<TKey, TValue> collection)
        {
            if (collection.Count == 0)
            {
                // Nothing to copy, all done
                return;
            }

            // This is not currently a true .AddRange as it needs to be an initialized dictionary
            // of the correct size, and also an empty dictionary with no current entities (and no argument checks).
            SystemDebug.Assert(collection._entries is not null);
            SystemDebug.Assert(_entries is not null);
            SystemDebug.Assert(_entries.Length >= collection.Count);
            SystemDebug.Assert(_count == 0);

            Entry<TKey, TValue>[] oldEntries = collection._entries;
            if (collection._comparer == _comparer)
            {
                // If comparers are the same, we can copy _entries without rehashing.
                CopyEntries(oldEntries, collection._count);
                return;
            }

            // Comparers differ need to rehash all the entires via Add
            int count = collection._count;
            for (int i = 0; i < count; i++)
            {
                // Only copy if an entry
                if (oldEntries[i].Next >= -1)
                {
                    Add(oldEntries[i].Key, oldEntries[i].Value);
                }
            }
        }

        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                return _comparer ?? EqualityComparer<TKey>.Default;
            }
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count - _freeCount;
        }

        public TempDictionaryKeyCollection<TKey, TValue> Keys => new TempDictionaryKeyCollection<TKey, TValue>(this);

        public TempDictionaryValueCollection<TKey, TValue> Values => new TempDictionaryValueCollection<TKey, TValue>(this);

        public TValue this[TKey key]
        {
            get
            {
                ref TValue value = ref FindValue(key);
                if (!Unsafe.IsNullRef(ref value))
                {
                    return value;
                }

                ThrowHelper.ThrowKeyNotFoundException(key);
                return default;
            }
            set
            {
                bool modified = TryInsert(key, value, InsertionBehavior.OverwriteExisting);
                SystemDebug.Assert(modified);
            }
        }

        public void Add(TKey key, TValue value)
        {
            bool modified = TryInsert(key, value, InsertionBehavior.ThrowOnExisting);
            SystemDebug.Assert(modified); // If there was an existing key and the Add failed, an exception will already have been thrown.
        }

        public void Clear()
        {
            int count = _count;
            if (count > 0)
            {
                SystemDebug.Assert(_buckets != null, "_buckets should be non-null");
                SystemDebug.Assert(_entries != null, "_entries should be non-null");

                Array.Clear(_buckets, 0, _buckets.Length);

                _count = 0;
                _freeList = -1;
                _freeCount = 0;
                Array.Clear(_entries, 0, count);
            }
        }

        public bool ContainsKey(TKey key) =>
            !Unsafe.IsNullRef(ref FindValue(key));

        public bool ContainsValue(TValue value)
        {
            Entry<TKey, TValue>[]? entries = _entries;
            if (value == null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].Next >= -1 && entries[i].Value == null)
                    {
                        return true;
                    }
                }
            }
            else if (typeof(TValue).IsValueType)
            {
                // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].Next >= -1 && EqualityComparer<TValue>.Default.Equals(entries[i].Value, value))
                    {
                        return true;
                    }
                }
            }
            else
            {
                // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize
                // https://github.com/dotnet/runtime/issues/10050
                // So cache in a local rather than get EqualityComparer per loop iteration
                EqualityComparer<TValue> defaultComparer = EqualityComparer<TValue>.Default;
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].Next >= -1 && defaultComparer.Equals(entries[i].Value, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(KeyValuePair<TKey, TValue>[] dest)
            => CopyTo(dest, 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(KeyValuePair<TKey, TValue>[] dest, int destIndex)
            => CopyTo(dest, destIndex, Count);

        public void CopyTo(KeyValuePair<TKey, TValue>[] dest, int destIndex, int count)
        {
            if (dest == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dest);
            }

            CopyTo(dest.AsSpan(), destIndex, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
            }

            info.AddValue(VersionName, _version);
            info.AddValue(ComparerName, Comparer, typeof(IEqualityComparer<TKey>));
            info.AddValue(HashSizeName, _buckets == null ? 0 : _buckets.Length); // This is the length of the bucket array

            if (_buckets.IsNullOrEmpty() == false)
            {
                var pool = ArrayPool<KeyValuePair<TKey, TValue>>.Shared;
                var array = pool.Rent(Count);
                CopyTo(array, 0);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
                pool.Return(array, s_clearEntries);
            }
        }

        internal ref TValue FindValue(TKey key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            ref Entry<TKey, TValue> entry = ref Unsafe.NullRef<Entry<TKey, TValue>>();
            if (_buckets.IsNullOrEmpty() == false)
            {
                SystemDebug.Assert(_entries != null, "expected entries to be != null");
                IEqualityComparer<TKey>? comparer = _comparer;
                if (comparer == null)
                {
                    uint hashCode = (uint)key.GetHashCode();
                    int i = GetBucket(hashCode);
                    Entry<TKey, TValue>[]? entries = _entries;
                    uint collisionCount = 0;
                    if (typeof(TKey).IsValueType)
                    {
                        // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic

                        i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
                        do
                        {
                            // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                            // Test in if to drop range check for following array access
                            if ((uint)i >= (uint)entries.Length)
                            {
                                goto ReturnNotFound;
                            }

                            entry = ref entries[i];
                            if (entry.HashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entry.Key, key))
                            {
                                goto ReturnFound;
                            }

                            i = entry.Next;

                            collisionCount++;
                        } while (collisionCount <= (uint)entries.Length);

                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        goto ConcurrentOperation;
                    }
                    else
                    {
                        // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize
                        // https://github.com/dotnet/runtime/issues/10050
                        // So cache in a local rather than get EqualityComparer per loop iteration
                        EqualityComparer<TKey> defaultComparer = EqualityComparer<TKey>.Default;

                        i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
                        do
                        {
                            // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                            // Test in if to drop range check for following array access
                            if ((uint)i >= (uint)entries.Length)
                            {
                                goto ReturnNotFound;
                            }

                            entry = ref entries[i];
                            if (entry.HashCode == hashCode && defaultComparer.Equals(entry.Key, key))
                            {
                                goto ReturnFound;
                            }

                            i = entry.Next;

                            collisionCount++;
                        } while (collisionCount <= (uint)entries.Length);

                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        goto ConcurrentOperation;
                    }
                }
                else
                {
                    uint hashCode = (uint)comparer.GetHashCode(key);
                    int i = GetBucket(hashCode);
                    Entry<TKey, TValue>[]? entries = _entries;
                    uint collisionCount = 0;
                    i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
                    do
                    {
                        // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                        // Test in if to drop range check for following array access
                        if ((uint)i >= (uint)entries.Length)
                        {
                            goto ReturnNotFound;
                        }

                        entry = ref entries[i];
                        if (entry.HashCode == hashCode && comparer.Equals(entry.Key, key))
                        {
                            goto ReturnFound;
                        }

                        i = entry.Next;

                        collisionCount++;
                    } while (collisionCount <= (uint)entries.Length);

                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    goto ConcurrentOperation;
                }
            }

            goto ReturnNotFound;

        ConcurrentOperation:
            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
        ReturnFound:
            ref TValue value = ref entry.Value;
        Return:
            return ref value;
        ReturnNotFound:
            value = ref Unsafe.NullRef<TValue>();
            goto Return;
        }

        private int Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            int[] buckets = _bucketPool.Rent(size);
            Entry<TKey, TValue>[] entries = _entryPool.Rent(size);

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _freeList = -1;

#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
#endif

            Array.Clear(buckets, 0, buckets.Length);

            _buckets = buckets;
            _entries = entries;

            return size;
        }

        internal bool TryInsert(TKey key, TValue value, InsertionBehavior behavior)
        {
            // NOTE: this method is mirrored in CollectionsMarshal.GetValueRefOrAddDefault below.
            // If you make any changes here, make sure to keep that version in sync as well.

            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (_buckets.IsNullOrEmpty())
            {
                Initialize(0);
            }
            SystemDebug.Assert(_buckets != null);

            Entry<TKey, TValue>[]? entries = _entries;
            SystemDebug.Assert(entries != null, "expected entries to be non-null");

            IEqualityComparer<TKey>? comparer = _comparer;
            uint hashCode = (uint)((comparer == null) ? key.GetHashCode() : comparer.GetHashCode(key));

            uint collisionCount = 0;
            ref int bucket = ref GetBucket(hashCode);
            int i = bucket - 1; // Value in _buckets is 1-based

            if (comparer == null)
            {
                if (typeof(TKey).IsValueType)
                {
                    // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                    while (true)
                    {
                        // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                        // Test uint in if rather than loop condition to drop range check for following array access
                        if ((uint)i >= (uint)entries.Length)
                        {
                            break;
                        }

                        if (entries[i].HashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entries[i].Key, key))
                        {
                            if (behavior == InsertionBehavior.OverwriteExisting)
                            {
                                entries[i].Value = value;
                                return true;
                            }

                            if (behavior == InsertionBehavior.ThrowOnExisting)
                            {
                                ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
                            }

                            return false;
                        }

                        i = entries[i].Next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop; which means a concurrent update has happened.
                            // Break out of the loop and throw, rather than looping forever.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }
                else
                {
                    // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize
                    // https://github.com/dotnet/runtime/issues/10050
                    // So cache in a local rather than get EqualityComparer per loop iteration
                    EqualityComparer<TKey> defaultComparer = EqualityComparer<TKey>.Default;
                    while (true)
                    {
                        // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                        // Test uint in if rather than loop condition to drop range check for following array access
                        if ((uint)i >= (uint)entries.Length)
                        {
                            break;
                        }

                        if (entries[i].HashCode == hashCode && defaultComparer.Equals(entries[i].Key, key))
                        {
                            if (behavior == InsertionBehavior.OverwriteExisting)
                            {
                                entries[i].Value = value;
                                return true;
                            }

                            if (behavior == InsertionBehavior.ThrowOnExisting)
                            {
                                ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
                            }

                            return false;
                        }

                        i = entries[i].Next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop; which means a concurrent update has happened.
                            // Break out of the loop and throw, rather than looping forever.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }
            }
            else
            {
                while (true)
                {
                    // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                    // Test uint in if rather than loop condition to drop range check for following array access
                    if ((uint)i >= (uint)entries.Length)
                    {
                        break;
                    }

                    if (entries[i].HashCode == hashCode && comparer.Equals(entries[i].Key, key))
                    {
                        if (behavior == InsertionBehavior.OverwriteExisting)
                        {
                            entries[i].Value = value;
                            return true;
                        }

                        if (behavior == InsertionBehavior.ThrowOnExisting)
                        {
                            ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
                        }

                        return false;
                    }

                    i = entries[i].Next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }
            }

            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                SystemDebug.Assert((StartOfFreeList - entries[_freeList].Next) >= -1, "shouldn't overflow because `next` cannot underflow");
                _freeList = StartOfFreeList - entries[_freeList].Next;
                _freeCount--;
            }
            else
            {
                int count = _count;
                if (count == entries.Length)
                {
                    Resize();
                    bucket = ref GetBucket(hashCode);
                }
                index = count;
                _count = count + 1;
                entries = _entries;
            }

            ref Entry<TKey, TValue> entry = ref entries![index];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1; // Value in _buckets is 1-based
            entry.Key = key;
            entry.Value = value;
            bucket = index + 1; // Value in _buckets is 1-based
            _version++;

            // Value types never rehash
            if (!typeof(TKey).IsValueType && collisionCount > HashHelpers.HashCollisionThreshold && comparer is NonRandomizedStringEqualityComparer)
            {
                // If we hit the collision threshold we'll need to switch to the comparer which is using randomized string hashing
                // i.e. EqualityComparer<string>.Default.
                Resize(entries.Length, true);
            }

            return true;
        }

        /// <summary>
        /// A helper class containing APIs exposed through <see cref="Runtime.InteropServices.CollectionsMarshal"/>.
        /// These methods are relatively niche and only used in specific scenarios, so adding them in a separate type avoids
        /// the additional overhead on each <see cref="TempDictionary{TKey, TValue}"/> instantiation, especially in AOT scenarios.
        /// </summary>
        internal static class CollectionsMarshalHelper
        {
            /// <summary>
            /// Gets a ref to a <typeparamref name="TValue"/> in the <see cref="TempDictionary{TKey, TValue}"/>, adding a new entry with a default value if it does not exist in the <paramref name="dictionary"/>.
            /// </summary>
            /// <param name="dictionary">The dictionary to get the ref to <typeparamref name="TValue"/> from.</param>
            /// <param name="key">The key used for lookup.</param>
            /// <param name="exists">Whether or not a new entry for the given key was added to the dictionary.</param>
            /// <remarks>Items should not be added to or removed from the <see cref="TempDictionary{TKey, TValue}"/> while the ref <typeparamref name="TValue"/> is in use.</remarks>
            public static ref TValue? GetValueRefOrAddDefault(TempDictionary<TKey, TValue> dictionary, TKey key, out bool exists)
            {
                // NOTE: this method is mirrored by Dictionary<TKey, TValue>.TryInsert above.
                // If you make any changes here, make sure to keep that version in sync as well.

                if (key == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                }

                if (dictionary._buckets.IsNullOrEmpty())
                {
                    dictionary.Initialize(0);
                }
                SystemDebug.Assert(dictionary._buckets != null);

                Entry<TKey, TValue>[]? entries = dictionary._entries;
                SystemDebug.Assert(entries != null, "expected entries to be non-null");

                IEqualityComparer<TKey>? comparer = dictionary._comparer;
                uint hashCode = (uint)((comparer == null) ? key.GetHashCode() : comparer.GetHashCode(key));

                uint collisionCount = 0;
                ref int bucket = ref dictionary.GetBucket(hashCode);
                int i = bucket - 1; // Value in _buckets is 1-based

                if (comparer == null)
                {
                    if (typeof(TKey).IsValueType)
                    {
                        // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                        while (true)
                        {
                            // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                            // Test uint in if rather than loop condition to drop range check for following array access
                            if ((uint)i >= (uint)entries.Length)
                            {
                                break;
                            }

                            if (entries[i].HashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entries[i].Key, key))
                            {
                                exists = true;

                                return ref entries[i].Value!;
                            }

                            i = entries[i].Next;

                            collisionCount++;
                            if (collisionCount > (uint)entries.Length)
                            {
                                // The chain of entries forms a loop; which means a concurrent update has happened.
                                // Break out of the loop and throw, rather than looping forever.
                                ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                            }
                        }
                    }
                    else
                    {
                        // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize
                        // https://github.com/dotnet/runtime/issues/10050
                        // So cache in a local rather than get EqualityComparer per loop iteration
                        EqualityComparer<TKey> defaultComparer = EqualityComparer<TKey>.Default;
                        while (true)
                        {
                            // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                            // Test uint in if rather than loop condition to drop range check for following array access
                            if ((uint)i >= (uint)entries.Length)
                            {
                                break;
                            }

                            if (entries[i].HashCode == hashCode && defaultComparer.Equals(entries[i].Key, key))
                            {
                                exists = true;

                                return ref entries[i].Value!;
                            }

                            i = entries[i].Next;

                            collisionCount++;
                            if (collisionCount > (uint)entries.Length)
                            {
                                // The chain of entries forms a loop; which means a concurrent update has happened.
                                // Break out of the loop and throw, rather than looping forever.
                                ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                            }
                        }
                    }
                }
                else
                {
                    while (true)
                    {
                        // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                        // Test uint in if rather than loop condition to drop range check for following array access
                        if ((uint)i >= (uint)entries.Length)
                        {
                            break;
                        }

                        if (entries[i].HashCode == hashCode && comparer.Equals(entries[i].Key, key))
                        {
                            exists = true;

                            return ref entries[i].Value!;
                        }

                        i = entries[i].Next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop; which means a concurrent update has happened.
                            // Break out of the loop and throw, rather than looping forever.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }

                int index;
                if (dictionary._freeCount > 0)
                {
                    index = dictionary._freeList;
                    SystemDebug.Assert((StartOfFreeList - entries[dictionary._freeList].Next) >= -1, "shouldn't overflow because `next` cannot underflow");
                    dictionary._freeList = StartOfFreeList - entries[dictionary._freeList].Next;
                    dictionary._freeCount--;
                }
                else
                {
                    int count = dictionary._count;
                    if (count == entries.Length)
                    {
                        dictionary.Resize();
                        bucket = ref dictionary.GetBucket(hashCode);
                    }
                    index = count;
                    dictionary._count = count + 1;
                    entries = dictionary._entries;
                }

                ref Entry<TKey, TValue> entry = ref entries![index];
                entry.HashCode = hashCode;
                entry.Next = bucket - 1; // Value in _buckets is 1-based
                entry.Key = key;
                entry.Value = default!;
                bucket = index + 1; // Value in _buckets is 1-based
                dictionary._version++;

                // Value types never rehash
                if (!typeof(TKey).IsValueType && collisionCount > HashHelpers.HashCollisionThreshold && comparer is NonRandomizedStringEqualityComparer)
                {
                    // If we hit the collision threshold we'll need to switch to the comparer which is using randomized string hashing
                    // i.e. EqualityComparer<string>.Default.
                    dictionary.Resize(entries.Length, true);

                    exists = false;

                    // At this point the entries array has been resized, so the current reference we have is no longer valid.
                    // We're forced to do a new lookup and return an updated reference to the new entry instance. This new
                    // lookup is guaranteed to always find a value though and it will never return a null reference here.
                    ref TValue? value = ref dictionary.FindValue(key)!;

                    SystemDebug.Assert(!Unsafe.IsNullRef(ref value), "the lookup result cannot be a null ref here");

                    return ref value;
                }

                exists = false;

                return ref entry.Value!;
            }
        }

        private void Resize() => Resize(HashHelpers.ExpandPrime(_count), false);

        private void Resize(int newSize, bool forceNewHashCodes)
        {
            // Value types never rehash
            SystemDebug.Assert(!forceNewHashCodes || !typeof(TKey).IsValueType);
            SystemDebug.Assert(_entries != null, "_entries should be non-null");
            SystemDebug.Assert(newSize >= _entries.Length);

            int count = _count;
            Entry<TKey, TValue>[] entries = _entryPool.Rent(newSize);
            Array.Copy(_entries, entries, count);

            if (!typeof(TKey).IsValueType && forceNewHashCodes)
            {
                SystemDebug.Assert(_comparer is NonRandomizedStringEqualityComparer);
                _comparer = EqualityComparer<TKey>.Default;

                for (int i = 0; i < count; i++)
                {
                    if (entries[i].Next >= -1)
                    {
                        entries[i].HashCode = (uint)_comparer.GetHashCode(entries[i].Key);
                    }
                }

                if (ReferenceEquals(_comparer, EqualityComparer<TKey>.Default))
                {
                    _comparer = null;
                }
            }

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            RenewBuckets(newSize);

#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
#endif

            for (int i = 0; i < count; i++)
            {
                if (entries[i].Next >= -1)
                {
                    ref int bucket = ref GetBucket(entries[i].HashCode);
                    entries[i].Next = bucket - 1; // Value in _buckets is 1-based
                    bucket = i + 1;
                }
            }

            _entryPool.Return(_entries, s_clearEntries);
            _entries = entries;
        }

        public bool Remove(TKey key)
        {
            // The overload Remove(TKey key, out TValue value) is a copy of this method with one additional
            // statement to copy the value for entry being removed into the output parameter.
            // Code has been intentionally duplicated for performance reasons.

            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (_buckets.IsNullOrEmpty() == false)
            {
                SystemDebug.Assert(_entries != null, "entries should be non-null");
                uint collisionCount = 0;
                uint hashCode = (uint)(_comparer?.GetHashCode(key) ?? key.GetHashCode());
                ref int bucket = ref GetBucket(hashCode);
                Entry<TKey, TValue>[]? entries = _entries;
                int last = -1;
                int i = bucket - 1; // Value in buckets is 1-based
                while (i >= 0)
                {
                    ref Entry<TKey, TValue> entry = ref entries[i];

                    if (entry.HashCode == hashCode && (_comparer?.Equals(entry.Key, key) ?? EqualityComparer<TKey>.Default.Equals(entry.Key, key)))
                    {
                        if (last < 0)
                        {
                            bucket = entry.Next + 1; // Value in buckets is 1-based
                        }
                        else
                        {
                            entries[last].Next = entry.Next;
                        }

                        SystemDebug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                        entry.Next = StartOfFreeList - _freeList;

                        if (s_isReferenceKey)
                        {
                            entry.Key = default!;
                        }

                        if (s_isReferenceValue)
                        {
                            entry.Value = default!;
                        }

                        _freeList = i;
                        _freeCount++;
                        return true;
                    }

                    last = i;
                    i = entry.Next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }
            }
            return false;
        }

        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            // This overload is a copy of the overload Remove(TKey key) with one additional
            // statement to copy the value for entry being removed into the output parameter.
            // Code has been intentionally duplicated for performance reasons.

            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (_buckets.IsNullOrEmpty() == false)
            {
                SystemDebug.Assert(_entries != null, "entries should be non-null");
                uint collisionCount = 0;
                uint hashCode = (uint)(_comparer?.GetHashCode(key) ?? key.GetHashCode());
                ref int bucket = ref GetBucket(hashCode);
                Entry<TKey, TValue>[]? entries = _entries;
                int last = -1;
                int i = bucket - 1; // Value in buckets is 1-based
                while (i >= 0)
                {
                    ref Entry<TKey, TValue> entry = ref entries[i];

                    if (entry.HashCode == hashCode && (_comparer?.Equals(entry.Key, key) ?? EqualityComparer<TKey>.Default.Equals(entry.Key, key)))
                    {
                        if (last < 0)
                        {
                            bucket = entry.Next + 1; // Value in buckets is 1-based
                        }
                        else
                        {
                            entries[last].Next = entry.Next;
                        }

                        value = entry.Value;

                        SystemDebug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                        entry.Next = StartOfFreeList - _freeList;

                        if (s_isReferenceKey)
                        {
                            entry.Key = default!;
                        }

                        if (s_isReferenceValue)
                        {
                            entry.Value = default!;
                        }

                        _freeList = i;
                        _freeCount++;
                        return true;
                    }

                    last = i;
                    i = entry.Next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }
            }

            value = default;
            return false;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            ref TValue valRef = ref FindValue(key);
            if (!Unsafe.IsNullRef(ref valRef))
            {
                value = valRef;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, TValue value) =>
            TryInsert(key, value, InsertionBehavior.None);

        /// <summary>
        /// Ensures that the dictionary can hold up to 'capacity' entries without any further expansion of its backing storage
        /// </summary>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            int currentCapacity = _entries == null ? 0 : _entries.Length;
            if (currentCapacity >= capacity)
            {
                return currentCapacity;
            }

            _version++;

            if (_buckets.IsNullOrEmpty())
            {
                return Initialize(capacity);
            }

            int newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize, forceNewHashCodes: false);
            return newSize;
        }

        /// <summary>
        /// Sets the capacity of this dictionary to what it would be if it had been originally initialized with all its entries
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize the memory overhead
        /// once it is known that no new elements will be added.
        ///
        /// To allocate minimum size storage array, execute the following statements:
        ///
        /// dictionary.Clear();
        /// dictionary.TrimExcess();
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrimExcess() => TrimExcess(Count);

        /// <summary>
        /// Sets the capacity of this dictionary to hold up 'capacity' entries without any further expansion of its backing storage
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize the memory overhead
        /// once it is known that no new elements will be added.
        /// </remarks>
        public void TrimExcess(int capacity)
        {
            if (capacity < Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            int newSize = HashHelpers.GetPrime(capacity);
            Entry<TKey, TValue>[]? oldEntries = _entries;
            int currentCapacity = oldEntries == null ? 0 : oldEntries.Length;
            if (newSize >= currentCapacity)
            {
                return;
            }

            int[] oldBuckets = _buckets;

            int oldCount = _count;
            _version++;
            Initialize(newSize);

            SystemDebug.Assert(oldEntries is not null);

            CopyEntries(oldEntries, oldCount);

            _bucketPool.Return(oldBuckets);
            _entryPool.Return(oldEntries, s_clearEntries);
        }

        private void CopyEntries(Entry<TKey, TValue>[] entries, int count)
        {
            SystemDebug.Assert(_entries is not null);

            Entry<TKey, TValue>[] newEntries = _entries;
            int newCount = 0;
            for (int i = 0; i < count; i++)
            {
                uint hashCode = entries[i].HashCode;
                if (entries[i].Next >= -1)
                {
                    ref Entry<TKey, TValue> entry = ref newEntries[newCount];
                    entry = entries[i];
                    ref int bucket = ref GetBucket(hashCode);
                    entry.Next = bucket - 1; // Value in _buckets is 1-based
                    bucket = newCount + 1;
                    newCount++;
                }
            }

            _count = newCount;
            _freeCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(uint hashCode)
        {
            int[] buckets = _buckets!;
#if TARGET_64BIT || PLATFORM_ARCH_64 || UNITY_64
            return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
#else
            return ref buckets[hashCode % (uint)buckets.Length];
#endif
        }

        private void RenewBuckets(int newSize)
        {
            if (_buckets.IsNullOrEmpty() == false)
            {
                try
                {
                    _bucketPool.Return(_buckets);
                }
                catch
                { }
            }

            var buckets = _bucketPool.Rent(newSize);
            Array.Clear(buckets, 0, buckets.Length);
            _buckets = buckets;
        }

        public ref struct Enumerator
        {
            private readonly TempDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;
            private readonly int _getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            public Enumerator(in TempDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _getEnumeratorRetType = getEnumeratorRetType;
                _current = default;
            }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                while ((uint)_index < (uint)_dictionary._count)
                {
                    ref Entry<TKey, TValue> entry = ref _dictionary._entries![_index++];

                    if (entry.Next >= -1)
                    {
                        _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                        return true;
                    }
                }

                _index = _dictionary._count + 1;
                _current = default;
                return false;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            public void Dispose() { }
        }
    }
}