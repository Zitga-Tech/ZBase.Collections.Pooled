using System.Runtime.CompilerServices;

namespace Collections.Pooled
{
    public static class SystemUnsafe
    {
        //
        // Summary:
        //     Determines if a given reference to a value of type T is a null reference.
        //
        // Parameters:
        //   source:
        //     The reference to check.
        //
        // Type parameters:
        //   T:
        //     The type of the reference.
        //
        // Returns:
        //     true if source is a null reference; otherwise, false.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsNullRef<T>(ref T source)
        {
            return Unsafe.AsPointer(ref source) == null;
        }

        //
        // Summary:
        //     Returns a reference to a value of type T that is a null reference.
        //
        // Type parameters:
        //   T:
        //     The type of the reference.
        //
        // Returns:
        //     A reference to a value of type T that is a null reference.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static ref T NullRef<T>()
        {
            return ref Unsafe.AsRef<T>(null);
        }
    }
}
