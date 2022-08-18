using System;
using System.Buffers;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct ValueQueueInternals<T> : IDisposable
    {
        [NonSerialized] public readonly int Head;
        [NonSerialized] public readonly int Tail;
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        public ValueQueueInternals(in ValueQueue<T> source)
        {
            Head = source._head;
            Tail = source._tail;
            Size = source._size;
            Version = source._version;
            ClearArray = ValueQueue<T>.s_clearArray;
            Array = source._array;
            Pool = source._pool;
        }

        public void Dispose()
        {
            if (Array.IsNullOrEmpty() == false)
            {
                try
                {
                    Pool?.Return(Array, ClearArray);
                }
                catch { }
            }
        }
    }

    partial class ValueCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static ValueQueueInternals<T> TakeOwnership<T>(
                ref ValueQueue<T> source
            )
        {
            var internals = new ValueQueueInternals<T>(source);

            source._array = null;
            source.Dispose();

            return internals;
        }
    }
}
