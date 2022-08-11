namespace Collections.Pooled
{
    public struct ArrayEntry<TKey>
    {
        public readonly TKey Key;
        internal readonly int Hashcode;
        internal int Previous;
        internal int Next;

        public ArrayEntry(ref TKey key, int hash, int previousNode)
        {
            Key = key;
            Hashcode = hash;
            Previous = previousNode;
            Next = -1;
        }

        public ArrayEntry(ref TKey key, int hash)
        {
            Key = key;
            Hashcode = hash;
            Previous = -1;
            Next = -1;
        }
    }
}