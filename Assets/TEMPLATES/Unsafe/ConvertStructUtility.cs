using UnityEngine;
using System;
using System.Runtime.InteropServices;

//Marshal
public static partial class ConvertStructUtility
{
    /// <summary>
    /// Кладет структуру в массив байт
    /// Только для структур(MarshalAs for pointers)
    /// </summary>
    public static void CopyTo<T>(this T structure, byte[] array, int offset = 0)
    {
        var type = structure.GetType();
        int size = Marshal.SizeOf(type);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(structure, ptr, false);
        //Debug.LogError("CopyTo start=" + offset + " size=" + size + " availableLen=" + array.Length);
        Marshal.Copy(ptr, array, offset, size);
        Marshal.FreeHGlobal(ptr);
    }
    public static T ToStruct<T>(this byte[] array, int offset = 0)
    {
        return (T)ToStruct(array, typeof(T), offset);
    }
    /// <summary>
    /// Создает структуру из массива байт
    /// Только для структур(MarshalAs for pointers) 
    /// </summary>
    public static object ToStruct(this byte[] array, System.Type type, int offset = 0)
    {
        int size = Marshal.SizeOf(type);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        //Debug.LogError("ToStruct start=" + offset + " size=" + size + " availableLen=" + array.Length);
        Marshal.Copy(array, offset, ptr, size);
        object obj = Marshal.PtrToStructure(ptr, type);
        Marshal.FreeHGlobal(ptr);
        return obj;
    }

    /// <summary>
    /// Кладет структуру в Writer(обертка над массивом байт)
    /// Только для структур(MarshalAs for pointers) 
    /// </summary>
    public static bool PutInWriter(this LiteNetLib.Utils.NetDataWriter writer, object obj, bool acceptOffset = true, bool reset = false)
    {
        try
        {
            if (reset) writer.Reset();
            int size = Marshal.SizeOf(obj.GetType());
            writer.ResizeIfNeed(writer.Length + size);
            obj.CopyTo(writer.Data, writer.Length);
            if (acceptOffset) writer.AddOffset(size);
            return true;
        }
        catch (Exception e) { Debug.LogError("PutInWriter is bad=" + e); }
        return false;
    }
    /// <summary>
    /// Считываем структуру в Reader(обертка над массивом байт)
    /// Только для структур(MarshalAs for pointers) 
    /// </summary>
    public static bool FromReader<T>(this LiteNetLib.Utils.NetDataReader reader, out T obj, bool acceptOffset = true)
    {
        try
        {
            int size = Marshal.SizeOf(typeof(T));
            obj = reader.RawData.ToStruct<T>(reader.Position);
            if (acceptOffset) reader.AddOffset(size);
            return true;
        }
        catch (Exception e) { Debug.LogError("FromReader is bad=" + e); }
        obj = default(T);
        return false;
    }
}
