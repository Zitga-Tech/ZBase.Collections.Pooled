using System.Runtime.CompilerServices;

namespace Collections.Pooled
{
    public static class SystemRuntimeHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReferenceOrContainsReferences<T>()
        {
            return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        }
    }
}
