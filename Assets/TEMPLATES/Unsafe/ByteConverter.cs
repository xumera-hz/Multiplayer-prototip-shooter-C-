using System;
using System.Collections.Generic;

//unsafe
public static class ByteConverter
{
    const int INT_SIZE = 4;
    const int CHAR_SIZE = 2;
    const int ERROR = -1;
    const int SUCCESS = 0;
    const int MAX_LENGTH_ARRAY_FOR_CONVERT_BYTE_ARRAY = int.MaxValue;
    public enum RefArrayOptions : int
    {

        Fix = 1, // если размер входного массива меньше заявленного количества элементов, то выдается ошибка
        Resize = 2, // если размер входного массива не совпадает с заявленным количеством элементов, то пересоздает массив
        Flexible = 3 // массив пересоздается, только если размер входного массива строго меньше заявленного количество элементов
    }

    static int GetMaxLen(int sizeElem, int sizeCount)
    {
        return (MAX_LENGTH_ARRAY_FOR_CONVERT_BYTE_ARRAY - sizeCount) / sizeElem;
    }

    static int ApplyRefArrayOptions<T>(ref T[] arg1, ref T[] outArray, int outLenArray, RefArrayOptions options)
    {
        int res = SUCCESS;
        switch (options)
        {
            case RefArrayOptions.Resize:
                if (arg1 == null || arg1.Length != outLenArray) outArray = new T[outLenArray];
                else outArray = arg1;
                break;
            case RefArrayOptions.Flexible:
                if (arg1 == null || arg1.Length < outLenArray) outArray = new T[outLenArray];
                else outArray = arg1;
                break;
            case RefArrayOptions.Fix:
                if (arg1 == null || arg1.Length < outLenArray) res = ERROR;
                break;
        }
        return res;
    }


    public unsafe static int ToBytes(this string arg1, byte[] bytes, int offset)
    {
        var str = arg1;
        if (str == null) str = string.Empty;
        //Проверяем, что (количество элем-ов + сами элем-ты) в байтах поместятся в наш массив байт
        int size = (int)CHAR_SIZE;
        int realSize = sizeof(char);
        if (size < 1 || size > realSize) return ERROR;
        int sizeIntCount = (int)INT_SIZE;
        int lenArray = str.Length;
        int lenBytes = lenArray * size + sizeIntCount;
        int num = offset;
        int tmp2 = int.MaxValue - num;
        if (tmp2 < lenBytes) return ERROR;
        if ((num + lenBytes) > bytes.Length) return ERROR;
        //Кладем и проверяем количество элем-ов в массив байт
        //проверить что берем первый байт//не понятно что проверять
        fixed (byte* pB = bytes) StringToBytes(str, bytes, pB, num, size, sizeIntCount);
        return lenBytes;
    }

    public unsafe static int ToInt(this byte[] bytes, int offset, out int arg1)
    {
        fixed (byte* pB = bytes)
        {
            int tmp = 0;
            byte* vp = (byte*)&tmp;
            int result = BytesToElem(pB, bytes.Length, offset, vp, sizeof(int), (int)INT_SIZE);
            arg1 = tmp;
            return result == SUCCESS ? INT_SIZE : result;
        }
    }
    public unsafe static int ToStringArray(this byte[] bytes, int offset, ref string[] arg1, out int outLenArray, RefArrayOptions options = RefArrayOptions.Resize)
    {
        outLenArray = 0;
        int lenArray;
        int realSize = sizeof(char);
        int res = CheckToArray(bytes, offset, out lenArray, realSize, CHAR_SIZE, INT_SIZE);
        if (res != SUCCESS) return res;
        offset += INT_SIZE;
        int lenBytes = INT_SIZE;
        string[] array = null;
        res = ApplyRefArrayOptions(ref arg1, ref array, lenArray, options);
        if (res != SUCCESS) return res;
        outLenArray = lenArray;
        if (lenArray > 0)
        {
            for (int i = 0; i < lenArray; i++)
            {
                res = bytes.ToString(offset, out array[i]);
                if (res == ERROR) return ERROR;
                offset += res;
                lenBytes += res;
            }
        }
        if (res != ERROR) arg1 = array;
        return res != ERROR ? lenBytes : res;
    }

