using System;
using System.Runtime.CompilerServices;

namespace ZBase.Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct ValueQueueInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Head;
        [NonSerialized] public readonly int Tail;
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;

        public ValueQueueInternalsRefUnsafe(in ValueQueue<T> source)
        {
            Head = source._head;
            Tail = source._tail;
            Size = source._size;
            Version = source._version;
            ClearArray = ValueQueue<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class ValueCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueQueueInternalsRefUnsafe<T> GetRef<T>(
                in ValueQueue<T> source
            )
            => new ValueQueueInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in this ValueQueue<T> source
                , out int head
                , out int tail
            )
        {
            head = source._head;
            tail = source._tail;
            return source._array.AsSpan(0, source._size);
        }

        /// <summary>
        /// Returns the internal array as a <see cref="Memory{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> AsMemory<T>(
                in this ValueQueue<T> source
                , out int head
                , out int tail
            )
        {
            head = source._head;
            tail = source._tail;
            return source._array.AsMemory(0, source._size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<T>(
                in this ValueQueue<T> source
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
