using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ListInternalsRef<T>
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearItems;
        [NonSerialized] public readonly ReadOnlySpan<T> Items;

        public ListInternalsRef(List<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearItems = List<T>.s_clearItems;
            Items = source._items;
        }
    }

    partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ListInternalsRef<T> GetRef<T>(
                List<T> source
            )
            => new ListInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                this List<T> source
            )
            => source._items.AsSpan(0, source._size);
    }
}
