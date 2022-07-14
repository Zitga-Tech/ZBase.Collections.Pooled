using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly ref struct TempArrayInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Length;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly Span<T> Array;

        public TempArrayInternalsRefUnsafe(in TempArray<T> source)
        {
            Length = source._length;
            ClearArray = TempArray<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class TempCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempArrayInternalsRefUnsafe<T> GetRef<T>(
                in TempArray<T> source
            )
            => new TempArrayInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in TempArray<T> source
            )
            => source._array.AsSpan(0, source._length);
    }
}
