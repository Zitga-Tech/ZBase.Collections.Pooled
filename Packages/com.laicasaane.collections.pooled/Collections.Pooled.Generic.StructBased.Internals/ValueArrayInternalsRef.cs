using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ValueArrayInternalsRef<T>
    {
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly ReadOnlySpan<T> Array;

        public ValueArrayInternalsRef(in ValueArray<T> source)
        {
            ClearArray = ValueArray<T>.s_clearArray;
            Array = source._array;
        }
    }

    public static partial class ValueCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueArrayInternalsRef<T> GetRef<T>(
                ValueArray<T> source
            )
            => new ValueArrayInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                ValueArray<T> source
            )
            => source._array.AsSpan();
    }
}
