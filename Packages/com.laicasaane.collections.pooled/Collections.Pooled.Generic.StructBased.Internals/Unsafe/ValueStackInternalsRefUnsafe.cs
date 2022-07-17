﻿using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Internals.Unsafe
{
    public readonly ref struct ValueStackInternalsRefUnsafe<T>
    {
        [NonSerialized] public readonly int Size;
        [NonSerialized] public readonly int Version;
        [NonSerialized] public readonly bool ClearArray;
        [NonSerialized] public readonly Span<T> Array;

        public ValueStackInternalsRefUnsafe(in ValueStack<T> source)
        {
            Size = source._size;
            Version = source._version;
            ClearArray = ValueStack<T>.s_clearArray;
            Array = source._array;
        }
    }

    partial class ValueCollectionInternalsUnsafe
    {
        /// <summary>
        /// Returns a structure that holds references to internal fields of <paramref name="source"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueStackInternalsRefUnsafe<T> GetRef<T>(
                in ValueStack<T> source
            )
            => new ValueStackInternalsRefUnsafe<T>(source);

        /// <summary>
        /// Returns the internal array as a <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(
                in ValueStack<T> source
            )
            => source._array.AsSpan(0, source._size);
    }
}