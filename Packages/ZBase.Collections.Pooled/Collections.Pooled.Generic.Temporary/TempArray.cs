using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic
{
    public ref partial struct TempArray<T>
    {
        internal static readonly bool s_clearArray = SystemRuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private static readonly T[] s_emptyArray = new T[0];

        internal T[] _array; // Do not rename (binary serialization)
        internal int _length; // Do not rename (binary serialization)

        [NonSerialized]
        internal ArrayPool<T> _pool;

        internal TempArray(int length, ArrayPool<T> pool)
        {
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            _length = length;
            _pool = pool ?? ArrayPool<T>.Shared;
            _array = _length == 0 ? s_emptyArray : pool.Rent(length);
        }

        internal TempArray(T[] array, int length, ArrayPool<T> pool)
        {
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            _array = array ?? s_emptyArray;
            _length = array == null ? 0 : length;
            _pool = pool ?? ArrayPool<T>.Shared;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.Length;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[index];
        }

        /// <summary>
        /// Copies this List into array, which must be of a compatible array type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest)
            => CopyTo(0, dest, 0, _length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest, int destIndex)
            => CopyTo(0, dest, destIndex, _length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest, int destIndex, int count)
            => CopyTo(0, dest, destIndex, count);

        public void CopyTo(int index, T[] dest, int destIndex, int count)
        {
            if (dest == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dest);

            CopyTo(index, dest.AsSpan(), destIndex, count);
        }

        /// <summary>
        /// Copies this List into array, which must be of a compatible array type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in TempArray<T> dest)
            => CopyTo(0, dest, 0, _array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in TempArray<T> dest, int destIndex)
            => CopyTo(0, dest, destIndex, _array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in TempArray<T> dest, int destIndex, int count)
            => CopyTo(0, dest, destIndex, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int index, in TempArray<T> dest, int destIndex, int count)
            => CopyTo(index, dest._array.AsSpan(), destIndex, count);

        /// <summary>
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest)
            => CopyTo(0, dest, 0, _length);

        /// <summary>
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest, int destIndex)
            => CopyTo(0, dest, destIndex, _length);

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

            if (dest.Length - destIndex < count || _length - index < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            Span<T> src = _array.AsSpan(0, _length);

            if (src.Length == 0)
                return;

            src.Slice(index, count).CopyTo(dest.Slice(destIndex, count));
        }

        private void ReturnArray(T[] replaceWith)
        {
            if (_array.IsNullOrEmpty() == false)
            {
                try
                {
                    _pool?.Return(_array, s_clearArray);
                }
                catch { }
            }

            _array = replaceWith ?? s_emptyArray;
        }

        public void Dispose()
        {
            ReturnArray(s_emptyArray);
            _length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(this);

        public ref struct Enumerator
        {
            private readonly TempArray<T> _array;
            private int _index;
            private T _current;

            public Enumerator(in TempArray<T> array)
            {
                _array = array;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if (((uint)_index < (uint)_array.Length))
                {
                    _current = _array._array[_index];
                    _index++;
                    return true;
                }

                _index = _array.Length + 1;
                _current = default;
                return false;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current!;
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }
        }
    }
}
