// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ZBase.Collections.Pooled
{
    /// <summary>
    /// NonRandomizedStringEqualityComparer is the comparer used by default with the PooledDictionary.
    /// We use NonRandomizedStringEqualityComparer as default comparer as it doesnt use the randomized string hashing which 
    /// keeps the performance not affected till we hit collision threshold and then we switch to the comparer which is using 
    /// randomized string hashing.
    /// </summary>
    [Serializable] // Required for compatibility with .NET Core 2.0 as we exposed the NonRandomizedStringEqualityComparer inside the serialization blob
    public sealed class NonRandomizedStringEqualityComparer : EqualityComparer<string>, ISerializable
    {
        private static readonly int s_empyStringHashCode = string.Empty.GetHashCode();

        public static new IEqualityComparer<string> Default { get; } = new NonRandomizedStringEqualityComparer();

        private NonRandomizedStringEqualityComparer() { }

        // This is used by the serialization engine.
        private NonRandomizedStringEqualityComparer(SerializationInfo information, StreamingContext context) { }

        public override bool Equals(string x, string y) => string.Equals(x, y);

        public override int GetHashCode(string str) 
            => str is null ? 0 : str.Length == 0 ? s_empyStringHashCode : HashHelpers.GetNonRandomizedHashCode(str);

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(NonRandomizedStringEqualityComparer));
        }
    }
}
