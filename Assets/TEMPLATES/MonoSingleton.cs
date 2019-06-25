using UnityEngine;
using System.Threading;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    //Можно выставить, что объект может автоматически создаваться, если нет его на сцене
    protected static bool AutoCreated = false;
    protected static string m_RootName;
    protected static T m_I;
    protected static int m_InstanceID;
    protected static int m_Destroyed = -1;
    private static readonly object syncRoot = new System.Object();

    static void InitInstance(GameObject go, bool local = true)
    {
        if (m_I != null) return;
#if UNITY_EDITOR
        if (m_Destroyed == 1)
        {
            Debug.LogError(typeof(T)+" Init after destroyed");
        }
#endif

        //Возможно тут Unity в многопоточность не сможет)
        Monitor.Enter(syncRoot);
        T temp;
        if (local)
        {
            temp = go.GetComponent<T>();
        }
        else
        {
            temp = new GameObject(typeof(T).ToString()).AddComponent<T>();
#if UNITY_EDITOR
            //if (string.IsNullOrEmpty(m_RootName)) m_RootName = ConstantObjects.ROOT_CONTROLLERS_GAME_MISC;
            SetParent(temp);
#endif
        }
#if UNITY_EDITOR
        //temp.gameObject.name = typeof(T).ToString();
#endif
        Interlocked.Exchange(ref m_I, temp);
        Monitor.Exit(syncRoot);
        if (temp != null) m_InstanceID = temp.GetInstanceID();

        #region Old

            //        lock (syncRoot)
            //        {
            //            if (local)
            //            {
            //                if (m_I == null)
            //                {
            //                    m_I = go.GetComponent<T>();
            //                    go.name = typeof(T).ToString();
            //                }
            //            }
            //            else
            //            {
            //                if (m_I == null)
            //                {
            //                    m_I = new GameObject(typeof(T).ToString()).AddComponent<T>();
            //#if UNITY_EDITOR
            //                    if (string.IsNullOrEmpty(m_RootName)) m_RootName = ConstantObjects.ROOT_CONTROLLERS_GAME_MISC;
            //                    m_I.transform.SetParent(ConstantObjects.GetGlobalObject(m_RootName));
            //#endif
            //                }
            //            }
            //        }

            #endregion
    }

    static void SetParent<T2>(T2 temp) where T2 : MonoBehaviour
    {
        //temp.transform.SetParent(m_RootName, true);
    }

    public static bool Is(Component obj)
    {
        return Can && obj == m_I;
    }

    public static bool Is(GameObject obj)
    {
        return Can && obj == m_I.gameObject;
    }

    public static bool Can
    {
        get { return m_I != null && m_I.gameObject != null; }
    }

    public static bool IsDestroyed { get { return m_Destroyed == 1; } }

    public static T I
    {
        get { if ((m_I == null || m_I.gameObject == null) && AutoCreated) InitInstance(null, false); return m_I; }
    }

    protected virtual void OnAwake() { }
    protected virtual void Destroy() { }

    //[ContextMenu("ManualValidate")]
    protected void CallValidate()
    {
        Validate();
    }

    protected virtual void Validate() { }

    //[RuntimeInitializeOnLoadMethod]
    //static void PreAwake()
    //{
    //    Debug.LogError("PreStatic");
    //}
    //[RuntimeInitializeOnLoadMethod]
    //static void AfterAwake()
    //{
    //    Debug.LogError("AfterStatic");
    //}
    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    //void PreAwake2()
    //{
    //    Debug.LogError("Pre");
    //}
    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    //void AfterAwake2()
    //{
    //    Debug.LogError("After");
    //}



#if UNITY_EDITOR
    protected void OnValidate()
    {
        var prefabType = UnityEditor.PrefabUtility.GetPrefabType(this.gameObject);
        if (prefabType != UnityEditor.PrefabType.PrefabInstance && prefabType != UnityEditor.PrefabType.DisconnectedPrefabInstance && prefabType != UnityEditor.PrefabType.MissingPrefabInstance && prefabType != UnityEditor.PrefabType.None)
        {
            Debug.LogWarning("MyValidate None on " + gameObject);
            //Debug.LogError(mono.GetType()+"on +"+ mono.gameObject+ " PrefabType=" + prefabType);
            return;
        }
        Validate();
    }
#endif

    protected void Awake()
    {
        if(m_I != null)
        {
#if UNITY_EDITOR
            Debug.LogError(typeof(T).ToString() + " dublicate on " + gameObject.name + " ID=" + gameObject.GetInstanceID());
#endif
            Destroy(gameObject);
            return;
        }
        m_Destroyed = 0;
        InitInstance(gameObject, true);
        OnAwake();
    }

    protected void OnDestroy()
    {
        if (m_InstanceID == GetInstanceID())
        {
            m_InstanceID = 0;
            m_I = null;
            m_Destroyed = 1;
            Destroy();
        }
    }

}


public abstract class Singleton<T> where T : class
{
    //Можно выставить, что объект может автоматически создаваться, если нет его на сцене
    protected static bool AutoCreated = false;
    protected static string m_RootName;
    protected static T m_I;
    protected static int m_InstanceID;
    protected static int m_Destroyed = -1;
    private static readonly object syncRoot = new System.Object();

    static void InitInstance(T obj)
    {
        if (m_I != null) return;
#if UNITY_EDITOR
        if (m_Destroyed == 1)
        {
            Debug.LogError(typeof(T) + " Init after destroyed");
        }
#endif
        //Возможно тут Unity в многопоточность не сможет)
        Monitor.Enter(syncRoot);
        Interlocked.Exchange(ref m_I, obj);
        Monitor.Exit(syncRoot);
    }

    public static bool Can
    {
        get { return m_I != null; }
    }

    public static bool IsDestroyed { get { return m_Destroyed == 1; } }

    public static T I
    {
        get { if (m_I == null && AutoCreated) InitInstance(null); return m_I; }
    }

    protected virtual void Init() { }
    protected virtual void Destroy() { }

    public Singleton()
    {
        if (m_I != null)
        {
#if UNITY_EDITOR
            Debug.LogError(typeof(T).ToString() + " dublicate");
#endif
            return;
        }
        m_Destroyed = 0;
        InitInstance(this as T);
        Init();
    }

    ~Singleton()
    {
        m_InstanceID = 0;
        m_I = null;
        m_Destroyed = 1;
        Destroy();
    }

}