using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly ref struct StackInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly Span<T> Array;

        public StackInternalsRefUnsafe(Stack<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearArray = Stack<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class CollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StackInternalsRefUnsafe<T> GetRef<T>(
                Stack<T> source
            )
            => new StackInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                Stack<T> source
            )
            => source._array.AsSpan(0, source._size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<T>(
                this Stack<T> source
                , out T[] array
                , out int count
            )
        {
            array = source._array;
            count = source._size;
        }
    }
}
