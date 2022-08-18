// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS8632

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Collections.Pooled.Generic
{
    [DebuggerTypeProxy(typeof(DictionaryKeyCollectionDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct ValueDictionaryKeyCollection<TKey, TValue> : ICollection<TKey>, IReadOnlyCollection<TKey>
    {
        private readonly ValueDictionary<TKey, TValue> _dictionary;

        public ValueDictionaryKeyCollection(ValueDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public Enumerator GetEnumerator() => new Enumerator(_dictionary);

        public void CopyTo(TKey[] array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (index < 0 || index > array.Length)
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
                if (entries![i].Next >= -1) array[index++] = entries[i].Key;
            }
        }

        public int Count => _dictionary.Count;

        bool ICollection<TKey>.IsReadOnly => true;

        void ICollection<TKey>.Add(TKey item) =>
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);

        void ICollection<TKey>.Clear() =>
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);

        bool ICollection<TKey>.Contains(TKey item) =>
            _dictionary.ContainsKey(item);

        bool ICollection<TKey>.Remove(TKey item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
            return false;
        }

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => new Enumerator(_dictionary);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_dictionary);

        public struct Enumerator : IEnumerator<TKey>, IEnumerator
        {
            private readonly ValueDictionary<TKey, TValue> _dictionary;
            private int _index;
            private readonly int _version;
            private TKey? _currentKey;

            public Enumerator(in ValueDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _currentKey = default;
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
                        _currentKey = entry.Key;
                        return true;
                    }
                }

                _index = _dictionary._count + 1;
                _currentKey = default;
                return false;
            }

            public TKey Current => _currentKey!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return _currentKey;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _dictionary._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _index = 0;
                _currentKey = default;
            }
        }
    }
}