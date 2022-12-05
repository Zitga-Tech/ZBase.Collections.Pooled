namespace ZBase.Collections.Pooled
{
    /// <summary>
    /// Represents an internal data structure for Dictionary
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public struct Entry<TKey, TValue>
    {
        public uint HashCode;
        /// <summary>
        /// 0-based index of next entry in chain: -1 means end of chain
        /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
        /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
        /// </summary>
        public int Next;
        public TKey Key;
        public TValue Value;
    }
}
