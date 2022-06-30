using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Game.Runtime
{
    public class DemoRuntime : MonoBehaviour
    {
        private void Start()
        {
            var x = new FixedBuffer_4<string>();

            for (var i = 0; i < x.Length; i++)
                x.SetUnsafe(i, i.ToString());

            for (var i = 0; i < x.Length; i++)
                Debug.Log(x.GetUnsafe(i));

        }
    }

    [UnsafeValueType, StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct FixedBuffer_4<T>
    {
        private T _e0;
        private T _e1;
        private T _e2;
        private T _e3;

        public const int LENGTH = 4;

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => LENGTH;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= LENGTH)
                    throw new IndexOutOfRangeException();

                return Unsafe.Add<T>(ref _e0, index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)index >= LENGTH)
                    throw new IndexOutOfRangeException();

                Unsafe.Add<T>(ref _e0, index) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetUnsafe(int index)
        {
            return Unsafe.Add<T>(ref _e0, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetUnsafe(int index, T value)
        {
            Unsafe.Add<T>(ref _e0, index) = value;
        }
    }
}
