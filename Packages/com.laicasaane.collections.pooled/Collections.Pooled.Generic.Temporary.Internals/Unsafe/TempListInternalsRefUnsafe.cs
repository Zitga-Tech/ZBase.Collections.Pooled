using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly struct TempListInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearItems;
        [NonSerialized] public readonly T[] Items;

        public TempListInternalsRefUnsafe(in TempList<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearItems = TempList<T>.s_clearItems;
            Items = source._items;
        }
    }

    partial class TempCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TempListInternalsRefUnsafe<T> GetRef<T>(
                in TempList<T> source
            )
            => new TempListInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in TempList<T> source
            )
            => source._items.AsSpan(0, source._size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUnsafe<T>(
                in this TempList<T> source
                , out T[] items
                , out int count
            )
        {
            items = source._items;
            count = source._size;
        }

        /// <summary>
        /// Advances the <see cref="Count"/> by the number of items specified,
        /// increasing the capacity if required, then returns a <see cref="Span{T}"/> representing
        /// the set of items to be added, allowing direct writes to that section
        /// of the collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> GetInsertSpan<T>(
                ref TempList<T> source, int index, int count
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
                ref TempList<T> source, int index, int count, bool clearSpan
            )
            => source.GetInsertSpan(index, count, clearSpan);
    }
}
