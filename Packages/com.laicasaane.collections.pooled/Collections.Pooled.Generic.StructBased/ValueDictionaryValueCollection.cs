// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS8632

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Collections.Pooled.Generic
{
    [DebuggerTypeProxy(typeof(DictionaryValueCollectionDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct ValueDictionaryValueCollection<TKey, TValue> : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly ValueDictionary<TKey, TValue> _dictionary;

        public ValueDictionaryValueCollection(ValueDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public Enumerator GetEnumerator() => new Enumerator(_dictionary);

        public void CopyTo(TValue[] array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if ((uint)index > array.Length)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (array.Length - index < _dictionary.Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            int count = _dictionary._count;
            Entry<TKey, TValue>[]? entries = _dictionary._entries;
            for (int i = 0; i < count; i++)
            {
                if (entries![i].Next >= -1) array[index++] = entries[i].Value;
            }
        }

        public int Count => _dictionary.Count;

        bool ICollection<TValue>.IsReadOnly => true;

        void ICollection<TValue>.Add(TValue item) =>
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);

        bool ICollection<TValue>.Remove(TValue item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
            return false;
        }

        void ICollection<TValue>.Clear() =>
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);

        bool ICollection<TValue>.Contains(TValue item) => _dictionary.ContainsValue(item);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new Enumerator(_dictionary);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_dictionary);

        public struct Enumerator : IEnumerator<TValue>, IEnumerator
        {
            private readonly ValueDictionary<TKey, TValue> _dictionary;
            private int _index;
            private readonly int _version;
            private TValue? _currentValue;

            public Enumerator(in ValueDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _currentValue = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                while ((uint)_index < (uint)_dictionary._count)
                {
                    ref Entry<TKey, TValue> entry = ref _dictionary._entries![_index++];

                    if (entry.Next >= -1)
                    {
                        _currentValue = entry.Value;
                        return true;
                    }
                }
                _index = _dictionary._count + 1;
                _currentValue = default;
                return false;
            }

            public TValue Current => _currentValue!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return _currentValue;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _dictionary._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _index = 0;
                _currentValue = default;
            }
        }
    }
}