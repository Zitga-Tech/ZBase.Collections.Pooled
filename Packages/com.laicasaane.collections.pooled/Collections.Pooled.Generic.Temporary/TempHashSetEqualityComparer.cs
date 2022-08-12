// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/HashSetEqualityComparer.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS8632
#pragma warning disable CS0184

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Collections.Pooled.Generic
{
    /// <summary>Equality comparer for hashsets of hashsets</summary>
    public readonly ref struct TempHashSetEqualityComparer<T>
    {
        public bool Equals(TempHashSet<T> x, TempHashSet<T> y)
        {
            // If they're the exact same instance, they're equal.
            if (ReferenceEquals(x._buckets, y._buckets))
            {
                return true;
            }

            // They're not both null, so if either is null, they're not equal.
            if (x._buckets?.Length < 1 || y._buckets?.Length < 1)
            {
                return false;
            }

            EqualityComparer<T> defaultComparer = EqualityComparer<T>.Default;

            // If both sets use the same comparer, they're equal if they're the same
            // size and one is a "subset" of the other.
            if (TempHashSet<T>.EqualityComparersAreEqual(x, y))
            {
                return x.Count == y.Count && y.IsSubsetOfHashSetWithSameComparer(x);
            }

            // Otherwise, do an O(N^2) match.
            foreach (T yi in y)
            {
                bool found = false;
                foreach (T xi in x)
                {
                    if (defaultComparer.Equals(yi, xi))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(TempHashSet<T> obj)
        {
            int hashCode = 0; // default to 0 for null/empty set

            if (obj._buckets?.Length > 0)
            {
                foreach (T t in obj)
                {
                    if (t != null)
                    {
                        hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                    }
                }
            }

            return hashCode;
        }

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is TempHashSetEqualityComparer<T>;

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode();
    }
}