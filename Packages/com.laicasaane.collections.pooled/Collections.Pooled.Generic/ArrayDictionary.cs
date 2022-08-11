// https://github.com/sebas77/Svelto.Common/blob/master/DataStructures/Dictionaries/SveltoDictionary.cs

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public class ArrayDictionary<TKey, TValue> : IDisposable
    {
        internal ArrayEntry<TKey>[] _valuesInfo;
        internal TValue[] _values;
        internal int[] _buckets;

        internal uint _freeValueCellIndex;
        internal uint _collisions;
        internal ulong _fastModBucketsMultiplier;

        [NonSerialized] internal ArrayPool<ArrayEntry<TKey>> _entryPool;
        [NonSerialized] internal ArrayPool<TValue> _valuePool;
        [NonSerialized] internal ArrayPool<int> _bucketPool;

        internal static readonly bool s_clearValuesInfo = SystemRuntimeHelpers.IsReferenceOrContainsReferences<TKey>();
        internal static readonly bool s_clearValues = SystemRuntimeHelpers.IsReferenceOrContainsReferences<TValue>();

        public ArrayDictionary(int capacity)
        {
            _entryPool = ArrayPool<ArrayEntry<TKey>>.Shared;
            _valuePool = ArrayPool<TValue>.Shared;
            _bucketPool = ArrayPool<int>.Shared;

            _valuesInfo = new ArrayEntry<TKey>[capacity];
            _values = new TValue[capacity];
            _buckets = new int[HashHelpers.GetPrime((int)capacity)];

            if (capacity > 0)
                _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)capacity);
        }

        public ArrayEntry<TKey>[] UnsafeKeys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _valuesInfo;
        }

        public TValue[] UnsafeValues
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values;
        }

        public int Count => (int)_freeValueCellIndex;

        public KeyEnumerable Keys => new KeyEnumerable(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //note, this returns readonly because the enumerator cannot be, but at the same time, it cannot be modified
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, in TValue value)
        {
            var ret = AddValue(key, out var index);

#if DEBUG
            if (ret == false)
                ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, in TValue value, out uint index)
        {
            var ret = AddValue(key, out index);

            if (ret == true)
                _values[index] = value;

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TKey key, in TValue value)
        {
            var ret = AddValue(key, out var index);

#if DEBUG
            if (ret == true)
                throw new InvalidOperationException("Try to set value on an unexisting key.");
#endif

            _values[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_freeValueCellIndex == 0)
                return;

            _freeValueCellIndex = 0;

            //Buckets cannot be FastCleared because it's important that the values are reset to 0
            Array.Clear(_buckets, 0, _buckets.Length);

            if (s_clearValuesInfo)
            {
                Array.Clear(_valuesInfo, 0, _valuesInfo.Length);
            }

            if (s_clearValues)
            {
                Array.Clear(_values, 0, _values.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastClear()
        {
            if (_freeValueCellIndex == 0)
                return;

            _freeValueCellIndex = 0;

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
        public bool TryGetValue(TKey key, out TValue result)
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                result = _values[(int)findIndex];
                return true;
            }

            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key)
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[(int)findIndex];
            }

            AddValue(key, out findIndex);

            _values[(int)findIndex] = default;

            return ref _values[(int)findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key, Func<TValue> builder)
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[(int)findIndex];
            }

            AddValue(key, out findIndex);

            _values[(int)findIndex] = builder();

            return ref _values[(int)findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd<W>(TKey key, FuncRef<W, TValue> builder, ref W parameter)
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[(int)findIndex];
            }

            AddValue(key, out findIndex);

            _values[(int)findIndex] = builder(ref parameter);

            return ref _values[(int)findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue RecycleOrAdd<TValueProxy>
            (TKey key, Func<TValueProxy> builder, ActionRef<TValueProxy> recycler) where TValueProxy : class, TValue
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[(int)findIndex];
            }

            AddValue(key, out findIndex);

            if (_values[(int)findIndex] == null)
                _values[(int)findIndex] = builder();
            else
                recycler(ref Unsafe.As<TValue, TValueProxy>(ref _values[(int)findIndex]));

            return ref _values[(int)findIndex];
        }

        /// <summary>
        /// RecycledOrCreate makes sense to use on dictionaries that are fast cleared and use objects
        /// as value. Once the dictionary is fast cleared, it will try to reuse object values that are
        /// recycled during the fast clearing.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="builder"></param>
        /// <param name="recycler"></param>
        /// <param name="parameter"></param>
        /// <typeparam name="TValueProxy"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue RecycleOrAdd<TValueProxy, U>(TKey key
            , FuncRef<U, TValue> builder
            , ActionRef<TValueProxy, U> recycler
            , ref U parameter
        )
            where TValueProxy : class, TValue
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[(int)findIndex];
            }

            AddValue(key, out findIndex);

            if (_values[(int)findIndex] == null)
                _values[(int)findIndex] = builder(ref parameter);
            else
                recycler(ref Unsafe.As<TValue, TValueProxy>(ref _values[(int)findIndex]), ref parameter);

            return ref _values[(int)findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multi-threaded parallel code
        public ref TValue GetDirectValueByRef(uint index)
        {
            return ref _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetValueByRef(TKey key)
        {
#if DEBUG
            if (TryFindIndex(key, out var findIndex) == true)
                return ref _values[(int)findIndex];

            ThrowHelper.ThrowKeyNotFoundException(key);
            return ref Unsafe.NullRef<TValue>();
#else
            //Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
            TryFindIndex(key, out var findIndex);

            return ref _values[(int) findIndex];
#endif
        }

        public void EnsureCapacity(uint size)
        {
            if (_values.Length < size)
            {
                var expandPrime = HashHelpers.ExpandPrime((int)size);

                Array.Resize(ref _values, expandPrime);
                Array.Resize(ref _valuesInfo, expandPrime);
            }
        }

        public void IncreaseCapacityBy(uint size)
        {
            var expandPrime = HashHelpers.ExpandPrime(_values.Length + (int)size);

            Array.Resize(ref _values, expandPrime);
            Array.Resize(ref _valuesInfo, expandPrime);
        }

        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[(int)GetIndex(key)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                AddValue(key, out var index);

                _values[index] = value;
            }
        }

        bool AddValue(TKey key, out uint indexSet)
        {
            int hash = key.GetHashCode(); //IEquatable doesn't enforce the override of GetHashCode
            uint bucketIndex = Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            //buckets value -1 means it's empty
            var valueIndex = _buckets[bucketIndex] - 1;

            if (valueIndex == -1)
            {
                ResizeIfNeeded();
                //create the info node at the last position and fill it with the relevant information
                _valuesInfo[_freeValueCellIndex] = new ArrayEntry<TKey>(ref key, hash);
            }
            else //collision or already exists
            {
                int currentValueIndex = valueIndex;
                do
                {
                    //must check if the key already exists in the dictionary
                    //Comparer<TKey>.default needs to create a new comparer, so it is much slower
                    //than assuming that Equals is implemented through IEquatable
                    ref var fasterDictionaryNode = ref _valuesInfo[currentValueIndex];
                    if (fasterDictionaryNode.Hashcode == hash && fasterDictionaryNode.Key.Equals(key) == true)
                    {
                        //the key already exists, simply replace the value!
                        indexSet = (uint)currentValueIndex;
                        return false;
                    }

                    currentValueIndex = fasterDictionaryNode.Previous;
                } while (currentValueIndex != -1); //-1 means no more values with key with the same hash

                ResizeIfNeeded();

                //oops collision!
                _collisions++;
                //create a new node which previous index points to node currently pointed in the bucket
                _valuesInfo[_freeValueCellIndex] = new ArrayEntry<TKey>(ref key, hash, valueIndex);
                //update the next of the existing cell to point to the new one
                //old one -> new one | old one <- next one
                _valuesInfo[valueIndex].Next = (int)_freeValueCellIndex;
                //Important: the new node is always the one that will be pointed by the bucket cell
                //so I can assume that the one pointed by the bucket is always the last value added
                //(next = -1)
            }

            //item with this bucketIndex will point to the last value created
            //ToDo: if instead I assume that the original one is the one in the bucket
            //I wouldn't need to update the bucket here. Small optimization but important
            _buckets[bucketIndex] = (int)(_freeValueCellIndex + 1);

            indexSet = _freeValueCellIndex;
            _freeValueCellIndex++;

            //too many collisions?
            if (_collisions > _buckets.Length)
            {
                //we need more space and less collisions
                _buckets = new int[HashHelpers.ExpandPrime((int)_collisions)];
                _collisions = 0;
                _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)_buckets.Length);

                //we need to get all the hash code of all the values stored so far and spread them over the new bucket
                //length
                for (int newValueIndex = 0; newValueIndex < _freeValueCellIndex; newValueIndex++)
                {
                    //get the original hash code and find the new bucketIndex due to the new length
                    ref var fasterDictionaryNode = ref _valuesInfo[newValueIndex];
                    bucketIndex = Reduce((uint)fasterDictionaryNode.Hashcode, (uint)_buckets.Length, _fastModBucketsMultiplier);
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
                        fasterDictionaryNode.Previous = existingValueIndex;
                        fasterDictionaryNode.Next = -1;
                        //and update the previous next index to the new one
                        _valuesInfo[existingValueIndex].Next = newValueIndex;
                    }
                    else
                    {
                        //ok nothing was indexed, the bucket was empty. We need to update the previous
                        //values of next and previous
                        fasterDictionaryNode.Next = -1;
                        fasterDictionaryNode.Previous = -1;
                    }
                }
            }

            return true;
        }

        void ResizeIfNeeded()
        {
            if (_freeValueCellIndex == _values.Length)
            {
                var expandPrime = HashHelpers.ExpandPrime((int)_freeValueCellIndex);

                Array.Resize(ref _values, expandPrime);
                Array.Resize(ref _valuesInfo, expandPrime);
            }
        }

        public bool Remove(TKey key)
        {
            return Remove(key, out _, out _);
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
                ref var fasterDictionaryNode = ref _valuesInfo[indexToValueToRemove];
                if (fasterDictionaryNode.Hashcode == hash && fasterDictionaryNode.Key.Equals(key) == true)
                {
                    //if the key is found and the bucket points directly to the node to remove
                    if (_buckets[bucketIndex] - 1 == indexToValueToRemove)
                    {
#if DEBUG
                        if (fasterDictionaryNode.Next != -1)
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
                        _buckets[bucketIndex] = fasterDictionaryNode.Previous + 1;
                    }
#if DEBUG
                    else
                    {
                        if (fasterDictionaryNode.Next == -1)
                            throw new InvalidOperationException("If the bucket points to another cell, next MUST exists");
                    }
#endif

                    UpdateLinkedList(indexToValueToRemove, ref _valuesInfo);

                    break;
                }

                indexToValueToRemove = fasterDictionaryNode.Previous;
            }

            if (indexToValueToRemove == -1)
            {
                index = default;
                value = default;
                return false; //not found!
            }

            index = indexToValueToRemove;

            _freeValueCellIndex--; //one less value to iterate
            value = _values[indexToValueToRemove];

            //Part two:
            //At this point nodes pointers and buckets are updated, but the _values array
            //still has got the value to delete. Remember the goal of this dictionary is to be able
            //to iterate over the values like an array, so the values array must always be up to date

            //if the cell to remove is the last one in the list, we can perform less operations (no swapping needed)
            //otherwise we want to move the last value cell over the value to remove
            if (indexToValueToRemove != _freeValueCellIndex)
            {
                //we can move the last value of both arrays in place of the one to delete.
                //in order to do so, we need to be sure that the bucket pointer is updated.
                //first we find the index in the bucket list of the pointer that points to the cell
                //to move
                ref var fasterDictionaryNode = ref _valuesInfo[_freeValueCellIndex];
                var movingBucketIndex = Reduce((uint)fasterDictionaryNode.Hashcode, (uint)_buckets.Length, _fastModBucketsMultiplier);

                //if the key is found and the bucket points directly to the node to remove
                //it must now point to the cell where it's going to be moved
                if (_buckets[movingBucketIndex] - 1 == _freeValueCellIndex)
                    _buckets[movingBucketIndex] = indexToValueToRemove + 1;

                //otherwise it means that there was more than one key with the same hash (collision), so 
                //we need to update the linked list and its pointers
                int next = fasterDictionaryNode.Next;
                int previous = fasterDictionaryNode.Previous;

                //they now point to the cell where the last value is moved into
                if (next != -1)
                    _valuesInfo[next].Previous = indexToValueToRemove;
                if (previous != -1)
                    _valuesInfo[previous].Next = indexToValueToRemove;

                //finally, actually move the values
                _valuesInfo[indexToValueToRemove] = fasterDictionaryNode;
                _values[indexToValueToRemove] = _values[_freeValueCellIndex];
            }

            return true;
        }

        public void TrimExcess()
        {
            Array.Resize(ref _values, (int)_freeValueCellIndex);
            Array.Resize(ref _valuesInfo, (int)_freeValueCellIndex);
        }

        //I store all the index with an offset + 1, so that in the bucket list 0 means actually not existing.
        //When read the offset must be offset by -1 again to be the real one. In this way
        //I avoid to initialize the array to -1

        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multithreaded parallel code
        public bool TryFindIndex(TKey key, out uint findIndex)
        {
            int hash = key.GetHashCode();

            uint bucketIndex = Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier);

            int valueIndex = _buckets[bucketIndex] - 1;

            //even if we found an existing value we need to be sure it's the one we requested
            while (valueIndex != -1)
            {
                //Comparer<TKey>.default needs to create a new comparer, so it is much slower
                //than assuming that Equals is implemented through IEquatable
                ref var fasterDictionaryNode = ref _valuesInfo[valueIndex];
                if (fasterDictionaryNode.Hashcode == hash && fasterDictionaryNode.Key.Equals(key) == true)
                {
                    //this is the one
                    findIndex = (uint)valueIndex;
                    return true;
                }

                valueIndex = fasterDictionaryNode.Previous;
            }

            findIndex = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetIndex(TKey key)
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

        public void Intersect<UValue>(ArrayDictionary<TKey, UValue> other)
        {
            var keys = UnsafeKeys;

            for (int i = Count - 1; i >= 0; i--)
            {
                var key = keys[i].Key;
                if (other.ContainsKey(key) == false)
                {
                    this.Remove(key);
                }
            }
        }

        public void Exclude<UValue>(ArrayDictionary<TKey, UValue> otherDicKeys)
        {
            var keys = UnsafeKeys;

            for (int i = Count - 1; i >= 0; i--)
            {
                var key = keys[i].Key;
                if (otherDicKeys.ContainsKey(key) == true)
                {
                    this.Remove(key);
                }
            }
        }

        public void Union(ArrayDictionary<TKey, TValue> other)
        {
            foreach (var kv in other)
            {
                this[kv.Key] = kv.Value;
            }
        }

        public void Dispose()
        {
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

        public struct Enumerator
        {
            public Enumerator(ArrayDictionary<TKey, TValue> dic)
            {
                _dic = dic;
                _index = -1;
                _count = dic.Count;
#if DEBUG
                _startCount = dic.Count;
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

            public ArrayKVPair<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new ArrayKVPair<TKey, TValue>(_dic._valuesInfo[_index].Key, _dic._values, _index);
            }

            public void SetRange(uint startIndex, uint count)
            {
                _index = (int)startIndex - 1;
                _count = (int)count;
#if DEBUG
                if (_count > _startCount)
                    throw new InvalidOperationException("Cannot set a count greater than the starting one");
                _startCount = (int)count;
#endif
            }

            private ArrayDictionary<TKey, TValue> _dic;

#if DEBUG
            private int _startCount;
#endif
            private int _count;

            private int _index;
        }

        public readonly struct KeyEnumerable
        {
            readonly ArrayDictionary<TKey, TValue> _dic;

            internal KeyEnumerable(ArrayDictionary<TKey, TValue> dic)
            {
                _dic = dic;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public KeyEnumerator GetEnumerator() => new KeyEnumerator(_dic);
        }

        public struct KeyEnumerator
        {
            internal KeyEnumerator(ArrayDictionary<TKey, TValue> dic)
            {
                _dic = dic;
                _index = -1;
                _count = dic.Count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
#if DEBUG
                if (_count != _dic.Count)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
#endif
                if (_index < _count - 1)
                {
                    ++_index;
                    return true;
                }

                return false;
            }

            public TKey Current => _dic._valuesInfo[_index].Key;

            private readonly ArrayDictionary<TKey, TValue> _dic;
            private readonly int _count;

            private int _index;
        }
    }
}
