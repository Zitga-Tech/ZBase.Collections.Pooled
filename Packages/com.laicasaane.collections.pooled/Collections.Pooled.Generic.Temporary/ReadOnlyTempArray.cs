using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public readonly ref struct ReadOnlyTempArray<T>
    {
        internal readonly TempArray<T> _array;

        internal ReadOnlyTempArray(in TempArray<T> array)
        {
            _array = array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyTempArray<T> Empty()
            => new ReadOnlyTempArray<T>(TempArray<T>.Empty());

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
        public void CopyTo(ref TempArray<T> dest)
            => CopyTo(0, ref dest, 0, _array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(ref TempArray<T> dest, int destIndex)
            => CopyTo(0, ref dest, destIndex, _array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(ref TempArray<T> dest, int destIndex, int count)
            => CopyTo(0, ref dest, destIndex, count);

        public void CopyTo(int index, ref TempArray<T> dest, int destIndex, int count)
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
        public TempArray<T>.Enumerator GetEnumerator()
            => _array.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
            => _array.Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyTempArray<T>(in TempArray<T> array)
            => new ReadOnlyTempArray<T>(array);
    }
}
