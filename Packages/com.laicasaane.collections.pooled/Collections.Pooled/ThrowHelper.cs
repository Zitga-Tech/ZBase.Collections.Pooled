// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// This file defines an public class used to throw exceptions in BCL code.
// The main purpose is to reduce code size.
//
// The old way to throw an exception generates quite a lot IL code and assembly code.
// Following is an example:
//     C# source
//          throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
//     IL code:
//          IL_0003:  ldstr      "key"
//          IL_0008:  ldstr      "ArgumentNull_Key"
//          IL_000d:  call       string System.Environment::GetResourceString(string)
//          IL_0012:  newobj     instance void System.ArgumentNullException::.ctor(string,string)
//          IL_0017:  throw
//    which is 21bytes in IL.
//
// So we want to get rid of the ldstr and call to Environment.GetResource in IL.
// In order to do that, I created two enums: ExceptionResource, ExceptionArgument to represent the
// argument name and resource name in a small integer. The source code will be changed to
//    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key, ExceptionResource.ArgumentNull_Key);
//
// The IL code will be 7 bytes.
//    IL_0008:  ldc.i4.4
//    IL_0009:  ldc.i4.4
//    IL_000a:  call       void System.ThrowHelper::ThrowArgumentNullException(valuetype System.ExceptionArgument)
//    IL_000f:  ldarg.0
//
// This will also reduce the Jitted code size a lot.
//
// It is very important we do this for generic classes because we can easily generate the same code
// multiple times for different instantiation.
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using EA = Collections.Pooled.ExceptionArgument;
using ER = Collections.Pooled.ExceptionResource;

namespace Collections.Pooled
{
    public static class ThrowHelper
    {
        public static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        public static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        public static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException();
        }

        public static void ThrowArgumentException_DestinationTooShort()
        {
            throw new ArgumentException("Destination too short.");
        }

        public static void ThrowArgumentException_OverlapAlignmentMismatch()
        {
            throw new ArgumentException("Overlap alignment mismatch.");
        }

        public static void ThrowArgumentOutOfRange_IndexException()
        {
            throw GetArgumentOutOfRangeException(EA.index, ER.ArgumentOutOfRange_Index);
        }

        public static void ThrowCapacityArgumentOutOfRange_NeedNonNegNumException()
        {
            throw GetArgumentOutOfRangeException(EA.capacity, ER.ArgumentOutOfRange_NeedNonNegNum);
        }

        public static void ThrowArgumentOutOfRange_IndexMustBeLessException()
        {
            throw GetArgumentOutOfRangeException(EA.index, ER.ArgumentOutOfRange_IndexMustBeLess);
        }

        public static void ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException()
        {
            throw GetArgumentOutOfRangeException(EA.index, ER.ArgumentOutOfRange_IndexMustBeLessOrEqual);
        }

        public static void ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess()
        {
            throw GetArgumentOutOfRangeException(EA.startIndex, ER.ArgumentOutOfRange_IndexMustBeLess);
        }

        public static void ThrowCountArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum()
        {
            throw GetArgumentOutOfRangeException(EA.count, ER.ArgumentOutOfRange_NeedNonNegNum);
        }

        public static void ThrowArrayIndexArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum()
        {
            throw GetArgumentOutOfRangeException(EA.arrayIndex, ER.ArgumentOutOfRange_NeedNonNegNum);
        }

        public static void ThrowDestIndexArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum()
        {
            throw GetArgumentOutOfRangeException(EA.destIndex, ER.ArgumentOutOfRange_NeedNonNegNum);
        }

        public static void ThrowArrayIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual()
        {
            throw GetArgumentOutOfRangeException(EA.arrayIndex, ER.ArgumentOutOfRange_IndexMustBeLessOrEqual);
        }

        public static void ThrowDestIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual()
        {
            throw GetArgumentOutOfRangeException(EA.destIndex, ER.ArgumentOutOfRange_IndexMustBeLessOrEqual);
        }

        public static void ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual()
        {
            throw GetArgumentOutOfRangeException(EA.startIndex, ER.ArgumentOutOfRange_IndexMustBeLessOrEqual);
        }

        public static void ThrowIndexArgumentOutOfRange_NeedNonNegNumException()
        {
            throw GetArgumentOutOfRangeException(EA.index, ER.ArgumentOutOfRange_NeedNonNegNum);
        }

