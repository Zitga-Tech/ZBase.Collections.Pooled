using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace Collections.Pooled
{
    public static class SystemDebug
    {
        [Conditional("DEBUG")]
        public static void Assert(
            [DoesNotReturnIf(false)] bool condition
        )
        {
#if (UNITY_EDITOR || DEBUG) && !(DISABLE_DEBUG || DISABLE_DEBUG_ASSERT)
            Debug.Assert(condition);
#endif
        }

        [Conditional("DEBUG")]
        public static void Assert(
            [DoesNotReturnIf(false)] bool condition
            , string message
        )
        {
#if (UNITY_EDITOR || DEBUG) && !(DISABLE_DEBUG || DISABLE_DEBUG_ASSERT)
            Debug.Assert(condition, message);
#endif
        }

        [Conditional("DEBUG")]
        [DoesNotReturn]
        public static void Fail(string message)
        {
#if (UNITY_EDITOR || DEBUG) && !(DISABLE_DEBUG || DISABLE_DEBUG_ASSERT)
            Debug.Fail(message);
#endif
        }
    }
}
