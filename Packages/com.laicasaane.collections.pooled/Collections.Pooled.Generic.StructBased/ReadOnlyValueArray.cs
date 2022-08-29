using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public readonly struct ReadOnlyValueArray<T> : IReadOnlyList<T>, IDisposable
    {
        internal readonly ValueArray<T> _array;

        internal ReadOnlyValueArray(in ValueArray<T> array)
        {
            _array = array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyValueArray<T> Empty()
            => new ReadOnlyValueArray<T>(ValueArray<T>.Empty());

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array[index];
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.Length;
        }

        /// <summary>
        /// Copies this List into array, which must be of a compatible array type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest)
            => CopyTo(0, dest, 0, _array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] dest, int destIndex)
            => CopyTo(0, dest, destIndex, _array.Length);

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
        public void CopyTo(ref ValueArray<T> dest)
            => CopyTo(0, ref dest, 0, _array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(ref ValueArray<T> dest, int destIndex)
            => CopyTo(0, ref dest, destIndex, _array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(ref ValueArray<T> dest, int destIndex, int count)
            => CopyTo(0, ref dest, destIndex, count);

        public void CopyTo(int index, ref ValueArray<T> dest, int destIndex, int count)
        {
            if (dest._array.IsNullOrEmpty())
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dest);

            CopyTo(index, dest._array.AsSpan(), destIndex, count);

            dest._length += count;
        }

        /// <summary>
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest)
            => CopyTo(0, dest, 0, _array.Length);

        /// <summary>
        /// Copies this List into the given span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest, int destIndex)
            => CopyTo(0, dest, destIndex, _array.Length);

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

            if (dest.Length - destIndex < count || _array.Length - index < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            Span<T> src = _array._array.AsSpan(0, _array._length);

            if (src.Length == 0)
                return;

            src.Slice(index, count).CopyTo(dest.Slice(destIndex, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueArray<T> .Enumerator GetEnumerator()
            => new ValueArray<T>.Enumerator(_array);

        int IReadOnlyCollection<T>.Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new ValueArray<T>.Enumerator(_array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new ValueArray<T>.Enumerator(_array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
            => _array.Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyValueArray<T>(in ValueArray<T> array)
            => new ReadOnlyValueArray<T>(array);
    }
}
