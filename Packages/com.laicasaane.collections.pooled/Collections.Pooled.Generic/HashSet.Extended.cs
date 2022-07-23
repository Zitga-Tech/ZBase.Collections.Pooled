#pragma warning disable CS8632

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if UNITY_2021_3_OR_NEWER && !UNITY_EDITOR
using Unsafe = System.Runtime.CompilerServices.Unsafe;
#else
using Unsafe = Collections.Pooled.SystemUnsafe;
#endif

namespace Collections.Pooled.Generic
{
    partial class HashSet<T> : IDisposable
    {
        public HashSet(T[] items)
            : this(items.AsSpan(), null)
        { }

        public HashSet(T[] items, IEqualityComparer<T>? comparer)
            : this(items.AsSpan(), comparer, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared)
        { }

        public HashSet(T[] items, IEqualityComparer<T>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<T>> entryPool)
            : this(items.AsSpan(), comparer, bucketPool, entryPool)
        { }

        public HashSet(in ReadOnlySpan<T> span)
            : this(span, null)
        { }

        public HashSet(in ReadOnlySpan<T> span, IEqualityComparer<T>? comparer)
            : this(span, comparer, ArrayPool<int>.Shared, ArrayPool<Entry<T>>.Shared)
        { }

        public HashSet(in ReadOnlySpan<T> span, IEqualityComparer<T>? comparer, ArrayPool<int> bucketPool, ArrayPool<Entry<T>> entryPool)
            : this(comparer, bucketPool, entryPool)
        {
            Initialize(span.Length);
            UnionWith(span);

            if (_count > 0 && _entries!.Length / _count > ShrinkThreshold)
            {
                TrimExcess();
            }
        }

        internal ref T FindValue(T equalValue)
        {
            if (_buckets != null)
            {
                int index = FindItemIndex(equalValue);
                if (index >= 0)
                {
                    return ref _entries![index].Value;
                }
            }

            return ref Unsafe.NullRef<T>();
        }

        /// <summary>
        /// Take the union of this PooledSet with other. Modifies this set.
        /// </summary>
        /// <param name="other"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionWith(T[] other)
            => UnionWith((ReadOnlySpan<T>)other);


        /// <summary>
        /// Take the union of this PooledSet with other. Modifies this set.
        /// </summary>
        /// <param name="other">enumerable with items to add</param>
        public void UnionWith(in ReadOnlySpan<T> other)
        {
            for (int i = 0, len = other.Length; i < len; i++)
            {
                AddIfNotPresent(other[i], out _);
            }
        }

        /// <summary>
        /// Takes the intersection of this set with other. Modifies this set.
        /// </summary>
        /// <remarks>
        /// Implementation Notes: 
        /// Iterate over the other and mark intersection by checking
        /// contains in this. Then loop over and delete any unmarked elements. Total cost is n2+n1. 
        /// 
        /// Attempts to return early based on counts alone, using the property that the 
        /// intersection of anything with the empty set is the empty set.
        /// </remarks>
        /// <param name="other">enumerable with items to add </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntersectWith(T[] other)
            => IntersectWith((ReadOnlySpan<T>)other);

        /// <summary>
        /// Takes the intersection of this set with other. Modifies this set.
        /// </summary>
        /// <remarks>
        /// Implementation Notes: 
        /// Iterate over the other and mark intersection by checking
        /// contains in this. Then loop over and delete any unmarked elements. Total cost is n2+n1. 
        /// 
        /// Attempts to return early based on counts alone, using the property that the 
        /// intersection of anything with the empty set is the empty set.
        /// </remarks>
        /// <param name="other">enumerable with items to add </param>
        public void IntersectWith(in ReadOnlySpan<T> other)
        {
            // intersection of anything with empty set is empty set, so return if count is 0
            if (_count == 0)
            {
                return;
            }

            // if other is empty, intersection is empty set; remove all elements and we're done
            if (other.Length == 0)
            {
                Clear();
                return;
            }

            IntersectWithSpan(other);
        }

        /// <summary>
        /// Iterate over other. If contained in this, mark an element in bit array corresponding to
        /// its position in _slots. If anything is unmarked (in bit array), remove it.
        /// 
        /// This attempts to allocate on the stack, if below StackAllocThreshold.
        /// </summary>
        /// <param name="other"></param>
        private void IntersectWithSpan(in ReadOnlySpan<T> other)
        {
            Debug.Assert(_buckets != null, "_buckets shouldn't be null; callers should check first");

            // keep track of current last index; don't want to move past the end of our bit array
            // (could happen if another thread is modifying the collection)
            int originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> span = stackalloc int[StackAllocThreshold];
            BitHelper bitHelper = intArrayLength <= StackAllocThreshold
                ? new BitHelper(span.Slice(0, intArrayLength), clear: true)
                : new BitHelper(new int[intArrayLength], clear: false);

            // mark if contains: find index of in slots array and mark corresponding element in bit array
            for (int i = 0, len = other.Length; i < len; i++)
            {
                int index = FindItemIndex(other[i]);
                if (index >= 0)
                {
                    bitHelper.MarkBit(index);
                }
            }

            // If anything unmarked, remove it. Perf can be optimized here if BitHelper had a
            // FindFirstUnmarked method.
            for (int i = 0; i < originalCount; i++)
            {
                ref Entry<T> entry = ref _entries![i];
                if (entry.Next >= -1 && !bitHelper.IsMarked(i))
                {
                    Remove(entry.Value);
                }
            }
        }

