// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Collections/src/System/Collections/Generic/StackDebugView.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Collections.Pooled.ValueTypes
{
    internal sealed class ValueStackDebugView<T>
    {
        private readonly ValueStack<T> _stack;

        public ValueStackDebugView(ValueStack<T> stack)
        {
            _stack = stack;
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