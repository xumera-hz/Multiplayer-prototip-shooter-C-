using UnityEngine;

public static partial class Vector3Extensions
{
    public static bool IsAbsoluteZero(this Vector3 v3)
    {
        return v3.x == 0f && v3.y == 0f && v3.z == 0f;
    }
}