        /// <summary>
        /// Remove items in other from this set. Modifies this set.
        /// </summary>
        /// <param name="other">enumerable with items to remove</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(T[] other)
            => ExceptWith((ReadOnlySpan<T>)other);

        /// <summary>
        /// Remove items in other from this set. Modifies this set.
        /// </summary>
        /// <param name="other">enumerable with items to remove</param>
        public void ExceptWith(in ReadOnlySpan<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // this is already the empty set; return
            if (_count == 0)
            {
                return;
            }

            // remove every element in other from this
            for (int i = 0, len = other.Length; i < len; i++)
            {
                Remove(other[i]);
            }
        }

        /// <summary>
        /// Takes symmetric difference (XOR) with other and this set. Modifies this set.
        /// </summary>
        /// <param name="other">array with items to XOR</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SymmetricExceptWith(T[] other)
            => SymmetricExceptWith((ReadOnlySpan<T>)other);

        /// <summary>
        /// Takes symmetric difference (XOR) with other and this set. Modifies this set.
        /// </summary>
        /// <param name="other">span with items to XOR</param>
        public void SymmetricExceptWith(in ReadOnlySpan<T> other)
        {
            // if set is empty, then symmetric difference is other
            if (_count == 0)
            {
                UnionWith(other);
                return;
            }

            SymmetricExceptWithSpan(other);
        }

        /// <summary>
        /// Implementation notes:
        ///
        /// Used for symmetric except when other isn't a HashSet. This is more tedious because
        /// other may contain duplicates. HashSet technique could fail in these situations:
        /// 1. Other has a duplicate that's not in this: HashSet technique would add then
        /// remove it.
        /// 2. Other has a duplicate that's in this: HashSet technique would remove then add it
        /// back.
        /// In general, its presence would be toggled each time it appears in other.
        ///
        /// This technique uses bit marking to indicate whether to add/remove the item. If already
        /// present in collection, it will get marked for deletion. If added from other, it will
        /// get marked as something not to remove.
        ///
        /// </summary>
        /// <param name="other"></param>
        private unsafe void SymmetricExceptWithSpan(in ReadOnlySpan<T> other)
        {
            int originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> itemsToRemoveSpan = stackalloc int[StackAllocThreshold / 2];
            BitHelper itemsToRemove = intArrayLength <= StackAllocThreshold / 2 ?
                new BitHelper(itemsToRemoveSpan.Slice(0, intArrayLength), clear: true) :
                new BitHelper(new int[intArrayLength], clear: false);

            Span<int> itemsAddedFromOtherSpan = stackalloc int[StackAllocThreshold / 2];
            BitHelper itemsAddedFromOther = intArrayLength <= StackAllocThreshold / 2 ?
                new BitHelper(itemsAddedFromOtherSpan.Slice(0, intArrayLength), clear: true) :
                new BitHelper(new int[intArrayLength], clear: false);

            for (int i = 0, len = other.Length; i < len; i++)
            {
                if (AddIfNotPresent(other[i], out int location))
                {
                    // wasn't already present in collection; flag it as something not to remove
                    // *NOTE* if location is out of range, we should ignore. BitHelper will
                    // detect that it's out of bounds and not try to mark it. But it's
                    // expected that location could be out of bounds because adding the item
                    // will increase _lastIndex as soon as all the free spots are filled.
                    itemsAddedFromOther.MarkBit(location);
                }
                else
                {
                    // already there...if not added from other, mark for remove.
                    // *NOTE* Even though BitHelper will check that location is in range, we want
                    // to check here. There's no point in checking items beyond originalCount
                    // because they could not have been in the original collection
                    if (location < originalCount && !itemsAddedFromOther.IsMarked(location))
                    {
                        itemsToRemove.MarkBit(location);
                    }
                }
            }

            // if anything marked, remove it
            for (int i = 0; i < originalCount; i++)
            {
                if (itemsToRemove.IsMarked(i))
                {
                    Remove(_entries![i].Value);
                }
            }
        }

