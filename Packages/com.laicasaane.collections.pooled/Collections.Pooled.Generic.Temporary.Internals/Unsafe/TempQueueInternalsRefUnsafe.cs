using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct TempQueueInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Head;
        [NonSerialized] public readonly int Tail;
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;

        internal TempQueueInternalsRefUnsafe(in TempQueue<T> source)
        {
            Head = source._head;
            Tail = source._tail;
            Size = source._size;
            Version = source._version;
            ClearArray = TempQueue<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class TempCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempQueueInternalsRefUnsafe<T> GetRef<T>(
                in TempQueue<T> source
            )
            => new TempQueueInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in TempQueue<T> source
                , out int size
                , out int head
                , out int tail
            )
        {
            size = source._size;
            head = source._head;
            tail = source._tail;
            return source._array.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<T>(
                in this TempQueue<T> source
                , out T[] array
                , out int count
                , out int head
                , out int tail
            )
        {
            array = source._array;
            count = source._size;
            head = source._head;
            tail = source._tail;
        }
    }
}
