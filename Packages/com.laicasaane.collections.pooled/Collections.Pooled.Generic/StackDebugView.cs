// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Collections/src/System/Collections/Generic/StackDebugView.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Collections.Pooled.Generic
{
    internal sealed class StackDebugView<T>
    {
        private readonly Stack<T> _stack;

        public StackDebugView(Stack<T> stack)
        {
            _stack = stack ?? throw new ArgumentNullException(nameof(stack));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _stack.ToArray();
            }
        }
    }
}