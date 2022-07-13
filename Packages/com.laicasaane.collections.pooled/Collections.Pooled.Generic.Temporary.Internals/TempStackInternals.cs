using System;
using System.Buffers;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct TempStackInternals<T> : IDisposable
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        public TempStackInternals(in TempStack<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearArray = TempStack<T>.s_clearArray;
            Array = source._array;
            Pool = source._pool;
        }

        public void Dispose()
        {
            if (Array != null && Array.Length > 0)
            {
                try
                {
                    Pool?.Return(Array, ClearArray);
                }
                catch { }
            }
        }
    }

    public static partial class TempCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static TempStackInternals<T> TakeOwnership<T>(
                TempStack<T> source
            )
        {
            var internals = new TempStackInternals<T>(source);

            source._array = null;
            source.Dispose();

            return internals;
        }
    }
}
