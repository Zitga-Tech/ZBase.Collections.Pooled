namespace Collections.Pooled
{
    public delegate void ActionRef<T>(ref T target);
    public delegate void ActionRef<T, U>(ref T target, ref U value);

    public delegate U FuncRef<T, U>(ref T target);
}
