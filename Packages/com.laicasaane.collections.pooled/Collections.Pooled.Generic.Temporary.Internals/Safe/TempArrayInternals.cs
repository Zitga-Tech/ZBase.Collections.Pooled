using System;
using System.Buffers;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct TempArrayInternals<T> : IDisposable
    {
        [NonSerialized] public readonly int Length;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        public TempArrayInternals(in TempArray<T> source)
        {
            Length = source._length;
            ClearArray = TempArray<T>.s_clearArray;
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

    partial class TempCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static TempArrayInternals<T> TakeOwnership<T>(
                ref TempArray<T> source
            )
        {
            var internals = new TempArrayInternals<T>(source);

            source._array = null;
            source.Dispose();

            return internals;
        }
    }
}
