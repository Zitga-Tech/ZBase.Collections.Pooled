using System;
using System.Buffers;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct ValueArrayInternals<T> : IDisposable
    {
        [NonSerialized] public readonly int Length;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        public ValueArrayInternals(in ValueArray<T> source)
        {
            Length = source._length;
            ClearArray = ValueArray<T>.s_clearArray;
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

    partial class ValueCollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static ValueArrayInternals<T> TakeOwnership<T>(
                ref ValueArray<T> source
            )
        {
            var internals = new ValueArrayInternals<T>(source);

            source._array = null;
            source.Dispose();

            return internals;
        }
    }
}
