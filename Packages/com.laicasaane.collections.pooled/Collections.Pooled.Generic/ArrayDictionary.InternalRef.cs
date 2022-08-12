using System;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    partial class ArrayDictionary<TKey, TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetOrAdd(TKey key)
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(key, out findIndex);

            _values[findIndex] = default;

            return ref _values[findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetOrAdd(in TKey key)
        {
            if (TryFindIndex(in key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(in key, out findIndex);

            _values[findIndex] = default;

            return ref _values[findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetOrAdd(TKey key, Func<TValue> builder)
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(key, out findIndex);

            _values[findIndex] = builder();

            return ref _values[findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetOrAdd(in TKey key, Func<TValue> builder)
        {
            if (TryFindIndex(in key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(in key, out findIndex);

            _values[findIndex] = builder();

            return ref _values[findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetOrAdd<W>(TKey key, FuncRef<W, TValue> builder, ref W parameter)
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(key, out findIndex);

            _values[findIndex] = builder(ref parameter);

            return ref _values[findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetOrAdd<W>(in TKey key, FuncRef<W, TValue> builder, ref W parameter)
        {
            if (TryFindIndex(in key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(in key, out findIndex);

            _values[findIndex] = builder(ref parameter);

            return ref _values[findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue RecycleOrAdd<TValueProxy>(TKey key, Func<TValueProxy> builder, ActionRef<TValueProxy> recycler)
            where TValueProxy : class, TValue
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(key, out findIndex);

            if (_values[findIndex] == null)
                _values[findIndex] = builder();
            else
                recycler(ref Unsafe.As<TValue, TValueProxy>(ref _values[findIndex]));

            return ref _values[findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue RecycleOrAdd<TValueProxy>(in TKey key, Func<TValueProxy> builder, ActionRef<TValueProxy> recycler)
            where TValueProxy : class, TValue
        {
            if (TryFindIndex(in key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(in key, out findIndex);

            if (_values[findIndex] == null)
                _values[findIndex] = builder();
            else
                recycler(ref Unsafe.As<TValue, TValueProxy>(ref _values[findIndex]));

            return ref _values[findIndex];
        }

        /// <summary>
        /// RecycledOrCreate makes sense to use on dictionaries that are fast cleared and use objects
        /// as value. Once the dictionary is fast cleared, it will try to reuse object values that are
        /// recycled during the fast clearing.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="builder"></param>
        /// <param name="recycler"></param>
        /// <param name="parameter"></param>
        /// <typeparam name="TValueProxy"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue RecycleOrAdd<TValueProxy, U>(TKey key
            , FuncRef<U, TValue> builder
            , ActionRef<TValueProxy, U> recycler
            , ref U parameter
        )
            where TValueProxy : class, TValue
        {
            if (TryFindIndex(key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(key, out findIndex);

            if (_values[findIndex] == null)
                _values[findIndex] = builder(ref parameter);
            else
                recycler(ref Unsafe.As<TValue, TValueProxy>(ref _values[findIndex]), ref parameter);

            return ref _values[findIndex];
        }

        /// <summary>
        /// RecycledOrCreate makes sense to use on dictionaries that are fast cleared and use objects
        /// as value. Once the dictionary is fast cleared, it will try to reuse object values that are
        /// recycled during the fast clearing.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="builder"></param>
        /// <param name="recycler"></param>
        /// <param name="parameter"></param>
        /// <typeparam name="TValueProxy"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue RecycleOrAdd<TValueProxy, U>(in TKey key
            , FuncRef<U, TValue> builder
            , ActionRef<TValueProxy, U> recycler
            , ref U parameter
        )
            where TValueProxy : class, TValue
        {
            if (TryFindIndex(in key, out var findIndex) == true)
            {
                return ref _values[findIndex];
            }

            TryGetIndex(in key, out findIndex);

            if (_values[findIndex] == null)
                _values[findIndex] = builder(ref parameter);
            else
                recycler(ref Unsafe.As<TValue, TValueProxy>(ref _values[findIndex]), ref parameter);

            return ref _values[findIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read 
        //constant states) because it will be used in multi-threaded parallel code
        internal ref TValue GetDirectValueByRef(int index)
        {
            return ref _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetValueByRef(TKey key)
        {
#if DEBUG
            if (TryFindIndex(key, out var findIndex) == true)
                return ref _values[findIndex];

            ThrowHelper.ThrowKeyNotFoundException(key);
            return ref Unsafe.NullRef<TValue>();
#else
            //Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
            TryFindIndex(key, out var findIndex);

            return ref _values[(int) findIndex];
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetValueByRef(in TKey key)
        {
#if DEBUG
            if (TryFindIndex(in key, out var findIndex) == true)
                return ref _values[findIndex];

            ThrowHelper.ThrowKeyNotFoundException(key);
            return ref Unsafe.NullRef<TValue>();
#else
            //Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
            TryFindIndex(in key, out var findIndex);

            return ref _values[(int) findIndex];
#endif
        }
    }
}