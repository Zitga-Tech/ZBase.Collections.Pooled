using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals
{
    public readonly ref struct ValueListInternalsRef<T>
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearItems;
        [NonSerialized] public readonly ReadOnlySpan<T> Items;

        public ValueListInternalsRef(in ValueList<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearItems = ValueList<T>.s_clearItems;
            Items = source._items;
        }
    }

    public static partial class CollectionInternals
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueListInternalsRef<T> GetRef<T>(
                ValueList<T> source
            )
            => new ValueListInternalsRef<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(
                ValueList<T> source
            )
            => source._items.AsSpan(0, source.Count);
    }
}
