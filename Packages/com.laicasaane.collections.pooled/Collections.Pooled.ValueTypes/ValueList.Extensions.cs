using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.ValueTypes
{
    partial struct ValueList<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Extensions GetExtensions()
            => new Extensions(this);

        public ref struct Extensions
        {
            private ValueList<T> _list;

            internal Extensions(ValueList<T> list)
            {
                _list = list;
            }

            public void ConvertAll<TOut, TOutput>(TOutput output, Converter<T, TOut> converter)
                where TOutput : ICollection<TOut>
            {
                if (converter == null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);

                if (output == null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.output);

                for (int i = 0; i < _list._size; i++)
                {
                    output.Add(converter(_list._items[i]));
                }
            }

            public void FindAll<TOutput>(TOutput output, Predicate<T> match)
                where TOutput : ICollection<T>
            {
                if (match == null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);

                if (output == null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.output);

                for (int i = 0; i < _list._size; i++)
                {
                    if (match(_list._items[i]))
                    {
                        output.Add(_list._items[i]);
                    }
                }
            }

            public ValueList<TOut> ConvertAll<TOut, TConverter>(TConverter converter) where TConverter : IConverter<T, TOut>
            {
                if (converter == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);
                }

                ValueList<TOut> list = new ValueList<TOut>(_list._size);
                for (int i = 0; i < _list._size; i++)
                {
                    list._items[i] = converter.Convert(_list._items[i]);
                }
                list._size = _list._size;
                return list;
            }

            public bool Exists<TPredicate>(TPredicate match) where TPredicate : IPredicate<T>
                => FindIndex(match) != -1;

            public T Find<TPredicate>(TPredicate match) where TPredicate : IPredicate<T>
            {
                if (match == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
                }

                for (int i = 0; i < _list._size; i++)
                {
                    if (match.Predicate(_list._items[i]))
                    {
                        return _list._items[i];
                    }
                }
                return default;
            }

            public ValueList<T> FindAll<TPredicate>(TPredicate match) where TPredicate : IPredicate<T>
            {
                if (match == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
                }

                ValueList<T> list = new ValueList<T>();
                for (int i = 0; i < _list._size; i++)
                {
                    if (match.Predicate(_list._items[i]))
                    {
                        list.Add(_list._items[i]);
                    }
                }
                return list;
            }

            public int FindIndex<TPredicate>(TPredicate match) where TPredicate : IPredicate<T>
                => FindIndex(0, _list._size, match);

            public int FindIndex<TPredicate>(int startIndex, TPredicate match) where TPredicate : IPredicate<T>
                => FindIndex(startIndex, _list._size - startIndex, match);

            public int FindIndex<TPredicate>(int startIndex, int count, TPredicate match) where TPredicate : IPredicate<T>
            {
                if ((uint)startIndex > (uint)_list._size)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
                }

                if (count < 0 || startIndex > _list._size - count)
                {
                    ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
                }

                if (match == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
                }

                int endIndex = startIndex + count;
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (match.Predicate(_list._items[i])) return i;
                }
                return -1;
            }

            public T FindLast<TPredicate>(TPredicate match) where TPredicate : IPredicate<T>
            {
                if (match == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
                }

                for (int i = _list._size - 1; i >= 0; i--)
                {
                    if (match.Predicate(_list._items[i]))
                    {
                        return _list._items[i];
                    }
                }
                return default;
            }

            public int FindLastIndex<TPredicate>(TPredicate match) where TPredicate : IPredicate<T>
                => FindLastIndex(_list._size - 1, _list._size, match);

            public int FindLastIndex<TPredicate>(int startIndex, TPredicate match) where TPredicate : IPredicate<T>
                => FindLastIndex(startIndex, startIndex + 1, match);

            public int FindLastIndex<TPredicate>(int startIndex, int count, TPredicate match) where TPredicate : IPredicate<T>
            {
                if (match == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
                }

                if (_list._size == 0)
                {
                    // Special case for 0 length List
                    if (startIndex != -1)
                    {
                        ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                    }
                }
                else
                {
                    // Make sure we're not out of range
                    if ((uint)startIndex >= (uint)_list._size)
                    {
                        ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                    }
                }

                // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
                if (count < 0 || startIndex - count + 1 < 0)
                {
                    ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
                }

                int endIndex = startIndex - count;
                for (int i = startIndex; i > endIndex; i--)
                {
                    if (match.Predicate(_list._items[i]))
                    {
                        return i;
                    }
                }
                return -1;
            }

            public void ForEach<TAction>(TAction action) where TAction : IAction<T>
            {
                if (action == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.action);
                }

                int version = _list._version;

                for (int i = 0; i < _list._size; i++)
                {
                    if (version != _list._version)
                    {
                        break;
                    }
                    action.Action(_list._items[i]);
                }

                if (version != _list._version)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
            }

            public bool TryFind<TPredicate>(TPredicate match, out T result) where TPredicate : IPredicate<T>
            {
                if (match == null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);

                for (int i = 0; i < _list._size; i++)
                {
                    if (match.Predicate(_list._items[i]))
                    {
                        result = _list._items[i];
                        return true;
                    }
                }

                result = default;
                return false;
            }

            public bool TryFindLast<TPredicate>(TPredicate match, out T result) where TPredicate : IPredicate<T>
            {
                if (match is null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);

                for (int i = _list._size - 1; i >= 0; i--)
                {
                    if (match.Predicate(_list._items[i]))
                    {
                        result = _list._items[i];
                        return true;
                    }
                }

                result = default;
                return false;
            }

            public int RemoveAll<TPredicate>(TPredicate match) where TPredicate : IPredicate<T>
            {
                if (match == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
                }

                int freeIndex = 0;   // the first free slot in items array

                // Find the first item which needs to be removed.
                while (freeIndex < _list._size && !match.Predicate(_list._items[freeIndex])) freeIndex++;
                if (freeIndex >= _list._size) return 0;

                int current = freeIndex + 1;
                while (current < _list._size)
                {
                    // Find the first item which needs to be kept.
                    while (current < _list._size && match.Predicate(_list._items[current])) current++;

                    if (current < _list._size)
                    {
                        // copy item to the free slot.
                        _list._items[freeIndex++] = _list._items[current++];
                    }
                }

                if (ValueList<T>.s_clearItems)
                {
                    Array.Clear(_list._items, freeIndex, _list._size - freeIndex); // Clear the elements so that the gc can reclaim the references.
                }

                int result = _list._size - freeIndex;
                _list._size = freeIndex;
                _list._version++;
                return result;
            }

            public void Sort<TComparison>(TComparison comparison) where TComparison : IComparison<T>
            {
                if (comparison == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
                }

                if (_list._size > 1)
                {
                    Array.Sort(_list._items, 0, _list._size, new Comparer<TComparison>(comparison));
                }
                _list._version++;
            }

            public bool TrueForAll<TPredicate>(TPredicate match) where TPredicate : IPredicate<T>
            {
                if (match == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
                }

                for (int i = 0; i < _list._size; i++)
                {
                    if (!match.Predicate(_list._items[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            private readonly struct Comparer<TComparison> : IComparer<T>
                where TComparison : IComparison<T>
            {
                private readonly TComparison _comparison;

                public Comparer(TComparison comparison)
                {
                    _comparison = comparison;
                }

                public int Compare(T x, T y) => _comparison.Compare(x, y);
            }
        }
    }
}
