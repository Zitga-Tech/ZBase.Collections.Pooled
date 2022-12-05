using System;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct TempStackInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;

        internal TempStackInternalsRefUnsafe(in TempStack<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearArray = TempStack<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class TempCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempStackInternalsRefUnsafe<T> GetRef<T>(
                in TempStack<T> source
            )
            => new TempStackInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in this TempStack<T> source
            )
            => source._array.AsSpan(0, source._size);

        /// <summary>
        /// Returns the internal array as a <see cref="Memory{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> AsMemory<T>(
                in this TempStack<T> source
            )
            => source._array.AsMemory(0, source._size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<T>(
                in this TempStack<T> source
                , out T[] array
                , out int count
            )
        {
            array = source._array;
            count = source._size;
        }
    }
}
