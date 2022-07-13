using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct TempArrayInternals<T> : IDisposable
    {
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly T[] Array;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        public TempArrayInternals(in TempArray<T> source)
        {
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

    public static partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static TempArrayInternals<T> TakeOwnership<T>(
                TempArray<T> source
            )
        {
            var internals = new TempArrayInternals<T>(source);

            source._array = null;
            source.Dispose();

            return internals;
        }

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                TempArray<T> source
            )
            => source._array.AsSpan();
    }
}
