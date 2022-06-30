namespace Collections.Pooled
{
    public interface IPredicate<in T>
    {
        bool Predicate(T value);
    }
}
