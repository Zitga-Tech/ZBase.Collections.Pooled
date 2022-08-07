// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Collections.Pooled
{
    public ref struct BitHelper
    {
        private const int IntSize = sizeof(int) * 8;
        private readonly Span<int> _span;

        public BitHelper(Span<int> span, bool clear)
        {
            if (clear)
            {
                span.Clear();
            }
            _span = span;
        }

        public void MarkBit(int bitPosition)
        {
            int bitArrayIndex = bitPosition / IntSize;
            Span<int> span = _span;

            if ((uint)bitArrayIndex < (uint)span.Length)
            {
                span[bitArrayIndex] |= (1 << (bitPosition % IntSize));
            }
        }

        public bool IsMarked(int bitPosition)
        {
            int bitArrayIndex = bitPosition / IntSize;
            Span<int> span = _span;

            return
                (uint)bitArrayIndex < (uint)span.Length &&
                (span[bitArrayIndex] & (1 << (bitPosition % IntSize))) != 0;
        }

        public int FindFirstUnmarked(int startPosition = 0)
        {
            int i = startPosition;
            Span<int> span = _span;

            for (int bi = i / IntSize; (uint)bi < (uint)span.Length; bi = ++i / IntSize)
            {
                if ((span[bi] & (1 << (i % IntSize))) == 0)
                    return i;
            }
            return -1;
        }

        public int FindFirstMarked(int startPosition = 0)
        {
            int i = startPosition;
            Span<int> span = _span;

            for (int bi = i / IntSize; (uint)bi < (uint)span.Length; bi = ++i / IntSize)
            {
                if ((span[bi] & (1 << (i % IntSize))) != 0)
                    return i;
            }
            return -1;
        }

        /// <summary>How many ints must be allocated to represent n bits. Returns (n+31)/32, but avoids overflow.</summary>
        public static int ToIntArrayLength(int n) => n > 0 ? ((n - 1) / IntSize + 1) : 0;
    }
}
