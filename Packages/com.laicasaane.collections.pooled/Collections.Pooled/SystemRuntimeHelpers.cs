using System.Runtime.CompilerServices;

namespace Collections.Pooled
{
    public static class SystemRuntimeHelpers
    {
        public static bool IsReferenceOrContainsReferences<T>()
        {
            return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        }
    }
}
