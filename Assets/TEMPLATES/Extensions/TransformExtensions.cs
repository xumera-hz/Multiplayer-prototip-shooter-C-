using UnityEngine;
using System.Collections.Generic;

public enum TypeTargetComponents { None, Self, Childs, Parent, Manual }

public static partial class TransformExtensions
{
    /// <summary>
    /// Поиск элементов через аниматор
    /// </summary>
    /// <param name="thisTF">объект, на котором должен висеть аниматор</param>
    /// <param name="_name"></param>
    /// <param name="bone"></param>
    /// <param name="include">Рекурсивный поиск, может быть дорогим</param>
    /// <returns></returns>
    public static Transform FindAnimTF(this Transform thisTF, string _name, HumanBodyBones bone, bool include = false)
    {
        Animator anim = thisTF.GetComponent<Animator>();
        if (anim == null) return null;
        return InnerFindTF(anim.GetBoneTransform(bone), _name, include);
    }

    public static Transform FindTF(this Transform thisTF, string _name, bool include = false)
    {
        return InnerFindTF(thisTF, _name, include);
    }


    static Transform InnerFindTF(Transform TF, string _name, bool include)
    {
        if (TF == null) return null;
        return include ? RecursiveFindTF(TF, _name) : TF.Find(_name);
    }

    static Transform InnerRecursiveFindTF(Transform TF, string _name, ref bool res)
    {
        //if (TF.childCount == 0) res = false;
        var tmp = TF.Find(_name);
        res = !(tmp == null);
        if (res) return tmp;
        for (int i = TF.childCount - 1; i >= 0; i--)
        {
            tmp = InnerRecursiveFindTF(TF.GetChild(i), _name, ref res);
            if (res) return tmp;
        }
        return null;

    }

    public static Transform RecursiveFindTF(this Transform thisTF, string _name)
    {
        bool res = false;
        return InnerRecursiveFindTF(thisTF, _name, ref res);
    }

    public static Transform CreateChild(this Transform tf, string _name, Vector3 localPos = default(Vector3))
    {
        Transform tmp = new GameObject(_name).transform;
        tmp.SetParent(tf, true);
        tmp.localPosition = localPos;
        return tmp;
    }

    public static void GetComponentsList<T>(this Transform targTF, TypeTargetComponents type, List<T> cache, bool includeInactive = false)
    {
        if (targTF == null || cache == null) return;
        var targGO = targTF.gameObject;
        cache.Clear();
        switch (type)
        {
            case TypeTargetComponents.Self:
                targGO.GetComponents(cache);
                break;
            case TypeTargetComponents.Childs:
                targGO.GetComponentsInChildren(includeInactive, cache);
                break;
            case TypeTargetComponents.Parent:
                var par = targTF.parent;
                if (par == null) par = targTF;
                targTF = par;
                targGO = targTF.gameObject;
                targGO.GetComponentsInChildren(includeInactive, cache);
                break;
        }
    }

    public static bool IsDestroy(this Transform targTF)
    {
        return targTF == null || targTF.gameObject == null;
    }

    #region Rotation

    /// <summary>
    /// Перевод поворота from local to world
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="rot"></param>
    /// <returns></returns>
    public static Quaternion TransformRotation(this Transform tf, Quaternion rot)
    {
        Transform par = tf.parent;
        if (par == null) return rot;
        return par.rotation * rot;
    }
    /// <summary>
    /// Перевод поворота from world to local
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="rot"></param>
    /// <returns></returns>
    public static Quaternion InverseTransformRotation(this Transform tf, Quaternion rot)
    {
        Transform par = tf.parent;
        if (par == null) return rot;
        return Quaternion.Inverse(par.rotation) * rot;
    }

    #endregion
}

public static partial class RectTransformExtensions
{
    //Выставление автоматически анчоров по размеру прямоугольника
    #region AutoAnchores

#if UNITY_EDITOR
    #region EditorMenu
    [UnityEditor.MenuItem("MyAssets/AutoAnchors", true)]
    static bool ValidateAutoAnchors() { return UnityEditor.Selection.activeGameObject != null; }
    [UnityEditor.MenuItem("MyAssets/AutoAnchors")]
    static void SetAutoAnchors() { AutoAnchors(UnityEditor.Selection.activeGameObject); }
    #endregion
#endif

    public static void AutoAnchors(this GameObject obj)
    {
        if (obj == null) return;
        AutoAnchors(obj.GetComponent<RectTransform>());
    }

    public static void AutoAnchors(this Transform obj)
    {
        AutoAnchors(obj as RectTransform);
    }

    public static void AutoAnchors(this RectTransform targ)
    {
        if (targ == null)
        {
#if UNITY_EDITOR
            Debug.LogError(typeof(RectTransformUtility) + " error: AutoAnchors target is null");
#endif
            return;
        }
        if (targ.parent == null)
        {
#if UNITY_EDITOR
            Debug.LogError(typeof(RectTransformUtility) + " error: AutoAnchors target's parent is null on" + targ);
#endif
            return;
        }
        var par = targ.parent as RectTransform;
        if (par == null)
        {
#if UNITY_EDITOR
            Debug.LogError(typeof(RectTransformUtility) + " error: AutoAnchors target's parent dont have " + typeof(RectTransform) + " component on" + targ);
#endif
            return;
        }
        var size1 = par.rect.size;
        var pos1 = par.localPosition;
        var size2 = targ.rect.size;
        var pos2 = targ.localPosition;
        float x = (size1.x < 1e-05f && size1.x > -1e-05f) ? 0f : (pos2.x / size1.x);
        float y = (size1.y < 1e-05f && size1.y > -1e-05f) ? 0f : (pos2.y / size1.y);
        var offsetPercents = new Vector2(x, y);
        x = (size1.x < 1e-05f && size1.x > -1e-05f) ? 0f : (size2.x / size1.x);
        y = (size1.y < 1e-05f && size1.y > -1e-05f) ? 0f : (size2.y / size1.y);
        var sizePercents = new Vector2(x, y);
        //var pivotOffset = targ.pivot - (Vector2.one * 0.5f);
        //pivotOffset.x *= sizePercents.x;
        //pivotOffset.y *= sizePercents.y;
        //var min = (par.pivot) + offsetPercents - sizePercents * 0.5f - pivotOffset;
        var pivotOffset = targ.pivot;
        pivotOffset.x *= sizePercents.x;
        pivotOffset.y *= sizePercents.y;
        var min = (par.pivot) + offsetPercents - pivotOffset;
        var max = min + sizePercents;
        min = new Vector2(Mathf.Clamp01(min.x), Mathf.Clamp01(min.y));
        max = new Vector2(Mathf.Clamp01(max.x), Mathf.Clamp01(max.y));
        targ.anchorMin = Vector2.zero;
        targ.anchorMax = Vector2.one;
        targ.anchorMin = min;
        targ.anchorMax = max;
        targ.localPosition = pos2;
        targ.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size2.x);
        targ.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size2.y);
    }

    #endregion
}