        public static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
        {
            throw GetArgumentOutOfRangeException(EA.value, ER.ArgumentOutOfRange_NeedNonNegNum);
        }

        public static void ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum()
        {
            throw GetArgumentOutOfRangeException(EA.length, ER.ArgumentOutOfRange_NeedNonNegNum);
        }

        public static void ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index()
        {
            throw GetArgumentOutOfRangeException(EA.startIndex, ER.ArgumentOutOfRange_Index);
        }

        public static void ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count()
        {
            throw GetArgumentOutOfRangeException(EA.count, ER.ArgumentOutOfRange_Count);
        }

        public static void ThrowWrongKeyTypeArgumentException<T>(T key, Type targetType)
        {
            // Generic key to move the boxing to the right hand side of throw
            throw GetWrongKeyTypeArgumentException((object)key, targetType);
        }

        public static void ThrowWrongValueTypeArgumentException<T>(T value, Type targetType)
        {
            // Generic key to move the boxing to the right hand side of throw
            throw GetWrongValueTypeArgumentException((object)value, targetType);
        }

        private static ArgumentException GetAddingDuplicateWithKeyArgumentException(object key)
        {
            return new ArgumentException($"Error adding duplicate with key: {key}.");
        }

        public static void ThrowAddingDuplicateWithKeyArgumentException<T>(T key)
        {
            // Generic key to move the boxing to the right hand side of throw
            throw GetAddingDuplicateWithKeyArgumentException((object)key);
        }

        public static void ThrowKeyNotFoundException<T>(T key)
        {
            // Generic key to move the boxing to the right hand side of throw
            throw GetKeyNotFoundException((object)key);
        }

        public static void ThrowArgumentException(ER resource)
        {
            throw GetArgumentException(resource);
        }

        public static void ThrowArgumentException(ER resource, EA argument)
        {
            throw GetArgumentException(resource, argument);
        }

        private static ArgumentNullException GetArgumentNullException(EA argument)
        {
            return new ArgumentNullException(GetArgumentName(argument));
        }

        public static void ThrowArgumentNullException(EA argument)
        {
            throw GetArgumentNullException(argument);
        }

        public static void ThrowArgumentNullException(ER resource)
        {
            throw new ArgumentNullException(GetResourceString(resource));
        }

        public static void ThrowArgumentNullException(EA argument, ER resource)
        {
            throw new ArgumentNullException(GetArgumentName(argument), GetResourceString(resource));
        }

