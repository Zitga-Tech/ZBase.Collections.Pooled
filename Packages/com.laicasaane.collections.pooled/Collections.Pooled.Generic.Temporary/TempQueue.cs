// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Queue.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
** Purpose: A circular-array implementation of a generic queue.
**
**
=============================================================================*/

#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Collections.Pooled.Generic
{
    public ref partial struct TempQueue<T>
    {
        internal T[] _array;
        internal int _head;       // The index from which to dequeue if the queue isn't empty.
        internal int _tail;       // The index at which to enqueue if the queue isn't full.
        internal int _size;       // Number of elements.
        internal int _version;

        [NonSerialized]
        internal ArrayPool<T> _pool;

        private static readonly T[] s_emptyArray = new T[0];

        internal static readonly bool s_clearArray = SystemRuntimeHelpers.IsReferenceOrContainsReferences<T>();

        // Creates a queue with room for capacity objects. The default grow factor
        // is used.
        internal TempQueue(int capacity, ArrayPool<T> pool)
        {
            if (capacity < 0)
                ThrowHelper.ThrowCapacityArgumentOutOfRange_NeedNonNegNumException();

            _head = default;
            _tail = default;
            _size = default;
            _version = default;
            _pool = pool ?? ArrayPool<T>.Shared;
            _array = capacity == 0 ? s_emptyArray : _pool.Rent(capacity);
        }

        // Fills a Queue with the elements of an ICollection.  Uses the enumerator
        // to get each of the elements.
        internal TempQueue(IEnumerable<T> collection, ArrayPool<T> pool)
        {
            if (collection == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);

            _head = default;
            _tail = default;
            _size = default;
            _version = default;
            _pool = pool ?? ArrayPool<T>.Shared;
            _array = EnumerableHelpers.ToArray(collection, s_emptyArray, _pool, out _size);
            if (_size != _array.Length) _tail = _size;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _size; }
        }

        // Removes all Objects from the queue.
        public void Clear()
        {
            if (_size != 0)
            {
                if (s_clearArray)
                {
                    if (_head < _tail)
                    {
                        Array.Clear(_array, _head, _size);
                    }
                    else
                    {
                        Array.Clear(_array, _head, _array.Length - _head);
                        Array.Clear(_array, 0, _tail);
                    }
                }

                _size = 0;
            }

            _head = 0;
            _tail = 0;
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest)
            => CopyTo(dest, 0, _size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest, int destIndex)
            => CopyTo(dest, destIndex, _size);

        public void CopyTo(T[] dest, int destIndex, int count)
        {
            if (dest == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dest);

            CopyTo(dest, destIndex, count);
        }

        // Adds item to the tail of the queue.
        public void Enqueue(T item)
        {
            if (_size == _array.Length)
            {
                Grow(_size + 1);
            }

            _array[_tail] = item;
            MoveNext(ref _tail);
            _size++;
            _version++;
        }

        // GetEnumerator returns an IEnumerator over this Queue.  This
        // Enumerator will support removing.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        // Removes the object at the head of the queue and returns it. If the queue
        // is empty, this method throws an
        // InvalidOperationException.
        public T Dequeue()
        {
            int head = _head;
            T[] array = _array;

            if (_size == 0)
            {
                ThrowForEmptyQueue();
            }

            T removed = array[head];
            if (s_clearArray)
            {
                array[head] = default!;
            }
            MoveNext(ref _head);
            _size--;
            _version++;
            return removed;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            int head = _head;
            T[] array = _array;

            if (_size == 0)
            {
                result = default!;
                return false;
            }

            result = array[head];
            if (s_clearArray)
            {
                array[head] = default!;
            }
            MoveNext(ref _head);
            _size--;
            _version++;
            return true;
        }

        // Returns the object at the head of the queue. The object remains in the
        // queue. If the queue is empty, this method throws an
        // InvalidOperationException.
        public T Peek()
        {
            if (_size == 0)
            {
                ThrowForEmptyQueue();
            }

            return _array[_head];
        }

        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            if (_size == 0)
            {
                result = default!;
                return false;
            }

            result = _array[_head];
            return true;
        }

        // Returns true if the queue contains at least one object equal to item.
        // Equality is determined using EqualityComparer<T>.Default.Equals().
        public bool Contains(T item)
        {
            if (_size == 0)
            {
                return false;
            }

            if (_head < _tail)
            {
                return Array.IndexOf(_array, item, _head, _size) >= 0;
            }

            // We've wrapped around. Check both partitions, the least recently enqueued first.
            return
                Array.IndexOf(_array, item, _head, _array.Length - _head) >= 0 ||
                Array.IndexOf(_array, item, 0, _tail) >= 0;
        }

        // Iterates over the objects in the queue, returning an array of the
        // objects in the Queue, or an empty array if the queue is empty.
        // The order of elements in the array is first in to last in, the same
        // order produced by successive calls to Dequeue.
        public T[] ToArray()
        {
            if (_size == 0)
            {
                return s_emptyArray;
            }

            T[] arr = new T[_size];

            if (_head < _tail)
            {
                Array.Copy(_array, _head, arr, 0, _size);
            }
            else
            {
                Array.Copy(_array, _head, arr, 0, _array.Length - _head);
                Array.Copy(_array, 0, arr, _array.Length - _head, _tail);
            }

            return arr;
        }

        // PRIVATE Grows or shrinks the buffer to hold capacity objects. Capacity
        // must be >= _size.
        private void SetCapacity(int capacity)
        {
            T[] newArray = _pool.Rent(capacity);

            if (capacity < _array.Length && newArray.Length >= _array.Length)
            {
                _pool.Return(newArray);
                return;
            }

            if (_size > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, newArray, 0, _size);
                }
                else
                {
                    Array.Copy(_array, _head, newArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, newArray, _array.Length - _head, _tail);
                }
            }

            ReturnArray(replaceWith: newArray);
            _head = 0;
            _tail = (_size == capacity) ? 0 : _size;
            _version++;
        }

        // Increments the index wrapping it if necessary.
        private void MoveNext(ref int index)
        {
            // It is tempting to use the remainder operator here but it is actually much slower
            // than a simple comparison and a rarely taken branch.
            // JIT produces better code than with ternary operator ?:
            int tmp = index + 1;
            if (tmp == _array.Length)
            {
                tmp = 0;
            }
            index = tmp;
        }

        private void ThrowForEmptyQueue()
        {
            Debug.Assert(_size == 0);
            ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EmptyQueue();
        }

        public void TrimExcess()
        {
            int threshold = (int)(_array.Length * 0.9);
            if (_size < threshold)
            {
                SetCapacity(_size);
            }
        }

        /// <summary>
        /// Ensures that the capacity of this Queue is at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        /// <returns>The new capacity of this queue.</returns>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_array.Length < capacity)
            {
                Grow(capacity);
            }

            return _array.Length;
        }

        private void Grow(int capacity)
        {
            Debug.Assert(_array.Length < capacity);

            const int GrowFactor = 2;
            const int MinimumGrow = 4;

            int newcapacity = GrowFactor * _array.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newcapacity > SystemArray.MaxLength) newcapacity = SystemArray.MaxLength;

            // Ensure minimum growth is respected.
            newcapacity = Math.Max(newcapacity, _array.Length + MinimumGrow);

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newcapacity < capacity) newcapacity = capacity;

            SetCapacity(newcapacity);
        }

        private void ReturnArray(T[] replaceWith)
        {
            if (_array.IsNullOrEmpty() == false)
            {
                try
                {
                    _pool.Return(_array, clearArray: s_clearArray);
                }
                catch { }
            }

            _array = replaceWith ?? s_emptyArray;
        }

        // Implements an enumerator for a Queue.  The enumerator uses the
        // internal version number of the list to ensure that no modifications are
        // made to the list while an enumeration is in progress.
        public ref struct Enumerator
        {
            private readonly TempQueue<T> _q;
            private readonly int _version;
            private int _index;   // -1 = not started, -2 = ended/disposed
            private T? _currentElement;

            internal Enumerator(TempQueue<T> q)
            {
                _q = q;
                _version = q._version;
                _index = -1;
                _currentElement = default;
            }

            public void Dispose()
            {
                _index = -2;
                _currentElement = default;
            }

            public bool MoveNext()
            {
                if (_version != _q._version)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();

                if (_index == -2)
                    return false;

                _index++;

                if (_index == _q._size)
                {
                    // We've run past the last element
                    _index = -2;
                    _currentElement = default;
                    return false;
                }

                // Cache some fields in locals to decrease code size
                T[] array = _q._array;
                int capacity = array.Length;

                // _index represents the 0-based index into the queue, however the queue
                // doesn't have to start from 0 and it may not even be stored contiguously in memory.

                int arrayIndex = _q._head + _index; // this is the actual index into the queue's backing array
                if (arrayIndex >= capacity)
                {
                    // NOTE: Originally we were using the modulo operator here, however
                    // on Intel processors it has a very high instruction latency which
                    // was slowing down the loop quite a bit.
                    // Replacing it with simple comparison/subtraction operations sped up
                    // the average foreach loop by 2x.

                    arrayIndex -= capacity; // wrap around if needed
                }

                _currentElement = array[arrayIndex];
                return true;
            }

            public T Current
            {
                get
                {
                    if (_index < 0)
                        ThrowEnumerationNotStartedOrEnded();
                    return _currentElement!;
                }
            }

            private void ThrowEnumerationNotStartedOrEnded()
            {
                Debug.Assert(_index == -1 || _index == -2);

                if (_index == -1)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                else
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
            }
        }
    }
}