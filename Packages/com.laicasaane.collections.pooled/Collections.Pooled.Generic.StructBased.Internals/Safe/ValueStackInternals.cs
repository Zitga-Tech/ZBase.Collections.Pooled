using System;
using System.Buffers;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct ValueStackInternals<T> : IDisposable
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        public ValueStackInternals(in ValueStack<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearArray = ValueStack<T>.s_clearArray;
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
        public static ValueStackInternals<T> TakeOwnership<T>(
                ref ValueStack<T> source
            )
        {
            var internals = new ValueStackInternals<T>(source);

            source._array = null;
            source.Dispose();

            return internals;
        }
    }
}
