using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class ObjectByType<TypeKey, TypeObject>
{
#pragma warning disable 649
    [System.Serializable]
    class Node
    {
        public TypeKey Type;
        public TypeObject Obj;
    }

    [SerializeField] Node[] m_Nodes;
#pragma warning restore 649

    Dictionary<TypeKey, TypeObject> m_Dict;

    public void Init(IEqualityComparer<TypeKey> m_Comparer)
    {
        m_Dict = new Dictionary<TypeKey, TypeObject>(m_Nodes.Length, m_Comparer);
        TypeKey _type;
        for (int i = 0; i < m_Nodes.Length; i++)
        {
            _type = m_Nodes[i].Type;
            if (m_Dict.ContainsKey(_type))
            {
#if UNITY_EDITOR
                Debug.LogError(_type + "is dublicated");
#endif
                continue;
            }
            m_Dict.Add(_type, m_Nodes[i].Obj);
        }
    }

    public TypeObject Get(TypeKey type)
    {
        TypeObject obj;
        if (m_Dict.TryGetValue(type, out obj)) return obj;
#if UNITY_EDITOR
        Debug.LogError(type + " not exist");
#endif
        return default(TypeObject);
    }
}

public sealed class ManagerProjectile : MonoSingleton<ManagerProjectile> {

    #region Classes

    sealed class ProjectileFactory : IFactoryElement<IProjectile>
    {
        GameObject m_Obj;
        Transform m_Root;
        bool m_SetParent;

        public ProjectileFactory(GameObject objectCreate, Transform root, bool setParent = true)
        {
            m_Obj = objectCreate;
            m_SetParent = setParent;
            m_Root = root;
        }

        public bool CreateElement(out IProjectile elem)
        {
            elem = null;
            GameObject go = Instantiate(m_Obj);
            if (go == null) return false;
            if (m_SetParent) go.transform.SetParent(m_Root, true);
            elem = go.GetComponent<IProjectile>();
            if (elem == null) return false;
            elem.Activation(false);
            return true;
        }
    }

    sealed class EmptyPool : IGetObject<IProjectile>
    {
        public bool TryGet(out IProjectile elem)
        {
            elem = new EmptyProjectile();
            return true;
        }

        public IProjectile Get()
        {
            IProjectile elem;
            return TryGet(out elem) ? elem : null;
        }
    }
    [Serializable]
    class Node
    {
        public ProjectileType Type = ProjectileType.Bullet;
        public GameObject Obj = null;
        public int BeginCapacity = 0;
    }

    #endregion

    [SerializeField] Node[] m_Nodes = null;

    Transform m_RootObjs;
    ManagerPools<IProjectile> m_PoolsProjectile;
    List<IProjectile> m_ActiveProjs;



    #region Public

    public event Action<IProjectile> CreatedProjectile;

    #region Static

    public static IGetObject<IProjectile> GetIPoolElement(ProjectileType type)
    {
        if (Can) return m_I.GetIPoolElementInner(type);
        return new EmptyPool();
    }

    public static bool Static_RegisterProjectile(IProjectile proj)
    {
        return Can && m_I.RegisterProjectile(proj);
    }
    public static bool Static_UnRegisterProjectile(IProjectile proj)
    {
        return Can && m_I.UnRegisterProjectile(proj);
    }

    #endregion

    public bool UnRegisterProjectile(IProjectile proj)
    {
        int ind = m_I.m_ActiveProjs.IndexOf(proj);
        if (ind != -1) m_I.m_ActiveProjs.RemoveAt(ind);
        proj.Reset();
        proj.Activation(false);
        //Debug.Log("proj="+ proj);
        I.m_PoolsProjectile.Return(proj.GetData.TypeProjectile, proj);
        return ind != -1;
    }

    public bool RegisterProjectile(IProjectile proj)
    {
        if (m_ActiveProjs.Contains(proj)) return false;
        m_ActiveProjs.Add(proj);
        CallCreatedProjectile(proj);
        return true;
    }

    #endregion

    #region Private

    void CallCreatedProjectile(IProjectile proj)
    {
        if (CreatedProjectile != null) CreatedProjectile(proj);
    }

    IGetObject<IProjectile> GetIPoolElementInner(ProjectileType type)
    {
        IPool<IProjectile> pool;
        bool res = m_PoolsProjectile.TryGet((int)type, out pool);
        if (res) return pool;
        return new EmptyPool();
    }

    void Load()
    {
        m_PoolsProjectile = new ManagerPools<IProjectile>();
        m_ActiveProjs = new List<IProjectile>(15);
    }

    void Init()
    {
//#if UNITY_EDITOR
        m_RootObjs = new GameObject("ROOT_PROJECTILES").transform;
        //m_RootObjs = ConstantObjects.GetGlobalObject(ConstantObjects.ROOT_PROJECTILES);
//#endif
        var prefabs = m_Nodes;
        for (int i = 0; i < prefabs.Length; i++)
        {
            var node = m_Nodes[i];
            m_PoolsProjectile.Add((int)node.Type, new ObjectsPool<IProjectile>(new ProjectileFactory(node.Obj, m_RootObjs).CreateElement, node.BeginCapacity/*30, 15*/));
        }
        m_Nodes = null;
    }

    void OnProjectileActive(IProjectile proj, int status)
    {
        //Debug.LogError("projActive="+status);
        if (proj == null) return;
        if (status == Projectile.DEACTIVE_STATUS) Static_UnRegisterProjectile(proj);
        else if (status == Projectile.ACTIVE_STATUS) Static_RegisterProjectile(proj);
        else if (status == Projectile.ACTIVE_IMMEDIATE_STATUS)
        {
            Static_RegisterProjectile(proj);
            proj.PreUpdate();
            proj.ManualUpdate();
        }
    }

    #endregion

    #region Monobehaviour

    protected override void Destroy()
    {
        Projectile.Active -= OnProjectileActive;
        m_RootObjs.DestroyGO();
    }

    protected override void OnAwake()
    {
        Load();
        Init();
    }

    void Start()
    {
    }

    void OnEnable()
    {
        Projectile.Active += OnProjectileActive;
    }

    void OnDisable()
    {
        Projectile.Active -= OnProjectileActive;
    }

    public void PreUpdate()
    {
        for (int i = m_ActiveProjs.Count - 1; i >= 0; i--)
        {
            if (m_ActiveProjs[i] == null)
            {
                m_ActiveProjs.RemoveAt(i);
                continue;
            }
            m_ActiveProjs[i].PreUpdate();
        }
    }

    public void ManualUpdate()
    {
        for (int i = m_ActiveProjs.Count - 1; i >= 0; i--)
        {
            if (m_ActiveProjs[i] == null)
            {
                m_ActiveProjs.RemoveAt(i);
                continue;
            }
            m_ActiveProjs[i].ManualUpdate();
        }
    }

    #endregion
}
