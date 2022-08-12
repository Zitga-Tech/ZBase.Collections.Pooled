namespace Collections.Pooled
{
    public struct ArrayEntry<TKey>
    {
        public readonly TKey Key;
        public readonly int Hashcode;

        public int Previous;
        public int Next;

        public ArrayEntry(TKey key, int hash, int previousNode)
        {
            Key = key;
            Hashcode = hash;
            Previous = previousNode;
            Next = -1;
        }

        public ArrayEntry(TKey key, int hash)
        {
            Key = key;
            Hashcode = hash;
            Previous = -1;
            Next = -1;
        }

        public ArrayEntry(in TKey key, int hash, int previousNode)
        {
            Key = key;
            Hashcode = hash;
            Previous = previousNode;
            Next = -1;
        }

        public ArrayEntry(in TKey key, int hash)
        {
            Key = key;
            Hashcode = hash;
            Previous = -1;
            Next = -1;
        }
    }
}