        /// <summary>
        /// Checks if this is a subset of other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a subset of other; false if not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(T[] other)
            => IsSubsetOf((ReadOnlySpan<T>)other);

        /// <summary>
        /// Checks if this is a subset of other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a subset of other; false if not</returns>
        public bool IsSubsetOf(in ReadOnlySpan<T> other)
        {
            // The empty set is a subset of any set
            if (_count == 0)
            {
                return true;
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount >= 0;
        }

        /// <summary>
        /// Checks if this is a proper subset of other (i.e. strictly contained in)
        /// </summary>
        /// <remarks>
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it's a proper subset of a set that contains at least
        /// one element, but it's not a proper subset of the empty set.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns>true if this is a proper subset of other; false if not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(T[] other)
            => IsProperSubsetOf((ReadOnlySpan<T>)other);

        /// <summary>
        /// Checks if this is a proper subset of other (i.e. strictly contained in)
        /// </summary>
        /// <remarks>
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it's a proper subset of a set that contains at least
        /// one element, but it's not a proper subset of the empty set.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns>true if this is a proper subset of other; false if not</returns>
        public bool IsProperSubsetOf(in ReadOnlySpan<T> other)
        {
            // no set is a proper subset of an empty set
            if (other.Length == 0)
            {
                return false;
            }

            // the empty set is a proper subset of anything but the empty set
            if (_count == 0)
            {
                return other.Length > 0;
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount > 0;
        }

        /// <summary>
        /// Determines counts that can be used to determine equality, subset, and superset. This
        /// is only used when other is an IEnumerable and not a HashSet. If other is a HashSet
        /// these properties can be checked faster without use of marking because we can assume
        /// other has no duplicates.
        ///
        /// The following count checks are performed by callers:
        /// 1. Equals: checks if unfoundCount = 0 and uniqueFoundCount = _count; i.e. everything
        /// in other is in this and everything in this is in other
        /// 2. Subset: checks if unfoundCount >= 0 and uniqueFoundCount = _count; i.e. other may
        /// have elements not in this and everything in this is in other
        /// 3. Proper subset: checks if unfoundCount > 0 and uniqueFoundCount = _count; i.e
        /// other must have at least one element not in this and everything in this is in other
        /// 4. Proper superset: checks if unfound count = 0 and uniqueFoundCount strictly less
        /// than _count; i.e. everything in other was in this and this had at least one element
        /// not contained in other.
        ///
        /// An earlier implementation used delegates to perform these checks rather than returning
        /// an ElementCount struct; however this was changed due to the perf overhead of delegates.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="returnIfUnfound">Allows us to finish faster for equals and proper superset
        /// because unfoundCount must be 0.</param>
        private unsafe (int UniqueCount, int UnfoundCount) CheckUniqueAndUnfoundElements(in ReadOnlySpan<T> other, bool returnIfUnfound)
        {
            // Need special case in case this has no elements.
            if (_count == 0)
            {
                int numElementsInOther = 0;
                foreach (T item in other)
                {
                    numElementsInOther++;
                    break; // break right away, all we want to know is whether other has 0 or 1 elements
                }

                return (UniqueCount: 0, UnfoundCount: numElementsInOther);
            }

            Debug.Assert((_buckets != null) && (_count > 0), "_buckets was null but count greater than 0");

            int originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> span = stackalloc int[StackAllocThreshold];
            BitHelper bitHelper = intArrayLength <= StackAllocThreshold ?
                new BitHelper(span.Slice(0, intArrayLength), clear: true) :
                new BitHelper(new int[intArrayLength], clear: false);

            int unfoundCount = 0; // count of items in other not found in this
            int uniqueFoundCount = 0; // count of unique items in other found in this

            for (int i = 0, len = other.Length; i < len; i++)
            {
                int index = FindItemIndex(other[i]);
                if (index >= 0)
                {
                    if (!bitHelper.IsMarked(index))
                    {
                        // Item hasn't been seen yet.
                        bitHelper.MarkBit(index);
                        uniqueFoundCount++;
                    }
                }
                else
                {
                    unfoundCount++;
                    if (returnIfUnfound)
                    {
                        break;
                    }
                }
            }

            return (uniqueFoundCount, unfoundCount);
        }

        /// <summary>
        /// Checks if this is a superset of other
        /// </summary>
        /// <remarks>
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If other has no elements (it's the empty set), then this is a superset, even if this
        /// is also the empty set.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns>true if this is a superset of other; false if not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(T[] other)
            => IsSupersetOf((ReadOnlySpan<T>)other);

        /// <summary>
        /// Checks if this is a superset of other
        /// </summary>
        /// <remarks>
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If other has no elements (it's the empty set), then this is a superset, even if this
        /// is also the empty set.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns>true if this is a superset of other; false if not</returns>
        public bool IsSupersetOf(in ReadOnlySpan<T> other)
        {
            // if other is the empty set then this is a superset
            if (other.Length == 0)
            {
                return true;
            }

            return ContainsAllElements(other);
        }

        /// <summary>
        /// Checks if this is a proper superset of other (i.e. other strictly contained in this)
        /// </summary>
        /// <remarks>
        /// Implementation Notes: 
        /// This is slightly more complicated than IsSupersetOf because we have to keep track if there
        /// was at least one element not contained in other.
        /// 
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it can't be a proper superset of any set, even if 
        /// other is the empty set.
        /// 2. If other is an empty set and this contains at least 1 element, then this is a proper
        /// superset.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns>true if this is a proper superset of other; false if not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(T[] other)
            => IsProperSupersetOf((ReadOnlySpan<T>)other);

        /// <summary>
        /// Checks if this is a proper superset of other (i.e. other strictly contained in this)
        /// </summary>
        /// <remarks>
        /// Implementation Notes: 
        /// This is slightly more complicated than IsSupersetOf because we have to keep track if there
        /// was at least one element not contained in other.
        /// 
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it can't be a proper superset of any set, even if 
        /// other is the empty set.
        /// 2. If other is an empty set and this contains at least 1 element, then this is a proper
        /// superset.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns>true if this is a proper superset of other; false if not</returns>
        public bool IsProperSupersetOf(in ReadOnlySpan<T> other)
        {
            // the empty set isn't a proper superset of any set.
            if (_count == 0)
            {
                return false;
            }

            if (other.Length == 0)
            {
                // note that this has at least one element, based on above check
                return true;
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount < Count && unfoundCount == 0;
        }

        /// <summary>
        /// Checks if this set overlaps other (i.e. they share at least one item)
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if these have at least one common element; false if disjoint</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(T[] other)
            => Overlaps((ReadOnlySpan<T>)other);

        /// <summary>
        /// Checks if this set overlaps other (i.e. they share at least one item)
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if these have at least one common element; false if disjoint</returns>
        public bool Overlaps(in ReadOnlySpan<T> other)
        {
            if (_count == 0)
            {
                return false;
            }

            for (int i = 0, len = other.Length; i < len; i++)
            {
                if (Contains(other[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if this and other contain the same elements. This is set equality: 
        /// duplicates and order are ignored
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(T[] other)
            => SetEquals((ReadOnlySpan<T>)other);

        /// <summary>
        /// Checks if this and other contain the same elements. This is set equality: 
        /// duplicates and order are ignored
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool SetEquals(in ReadOnlySpan<T> other)
        {
            // if this count is 0 but other contains at least one element, they can't be equal
            if (_count == 0 && other.Length > 0)
            {
                return false;
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
            return uniqueCount == Count && unfoundCount == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> span)
            => CopyTo(span, 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in Span<T> dest, int destIndex)
            => CopyTo(dest, destIndex, Count);

        public void CopyTo(in Span<T> dest, int destIndex, int count)
        {
            // Check array index valid index into array.
            if (destIndex < 0 || destIndex > dest.Length)
            {
                ThrowHelper.ThrowDestIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            // Also throw if count less than 0.
            if (count < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            }

            // Will the array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (dest.Length - destIndex < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            Span<Entry<T>> src = _entries.AsSpan(0, _count);

            if (src.Length == 0)
                return;

            for (int i = 0, len = src.Length; i < len && count > 0; i++)
            {
                ref Entry<T> entry = ref src[i];
                if (entry.Next >= -1)
                {
                    dest[destIndex++] = entry.Value;
                    count--;
                }
            }
        }

        /// <summary>
        /// Checks if this contains of other's elements. Iterates over other's elements and 
        /// returns false as soon as it finds an element in other that's not in this.
        /// Used by SupersetOf, ProperSupersetOf, and SetEquals.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool ContainsAllElements(in ReadOnlySpan<T> other)
        {
            for (int i = 0, len = other.Length; i < len; i++)
            {
                if (!Contains(other[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private void ReturnBuckets(int[] replaceWith)
        {
            if (_buckets?.Length > 0)
            {
                try
                {
                    _bucketPool.Return(_buckets);
                }
                catch { }
            }

            _buckets = replaceWith ?? s_emptyBuckets;
        }

        private void ReturnEntries(Entry<T>[] replaceWith)
        {
            if (_entries?.Length > 0)
            {
                try
                {
                    _entryPool.Return(_entries, s_clearEntries);
                }
                catch { }
            }

            _entries = replaceWith ?? s_emptyEntries;
        }

        public void Dispose()
        {
            ReturnBuckets(s_emptyBuckets);
            ReturnEntries(s_emptyEntries);
            _count = 0;
            _freeList = -1;
            _freeCount = 0;
            _version++;
        }
    }
}