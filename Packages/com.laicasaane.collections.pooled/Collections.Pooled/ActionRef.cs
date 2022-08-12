namespace Collections.Pooled
{
    public delegate void ActionRef<T>(ref T arg);

    public delegate void ActionRef<T1, T2>(ref T1 arg1, ref T2 arg2);

    public delegate TResult FuncRef<T, TResult>(ref T arg);
}
