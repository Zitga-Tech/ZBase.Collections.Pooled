// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Collections/src/System/Collections/Generic/Stack.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
** Purpose: An array implementation of a generic stack.
**
**
=============================================================================*/

#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace ZBase.Collections.Pooled.Generic
{
    // A simple stack of objects.  Internally it is implemented as an array,
    // so Push can be O(n).  Pop is O(1).

    [DebuggerTypeProxy(typeof(ValueStackDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public partial struct ValueStack<T> : IEnumerable<T>, IReadOnlyCollection<T>, IDeserializationCallback
    {
        internal T[] _array; // Storage for stack elements. Do not rename (binary serialization)
        internal int _size; // Number of items in the stack. Do not rename (binary serialization)
        internal int _version; // Used to keep enumerator in sync w/ collection. Do not rename (binary serialization)

        [NonSerialized]
        internal ArrayPool<T> _pool;

        private static readonly T[] s_emptyArray = new T[0];

        internal static readonly bool s_clearArray = SystemRuntimeHelpers.IsReferenceOrContainsReferences<T>();

        private const int DefaultCapacity = 4;

        // Create a stack with a specific initial capacity.  The initial capacity
        // must be a non-negative number.
        internal ValueStack(int capacity, ArrayPool<T> pool)
        {
            if (capacity < 0)
                ThrowHelper.ThrowCapacityArgumentOutOfRange_NeedNonNegNumException();

            _size = default;
            _version = default;
            _pool = pool ?? ArrayPool<T>.Shared;
            _array = capacity == 0 ? s_emptyArray : _pool.Rent(capacity);
        }

        // Fills a Stack with the contents of a particular collection.  The items are
        // pushed onto the stack in the same order they are read by the enumerator.
        internal ValueStack(IEnumerable<T> collection, ArrayPool<T> pool)
        {
            if (collection == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);

            _size = default;
            _version = default;
            _pool = pool ?? ArrayPool<T>.Shared;
            _array = EnumerableHelpers.ToArray(collection, s_emptyArray, _pool, out _size);
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _size; }
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array != null;
        }

        // Removes all Objects from the Stack.
        public void Clear()
        {
            if (s_clearArray)
            {
                Array.Clear(_array, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            }
            _size = 0;
            _version++;
        }

        public bool Contains(T item)
        {
            // Compare items using the default equality comparer

            // PERF: Internally Array.LastIndexOf calls
            // EqualityComparer<T>.Default.LastIndexOf, which
            // is specialized for different types. This
            // boosts performance since instead of making a
            // virtual method call each iteration of the loop,
            // via EqualityComparer<T>.Default.Equals, we
            // only make one virtual call to EqualityComparer.LastIndexOf.

            return _size != 0 && Array.LastIndexOf(_array, item, _size - 1) != -1;
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

            CopyTo(dest.AsSpan(), destIndex, count);
        }

        // Returns an IEnumerator for this Stack.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <internalonly/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void TrimExcess()
        {
            if (_size == 0)
            {
                ReturnArray(replaceWith: s_emptyArray);
                _version++;
                return;
            }

            int threshold = (int)(_array.Length * 0.9);
            if (_size < threshold)
            {
                var newArray = _pool.Rent(_size);
                if (newArray.Length < _array.Length)
                {
                    Array.Copy(_array, newArray, _size);
                    ReturnArray(replaceWith: newArray);
                    _version++;
                }
                else
                {
                    // The array from the pool wasn't any smaller than the one we already had,
                    // (we can only control minimum size) so return it and do nothing.
                    // If we create an exact-sized array not from the pool, we'll
                    // get an exception when returning it to the pool.
                    _pool.Return(newArray);
                }
            }
        }

        // Returns the top object on the stack without removing it.  If the stack
        // is empty, Peek throws an InvalidOperationException.
        public T Peek()
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                ThrowForEmptyStack();
            }

            return array[size];
        }

        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default!;
                return false;
            }
            result = array[size];
            return true;
        }

        // Pops an item from the top of the stack.  If the stack is empty, Pop
        // throws an InvalidOperationException.
        public T Pop()
        {
            int size = _size - 1;
            T[] array = _array;

            // if (_size == 0) is equivalent to if (size == -1), and this case
            // is covered with (uint)size, thus allowing bounds check elimination
            // https://github.com/dotnet/coreclr/pull/9773
            if ((uint)size >= (uint)array.Length)
            {
                ThrowForEmptyStack();
            }

            _version++;
            _size = size;
            T item = array[size];
            if (s_clearArray)
            {
                array[size] = default!;     // Free memory quicker.
            }
            return item;
        }

        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default!;
                return false;
            }

            _version++;
            _size = size;
            result = array[size];
            if (s_clearArray)
            {
                array[size] = default!;
            }
            return true;
        }

        // Pushes an item to the top of the stack.
        public void Push(T item)
        {
            int size = _size;
            T[] array = _array;

            if ((uint)size < (uint)array.Length)
            {
                array[size] = item;
                _version++;
                _size = size + 1;
            }
            else
            {
                PushWithResize(item);
            }
        }

        // Non-inline from Stack.Push to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushWithResize(T item)
        {
            SystemDebug.Assert(_size == _array.Length);
            Grow(_size + 1);
            _array[_size] = item;
            _version++;
            _size++;
        }

        /// <summary>
        /// Ensures that the capacity of this Stack is at least the specified <paramref name="capacity"/>.
        /// If the current capacity of the Stack is less than specified <paramref name="capacity"/>,
        /// the capacity is increased by continuously twice current capacity until it is at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        /// <returns>The new capacity of this stack.</returns>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowCapacityArgumentOutOfRange_NeedNonNegNumException();
            }

            if (_array.Length < capacity)
            {
                Grow(capacity);
                _version++;
            }

            return _array.Length;
        }

        private void Grow(int capacity)
        {
            SystemDebug.Assert(_array.Length < capacity);

            int newCapacity = _array.Length == 0 ? DefaultCapacity : 2 * _array.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast.
            if ((uint)newCapacity > SystemArray.MaxLength) newCapacity = SystemArray.MaxLength;

            // If computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newCapacity < capacity) newCapacity = capacity;

            var newArray = _pool.Rent(newCapacity);
            Array.Copy(_array, newArray, _size);
            _pool.Return(_array);
            _array = newArray;
        }

        // Copies the Stack to an array, in the same order Pop would return the items.
        public T[] ToArray()
        {
            if (_size == 0)
                return s_emptyArray;

            T[] objArray = new T[_size];
            int i = 0;
            while (i < _size)
            {
                objArray[i] = _array[_size - i - 1];
                i++;
            }
            return objArray;
        }

        private void ReturnArray(T[] replaceWith = null)
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

        private void ThrowForEmptyStack()
        {
            SystemDebug.Assert(_size == 0);
            ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EmptyStack();
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            // We can't serialize array pools, so deserialized PooledQueue will
            // have to use the shared pool, even if they were using a custom pool
            // before serialization.
            _pool = ArrayPool<T>.Shared;
        }

        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private readonly ValueStack<T> _stack;
            private readonly int _version;
            private int _index;
            private T? _currentElement;

            public Enumerator(in ValueStack<T> stack)
            {
                _stack = stack;
                _version = stack._version;
                _index = -2;
                _currentElement = default;
            }

            public void Dispose()
            {
                _index = -1;
            }

            public bool MoveNext()
            {
                bool retval;
                if (_version != _stack._version)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                if (_index == -2)
                {  // First call to enumerator.
                    _index = _stack._size - 1;
                    retval = (_index >= 0);
                    if (retval)
                        _currentElement = _stack._array[_index];
                    return retval;
                }
                if (_index == -1)
                {  // End of enumeration.
                    return false;
                }

                retval = (--_index >= 0);
                if (retval)
                    _currentElement = _stack._array[_index];
                else
                    _currentElement = default;
                return retval;
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
                SystemDebug.Assert(_index == -1 || _index == -2);

                if (_index == -2)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                else
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
            }

            object? System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                if (_version != _stack._version)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                _index = -2;
                _currentElement = default;
            }
        }
    }
}