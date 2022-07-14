using System;
using System.Buffers;

namespace Collections.Pooled.Generic.Internals
{
    public readonly struct ValueListInternals<T> : IDisposable
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearItems;
        [NonSerialized] public readonly T[] Items;
        [NonSerialized] public readonly ArrayPool<T> Pool;

        public ValueListInternals(in ValueList<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearItems = ValueList<T>.s_clearItems;
            Items = source._items;
            Pool = source._pool;
        }

        public void Dispose()
        {
            if (Items != null && Items.Length > 0)
            {
                try
                {
                    Pool?.Return(Items, ClearItems);
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
        public static ValueListInternals<T> TakeOwnership<T>(
                ref ValueList<T> source
            )
        {
            var internals = new ValueListInternals<T>(source);

            source._items = null;
            source.Dispose();

            return internals;
        }
    }
}
