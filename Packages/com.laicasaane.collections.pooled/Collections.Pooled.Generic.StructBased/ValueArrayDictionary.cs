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
    [Serializable]
    public partial struct ValueArrayDictionary<TKey, TValue>
        : IArrayDictionary<TKey, TValue>
        , IDictionary<TKey, TValue>
        , ISerializable
        , IDeserializationCallback
        , IDisposable
        where TKey : notnull
    {
        // constants for serialization
        private const string CountName = "Count"; // Do not rename (binary serialization). Must save buckets.Length
        private const string KeyValuePairsName = "KeyValuePairs"; // Do not rename (binary serialization)

        internal ArrayEntry<TKey>[] _entries;
        internal TValue[] _values;
        internal int[] _buckets;

        internal int _freeEntryIndex;
        internal int _collisions;
        internal ulong _fastModBucketsMultiplier;

        [NonSerialized] internal ArrayPool<ArrayEntry<TKey>> _entryPool;
        [NonSerialized] internal ArrayPool<TValue> _valuePool;
        [NonSerialized] internal ArrayPool<int> _bucketPool;

        internal static readonly bool s_clearEntries = SystemRuntimeHelpers.IsReferenceOrContainsReferences<TKey>();
        internal static readonly bool s_clearValues = SystemRuntimeHelpers.IsReferenceOrContainsReferences<TValue>();

        private static readonly ArrayEntry<TKey>[] s_emptyEntries = new ArrayEntry<TKey>[0];
        private static readonly TValue[] s_emptyValues = new TValue[0];
        private static readonly int[] s_emptyBuckets = new int[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayDictionary<TKey, TValue> Create()
            => new ValueArrayDictionary<TKey, TValue>(0
                , ArrayPool<ArrayEntry<TKey>>.Shared
                , ArrayPool<TValue>.Shared
                , ArrayPool<int>.Shared
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayDictionary<TKey, TValue> Create(int capacity)
            => new ValueArrayDictionary<TKey, TValue>(capacity
                , ArrayPool<ArrayEntry<TKey>>.Shared
                , ArrayPool<TValue>.Shared
                , ArrayPool<int>.Shared
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayDictionary<TKey, TValue> Create(int capacity
            , ArrayPool<ArrayEntry<TKey>> entryPool
            , ArrayPool<TValue> valuePool
            , ArrayPool<int> bucketPool
        )
            => new ValueArrayDictionary<TKey, TValue>(capacity, entryPool, valuePool, bucketPool);

        internal ValueArrayDictionary(int capacity
            , ArrayPool<ArrayEntry<TKey>> entryPool
            , ArrayPool<TValue> valuePool
            , ArrayPool<int> bucketPool
        )
            : this()
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            _entryPool = entryPool ?? ArrayPool<ArrayEntry<TKey>>.Shared;
            _valuePool = valuePool ?? ArrayPool<TValue>.Shared;
            _bucketPool = bucketPool ?? ArrayPool<int>.Shared;

            Initialize(capacity);

            if (capacity > 0)
                _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)capacity);
        }

        private void Initialize(int capacity)
        {
            capacity = HashHelpers.GetPrime(capacity);

            _entries = _entryPool.Rent(capacity);
            _values = _valuePool.Rent(capacity);
            _buckets = _bucketPool.Rent(capacity);
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
                var array = new KeyValuePair<TKey, TValue>[count];
                CopyTo(array);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
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

                KeyValuePair<TKey, TValue>[]? array = (KeyValuePair<TKey, TValue>[]?)
                    siInfo.GetValue(KeyValuePairsName, typeof(KeyValuePair<TKey, TValue>[]));

                if (array == null)
                {
                    ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Key == null)
                    {
                        ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_NullKey);
                    }

                    Add(array[i].Key, array[i].Value);
                }
            }

            HashHelpers.SerializationInfoTable.Remove(this);
        }

        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[GetIndex(key)];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                TryGetIndex(key, out var index);

                _values[index] = value;
            }
        }

        public TValue this[in TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[GetIndex(in key)];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                TryGetIndex(in key, out var index);

                _values[index] = value;
            }
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _freeEntryIndex;
        }

        public ValueArrayDictionaryKeyCollection<TKey, TValue> Keys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ValueArrayDictionaryKeyCollection<TKey, TValue>(this);
        }

        public ValueArrayDictionaryValueCollection<TKey, TValue> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ValueArrayDictionaryValueCollection<TKey, TValue>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, TValue value)
        {
            var ret = TryGetIndex(key, out var index);

#if DEBUG
            if (ret == false)
                ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, in TValue value)
        {
            var ret = TryGetIndex(key, out var index);

#if DEBUG
            if (ret == false)
                ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in TKey key, TValue value)
        {
            var ret = TryGetIndex(in key, out var index);

#if DEBUG
            if (ret == false)
                ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in TKey key, in TValue value)
        {
            var ret = TryGetIndex(in key, out var index);

#if DEBUG
            if (ret == false)
                ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, TValue value, out int index)
        {
            var ret = TryGetIndex(key, out index);

            if (ret == true)
                _values[index] = value;

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, in TValue value, out int index)
        {
            var ret = TryGetIndex(key, out index);

            if (ret == true)
                _values[index] = value;

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(in TKey key, TValue value, out int index)
        {
            var ret = TryGetIndex(in key, out index);

            if (ret == true)
                _values[index] = value;

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(in TKey key, in TValue value, out int index)
        {
            var ret = TryGetIndex(in key, out index);

            if (ret == true)
                _values[index] = value;

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TKey key, TValue value)
        {
            var ret = TryGetIndex(key, out var index);

#if DEBUG
            if (ret == true)
                throw new InvalidOperationException("Try to set value on an unexisting key.");
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TKey key, in TValue value)
        {
            var ret = TryGetIndex(key, out var index);

#if DEBUG
            if (ret == true)
                throw new InvalidOperationException("Try to set value on an unexisting key.");
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(in TKey key, TValue value)
        {
            var ret = TryGetIndex(in key, out var index);

#if DEBUG
            if (ret == true)
                throw new InvalidOperationException("Try to set value on an unexisting key.");
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(in TKey key, in TValue value)
        {
            var ret = TryGetIndex(in key, out var index);

#if DEBUG
            if (ret == true)
                throw new InvalidOperationException("Try to set value on an unexisting key.");
#endif

            _values[index] = value;
        }

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

            if (s_clearValues)
            {
                Array.Clear(_values, 0, _values.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastClear()
        {
            if (_freeEntryIndex == 0)
                return;

            _freeEntryIndex = 0;

            //Buckets cannot be FastCleared because it's important that the values are reset to 0
            Array.Clear(_buckets, 0, _buckets.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool ContainsKey(TKey key)
        {
            return TryFindIndex(key, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool ContainsKey(in TKey key)
        {
            return TryFindIndex(in key, out _);
        }

        public bool ContainsValue(TValue value)
        {
            TValue[] values = _values;

            if (value == null)
            {
                foreach (var item in values)
                {
                    if (item == null)
                        return true;
                }
            }
            else if (typeof(TValue).IsValueType)
            {
                // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                foreach (var item in values)
                {
                    if (EqualityComparer<TValue>.Default.Equals(item, value))
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
                foreach (var item in values)
                {
                    if (defaultComparer.Equals(item, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ContainsValue(in TValue value)
        {
            TValue[] values = _values;

            if (value == null)
            {
                foreach (var item in values)
                {
                    if (item == null)
                        return true;
                }
            }
            else if (typeof(TValue).IsValueType)
            {
                // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                foreach (var item in values)
                {
                    if (EqualityComparer<TValue>.Default.Equals(item, value))
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
                foreach (var item in values)
                {
                    if (defaultComparer.Equals(item, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool TryGetValue(TKey key, out TValue result)
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                result = _values[findIndex];
                return true;
            }

            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool TryGetValue(in TKey key, out TValue result)
        {
            if (TryFindIndex(in key, out var findIndex) == true)
            {
                result = _values[findIndex];
                return true;
            }

            result = default;
            return false;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            if (_values.Length < capacity)
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

            Resize(this.Count, HashHelpers.ExpandPrime(_values.Length + capacity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(TKey key)
        {
#if DEBUG
            if (TryFindIndex(key, out var findIndex) == true)
                return findIndex;

            ThrowHelper.ThrowKeyNotFoundException(key);
            return default;
#else
            //Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
            TryFindIndex(key, out var findIndex);
            
            return findIndex;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(in TKey key)
        {
#if DEBUG
            if (TryFindIndex(in key, out var findIndex) == true)
                return findIndex;

            ThrowHelper.ThrowKeyNotFoundException(key);
            return default;
#else
            //Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
            TryFindIndex(in key, out var findIndex);
            
            return findIndex;
#endif
        }

        bool TryGetIndex(TKey key, out int index)
        {
            int hash = key.GetHashCode(); //IEquatable doesn't enforce the override of GetHashCode
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //buckets value -1 means it's empty
            var valueIndex = _buckets[bucketIndex] - 1;

            if (valueIndex == -1)
            {
                ResizeIfNeeded();
                //create the info node at the last position and fill it with the relevant information
                _entries[_freeEntryIndex] = new ArrayEntry<TKey>(key, hash);
            }
            else //collision or already exists
            {
                int currentValueIndex = valueIndex;
                do
                {
                    //must check if the key already exists in the dictionary
                    //Comparer<TKey>.default needs to create a new comparer, so it is much slower
                    //than assuming that Equals is implemented through IEquatable
                    ref var entry = ref _entries[currentValueIndex];
                    if (entry.Hashcode == hash && entry.Key.Equals(key) == true)
                    {
                        //the key already exists, simply replace the value!
                        index = currentValueIndex;
                        return false;
                    }

                    currentValueIndex = entry.Previous;
                } while (currentValueIndex != -1); //-1 means no more values with key with the same hash

                ResizeIfNeeded();

                //oops collision!
                _collisions++;
                //create a new node which previous index points to node currently pointed in the bucket
                _entries[_freeEntryIndex] = new ArrayEntry<TKey>(key, hash, valueIndex);
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

        bool TryGetIndex(in TKey key, out int index)
        {
            int hash = key.GetHashCode(); //IEquatable doesn't enforce the override of GetHashCode
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //buckets value -1 means it's empty
            var valueIndex = _buckets[bucketIndex] - 1;

            if (valueIndex == -1)
            {
                ResizeIfNeeded();
                //create the info node at the last position and fill it with the relevant information
                _entries[_freeEntryIndex] = new ArrayEntry<TKey>(in key, hash);
            }
            else //collision or already exists
            {
                int currentValueIndex = valueIndex;
                do
                {
                    //must check if the key already exists in the dictionary
                    //Comparer<TKey>.default needs to create a new comparer, so it is much slower
                    //than assuming that Equals is implemented through IEquatable
                    ref var entry = ref _entries[currentValueIndex];
                    if (entry.Hashcode == hash && entry.Key.Equals(key) == true)
                    {
                        //the key already exists, simply replace the value!
                        index = currentValueIndex;
                        return false;
                    }

                    currentValueIndex = entry.Previous;
                } while (currentValueIndex != -1); //-1 means no more values with key with the same hash

                ResizeIfNeeded();

                //oops collision!
                _collisions++;
                //create a new node which previous index points to node currently pointed in the bucket
                _entries[_freeEntryIndex] = new ArrayEntry<TKey>(in key, hash, valueIndex);
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
            if (_freeEntryIndex == _values.Length)
            {
                Resize(this.Count, HashHelpers.ExpandPrime(_freeEntryIndex));
            }
        }

        void Resize(int count, int newCapacity)
        {
            TValue[] values = _values;

            if (newCapacity < values.Length)
            {
                var newValues = _valuePool.Rent(newCapacity);

                if (newValues.Length < values.Length)
                {
                    if (count > 0)
                        Array.Copy(values, newValues, count);

                    _values = newValues;

                    if (values?.Length > 0)
                        _valuePool.Return(values, s_clearValues);
                }
                else
                {
                    _valuePool.Return(newValues);
                }
            }

            ArrayEntry<TKey>[] entries = _entries;

            if (newCapacity < entries.Length)
            {
                var newEntries = _entryPool.Rent(newCapacity);

                if (newEntries.Length < entries.Length)
                {
                    if (count > 0)
                        Array.Copy(entries, newEntries, count);

                    _entries = newEntries;

                    if (entries?.Length > 0)
                        _entryPool.Return(entries, s_clearEntries);
                }
                else
                {
                    _entryPool.Return(newEntries);
                }
            }
        }

        public bool Remove(TKey key)
        {
            return Remove(key, out _, out _);
        }

        public bool Remove(in TKey key)
        {
            return Remove(in key, out _, out _);
        }

        public bool Remove(TKey key, out int index, out TValue value)
        {
            int hash = key.GetHashCode();
            uint bucketIndex = Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //find the bucket
            int indexToValueToRemove = _buckets[bucketIndex] - 1;

            //Part one: look for the actual key in the bucket list if found I update the bucket list so that it doesn't
            //point anymore to the cell to remove
            while (indexToValueToRemove != -1)
            {
                ref var entry = ref _entries[indexToValueToRemove];
                if (entry.Hashcode == hash && entry.Key.Equals(key) == true)
                {
                    //if the key is found and the bucket points directly to the node to remove
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
                value = default;
                return false; //not found!
            }

            index = indexToValueToRemove;

            _freeEntryIndex--; //one less value to iterate
            value = _values[indexToValueToRemove];

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

                //if the key is found and the bucket points directly to the node to remove
                //it must now point to the cell where it's going to be moved
                if (_buckets[movingBucketIndex] - 1 == _freeEntryIndex)
                    _buckets[movingBucketIndex] = indexToValueToRemove + 1;

                //otherwise it means that there was more than one key with the same hash (collision), so 
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
                _values[indexToValueToRemove] = _values[_freeEntryIndex];
            }

            return true;
        }

        public bool Remove(in TKey key, out int index, out TValue value)
        {
            int hash = key.GetHashCode();
            uint bucketIndex = Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //find the bucket
            int indexToValueToRemove = _buckets[bucketIndex] - 1;

            //Part one: look for the actual key in the bucket list if found I update the bucket list so that it doesn't
            //point anymore to the cell to remove
            while (indexToValueToRemove != -1)
            {
                ref var entry = ref _entries[indexToValueToRemove];
                if (entry.Hashcode == hash && entry.Key.Equals(key) == true)
                {
                    //if the key is found and the bucket points directly to the node to remove
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
                value = default;
                return false; //not found!
            }

            index = indexToValueToRemove;

            _freeEntryIndex--; //one less value to iterate
            value = _values[indexToValueToRemove];

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

                //if the key is found and the bucket points directly to the node to remove
                //it must now point to the cell where it's going to be moved
                if (_buckets[movingBucketIndex] - 1 == _freeEntryIndex)
                    _buckets[movingBucketIndex] = indexToValueToRemove + 1;

                //otherwise it means that there was more than one key with the same hash (collision), so 
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
                _values[indexToValueToRemove] = _values[_freeEntryIndex];
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
        public bool TryFindIndex(TKey key, out int findIndex)
        {
            int hash = key.GetHashCode();

            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            int valueIndex = _buckets[bucketIndex] - 1;

            //even if we found an existing value we need to be sure it's the one we requested
            while (valueIndex != -1)
            {
                //Comparer<TKey>.default needs to create a new comparer, so it is much slower
                //than assuming that Equals is implemented through IEquatable
                ref var entry = ref _entries[valueIndex];
                if (entry.Hashcode == hash && entry.Key.Equals(key) == true)
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
        public bool TryFindIndex(in TKey key, out int findIndex)
        {
            int hash = key.GetHashCode();

            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            int valueIndex = _buckets[bucketIndex] - 1;

            //even if we found an existing value we need to be sure it's the one we requested
            while (valueIndex != -1)
            {
                //Comparer<TKey>.default needs to create a new comparer, so it is much slower
                //than assuming that Equals is implemented through IEquatable
                ref var entry = ref _entries[valueIndex];
                if (entry.Hashcode == hash && entry.Key.Equals(key) == true)
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
        public void CopyTo(KVPair<TKey, TValue>[] dest)
            => CopyTo(dest.AsSpan(), 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(KVPair<TKey, TValue>[] dest, int destIndex)
            => CopyTo(dest.AsSpan(), destIndex, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(KVPair<TKey, TValue>[] dest, int destIndex, int count)
            => CopyTo(dest.AsSpan(), destIndex, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<KVPair<TKey, TValue>> dest)
            => CopyTo(dest, 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<KVPair<TKey, TValue>> dest, int destIndex)
            => CopyTo(dest, destIndex, Count);

        public void CopyTo(in Span<KVPair<TKey, TValue>> dest, int destIndex, int count)
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

            Span<ArrayEntry<TKey>> keys = _entries.AsSpan();
            Span<TValue> values = _values.AsSpan();

            if (keys.Length == 0 || values.Length == 0)
                return;

            for (int i = 0, len = this.Count; i < len && count > 0; i++)
            {
                dest[destIndex++] = new KVPair<TKey, TValue>(keys[i].Key, values[i]);
                count--;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(KeyValuePair<TKey, TValue>[] dest)
            => CopyTo(dest.AsSpan(), 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(KeyValuePair<TKey, TValue>[] dest, int destIndex)
            => CopyTo(dest.AsSpan(), destIndex, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(KeyValuePair<TKey, TValue>[] dest, int destIndex, int count)
            => CopyTo(dest.AsSpan(), destIndex, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<KeyValuePair<TKey, TValue>> dest)
            => CopyTo(dest, 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<KeyValuePair<TKey, TValue>> dest, int destIndex)
            => CopyTo(dest, destIndex, Count);

        public void CopyTo(in Span<KeyValuePair<TKey, TValue>> dest, int destIndex, int count)
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

            Span<ArrayEntry<TKey>> keys = _entries.AsSpan();
            Span<TValue> values = _values.AsSpan();

            if (keys.Length == 0 || values.Length == 0)
                return;

            for (int i = 0, len = this.Count; i < len && count > 0; i++)
            {
                dest[destIndex++] = new KeyValuePair<TKey, TValue>(keys[i].Key, values[i]);
                count--;
            }
        }

        public void Intersect<UValue>(in ValueArrayDictionary<TKey, UValue> other)
        {
            var keys = _entries;

            for (int i = Count - 1; i >= 0; i--)
            {
                var key = keys[i].Key;
                if (other.ContainsKey(key) == false)
                {
                    this.Remove(key);
                }
            }
        }

        public void Exclude<UValue>(in ValueArrayDictionary<TKey, UValue> otherDicKeys)
        {
            var keys = _entries;

            for (int i = Count - 1; i >= 0; i--)
            {
                var key = keys[i].Key;
                if (otherDicKeys.ContainsKey(key) == true)
                {
                    this.Remove(key);
                }
            }
        }

        public void Union(in ValueArrayDictionary<TKey, TValue> other)
        {
            foreach (var kv in other)
            {
                this[kv.Key] = kv.Value;
            }
        }

        public void Dispose()
        {
            ReturnBuckets(s_emptyBuckets);
            ReturnEntries(s_emptyEntries);
            ReturnValues(s_emptyValues);
        }

        private void RenewBuckets(int newSize)
        {
            if (_buckets?.Length > 0)
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
            if (_buckets?.Length > 0)
            {
                try
                {
                    _bucketPool.Return(_buckets);
                }
                catch { }
            }

            _buckets = replaceWith ?? s_emptyBuckets;
        }

        private void ReturnEntries(ArrayEntry<TKey>[] replaceWith)
        {
            if (_entries?.Length > 0)
            {
                try
                {
                    _entryPool.Return(_entries, s_clearEntries);
                }
                catch { }
            }

            _entries = replaceWith ?? s_emptyEntries;
        }

        private void ReturnValues(TValue[] replaceWith)
        {
            if (_values?.Length > 0)
            {
                try
                {
                    _valuePool.Return(_values, s_clearValues);
                }
                catch { }
            }

            _values = replaceWith ?? s_emptyValues;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Reduce(uint hashcode, uint N, ulong fastModBucketsMultiplier)
        {
            if (hashcode >= N) //is the condition return actually an optimization?
                return Environment.Is64BitProcess ? HashHelpers.FastMod(hashcode, N, fastModBucketsMultiplier) : hashcode % N;

            return hashcode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpdateLinkedList(int index, ref ArrayEntry<TKey>[] valuesInfo)
        {
            int next = valuesInfo[index].Next;
            int previous = valuesInfo[index].Previous;

            if (next != -1)
                valuesInfo[next].Previous = previous;
            if (previous != -1)
                valuesInfo[previous].Next = next;
        }

        bool ICollection<KVPair<TKey, TValue>>.IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICollection<KVPair<TKey, TValue>>.Add(KVPair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICollection<KVPair<TKey, TValue>>.Contains(KVPair<TKey, TValue> item)
            => ContainsKey(item.Key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICollection<KVPair<TKey, TValue>>.Remove(KVPair<TKey, TValue> item)
            => Remove(item.Key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
            => new KVPairEnumerator(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new KVPairEnumerator(this);

        bool ICollection<ArrayKVPair<TKey, TValue>>.IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICollection<ArrayKVPair<TKey, TValue>>.Add(ArrayKVPair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICollection<ArrayKVPair<TKey, TValue>>.Contains(ArrayKVPair<TKey, TValue> item)
            => ContainsKey(item.Key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICollection<ArrayKVPair<TKey, TValue>>.CopyTo(ArrayKVPair<TKey, TValue>[] dest, int destIndex)
        {
            if (destIndex < 0 || destIndex > dest.Length)
            {
                ThrowHelper.ThrowDestIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (dest.Length - destIndex < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            Span<ArrayEntry<TKey>> keys = _entries.AsSpan();
            TValue[] values = _values ?? s_emptyValues;

            if (keys.Length == 0 || values.Length == 0)
                return;

            for (int i = 0, len = this.Count; i < len; i++)
            {
                dest[destIndex++] = new ArrayKVPair<TKey, TValue>(keys[i].Key, values, i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICollection<ArrayKVPair<TKey, TValue>>.Remove(ArrayKVPair<TKey, TValue> item)
            => Remove(item.Key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<ArrayKVPair<TKey, TValue>> IEnumerable<ArrayKVPair<TKey, TValue>>.GetEnumerator()
            => new Enumerator(this);

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ValueArrayDictionaryKeyCollection<TKey, TValue>(this);
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ValueArrayDictionaryValueCollection<TKey, TValue>(this);
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ValueArrayDictionaryKeyCollection<TKey, TValue>(this);
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ValueArrayDictionaryValueCollection<TKey, TValue>(this);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => ContainsKey(item.Key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
            => Remove(item.Key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => new KeyValuePairEnumerator(this);
    }
}
