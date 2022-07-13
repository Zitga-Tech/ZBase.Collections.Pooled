using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct ValueArrayInternals<T> : IDisposable
    {
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        public ValueArrayInternals(in ValueArray<T> source)
        {
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

    public static partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static ValueArrayInternals<T> TakeOwnership<T>(
                ValueArray<T> source
            )
        {
            var internals = new ValueArrayInternals<T>(source);

            source._array = null;
            source.Dispose();

            return internals;
        }

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                ValueArray<T> source
            )
            => source._array.AsSpan();
    }
}
