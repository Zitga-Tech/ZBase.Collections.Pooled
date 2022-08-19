// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Collections.Pooled
{
    static partial class HashHelpers
    {
        // https://github.com/dotnet/runtime/blob/50c3df750a2ad6996159100245645d010c693d87/src/libraries/System.Private.CoreLib/src/System/String.Comparison.cs#L820
        public static unsafe int GetNonRandomizedHashCode(string str)
        {
            ReadOnlySpan<char> chars = str.AsSpan();

            fixed (char* src = chars)
            {
                SystemDebug.Assert(src[chars.Length] == '\0', "src[this.Length] == '\\0'");
                SystemDebug.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");

                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                uint* ptr = (uint*)src;
                int length = chars.Length;

                while (length > 2)
                {
                    length -= 4;
                    // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                    ptr += 2;
                }

                if (length > 0)
                {
                    // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[0];
                }

                return (int)(hash1 + (hash2 * 1566083941));
            }
        }
    }
}