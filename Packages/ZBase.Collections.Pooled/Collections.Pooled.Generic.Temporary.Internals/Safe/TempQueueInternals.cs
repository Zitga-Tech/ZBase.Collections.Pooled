using System;
using System.Buffers;

namespace ZBase.Collections.Pooled.Generic.Internals
{
    public readonly struct TempQueueInternals<T> : IDisposable
    {
        [NonSerialized] public readonly int Head;
        [NonSerialized] public readonly int Tail;
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        internal TempQueueInternals(in TempQueue<T> source)
        {
            Head = source._head;
            Tail = source._tail;
            Size = source._size;
            Version = source._version;
            ClearArray = TempQueue<T>.s_clearArray;
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

    partial class TempCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static TempQueueInternals<T> TakeOwnership<T>(
                ref TempQueue<T> source
            )
        {
            var internals = new TempQueueInternals<T>(source);

            source._array = null;
            source.Dispose();

            return internals;
        }
    }
}
