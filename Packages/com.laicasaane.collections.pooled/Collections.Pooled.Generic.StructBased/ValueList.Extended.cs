using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    partial struct ValueList<T> : IDisposable
    {
        public ValueList(T[] items) : this(items.AsSpan(), ArrayPool<T>.Shared)
        { }

        public ValueList(T[] items, ArrayPool<T> pool) : this(items.AsSpan(), pool)
        { }

        public ValueList(in ReadOnlySpan<T> span) : this(span, ArrayPool<T>.Shared)
        { }

        public ValueList(in ReadOnlySpan<T> span, ArrayPool<T> pool)
        {
            _pool = pool ?? ArrayPool<T>.Shared;

            int count = span.Length;

            if (count == 0)
            {
                _items = s_emptyArray;
                _size = 0;
            }
            else
            {
                _items = _pool.Rent(count);
                span.CopyTo(_items);
                _size = count;
            }

            _version = 0;
        }

        /// <summary>
        /// Advances the <see cref="Count"/> by the number of items specified,
        /// increasing the capacity if required, then returns a <see cref="Span{T}"/> representing
        /// the set of items to be added, allowing direct writes to that section
        /// of the collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<T> GetInsertSpan(int index, int count)
            => GetInsertSpan(index, count, true);

        internal Span<T> GetInsertSpan(int index, int count, bool clearSpan)
        {
            EnsureCapacity(_size + count);

            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + count, _size - index);
            }

            _size += count;
            _version++;

            var output = _items.AsSpan(index, count);

            if (clearSpan && s_clearItems)
            {
                output.Clear();
            }

            return output;
        }

        public void InsertRange(int index, T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            InsertRange(index, array.AsSpan());
        }

        public void InsertRange(int index, in ReadOnlySpan<T> span)
        {
            if ((uint)index > (uint)_size)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            var newSpan = GetInsertSpan(index, span.Length, false);
            span.CopyTo(newSpan);
        }

        /// <summary>
        /// Adds the elements of the given array to the end of this list. If
        /// required, the capacity of the list is increased to twice the previous
        /// capacity or the new size, whichever is larger.
        /// </summary>
        public void AddRange(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            AddRange(array.AsSpan());
        }

        /// <summary>
        /// Adds the elements of the given <see cref="ReadOnlySpan{T}"/> to the end of this list. If
        /// required, the capacity of the list is increased to twice the previous
        /// capacity or the new size, whichever is larger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(in ReadOnlySpan<T> span)
            => span.CopyTo(GetInsertSpan(_size, span.Length, false));

        /// <summary>
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest)
            => CopyTo(0, dest, 0, _size);

        /// <summary>
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest, int destIndex)
            => CopyTo(0, dest, destIndex, _size);

        /// <summary>
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest, int destIndex, int count)
            => CopyTo(0, dest, destIndex, count);

        public void CopyTo(int index, in Span<T> dest, int destIndex, int count)
        {
            if (destIndex < 0 || destIndex > dest.Length)
            {
                ThrowHelper.ThrowDestIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (count < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            }

            if (dest.Length - destIndex < count || _size - index < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            Span<T> src = _items.AsSpan(0, _size);

            if (src.Length == 0)
                return;

            src.Slice(index, count).CopyTo(dest.Slice(destIndex, count));
        }

        public void ConvertAll<TOut>(ValueList<TOut> output, Converter<T, TOut> converter)
        {
            if (converter == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);

            for (int i = 0; i < _size; i++)
            {
                output.Add(converter(_items[i]));
            }
        }

        public void FindAll(ValueList<T> output, Predicate<T> match)
        {
            if (match == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);

            for (int i = 0; i < _size; i++)
            {
                if (match(_items[i]))
                {
                    output.Add(_items[i]);
                }
            }
        }

        public bool TryFind(Predicate<T> match, out T result)
        {
            if (match == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);

            for (int i = 0; i < _size; i++)
            {
                if (match(_items[i]))
                {
                    result = _items[i];
                    return true;
                }
            }

            result = default;
            return false;
        }

        public bool TryFindLast(Predicate<T> match, out T result)
        {
            if (match is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);

            for (int i = _size - 1; i >= 0; i--)
            {
                if (match(_items[i]))
                {
                    result = _items[i];
                    return true;
                }
            }

            result = default;
            return false;
        }

        public void Dispose()
        {
            ReturnArray(s_emptyArray);
            _size = 0;
            _version++;
        }

    }
}