    public unsafe static int GetLen(this string[] arg1)
    {
        int size = (int)CHAR_SIZE;
        int realSize = sizeof(char);
        if (size < 1 || size > realSize) return ERROR;
        int sizeIntCount = (int)INT_SIZE;
        int stringLenSize = (int)INT_SIZE;

        string str;
        int maxLen = GetMaxLen(size, stringLenSize);
        int curLen = maxLen;
        //Собираем полный размер по размерам всех строк
        for (int i = 0; i < arg1.Length; i++)
        {
            str = arg1[i];
            int arraySize = str == null ? 0 : str.Length;
            if (arraySize > curLen) return ERROR;
            curLen -= arraySize;
        }
        int lenArray = arg1.Length;
        int lenBytes = (maxLen - curLen) * size + sizeIntCount * lenArray + stringLenSize;
        return lenBytes;
    }

    public unsafe static int GetLen(this List<string> arg1)
    {
        int size = (int)CHAR_SIZE;
        int realSize = sizeof(char);
        if (size < 1 || size > realSize) return ERROR;
        int sizeIntCount = (int)INT_SIZE;
        int stringLenSize = (int)INT_SIZE;

        string str;
        int maxLen = GetMaxLen(size, stringLenSize);
        int curLen = maxLen;
        //Собираем полный размер по размерам всех строк
        int len = arg1.Count;
        for (int i = 0; i < len; i++)
        {
            str = arg1[i];
            int arraySize = str == null ? 0 : str.Length;
            if (arraySize > curLen) return ERROR;
            curLen -= arraySize;
        }
        int lenBytes = (maxLen - curLen) * size + sizeIntCount * len + stringLenSize;
        return lenBytes;
    }

    public unsafe static int ToBytes(this string[] arg1, byte[] bytes, int offset)
    {
        /*int size = (int)CHAR_SIZE;
        int realSize = sizeof(char);
        if (size < 1 || size > realSize) return ERROR;
        int sizeIntCount = (int)INT_SIZE;
        int stringLenSize = (int)INT_SIZE;

        int i = 0;
        string str;
        int maxLen = GetMaxLen(size, stringLenSize);
        int curLen = maxLen;
        //Собираем полный размер по размерам всех строк
        for (; i < arg1.Length; i++)
        {
            str = arg1[i];
            int arraySize = str == null ? 0 : str.Length;
            if (arraySize > curLen) return ERROR;
            curLen -= arraySize;
        }
        int lenArray = arg1.Length;
        int lenBytes = (maxLen - curLen) * size + sizeIntCount * lenArray + stringLenSize;*/

        int size = (int)CHAR_SIZE;
        int sizeIntCount = (int)INT_SIZE;
        int stringLenSize = (int)INT_SIZE;
        int lenArray = arg1.Length;

        int lenBytes = GetLen(arg1);
        if (lenBytes == ERROR) return ERROR;
        int num = offset;
        int canCount = MAX_LENGTH_ARRAY_FOR_CONVERT_BYTE_ARRAY - num;
        if (canCount < lenBytes) return ERROR;
        if ((num + lenBytes) > bytes.Length) return ERROR;
        fixed (byte* pB = bytes)
        {
            byte* vp = (byte*)&lenArray;
            for (int i = 0; i < stringLenSize; i++) *(pB + (num + i)) = *(vp + i);
            num += stringLenSize;
            for (int i = 0; i < arg1.Length; i++)
            {
                int len = StringToBytes(arg1[i], bytes, pB, num, size, sizeIntCount);
                num = num + len;
            }
        }
        return lenBytes;
    }
    public unsafe static int ToString(this byte[] bytes, int offset, out string arg1)
    {
        arg1 = null;
        int lenArray;
        int realSize = sizeof(char);
        int res = CheckToArray(bytes, offset, out lenArray, realSize, CHAR_SIZE, INT_SIZE);
        if (res != SUCCESS)
        {
            return res;
        }
        int lenBytes = INT_SIZE;
        offset += INT_SIZE;
        if (lenArray > 0)
        {
            char* pArg = stackalloc char[lenArray];
            fixed (byte* pBytes = bytes) res = ToArray(bytes, pBytes, null, (byte*)pArg, lenArray, offset, (int)CHAR_SIZE);
            if (res == SUCCESS)
            {
                arg1 = new string(pArg, 0, lenArray);
                offset += lenArray * CHAR_SIZE;
                lenBytes += lenArray * CHAR_SIZE;
            }
            else arg1 = string.Empty;
        }
        else arg1 = string.Empty;
        return res == SUCCESS ? lenBytes : res;
    }


