using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static partial class ComponentExtensions
{
#if UNITY_EDITOR
    static bool debug = false;
    public static bool IsPrefab(this Component obj)
    {
        return !obj.IsNullOrDestroy() && obj.gameObject.IsPrefab();
    }
#endif

    public static T Instantiate<T>(this T obj) where T : Component
    {
        if (obj.IsNullOrDestroy()) return default(T);
        return UnityEngine.GameObject.Instantiate(obj) as T;
    }

    public static void Destroy<T>(this T obj) where T : Component
    {
        if (obj.IsNullOrDestroy()) return;
        UnityEngine.GameObject.Destroy(obj);
    }

    public static void DestroyGO<T>(this T obj) where T : Component
    {
        if (obj.IsNullOrDestroy()) return;
        UnityEngine.GameObject.Destroy(obj.gameObject);
    }

    /// <summary>
    /// Возвращает go от attachRigidbody или собственно  коллайдера(если rigidbody = null)
    /// </summary>
    /// <param name="cld"></param>
    /// <param name=""></param>
    /// <returns></returns>
    public static GameObject GameObject(this Collider cld)
    {
        if (cld.IsNullOrDestroy()) return null;
        var rb = cld.attachedRigidbody;
        return rb == null ? cld.gameObject : rb.gameObject;
    }

    public static void Reactive(this Component obj)
    {
        if (obj.IsNullOrDestroy()) return;
        obj.gameObject.SetActive(false);
        obj.gameObject.SetActive(true);
    }

    public static bool IsNullOrDestroy(this Component obj)
    {
        return obj == null || obj.gameObject == null;
    }

    public static bool IsFullActiveGO(this Component obj)
    {
        if (obj.IsNullOrDestroy()) return false;
        var go = obj.gameObject;
        return go != null && go.activeInHierarchy;
    }

    public static void FindExclusiveObject<T>(this Component component, ref T obj) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        var _go = component.gameObject;
        var prefabType = PrefabUtility.GetPrefabType(_go);
        //Is the selected gameobject a prefab?
        if (prefabType != PrefabType.PrefabInstance && prefabType != PrefabType.DisconnectedPrefabInstance && prefabType != PrefabType.MissingPrefabInstance && prefabType != PrefabType.None)
        {
#if UNITY_EDITOR
            //Debug.LogError(mono.GetType()+"on +"+ mono.gameObject+ " PrefabType=" + prefabType);
#endif
            return;
        }
#endif
        if (Application.isPlaying) return;
        if (obj != null) return;


        var _objs = UnityEngine.Object.FindObjectsOfType<T>();
#if UNITY_EDITOR
        if (debug) Debug.LogError("Find:_obj.Length=" + _objs.Length);
#endif

        if (_objs.Length <= 0)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;
            var roots = scene.GetRootGameObjects();
            System.Collections.Generic.List<T> list = new System.Collections.Generic.List<T>(100);
            System.Collections.Generic.List<T> list2 = new System.Collections.Generic.List<T>(100);
            if (roots != null)
            {
                for (int i = 0; i < roots.Length; i++)
                {
                    //GetComponentsInChildren сам вызывает Clear, но на всякий
                    list2.Clear();
                    roots[i].GetComponentsInChildren<T>(true, list2);
                    list.AddRange(list2);
                    //list.AddRange(roots[i].GetComponentsInChildren<T>(true));
                }
            }
            _objs = list.ToArray();
#if UNITY_EDITOR
            if (debug) Debug.LogError("Root:_obj.Length=" + _objs.Length);
#endif
        }

        if (_objs.Length <= 0)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            if (roots != null)
            {
                for (int i = 0; i < roots.Length; i++)
                {
                    _objs = roots[i].GetComponentsInChildren<T>(true);
                }
            }
            Debug.LogError(component.GetType() + " error: Оbjects of " + typeof(T) + " type is not exist");
            return;
        }
        else if (_objs.Length > 1)
        {
            Debug.LogError(component.GetType() + " error: Оbjects of " + typeof(T) + " type is more than one");
        }
        obj = _objs[0];
#if UNITY_EDITOR
        if (obj == null) Debug.LogError(component.GetType() + " error: Object is null");
#endif

    }
}
