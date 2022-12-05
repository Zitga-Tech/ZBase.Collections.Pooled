namespace ZBase.Collections.Pooled
{
    public interface IComparison<in T>
    {
        int Compare(T x, T y);
    }
}