        public static void ThrowArgumentOutOfRangeException(EA argument)
        {
            throw new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        public static void ThrowArgumentOutOfRangeException(EA argument, ER resource)
        {
            throw GetArgumentOutOfRangeException(argument, resource);
        }

        public static void ThrowArgumentOutOfRangeException(EA argument, int paramNumber, ER resource)
        {
            throw GetArgumentOutOfRangeException(argument, paramNumber, resource);
        }

        public static void ThrowInvalidOperationException(ER resource)
        {
            throw GetInvalidOperationException(resource);
        }

        public static void ThrowInvalidOperationException(ER resource, Exception e)
        {
            throw new InvalidOperationException(GetResourceString(resource), e);
        }

        public static void ThrowSerializationException(ER resource)
        {
            throw new SerializationException(GetResourceString(resource));
        }

        public static void ThrowSecurityException(ER resource)
        {
            throw new System.Security.SecurityException(GetResourceString(resource));
        }

        public static void ThrowRankException(ER resource)
        {
            throw new RankException(GetResourceString(resource));
        }

        public static void ThrowNotSupportedException(ER resource)
        {
            throw new NotSupportedException(GetResourceString(resource));
        }

        public static void ThrowUnauthorizedAccessException(ER resource)
        {
            throw new UnauthorizedAccessException(GetResourceString(resource));
        }

        public static void ThrowObjectDisposedException(string objectName, ER resource)
        {
            throw new ObjectDisposedException(objectName, GetResourceString(resource));
        }

        public static void ThrowObjectDisposedException(ER resource)
        {
            throw new ObjectDisposedException(null, GetResourceString(resource));
        }

        public static void ThrowNotSupportedException()
        {
            throw new NotSupportedException();
        }

        public static void ThrowAggregateException(List<Exception> exceptions)
        {
            throw new AggregateException(exceptions);
        }

        public static void ThrowOutOfMemoryException()
        {
            throw new OutOfMemoryException();
        }

        public static void ThrowArgumentException_Argument_InvalidArrayType()
        {
            throw new ArgumentException("Invalid array type.");
        }

        public static void ThrowInvalidOperationException_InvalidOperation_EmptyQueue()
        {
            throw new InvalidOperationException("Queue empty.");
        }

        public static void ThrowInvalidOperationException_InvalidOperation_EmptyStack()
        {
            throw new InvalidOperationException("Stack empty.");
        }

        public static void ThrowInvalidOperationException_InvalidOperation_EnumNotStarted()
        {
            throw new InvalidOperationException("Enumeration has not started.");
        }

        public static void ThrowInvalidOperationException_InvalidOperation_EnumEnded()
        {
            throw new InvalidOperationException("Enumeration has ended.");
        }

        public static void ThrowInvalidOperationException_EnumCurrent(int index)
        {
            throw GetInvalidOperationException_EnumCurrent(index);
        }

        public static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
        {
            throw new InvalidOperationException("Collection was modified during enumeration.");
        }

        public static void ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen()
        {
            throw new InvalidOperationException("Invalid enumerator state: enumeration cannot proceed.");
        }

        public static void ThrowInvalidOperationException_InvalidOperation_NoValue()
        {
            throw new InvalidOperationException("No value provided.");
        }

        public static void ThrowInvalidOperationException_InvalidOperation_SpanOverlappedOperation()
        {
            throw new InvalidOperationException("This operation is invalid on overlapping buffers.");
        }

        public static void ThrowInvalidOperationException_ConcurrentOperationsNotSupported()
        {
            throw new InvalidOperationException("Concurrent operations are not supported.");
        }

        public static void ThrowInvalidOperationException_HandleIsNotInitialized()
        {
            throw new InvalidOperationException("Handle is not initialized.");
        }

        public static void ThrowFormatException_BadFormatSpecifier()
        {
            throw new FormatException("Bad format specifier.");
        }

        private static ArgumentException GetArgumentException(ER resource)
        {
            return new ArgumentException(GetResourceString(resource));
        }

        private static InvalidOperationException GetInvalidOperationException(ER resource)
        {
            return new InvalidOperationException(GetResourceString(resource));
        }

        private static ArgumentException GetWrongKeyTypeArgumentException(object key, Type targetType)
        {
            return new ArgumentException($"Wrong key type. Expected {targetType}, got: '{key}'.", nameof(key));
        }

        private static ArgumentException GetWrongValueTypeArgumentException(object value, Type targetType)
        {
            return new ArgumentException($"Wrong value type. Expected {targetType}, got: '{value}'.", nameof(value));
        }

        private static KeyNotFoundException GetKeyNotFoundException(object key)
        {
            return new KeyNotFoundException($"Key not found: {key}");
        }

        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(EA argument, ER resource)
        {
            return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
        }

        private static ArgumentException GetArgumentException(ER resource, EA argument)
        {
            return new ArgumentException(GetResourceString(resource), GetArgumentName(argument));
        }

        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(EA argument, int paramNumber, ER resource)
        {
            return new ArgumentOutOfRangeException($"{GetArgumentName(argument)}[{paramNumber}]", GetResourceString(resource));
        }

        private static InvalidOperationException GetInvalidOperationException_EnumCurrent(int index)
        {
            return new InvalidOperationException(
                index < 0 
                ? "Enumeration has not started"
                : "Enumeration has ended"
            );
        }

        // Allow nulls for reference types and Nullable<U>, but not for value types.
        // Aggressively inline so the jit evaluates the if in place and either drops the call altogether
        // Or just leaves null test and call to the Non-returning ThrowHelper.ThrowArgumentNullException
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNullAndNullsAreIllegalThenThrow<T>(object value, EA argName)
        {
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            if (!(default(T) == null) && value == null)
                ThrowHelper.ThrowArgumentNullException(argName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowForUnsupportedVectorBaseType<T>() where T : struct
        {
            if (typeof(T) != typeof(byte) && typeof(T) != typeof(sbyte) &&
                typeof(T) != typeof(short) && typeof(T) != typeof(ushort) &&
                typeof(T) != typeof(int) && typeof(T) != typeof(uint) &&
                typeof(T) != typeof(long) && typeof(T) != typeof(ulong) &&
                typeof(T) != typeof(float) && typeof(T) != typeof(double))
            {
                ThrowNotSupportedException(ER.Arg_TypeNotSupported);
            }
        }

#if false // Reflection-based implementation does not work for CoreRT/ProjectN
        // This function will convert an ExceptionArgument enum value to the argument name string.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetArgumentName(ExceptionArgument argument)
        {
            SystemDebug.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument),
                "The enum value is not defined, please check the ExceptionArgument Enum.");

            return argument.ToString();
        }
#endif

        private static string GetArgumentName(EA argument)
        {
            switch (argument)
            {
                case EA.obj:
                    return nameof(EA.obj);
                case EA.dictionary:
                    return nameof(EA.dictionary);
                case EA.stack:
                    return nameof(EA.stack);
                case EA.queue:
                    return nameof(EA.queue);
                case EA.array:
                    return nameof(EA.array);
                case EA.dest:
                    return nameof(EA.dest);
                case EA.info:
                    return nameof(EA.info);
                case EA.key:
                    return nameof(EA.key);
                case EA.text:
                    return nameof(EA.text);
                case EA.values:
                    return nameof(EA.values);
                case EA.value:
                    return nameof(EA.value);
                case EA.startIndex:
                    return nameof(EA.startIndex);
                case EA.task:
                    return nameof(EA.task);
                case EA.ch:
                    return nameof(EA.ch);
                case EA.s:
                    return nameof(EA.s);
                case EA.input:
                    return nameof(EA.input);
                case EA.list:
                    return nameof(EA.list);
                case EA.index:
                    return nameof(EA.index);
                case EA.capacity:
                    return nameof(EA.capacity);
                case EA.collection:
                    return nameof(EA.collection);
                case EA.item:
                    return nameof(EA.item);
                case EA.converter:
                    return nameof(EA.converter);
                case EA.match:
                    return nameof(EA.match);
                case EA.count:
                    return nameof(EA.count);
                case EA.action:
                    return nameof(EA.action);
                case EA.comparison:
                    return nameof(EA.comparison);
                case EA.exceptions:
                    return nameof(EA.exceptions);
                case EA.exception:
                    return nameof(EA.exception);
                case EA.enumerable:
                    return nameof(EA.enumerable);
                case EA.start:
                    return nameof(EA.start);
                case EA.format:
                    return nameof(EA.format);
                case EA.culture:
                    return nameof(EA.culture);
                case EA.comparer:
                    return nameof(EA.comparer);
                case EA.comparable:
                    return nameof(EA.comparable);
                case EA.source:
                    return nameof(EA.source);
                case EA.state:
                    return nameof(EA.state);
                case EA.length:
                    return nameof(EA.length);
                case EA.output:
                    return nameof(EA.output);
                case EA.comparisonType:
                    return nameof(EA.comparisonType);
                case EA.manager:
                    return nameof(EA.manager);
                case EA.sourceBytesToCopy:
                    return nameof(EA.sourceBytesToCopy);
                case EA.callBack:
                    return nameof(EA.callBack);
                case EA.creationOptions:
                    return nameof(EA.creationOptions);
                case EA.function:
                    return nameof(EA.function);
                case EA.delay:
                    return nameof(EA.delay);
                case EA.millisecondsDelay:
                    return nameof(EA.millisecondsDelay);
                case EA.millisecondsTimeout:
                    return nameof(EA.millisecondsTimeout);
                case EA.timeout:
                    return nameof(EA.timeout);
                case EA.type:
                    return nameof(EA.type);
                case EA.sourceIndex:
                    return nameof(EA.sourceIndex);
                case EA.sourceArray:
                    return nameof(EA.sourceArray);
                case EA.destinationIndex:
                    return nameof(EA.destinationIndex);
                case EA.destinationArray:
                    return nameof(EA.destinationArray);
                case EA.other:
                    return nameof(EA.other);
                case EA.newSize:
                    return nameof(EA.newSize);
                case EA.lowerBounds:
                    return nameof(EA.lowerBounds);
                case EA.lengths:
                    return nameof(EA.lengths);
                case EA.len:
                    return nameof(EA.len);
                case EA.keys:
                    return nameof(EA.keys);
                case EA.indices:
                    return nameof(EA.indices);
                case EA.endIndex:
                    return nameof(EA.endIndex);
                case EA.elementType:
                    return nameof(EA.elementType);
                case EA.arrayIndex:
                    return nameof(EA.arrayIndex);
                case EA.destIndex:
                    return nameof(EA.destIndex);
                case EA.codePoint:
                    return nameof(EA.codePoint);
                default:
                    SystemDebug.Fail("The enum value is not defined, please check the ExceptionArgument Enum.");
                    return argument.ToString();
            }
        }

#if false // Reflection-based implementation does not work for CoreRT/ProjectN
        // This function will convert an ExceptionResource enum value to the resource string.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetResourceString(ExceptionResource resource)
        {
            SystemDebug.Assert(Enum.IsDefined(typeof(ExceptionResource), resource),
                "The enum value is not defined, please check the ExceptionResource Enum.");

            return SR.GetResourceString(resource.ToString());
        }
#endif

        private static string GetResourceString(ER resource)
        {
            switch (resource)
            {
                case ER.ArgumentOutOfRange_Index:
                    return "Argument 'index' was out of the range of valid values.";
                case ER.ArgumentOutOfRange_IndexMustBeLess:
                    return "Index was out of range. Must be non-negative and less than the size of the collection.";
                case ER.ArgumentOutOfRange_IndexMustBeLessOrEqual:
                    return "Index was out of range. Must be non-negative and less than or equal to the size of the collection.";
                case ER.ArgumentOutOfRange_Count:
                    return "Argument 'count' was out of the range of valid values.";
                case ER.ArgumentOutOfRange_Output_SmallerThanCollection:
                    return "Argument 'output' was smaller than the size of the collection.";
                case ER.Arg_ArrayPlusOffTooSmall:
                    return "Array plus offset too small.";
                case ER.NotSupported_ReadOnlyCollection:
                    return "This operation is not supported on a read-only collection.";
                case ER.Arg_RankMultiDimNotSupported:
                    return "Multi-dimensional arrays are not supported.";
                case ER.Arg_NonZeroLowerBound:
                    return "Arrays with a non-zero lower bound are not supported.";
                case ER.ArgumentOutOfRange_ListInsert:
                    return "Insertion index was out of the range of valid values.";
                case ER.ArgumentOutOfRange_NeedNonNegNum:
                    return "The number must be non-negative.";
                case ER.ArgumentOutOfRange_SmallCapacity:
                    return "The capacity cannot be set below the current Count.";
                case ER.Argument_InvalidOffLen:
                    return "Invalid offset length.";
                case ER.ArgumentOutOfRange_BiggerThanCollection:
                    return "The given value was larger than the size of the collection.";
                case ER.Serialization_MissingKeys:
                    return "Serialization error: missing keys.";
                case ER.Serialization_NullKey:
                    return "Serialization error: null key.";
                case ER.NotSupported_KeyCollectionSet:
                    return "The KeyCollection does not support modification.";
                case ER.NotSupported_ValueCollectionSet:
                    return "The ValueCollection does not support modification.";
                case ER.InvalidOperation_NullArray:
                    return "Null arrays are not supported.";
                case ER.InvalidOperation_HSCapacityOverflow:
                    return "Set hash capacity overflow. Cannot increase size.";
                case ER.NotSupported_StringComparison:
                    return "String comparison not supported.";
                case ER.ConcurrentCollection_SyncRoot_NotSupported:
                    return "SyncRoot not supported.";
                case ER.ArgumentException_OtherNotArrayOfCorrectLength:
                    return "The other array is not of the correct length.";
                case ER.ArgumentOutOfRange_EndIndexStartIndex:
                    return "The end index does not come after the start index.";
                case ER.ArgumentOutOfRange_HugeArrayNotSupported:
                    return "Huge arrays are not supported.";
                case ER.Argument_AddingDuplicate:
                    return "Duplicate item added.";
                case ER.Argument_InvalidArgumentForComparison:
                    return "Invalid argument for comparison.";
                case ER.Arg_LowerBoundsMustMatch:
                    return "Array lower bounds must match.";
                case ER.Arg_MustBeType:
                    return "Argument must be of type: ";
                case ER.InvalidOperation_IComparerFailed:
                    return "IComparer failed.";
                case ER.NotSupported_FixedSizeCollection:
                    return "This operation is not suppored on a fixed-size collection.";
                case ER.Rank_MultiDimNotSupported:
                    return "Multi-dimensional arrays are not supported.";
                case ER.Arg_TypeNotSupported:
                    return "Type not supported.";
                case ER.Serialization_Pooled_MissingData:
                    return "Serialized PooledDictionary missing data.";
                case ER.Serialization_Pooled_NullKey:
                    return "Serialized PooledDictionary had null key.";
                case ER.Destination_Span_Shorter_Than_List_To_Copy:
                    return "Destination span is shorter than the list to be copied.";
                case ER.Destination_Span_Shorter_Than_Count:
                    return "Destination span is shorter than the number of items to be copied.";
                case ER.Queue_Empty:
                    return "Queue is empty.";
                case ER.Collection_Modified:
                    return "Collection was modified during enumeration.";
                case ER.Enumeration_NotStarted:
                    return "Enumeration was not started.";
                case ER.Enumeration_Ended:
                    return "Enumeration has ended.";
                default:
                    SystemDebug.Assert(false,
                        "The enum value is not defined, please check the ExceptionResource Enum.");
                    return resource.ToString();
            }
        }
    }

