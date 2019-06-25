using System;
using System.Runtime.InteropServices;


/*[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UnsafeStructToByteArray
{
    public static readonly int SIZE = Marshal.SizeOf(typeof(PlayerData));

    public unsafe bool UnsafeCopyTo(byte[] array, int offset = 0)
    {
        var t = this;
        byte* p1 = (byte*)&t;
        return array.CopyTo(p1, SIZE, offset);
    }

    public unsafe bool UnsafeCopyFrom(byte[] array, int offset = 0)
    {
        var t = this;
        byte* p1 = (byte*)&t;
        bool res = array.CopyFrom(p1, SIZE, offset);
        if (res) this = t;
        return res;
    }
}*/

public static class ArrayUnsafeUtils
{
    static int GetSize<T>()
    {
        return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
    }

    public static unsafe bool CopyTo<T>(this byte[] dest, byte* srcPtr, int destOffset = 0, int srcOffset = 0)
    {
        return CopyTo(dest, srcPtr, GetSize<T>(), destOffset, srcOffset);
    }

    public static unsafe bool CopyTo(this byte[] dest, byte* srcPtr, int count, int destOffset = 0, int srcOffset = 0)
    {
        if (dest == null) return false;

        if (count == 0) return true;
        if (destOffset + count > dest.Length) return false;
        byte* p1 = srcPtr;
        fixed (byte* p2 = &dest[destOffset])
        {
            int half = count / 2;
            int size2 = half + half;
            for (int i = 0; i < size2; i += 2)
            {
                p2[i] = p1[i];
                p2[i + 1] = p1[i + 1];
            }
            if (size2 != count) p2[count - 1] = p1[count - 1];
        }
        return true;
    }

    public static unsafe bool CopyFrom<T>(this byte[] src, byte* destPtr, int srcOffset = 0, int destOffset = 0)
    {
        return CopyFrom(src, destPtr, GetSize<T>(), srcOffset, destOffset);
    }

    public static unsafe bool CopyFrom(this byte[] src, byte* destPtr, int count, int srcOffset = 0, int destOffset = 0)
    {
        if (src == null) return false;

        if (count == 0) return true;
        if (srcOffset + count > src.Length) return false;
        byte* p1 = destPtr;
        fixed (byte* p2 = &src[srcOffset])
        {
            int half = count / 2;
            int size2 = half + half;
            for (int i = 0; i < size2; i += 2)
            {
                p1[i] = p2[i];
                p1[i + 1] = p2[i + 1];
            }
            if (size2 != count) p1[count - 1] = p2[count - 1];
        }
        return true;
    }
}
