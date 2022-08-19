# Collections.Pooled

- Inspired by [jtmueller/Collections.Pooled](https://github.com/jtmueller/Collections.Pooled)
- The source code of `List`, `Queue`, `Stack`, `HashSet`, `Dictionary` are copied from [dotnet/runtime/System/Collections/Generic](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/), along with other utilities to accomodate their functionality.
- The source code of `ArrayDictionary` is copied from [sebas77/Svelto.Common/SveltoDictionary](https://github.com/sebas77/Svelto.Common/blob/master/DataStructures/Dictionaries/SveltoDictionary.cs).


# Installation

## Install via Open UPM

You can install this package from the [Open UPM](https://openupm.com/packages/com.laicasaane.collections.pooled/) registry.

More details [here](https://github.com/openupm/openupm-cli#installation).

```
openupm add com.laicasaane.collections.pooled
```


## Install via Package Manager

Or, you can add this package by opening the **Package Manager** window and entering

```
https://github.com/laicasaane/Collections.Pooled.git?path=Packages/com.laicasaane.collections.pooled
```

from the `Add package from git URL` option.


# Collections.Pooled

## 1. Class-based collections

- `List<T>`
- `Queue<T>`
- `Stack<T>`
- `HashSet<T>`
- `Dictionary<TKey, TValue>`
- `ArrayDictionary<TKey, TValue>`

Their functionality is the same as provided by the standard collections in `System.Collections.Generic` namespace.

There are also some additional APIs for high performant scenarios.

Designed as quick drop-in replacements for their standard counterparts.

## 2. Struct-based collections

- `ValueArray<T>`
- `ValueList<T>`
- `ValueQueue<T>`
- `ValueStack<T>`
- `ValueHashSet<T>`
- `ValueDictionary<TKey, TValue>`
- `ValueArrayDictionary<TKey, TValue>`

Functionally the same as their class-based counterparts.

Designed as `struct`s to reduce GC allocation of the collection itself.

:warning: Must be created using the static `Create` methods.

## 3. Temporary struct-based collections

- `TempArray<T>`
- `TempList<T>`
- `TempQueue<T>`
- `TempStack<T>`
- `TempHashSet<T>`
- `TempDictionary<TKey, TValue>`
- `TempArrayDictionary<TKey, TValue>`

Functionally the same as their struct-based counterparts.

Designed as `ref struct`s to prevent unintentional GC allocations.

:warning: Must be created using the static `Create` methods.

## Usage

At the end of their lifetime, the collections should be `Dispose`d in order to return their internal arrays to the pool.

```cs
using Collections.Pooled.Generic;

void Test_Manual_Disposing()
{
    var lst = new List<int>();
    using var arr = ValueArray<int>.Create(10);
    ...
    lst.Dispose();
    arr.Dispose();
}

void Test_Automatic_Disposing()
{
    using var lst = new List<int>();
    using var arr = TempArray<int>.Create(10);
    ...
} // at the end of this method, `lst` and `arr` will be disposed automatically
```

If the lifetime of the collections is extended, they should be disposed through the use of `IDisposable` interface.

```cs
using System;
using Collections.Pooled.Generic;

class ExtendedLifetime : IDisposable
{
    // The lifetime of these collections is extended
    private Queue<int> _queue = new Queue<int>();
    private ValueDictionary<int, string> _dict = new ValueDictionary<int, string>()

    public void Dispose()
    {
        _queue.Dispose();
        _dict.Dispose();
    }
}

void Test_Extended_Lifetime()
{
    using var xyz = new ExtendedLifetime();
    ...
} // at the end of this method, `xyz` will be disposed automatically, along with its internal collections
```

# Internals APIs

To enable the scenario of developing high performant extension modules, this library allows direct access to the internal fields of each collection.

## Internals data structures

- Collections.Pooled.Generic
  - `*Internals`
  - `*InternalsRef`
  - `*InternalsRefUnsafe`

- Collections.Pooled.Generic.StructBased
  - `Value*Internals`
  - `Value*InternalsRef`
  - `Value*InternalsRefUnsafe`

- Collections.Pooled.Generic.Temporary
  - `Temp*Internals`
  - `Temp*InternalsRef`
  - `Temp*InternalsRefUnsafe`

## Safe context

Safe internals accessing APIs are exposed through the static `*CollectionsInternals` classes.

### `TakeOwnership` methods

```cs
CollectionInternals      .TakeOwnership(collection)
ValueCollectionInternals .TakeOwnership(valueCollection)
TempCollectionInternals  .TakeOwnership(tempCollection)
```

The procedure follows these steps:

1. Creates an `*Internals` structure to hold references to internal arrays of a source collection.

2. Removes the source references by assigning them to `null`.

3. `Dispose` the source collection.

4. Returns that structure to the outside.

:arrow_forward: Consequentially, this structure has overtaken the **ownership** of those internal arrays.

:x: **DO NOT** use the source collections after this procedure to avoid unknown behaviours, because they have already been disposed.

```cs
using Collections.Pooled.Generic;
using Collections.Pooled.Generic.Internals;

void Test_TakeOwnership()
{
    using var list = new List<int>();
    using var internals = CollectionInternals.TakeOwnership(list);
    // `list` has already been disposed
    // DO NOT use `list` after this line
    ...
} // at the end of this method, `internals` will be disposed automatically
```

Sometimes it is desired for `*Internals` structures to not be disposed because the ownership is going to be transferred to other places. In that case, do not use `using` or call `Dispose` on the returned structure.

```cs
using Collections.Pooled.Generic;
using Collections.Pooled.Generic.Internals;

void Test_Ownership_Transferring()
{
    using var list = new List<int>();
    var internals = CollectionInternals.TakeOwnership(list);
    var customList = SomeCustomList<int>.From(internals);
    // `customList` is holding ownership of the internals of `list`
    // no need to declare `using var internals = ...`
    // no need to call `internals.Dispose()`
    ...
} // at the end of this method, `internals` WON'T be disposed automatically
```

### `GetRef` methods

```cs
CollectionInternals      .GetRef(collection)
ValueCollectionInternals .GetRef(valueCollection)
TempCollectionInternals  .GetRef(tempCollection)
```

- This method returns an `*InternalsRef` structure that holds **indirect** references to internal arrays of a source collection.

- The indirect references are represented as `ReadOnlySpan`s to only expose high performant read operations.

- `*InternalsRef`s are designed as `ref struct`s to prevent unintentional lingering effect of the references (could cause memory leaks).

:warning: The source collections **WON'T** be disposed, they still hold the ownership to internal arrays of their own.

```cs
using Collections.Pooled.Generic;
using Collections.Pooled.Generic.Internals;

void Test_GetRef_Safe()
{
    using var list = new List<int>();
    var internalsRef = CollectionInternals.GetRef(list);
    // `list` is NOT disposed
    // `list` is USABLE even after this line
    ...
}
```

### `AsReadOnlySpan` methods

```cs
CollectionInternals      .AsReadOnlySpan(collection)
ValueCollectionInternals .AsReadOnlySpan(valueCollection)
TempCollectionInternals  .AsReadOnlySpan(tempCollection)
```

- This method returns an indirect reference to the internal main array of a source collection.

- The indirect references are represented as `ReadOnlySpan` to only expose read operations.

- For `Dictionary` and `HashSet`, it returns the `_entries` array.

- For other collections, it returns `_items` or `_array`.

## Unsafe context

Unsafe internals accessing APIs are exposed through the static `*CollectionInternalsUnsafe` classes.

### `GetRef` methods

```cs
CollectionInternalsUnsafe      .GetRef(collection)
ValueCollectionInternalsUnsafe .GetRef(valuecollection)
TempCollectionInternalsUnsafe  .GetRef(tempCollection)
```

- This method returns an `*InternalsRefUnsafe` structure that holds **direct** references to internal arrays of a source collection.

:warning: `*InternalsRefUnsafe`s are designed as `struct`s to be usable in any context. Coupled with direct references to internal arrays, they should be used carefully.

:warning: The source collections **WON'T** be disposed, they still hold the ownership to internal arrays of their own.

```cs
using Collections.Pooled.Generic;
using Collections.Pooled.Generic.Internals.Unsafe;

void Test_GetRef_Unsafe()
{
    using var list = new List<int>();
    var internalsRefUnsafe = CollectionInternalsUnsafe.GetRef(list);
    // `list` is NOT disposed
    // `list` is USABLE even after this line
    ...
}
```

### `AsSpan` methods

```cs
CollectionInternals      .AsSpan(collection)
ValueCollectionInternals .AsSpan(valueCollection)
TempCollectionInternals  .AsSpan(tempCollection)
```

- This method returns an **indirect** reference to the internal main array of a source collection.

- The indirect references are represented as `Span` to expose read and write operations.

- For `Dictionary` and `HashSet`, it returns the `_entries` array.

- For other collections, it returns `_items` or `_array`.

### `GetUnsafe` methods

```cs
collection      .GetUnsafe(out array, out count)
valueCollection .GetUnsafe(out array, out count)
tempCollection  .GetUnsafe(out array, out count)
```

- This method returns a **direct** reference to the internal main array of a source collection, along with the size of that array (named `count`).

- For `Dictionary` and `HashSet`, it returns the `_entries` array.

- For other collections, it returns `_items` or `_array`.

- For `Queue`, it also returns `head` and `tail` values.

```cs
queue      .GetUnsafe(out array, out count, out head, out tail)
valueQueue .GetUnsafe(out array, out count, out head, out tail)
tempQueue  .GetUnsafe(out array, out count, out head, out tail)
```

# Changelog

## 2.5.0

### Features

- Added `*ArrayDictionary` types based on [`SveltoDictionary`](https://github.com/sebas77/Svelto.Common/blob/master/DataStructures/Dictionaries/SveltoDictionary.cs)
    - This data structure allows iterating over its keys and values as an array.

```cs
var dict = new ArrayDictionary<int, int>();

dict.GetUnsafe(out var keys, out var values, out var count);

for (var i = 0; i < count; i++)
{
    Debug.Log($"{keys[i].Key} = {values[i]}");
}
```

- Added `*Internals` types related to `*ArrayDictionary<TKey, TValue>`

### Fixes

- For `*Dictionary` and `*HashSet`
    - Internal pools must be set firstly in the constructors
    - `TrimExcess` must return old arrays to the pools
    - Serialization should not use `ArrayPool`
    - Remove `null`s and null checks for the internal `_buckets` array
    - Correct internal arrays initialization

## 2.5.2

### Changes

- Add `1` to the `HashHelpers.primes` array to support the scenarios of only 1 element

## 2.6.0

### Fixes

- Correct the reference to `System.Runtime.CompilerServices.dll` version `6.0.0`

## 2.6.1

### Breaking Changes

- Expose some internal methods of `*ArrayDictionary` because they are safe
- Remove the related methods from `*CollectionInternalUnsafe` for the same reason

## 2.6.2

### Features

- Add `ReadOnlyValueArray` and `ReadOnlyTempArray`

### Changes

- Use `IsNullOrEmpty` extension method to check if arrays are null or empty

### Fixes

- Correct `ValueArray` and `TempArray` constructor

## 2.6.3

### Features

- Add `ReadOnlyArray`

## 2.6.4

### Features

- Add internal APIs for readonly arrays

### Changes

- Improve Enumerators
- Make `AsReadOnlySpan` and `AsSpan` extension methods

## 2.6.5

### Features

- Add APIs to create `ValueArray` and `TempArray` in unsafe context

## 2.6.6

### Features

- Add `SystemDebug`

### Changes

- Replace `System.Debug` with `SystemDebug` to allow disabling `Debug.Assert`