    //
    // The convention for this enum is using the argument name as the enum name
    //
    public enum ExceptionArgument
    {
        obj,
        dictionary,
        stack,
        queue,
        array,
        dest,
        info,
        key,
        text,
        values,
        value,
        startIndex,
        task,
        ch,
        s,
        input,
        list,
        index,
        capacity,
        collection,
        item,
        converter,
        match,
        count,
        action,
        comparison,
        exceptions,
        exception,
        enumerable,
        start,
        format,
        culture,
        comparer,
        comparable,
        source,
        state,
        length,
        output,
        comparisonType,
        manager,
        sourceBytesToCopy,
        callBack,
        creationOptions,
        function,
        delay,
        millisecondsDelay,
        millisecondsTimeout,
        timeout,
        type,
        sourceIndex,
        sourceArray,
        destinationIndex,
        destinationArray,
        other,
        newSize,
        lowerBounds,
        lengths,
        len,
        keys,
        indices,
        endIndex,
        elementType,
        arrayIndex,
        destIndex,
        codePoint
    }

    //
    // The convention for this enum is using the resource name as the enum name
    //
    public enum ExceptionResource
    {
        ArgumentOutOfRange_Index,
        ArgumentOutOfRange_IndexMustBeLess,
        ArgumentOutOfRange_IndexMustBeLessOrEqual,
        ArgumentOutOfRange_Count,
        ArgumentOutOfRange_Output_SmallerThanCollection,
        Arg_ArrayPlusOffTooSmall,
        NotSupported_ReadOnlyCollection,
        Arg_RankMultiDimNotSupported,
        Arg_NonZeroLowerBound,
        ArgumentOutOfRange_ListInsert,
        ArgumentOutOfRange_NeedNonNegNum,
        ArgumentOutOfRange_SmallCapacity,
        Argument_InvalidOffLen,
        ArgumentOutOfRange_BiggerThanCollection,
        Serialization_MissingKeys,
        Serialization_NullKey,
        NotSupported_KeyCollectionSet,
        NotSupported_ValueCollectionSet,
        InvalidOperation_NullArray,
        InvalidOperation_HSCapacityOverflow,
        NotSupported_StringComparison,
        ConcurrentCollection_SyncRoot_NotSupported,
        ArgumentException_OtherNotArrayOfCorrectLength,
        ArgumentOutOfRange_EndIndexStartIndex,
        ArgumentOutOfRange_HugeArrayNotSupported,
        Argument_AddingDuplicate,
        Argument_InvalidArgumentForComparison,
        Arg_LowerBoundsMustMatch,
        Arg_MustBeType,
        InvalidOperation_IComparerFailed,
        NotSupported_FixedSizeCollection,
        Rank_MultiDimNotSupported,
        Arg_TypeNotSupported,
        Serialization_Pooled_NullKey,
        Serialization_Pooled_MissingData,
        Destination_Span_Shorter_Than_List_To_Copy,
        Destination_Span_Shorter_Than_Count,
        Queue_Empty,
        Collection_Modified,
        Enumeration_NotStarted,
        Enumeration_Ended,
    }
}
