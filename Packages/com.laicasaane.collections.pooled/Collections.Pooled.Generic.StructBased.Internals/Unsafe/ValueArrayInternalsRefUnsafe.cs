using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly ref struct ValueArrayInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Length;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly Span<T> Array;

        public ValueArrayInternalsRefUnsafe(in ValueArray<T> source)
        {
            Length = source._length;
            ClearArray = ValueArray<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class ValueCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayInternalsRefUnsafe<T> GetRef<T>(
                in ValueArray<T> source
            )
            => new ValueArrayInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in ValueArray<T> source
            )
            => source._array.AsSpan(0, source._length);
    }
}
