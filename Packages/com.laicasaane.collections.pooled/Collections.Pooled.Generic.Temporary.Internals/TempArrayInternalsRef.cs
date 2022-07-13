using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct TempArrayInternalsRef<T>
    {
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly ReadOnlySpan<T> Array;

        public TempArrayInternalsRef(in TempArray<T> source)
        {
            ClearArray = TempArray<T>.s_clearArray;
            Array = source._array;
        }
    }

    public static partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArrayInternalsRef<T> GetRef<T>(
                TempArray<T> source
            )
            => new TempArrayInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                TempArray<T> source
            )
            => source._array.AsSpan();
    }
}
