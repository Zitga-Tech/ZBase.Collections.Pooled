using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic
{
    public readonly struct ReadOnlyArray<T> : IReadOnlyList<T>
    {
        private static readonly T[] s_emptyArray = new T[0];

        internal readonly T[] _array;

        internal ReadOnlyArray(T[] array)
        {
            _array = array ?? s_emptyArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyArray<T> Empty()
            => new ReadOnlyArray<T>(s_emptyArray);

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array[index];
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array?.Length ?? 0;
        }

        /// <summary>
        /// Copies this List into array, which must be of a compatible array type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest)
            => CopyTo(0, dest, 0, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest, int destIndex)
            => CopyTo(0, dest, destIndex, Length);

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
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest)
            => CopyTo(0, dest, 0, Length);

        /// <summary>
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest, int destIndex)
            => CopyTo(0, dest, destIndex, Length);

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

            Span<T> src = _array.AsSpan();

            if (dest.Length - destIndex < count || src.Length - index < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            if (src.Length == 0)
                return;

            src.Slice(index, count).CopyTo(dest.Slice(destIndex, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(_array);

        int IReadOnlyCollection<T>.Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array?.Length ?? 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new Enumerator(_array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(_array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyArray<T>(T[] array)
            => new ReadOnlyArray<T>(array);

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly T[] _array;
            private readonly int _length;
            private int _index;
            private T _current;

            public Enumerator(T[] array)
            {
                _array = array ?? s_emptyArray;
                _length = _array.Length;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if ((uint)_index < (uint)_length)
                {
                    _current = _array[_index];
                    _index++;
                    return true;
                }

                _index = _length + 1;
                _current = default;
                return false;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current!;
            }

            object IEnumerator.Current
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
