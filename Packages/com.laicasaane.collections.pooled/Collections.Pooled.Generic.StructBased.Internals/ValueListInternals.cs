using System;
using System.Buffers;
using System.Runtime.CompilerServices;

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

    public static partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds ownership of internal fields of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// Afterward <paramref name="source"/> will be disposed.
        /// </remarks>
        public static ValueListInternals<T> TakeOwnership<T>(
                ValueList<T> source
            )
        {
            var internals = new ValueListInternals<T>(source);

            source._items = null;
            source.Dispose();

            return internals;
        }

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                ValueList<T> source
            )
            => source._items.AsSpan(0, source._size);

        /// <summary>
        /// Advances the <see cref="Count"/> by the number of items specified,
        /// increasing the capacity if required, then returns a <see cref="Span{T}"/> representing
        /// the set of items to be added, allowing direct writes to that section
        /// of the collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> GetInsertSpan<T>(
                ValueList<T> source, int index, int count
            )
            => source.GetInsertSpan(index, count, true);

        /// <summary>
        /// Advances the <see cref="Count"/> by the number of items specified,
        /// increasing the capacity if required, then returns a <see cref="Span{T}"/> representing
        /// the set of items to be added, allowing direct writes to that section
        /// of the collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> GetInsertSpan<T>(
                ValueList<T> source, int index, int count, bool clearSpan
            )
            => source.GetInsertSpan(index, count, clearSpan);
    }
}
