using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static partial class GameObjectExtensions {

#if UNITY_EDITOR
    public static bool IsPrefab(this GameObject go)
    {
        var prefabType = PrefabUtility.GetPrefabType(go);
        return prefabType != PrefabType.PrefabInstance && prefabType != PrefabType.DisconnectedPrefabInstance && prefabType != PrefabType.MissingPrefabInstance && prefabType != PrefabType.None;
    }
#endif

    public static T AddComponentEx<T>(this GameObject go, bool checkExist = true) where T : Component
    {
        if (go == null) return null;
        T cmp = null;
        if (checkExist) cmp = go.GetComponent<T>();
        if (cmp == null) cmp = go.AddComponent<T>();
        return cmp;
    }

    public static GameObject Instantiate(this GameObject obj)
    {
        if (obj == null) return null;
        return GameObject.Instantiate(obj) as GameObject;
    }

}
