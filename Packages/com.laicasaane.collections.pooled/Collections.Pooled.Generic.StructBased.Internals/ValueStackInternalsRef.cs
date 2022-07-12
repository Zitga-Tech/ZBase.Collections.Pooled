using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ValueStackInternalsRef<T>
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly ReadOnlySpan<T> Array;

        public ValueStackInternalsRef(in ValueStack<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearArray = ValueStack<T>.s_clearArray;
            Array = source._array;
        }
    }

    public static partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueStackInternalsRef<T> GetRef<T>(
                ValueStack<T> source
            )
            => new ValueStackInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                ValueStack<T> source
            )
            => source._array.AsSpan(0, source.Count);
    }
}