    unsafe static int ToArray(Array source, byte* pSource, Array dest, byte* pDest, int lenDest, int offset, int size)
    {
        int num = offset;
        int lenBytes = lenDest * size;
        int res = SUCCESS;
        if (lenDest > 0)
        {
            if (dest != null)
            {
                if (source.GetType().GetElementType().IsPrimitive && dest.GetType().GetElementType().IsPrimitive) Buffer.BlockCopy(source, num, dest, 0, lenBytes);
                else return ERROR;
            }
            else
            {
                for (int i = 0; i < lenBytes; i++)
                {
                    *(pDest + i) = *(pSource + (offset + i));
                }
            }
        }
        //if (res == SUCCESS) offset = offset + lenBytes;
        return res;
    }
    unsafe static int ToBytes(Array source, byte* pSource, Array dest, byte* pDest, int realSize, int offset, int sizeElem, int sizeIntCount)
    {
        if (sizeElem < 1 || sizeElem > realSize) return ERROR;
        int lenArray = source.Length;

        int maxLen = GetMaxLen(sizeElem, sizeIntCount);
        if (lenArray > maxLen) return ERROR;

        //if (lenArray <= 0)
        //{
        //    offset += sizeIntCount;
        //    return SUCCESS;
        //}

        int lenBytes = lenArray * sizeElem + sizeIntCount;
        int num = offset;
        int canCount = MAX_LENGTH_ARRAY_FOR_CONVERT_BYTE_ARRAY - num;
        if (canCount < lenBytes) return ERROR;
        if ((uint)(num + lenBytes) > dest.Length) return ERROR;
        byte* vp = (byte*)&lenArray;
        for (int i = 0; i < sizeIntCount; i++) *(pDest + (num + i)) = *(vp + i);
        num += sizeIntCount;
        //Кладем все элементы по порядку
        if (lenArray > 0)
        {
            if (source.GetType().IsPrimitive && dest.GetType().IsPrimitive) Buffer.BlockCopy(source, 0, dest, num, lenBytes - sizeIntCount);
            else return ERROR;
        }
        offset = offset + lenBytes;
        return SUCCESS;
    }
    unsafe static int BytesToElem(byte* source, int lenSource, int offsetSource, byte* dest, int realSizeDest, int compressSizeDest)
    {
        if (compressSizeDest > realSizeDest) return ERROR;
        int num = offsetSource;
        int canCount = MAX_LENGTH_ARRAY_FOR_CONVERT_BYTE_ARRAY - num;
        if (canCount < compressSizeDest) return ERROR;
        if ((num + compressSizeDest) > lenSource) return ERROR;
        for (int i = 0; i < compressSizeDest; i++) *(dest + i) = *(source + (num + i));
        //offsetSource += compressSizeDest;
        return SUCCESS;
    }
    unsafe static int CheckToArray(byte[] bytes, int offset, out int lenArray, int realSize, int sizeElem, int sizeCount)
    {
        lenArray = 0;
        if (offset >= bytes.Length) return ERROR;
        int size = (int)sizeElem;
        if (size < 1 || size > realSize) return ERROR;
        int num = offset;
        int result = bytes.ToInt(num, out lenArray);
        if (result == ERROR) return ERROR;
        int sizeIntCount = (int)sizeCount;
        int delta = bytes.Length - offset;
        int lenBytes = lenArray * size + sizeIntCount;
        if (lenBytes > delta) return ERROR;
        return SUCCESS;
    }
    unsafe static int StringToBytes(string source, Array dest, byte* pDest, int offset, int size, int sizeIntCount)
    {
        int lenArray = source == null ? 0 : source.Length;
        //Кладем и проверяем количество элем-ов в массив байт
        //int* bb = (int*)pDest;
        byte* vp = (byte*)&lenArray;
        for (int i = 0; i < sizeIntCount; i++) *(pDest + (offset + i)) = *(vp + i);
        offset += sizeIntCount;
        int count = lenArray * size;
        if (count > 0)
        {
            //Кладем все элементы по порядку
            fixed (char* pSTR = source)
            {
                vp = (byte*)pSTR;
                for (int i = 0; i < count; i++)
                {
                    *(pDest + (offset + i)) = *(vp + i);
                }
            }
        }
        return count + sizeIntCount;
    }
}
