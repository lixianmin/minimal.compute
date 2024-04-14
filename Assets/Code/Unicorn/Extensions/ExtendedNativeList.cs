/********************************************************************
created:    2024-03-18
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

using Unicorn.Collections;
using Unity.Collections;

namespace Unicorn
{
    public static class ExtendedNativeList
    {
        public static unsafe void AddRange<T>(this NativeList<T> my, T[] array) where T : unmanaged
        {
            if (array != null)
            {
                fixed (T* ptr = array)
                {
                    my.AddRange(ptr, array.Length);
                }
            }
        }

        public static unsafe void AddRange<T>(this NativeList<T> my, Slice<T> slice) where T : unmanaged
        {
            if (slice != null)
            {
                fixed (T* ptr = slice.Items)
                {
                    my.AddRange(ptr, slice.Size);
                }
            }
        }
    }
}