// https://github.com/sebas77/Svelto.Common/blob/master/DataStructures/Dictionaries/SveltoDictionary.cs

#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Collections.Pooled.Generic
{
    /// <summary>
    /// HashSet that <typeparamref name="T"/> are stored in dense arrays.
    /// Effectively, internal items can be iterated over like normal arrays.
    /// </summary>
    /// <remarks>
    /// To iterate over items as arrays, it must be get through unsafe APIs.
    /// </remarks>
    [Serializable]
    public partial struct ValueArrayHashSet<T>
        : IArrayHashSet<T>
        , ISerializable
        , IDeserializationCallback
        , IDisposable
        where T : notnull
    {
        // constants for serialization
        private const string CountName = "Count"; // Do not rename (binary serialization). Must save buckets.Length
        private const string EntriesName = "Entries"; // Do not rename (binary serialization)

        internal ArrayEntry<T>[] _entries;
        internal int[] _buckets;

        internal int _freeEntryIndex;
        internal int _collisions;
        internal ulong _fastModBucketsMultiplier;

        [NonSerialized] internal ArrayPool<ArrayEntry<T>> _entryPool;
        [NonSerialized] internal ArrayPool<int> _bucketPool;

        internal static readonly bool s_clearEntries = SystemRuntimeHelpers.IsReferenceOrContainsReferences<T>();

        private static readonly Type s_typeOfKey = typeof(T);
        private static readonly ArrayEntry<T>[] s_emptyEntries = new ArrayEntry<T>[0];
        private static readonly int[] s_emptyBuckets = new int[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayHashSet<T> Create()
            => new ValueArrayHashSet<T>(0
                , ArrayPool<ArrayEntry<T>>.Shared
                , ArrayPool<int>.Shared
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayHashSet<T> Create(int capacity)
            => new ValueArrayHashSet<T>(capacity
                , ArrayPool<ArrayEntry<T>>.Shared
                , ArrayPool<int>.Shared
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayHashSet<T> Create(int capacity
                , ArrayPool<ArrayEntry<T>> entryPool
                , ArrayPool<int> bucketPool
            )
            => new ValueArrayHashSet<T>(capacity, entryPool, bucketPool);

        internal ValueArrayHashSet(int capacity
            , ArrayPool<ArrayEntry<T>> entryPool
            , ArrayPool<int> bucketPool
        )
            : this()
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            _entryPool = entryPool ?? ArrayPool<ArrayEntry<T>>.Shared;
            _bucketPool = bucketPool ?? ArrayPool<int>.Shared;

            Initialize(capacity);

            if (capacity > 0)
                _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)capacity);
        }

        private void Initialize(int capacity)
        {
            capacity = HashHelpers.GetPrime(capacity);

            var buckets = _bucketPool.Rent(capacity);
            Array.Clear(buckets, 0, buckets.Length);

            _buckets = buckets;
            _entries = _entryPool.Rent(capacity);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
            }

            var count = this.Count;

            info.AddValue(CountName, count);

            if (count > 0)
            {
                var array = new T[count];
                CopyTo(array);
                info.AddValue(EntriesName, array, typeof(T[]));
            }
        }

        public void OnDeserialization(object sender)
        {
            HashHelpers.SerializationInfoTable.TryGetValue(this, out SerializationInfo? siInfo);

            if (siInfo == null)
            {
                // We can return immediately if this function is called twice.
                // Note we remove the serialization info from the table at the end of this method.
                return;
            }

            int count = siInfo.GetInt32(CountName);

            if (count > 0)
            {
                Resize(this.Count, count);

                T[]? array = (T[]?)
                    siInfo.GetValue(EntriesName, typeof(T[]));

                if (array == null)
                {
                    ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] == null)
                    {
                        ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_NullKey);
                    }

                    Add(array[i]);
                }
            }

            HashHelpers.SerializationInfoTable.Remove(this);
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _freeEntryIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(T item)
            => TryGetIndex(item, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(in T item)
            => TryGetIndex(in item, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(T item, out int index)
            => TryGetIndex(item, out index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(in T item, out int index)
            => TryGetIndex(in item, out index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_freeEntryIndex == 0)
                return;

            _freeEntryIndex = 0;

            //Buckets cannot be FastCleared because it's important that the values are reset to 0
            Array.Clear(_buckets, 0, _buckets.Length);

            if (s_clearEntries)
            {
                Array.Clear(_entries, 0, _entries.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool Contains(T item)
        {
            return TryFindIndex(item, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool Contains(in T item)
        {
            return TryFindIndex(in item, out _);
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            if (_entries.Length < capacity)
            {
                Resize(this.Count, HashHelpers.ExpandPrime(capacity));
            }
        }

        public void IncreaseCapacityBy(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            Resize(this.Count, HashHelpers.ExpandPrime(_entries.Length + capacity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(T item)
        {
#if DEBUG
            if (TryFindIndex(item, out var findIndex) == true)
                return findIndex;

            ThrowHelper.ThrowKeyNotFoundException(item);
            return default;
#else
            //Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
            TryFindIndex(item, out var findIndex);
            
            return findIndex;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(in T item)
        {
#if DEBUG
            if (TryFindIndex(in item, out var findIndex) == true)
                return findIndex;

            ThrowHelper.ThrowKeyNotFoundException(item);
            return default;
#else
            //Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
            TryFindIndex(in item, out var findIndex);
            
            return findIndex;
#endif
        }

        bool TryGetIndex(T item, out int index)
        {
            int hash = item.GetHashCode(); //IEquatable doesn't enforce the override of GetHashCode
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //buckets value -1 means it's empty
            var valueIndex = _buckets[bucketIndex] - 1;

            if (valueIndex == -1)
            {
                ResizeIfNeeded();
                //create the info node at the last position and fill it with the relevant information
                _entries[_freeEntryIndex] = new ArrayEntry<T>(item, hash);
            }
            else //collision or already exists
            {
                if (s_typeOfKey.IsValueType == true)
                {
                    int currentValueIndex = valueIndex;
                    do
                    {
                        //must check if the item already exists in the dictionary
                        //ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic, since .NET Core 2.1

                        ref var entry = ref _entries[currentValueIndex];
                        if (entry.Hashcode == hash && EqualityComparer<T>.Default.Equals(entry.Key, item) == true)
                        {
                            //the item already exists, simply replace the value!
                            index = currentValueIndex;
                            return false;
                        }

                        currentValueIndex = entry.Previous;
                    } while (currentValueIndex != -1); //-1 means no more values with item with the same hash
                }
                else
                {
                    // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize
                    // https://github.com/dotnet/runtime/issues/10050
                    // So cache in a local rather than get EqualityComparer per loop iteration
                    EqualityComparer<T> defaultComparer = EqualityComparer<T>.Default;

                    int currentValueIndex = valueIndex;
                    do
                    {
                        //must check if the item already exists in the dictionary

                        ref var entry = ref _entries[currentValueIndex];
                        if (entry.Hashcode == hash && defaultComparer.Equals(entry.Key, item) == true)
                        {
                            //the item already exists, simply replace the value!
                            index = currentValueIndex;
                            return false;
                        }

                        currentValueIndex = entry.Previous;
                    } while (currentValueIndex != -1); //-1 means no more values with item with the same hash

                }
                ResizeIfNeeded();

                //oops collision!
                _collisions++;
                //create a new node which previous index points to node currently pointed in the bucket
                _entries[_freeEntryIndex] = new ArrayEntry<T>(item, hash, valueIndex);
                //update the next of the existing cell to point to the new one
                //old one -> new one | old one <- next one
                _entries[valueIndex].Next = _freeEntryIndex;
                //Important: the new node is always the one that will be pointed by the bucket cell
                //so I can assume that the one pointed by the bucket is always the last value added
                //(next = -1)
            }

            //item with this bucketIndex will point to the last value created
            //ToDo: if instead I assume that the original one is the one in the bucket
            //I wouldn't need to update the bucket here. Small optimization but important
            _buckets[bucketIndex] = _freeEntryIndex + 1;

            index = _freeEntryIndex;
            _freeEntryIndex++;

            //too many collisions?
            if (_collisions > _buckets.Length)
            {
                //we need more space and less collisions
                RenewBuckets(HashHelpers.ExpandPrime(_collisions));
                _collisions = 0;
                _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)_buckets.Length);

                //we need to get all the hash code of all the values stored so far and spread them over the new bucket
                //length
                for (int newValueIndex = 0; newValueIndex < _freeEntryIndex; newValueIndex++)
                {
                    //get the original hash code and find the new bucketIndex due to the new length
                    ref var entry = ref _entries[newValueIndex];
                    bucketIndex = (int)Reduce((uint)entry.Hashcode, (uint)_buckets.Length, _fastModBucketsMultiplier);
                    //bucketsIndex can be -1 or a next value. If it's -1 means no collisions. If there is collision,
                    //we create a new node which prev points to the old one. Old one next points to the new one.
                    //the bucket will now points to the new one
                    //In this way we can rebuild the linkedlist.
                    //get the current valueIndex, it's -1 if no collision happens
                    int existingValueIndex = _buckets[bucketIndex] - 1;
                    //update the bucket index to the index of the current item that share the bucketIndex
                    //(last found is always the one in the bucket)
                    _buckets[bucketIndex] = newValueIndex + 1;
                    if (existingValueIndex != -1)
                    {
                        //oops a value was already being pointed by this cell in the new bucket list,
                        //it means there is a collision, problem
                        _collisions++;
                        //the bucket will point to this value, so 
                        //the previous index will be used as previous for the new value.
                        entry.Previous = existingValueIndex;
                        entry.Next = -1;
                        //and update the previous next index to the new one
                        _entries[existingValueIndex].Next = newValueIndex;
                    }
                    else
                    {
                        //ok nothing was indexed, the bucket was empty. We need to update the previous
                        //values of next and previous
                        entry.Next = -1;
                        entry.Previous = -1;
                    }
                }
            }

            return true;
        }

        bool TryGetIndex(in T item, out int index)
        {
            int hash = item.GetHashCode(); //IEquatable doesn't enforce the override of GetHashCode
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //buckets value -1 means it's empty
            var valueIndex = _buckets[bucketIndex] - 1;

            if (valueIndex == -1)
            {
                ResizeIfNeeded();
                //create the info node at the last position and fill it with the relevant information
                _entries[_freeEntryIndex] = new ArrayEntry<T>(in item, hash);
            }
            else //collision or already exists
            {
                if (s_typeOfKey.IsValueType == true)
                {
                    int currentValueIndex = valueIndex;
                    do
                    {
                        //must check if the item already exists in the dictionary
                        //ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic, since .NET Core 2.1

                        ref var entry = ref _entries[currentValueIndex];
                        if (entry.Hashcode == hash && EqualityComparer<T>.Default.Equals(entry.Key, item) == true)
                        {
                            //the item already exists, simply replace the value!
                            index = currentValueIndex;
                            return false;
                        }

                        currentValueIndex = entry.Previous;
                    } while (currentValueIndex != -1); //-1 means no more values with item with the same hash
                }
                else
                {
                    // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize
                    // https://github.com/dotnet/runtime/issues/10050
                    // So cache in a local rather than get EqualityComparer per loop iteration
                    EqualityComparer<T> defaultComparer = EqualityComparer<T>.Default;

                    int currentValueIndex = valueIndex;
                    do
                    {
                        //must check if the item already exists in the dictionary

                        ref var entry = ref _entries[currentValueIndex];
                        if (entry.Hashcode == hash && defaultComparer.Equals(entry.Key, item) == true)
                        {
                            //the item already exists, simply replace the value!
                            index = currentValueIndex;
                            return false;
                        }

                        currentValueIndex = entry.Previous;
                    } while (currentValueIndex != -1); //-1 means no more values with item with the same hash
                }

                ResizeIfNeeded();

                //oops collision!
                _collisions++;
                //create a new node which previous index points to node currently pointed in the bucket
                _entries[_freeEntryIndex] = new ArrayEntry<T>(in item, hash, valueIndex);
                //update the next of the existing cell to point to the new one
                //old one -> new one | old one <- next one
                _entries[valueIndex].Next = _freeEntryIndex;
                //Important: the new node is always the one that will be pointed by the bucket cell
                //so I can assume that the one pointed by the bucket is always the last value added
                //(next = -1)
            }

            //item with this bucketIndex will point to the last value created
            //ToDo: if instead I assume that the original one is the one in the bucket
            //I wouldn't need to update the bucket here. Small optimization but important
            _buckets[bucketIndex] = _freeEntryIndex + 1;

            index = _freeEntryIndex;
            _freeEntryIndex++;

            //too many collisions?
            if (_collisions > _buckets.Length)
            {
                //we need more space and less collisions
                RenewBuckets(HashHelpers.ExpandPrime(_collisions));
                _collisions = 0;
                _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)_buckets.Length);

                //we need to get all the hash code of all the values stored so far and spread them over the new bucket
                //length
                for (int newValueIndex = 0; newValueIndex < _freeEntryIndex; newValueIndex++)
                {
                    //get the original hash code and find the new bucketIndex due to the new length
                    ref var entry = ref _entries[newValueIndex];
                    bucketIndex = (int)Reduce((uint)entry.Hashcode, (uint)_buckets.Length, _fastModBucketsMultiplier);
                    //bucketsIndex can be -1 or a next value. If it's -1 means no collisions. If there is collision,
                    //we create a new node which prev points to the old one. Old one next points to the new one.
                    //the bucket will now points to the new one
                    //In this way we can rebuild the linkedlist.
                    //get the current valueIndex, it's -1 if no collision happens
                    int existingValueIndex = _buckets[bucketIndex] - 1;
                    //update the bucket index to the index of the current item that share the bucketIndex
                    //(last found is always the one in the bucket)
                    _buckets[bucketIndex] = newValueIndex + 1;
                    if (existingValueIndex != -1)
                    {
                        //oops a value was already being pointed by this cell in the new bucket list,
                        //it means there is a collision, problem
                        _collisions++;
                        //the bucket will point to this value, so 
                        //the previous index will be used as previous for the new value.
                        entry.Previous = existingValueIndex;
                        entry.Next = -1;
                        //and update the previous next index to the new one
                        _entries[existingValueIndex].Next = newValueIndex;
                    }
                    else
                    {
                        //ok nothing was indexed, the bucket was empty. We need to update the previous
                        //values of next and previous
                        entry.Next = -1;
                        entry.Previous = -1;
                    }
                }
            }

            return true;
        }

        void ResizeIfNeeded()
        {
            if (_freeEntryIndex == _entries.Length)
            {
                Resize(this.Count, HashHelpers.ExpandPrime(_freeEntryIndex));
            }
        }

        void Resize(int count, int newCapacity)
        {
            ArrayEntry<T>[] entries = _entries;

            if (newCapacity > entries.Length)
            {
                var newEntries = _entryPool.Rent(newCapacity);

                if (newEntries.Length > entries.Length)
                {
                    if (count > 0)
                        Array.Copy(entries, newEntries, count);

                    _entries = newEntries;

                    if (entries.IsNullOrEmpty() == false)
                        _entryPool.Return(entries, s_clearEntries);
                }
                else
                {
                    _entryPool.Return(newEntries);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            return Remove(item, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in T item)
        {
            return Remove(in item, out _);
        }

        public bool Remove(T item, out int index)
        {
            int hash = item.GetHashCode();
            uint bucketIndex = Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //find the bucket
            int indexToValueToRemove = _buckets[bucketIndex] - 1;

            //Part one: look for the actual item in the bucket list if found I update the bucket list so that it doesn't
            //point anymore to the cell to remove
            while (indexToValueToRemove != -1)
            {
                ref var entry = ref _entries[indexToValueToRemove];
                if (entry.Hashcode == hash && EqualityComparer<T>.Default.Equals(entry.Key, item) == true)
                {
                    //if the item is found and the bucket points directly to the node to remove
                    if (_buckets[bucketIndex] - 1 == indexToValueToRemove)
                    {
#if DEBUG
                        if (entry.Next != -1)
                            throw new InvalidOperationException("If the bucket points to the cell, next MUST NOT exists");
#endif
                        //the bucket will point to the previous cell. if a previous cell exists
                        //its next pointer must be updated!
                        //<--- iteration order  
                        //                      Bucket points always to the last one
                        //   ------- ------- -------
                        //   |  1  | |  2  | |  3  | //bucket cannot have next, only previous
                        //   ------- ------- -------
                        //--> insert order
                        _buckets[bucketIndex] = entry.Previous + 1;
                    }
#if DEBUG
                    else
                    {
                        if (entry.Next == -1)
                            throw new InvalidOperationException("If the bucket points to another cell, next MUST exists");
                    }
#endif

                    UpdateLinkedList(indexToValueToRemove, ref _entries);

                    break;
                }

                indexToValueToRemove = entry.Previous;
            }

            if (indexToValueToRemove == -1)
            {
                index = default;
                return false; //not found!
            }

            index = indexToValueToRemove;

            _freeEntryIndex--; //one less value to iterate

            //Part two:
            //At this point nodes pointers and buckets are updated, but the _values array
            //still has got the value to delete. Remember the goal of this dictionary is to be able
            //to iterate over the values like an array, so the values array must always be up to date

            //if the cell to remove is the last one in the list, we can perform less operations (no swapping needed)
            //otherwise we want to move the last value cell over the value to remove
            if (indexToValueToRemove != _freeEntryIndex)
            {
                //we can move the last value of both arrays in place of the one to delete.
                //in order to do so, we need to be sure that the bucket pointer is updated.
                //first we find the index in the bucket list of the pointer that points to the cell
                //to move
                ref var entry = ref _entries[_freeEntryIndex];
                var movingBucketIndex = Reduce((uint)entry.Hashcode, (uint)_buckets.Length, _fastModBucketsMultiplier);

                //if the item is found and the bucket points directly to the node to remove
                //it must now point to the cell where it's going to be moved
                if (_buckets[movingBucketIndex] - 1 == _freeEntryIndex)
                    _buckets[movingBucketIndex] = indexToValueToRemove + 1;

                //otherwise it means that there was more than one item with the same hash (collision), so 
                //we need to update the linked list and its pointers
                int next = entry.Next;
                int previous = entry.Previous;

                //they now point to the cell where the last value is moved into
                if (next != -1)
                    _entries[next].Previous = indexToValueToRemove;
                if (previous != -1)
                    _entries[previous].Next = indexToValueToRemove;

                //finally, actually move the values
                _entries[indexToValueToRemove] = entry;
            }

            return true;
        }

        public bool Remove(in T item, out int index)
        {
            int hash = item.GetHashCode();
            uint bucketIndex = Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //find the bucket
            int indexToValueToRemove = _buckets[bucketIndex] - 1;

            //Part one: look for the actual item in the bucket list if found I update the bucket list so that it doesn't
            //point anymore to the cell to remove
            while (indexToValueToRemove != -1)
            {
                ref var entry = ref _entries[indexToValueToRemove];
                if (entry.Hashcode == hash && EqualityComparer<T>.Default.Equals(entry.Key, item) == true)
                {
                    //if the item is found and the bucket points directly to the node to remove
                    if (_buckets[bucketIndex] - 1 == indexToValueToRemove)
                    {
#if DEBUG
                        if (entry.Next != -1)
                            throw new InvalidOperationException("If the bucket points to the cell, next MUST NOT exists");
#endif
                        //the bucket will point to the previous cell. if a previous cell exists
                        //its next pointer must be updated!
                        //<--- iteration order  
                        //                      Bucket points always to the last one
                        //   ------- ------- -------
                        //   |  1  | |  2  | |  3  | //bucket cannot have next, only previous
                        //   ------- ------- -------
                        //--> insert order
                        _buckets[bucketIndex] = entry.Previous + 1;
                    }
#if DEBUG
                    else
                    {
                        if (entry.Next == -1)
                            throw new InvalidOperationException("If the bucket points to another cell, next MUST exists");
                    }
#endif

                    UpdateLinkedList(indexToValueToRemove, ref _entries);

                    break;
                }

                indexToValueToRemove = entry.Previous;
            }

            if (indexToValueToRemove == -1)
            {
                index = default;
                return false; //not found!
            }

            index = indexToValueToRemove;

            _freeEntryIndex--; //one less value to iterate

            //Part two:
            //At this point nodes pointers and buckets are updated, but the _values array
            //still has got the value to delete. Remember the goal of this dictionary is to be able
            //to iterate over the values like an array, so the values array must always be up to date

            //if the cell to remove is the last one in the list, we can perform less operations (no swapping needed)
            //otherwise we want to move the last value cell over the value to remove
            if (indexToValueToRemove != _freeEntryIndex)
            {
                //we can move the last value of both arrays in place of the one to delete.
                //in order to do so, we need to be sure that the bucket pointer is updated.
                //first we find the index in the bucket list of the pointer that points to the cell
                //to move
                ref var entry = ref _entries[_freeEntryIndex];
                var movingBucketIndex = Reduce((uint)entry.Hashcode, (uint)_buckets.Length, _fastModBucketsMultiplier);

                //if the item is found and the bucket points directly to the node to remove
                //it must now point to the cell where it's going to be moved
                if (_buckets[movingBucketIndex] - 1 == _freeEntryIndex)
                    _buckets[movingBucketIndex] = indexToValueToRemove + 1;

                //otherwise it means that there was more than one item with the same hash (collision), so 
                //we need to update the linked list and its pointers
                int next = entry.Next;
                int previous = entry.Previous;

                //they now point to the cell where the last value is moved into
                if (next != -1)
                    _entries[next].Previous = indexToValueToRemove;
                if (previous != -1)
                    _entries[previous].Next = indexToValueToRemove;

                //finally, actually move the values
                _entries[indexToValueToRemove] = entry;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrimExcess()
            => Resize(this.Count, this.Count);

        //I store all the index with an offset + 1, so that in the bucket list 0 means actually not existing.
        //When read the offset must be offset by -1 again to be the real one. In this way
        //I avoid to initialize the array to -1

        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool TryFindIndex(T item, out int findIndex)
        {
            int hash = item.GetHashCode();

            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            int valueIndex = _buckets[bucketIndex] - 1;

            //even if we found an existing value we need to be sure it's the one we requested
            while (valueIndex != -1)
            {
                ref var entry = ref _entries[valueIndex];
                if (entry.Hashcode == hash && EqualityComparer<T>.Default.Equals(entry.Key, item) == true)
                {
                    //this is the one
                    findIndex = valueIndex;
                    return true;
                }

                valueIndex = entry.Previous;
            }

            findIndex = 0;
            return false;
        }

        //I store all the index with an offset + 1, so that in the bucket list 0 means actually not existing.
        //When read the offset must be offset by -1 again to be the real one. In this way
        //I avoid to initialize the array to -1

        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool TryFindIndex(in T item, out int findIndex)
        {
            int hash = item.GetHashCode();

            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            int valueIndex = _buckets[bucketIndex] - 1;

            //even if we found an existing value we need to be sure it's the one we requested
            while (valueIndex != -1)
            {
                ref var entry = ref _entries[valueIndex];
                if (entry.Hashcode == hash && EqualityComparer<T>.Default.Equals(entry.Key, item) == true)
                {
                    //this is the one
                    findIndex = valueIndex;
                    return true;
                }

                valueIndex = entry.Previous;
            }

            findIndex = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest)
            => CopyTo(dest.AsSpan(), 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest, int destIndex)
            => CopyTo(dest.AsSpan(), destIndex, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest, int destIndex, int count)
            => CopyTo(dest.AsSpan(), destIndex, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest)
            => CopyTo(dest, 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest, int destIndex)
            => CopyTo(dest, destIndex, Count);

        public void CopyTo(in Span<T> dest, int destIndex, int count)
        {
            if (destIndex < 0 || destIndex > dest.Length)
            {
                ThrowHelper.ThrowDestIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (count < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            }

            if (dest.Length - destIndex < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            Span<ArrayEntry<T>> items = _entries.AsSpan();

            if (items.Length == 0)
                return;

            for (int i = 0, len = this.Count; i < len && count > 0; i++)
            {
                dest[destIndex++] = items[i].Key;
                count--;
            }
        }

        public void Dispose()
        {
            ReturnBuckets(s_emptyBuckets);
            ReturnEntries(s_emptyEntries);
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

        private void ReturnBuckets(int[] replaceWith)
        {
            if (_buckets.IsNullOrEmpty() == false)
            {
                try
                {
                    _bucketPool.Return(_buckets);
                }
                catch { }
            }

            _buckets = replaceWith ?? s_emptyBuckets;
        }

        private void ReturnEntries(ArrayEntry<T>[] replaceWith)
        {
            if (_entries.IsNullOrEmpty() == false)
            {
                try
                {
                    _entryPool.Return(_entries, s_clearEntries);
                }
                catch { }
            }

            _entries = replaceWith ?? s_emptyEntries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Reduce(uint hashcode, uint N, ulong fastModBucketsMultiplier)
        {
            if (hashcode >= N) //is the condition return actually an optimization?
                return Environment.Is64BitProcess ? HashHelpers.FastMod(hashcode, N, fastModBucketsMultiplier) : hashcode % N;

            return hashcode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpdateLinkedList(int index, ref ArrayEntry<T>[] valuesInfo)
        {
            int next = valuesInfo[index].Next;
            int previous = valuesInfo[index].Previous;

            if (next != -1)
                valuesInfo[next].Previous = previous;
            if (previous != -1)
                valuesInfo[previous].Next = next;
        }

        bool ICollection<T>.IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICollection<T>.Add(T item)
            => Add(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new Enumerator(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>
        {

            private readonly ValueArrayHashSet<T> _set;

#if DEBUG
            private int _startCount;
#endif

            private int _count;
            private int _index;

            public Enumerator(in ValueArrayHashSet<T> set)
            {
                _set = set;
                _index = -1;
                _count = set.Count;
#if DEBUG
                _startCount = set.Count;
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
#if DEBUG
                if (_count != _startCount)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
#endif
                if (_index < _count - 1)
                {
                    ++_index;
                    return true;
                }

                return false;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _set._entries[_index].Key;
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _set._entries[_index].Key;
            }

            public void SetRange(int startIndex, int count)
            {
                _index = startIndex - 1;
                _count = count;
#if DEBUG
                if (_count > _startCount)
                    throw new InvalidOperationException("Cannot set a count greater than its starting value");

                _startCount = count;
#endif
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose() { }
        }
    }
}
