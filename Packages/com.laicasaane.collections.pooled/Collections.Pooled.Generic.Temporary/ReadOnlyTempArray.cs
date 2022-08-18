using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public ref struct ReadOnlyTempArray<T>
    {
        private TempArray<T> _array;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int index)
            => _array.CopyTo(array, index);

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
