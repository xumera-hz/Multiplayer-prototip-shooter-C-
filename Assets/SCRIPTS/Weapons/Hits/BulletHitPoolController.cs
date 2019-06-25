using UnityEngine;
using System.Collections.Generic;
using System;

public enum ProjectileHitEffect { Concrete, Metal, Wood, Dirt, Sand, Blood }

public class BulletHitPoolController : MonoSingleton<BulletHitPoolController>
{
    [SerializeField]
    ProjectileHitEffect m_DefaultHit = ProjectileHitEffect.Concrete;


    void Start()
    {
        InitPoolProjectileHitEffects();
    }

    protected override void Destroy()
    {
        m_RootObjs.DestroyGO();
    }

    private void Update()
    {
        for (int i = m_Actives.Count - 1; i >= 0; i--)
        {
            var node = m_Actives[i];
            if(node.GO == null)
            {
                m_Actives.RemoveAt(i);
                continue;
            }
            if (!node.GO.activeInHierarchy)
            {
                m_Actives.RemoveAt(i);
                m_PoolProjectileHitEffects.Return(node.ID, node.GO);
            }
        }
    }

    #region Factory

    const string ProjectileHitEffectsPath = "ProjectileHitEffects/";

    sealed class ProjectileHitFactory : SimplePrefabFactory<int, GameObject>
    {
        public static readonly int CountHitEffects = Enum.GetValues(typeof(ProjectileHitEffect)).Length;
        int m_Count;
        string m_Path;
        Transform m_RootObjs;


        public ProjectileHitFactory(string loadPath, Transform rootObjs = null)
        {
            m_Count = CountHitEffects;
            objs = new Dictionary<int, GameObject>(m_Count);
            m_Path = loadPath;
            m_RootObjs = rootObjs;
        }

        public override bool Equals(int x, int y) { return x == y; }

        public override int[] GetKeys()
        {
            int[] ids = new int[m_Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = i;
            return ids;
        }

        protected override bool CheckKey(int key)
        {
            return key < -1 || key >= m_Count;
        }

        protected override string GetPath(int key)
        {
            return m_Path + ((ProjectileHitEffect)key).ToString();
        }

        public bool CreatePoolElem(int key, out GameObject obj)
        {
            GameObject go;
            bool res = TryCreate(key, out go);
            res = go != null;
            if (res)
            {
                var tf = go.transform;
                tf.SetParent(m_RootObjs);
            }
            obj = res ? go : null;
            return res;
        }
    }

    ObjectsPoolByKey<int, GameObject> m_PoolProjectileHitEffects;
    ProjectileHitFactory m_Factory;
    Transform m_RootObjs;

    void InitPoolProjectileHitEffects()
    {
//#if UNITY_EDITOR
        m_RootObjs = new GameObject("ROOT_HIT_EFFECTS").transform;
        //rootObjs = ConstantObjects.GetGlobalObject(ConstantObjects.ROOT_HIT_EFFECTS);
//#endif
        m_Factory = new ProjectileHitFactory(ProjectileHitEffectsPath, m_RootObjs);
        m_PoolProjectileHitEffects = new ObjectsPoolByKey<int, GameObject>(m_Factory.CreatePoolElem, ProjectileHitFactory.CountHitEffects, m_Factory);
    }

    #endregion

    struct Node
    {
        public int ID;
        public GameObject GO;
        public Node(int id, GameObject go)
        {
            ID = id;
            GO = go;
        }
    }

    List<Node> m_Actives = new List<Node>(30);

    public void CreateProjectileHitEffect(int id, Vector3 pos, Quaternion rot)
    {
        GameObject obj;
        if (m_PoolProjectileHitEffects.TryGet(id, out obj))
        {
            var tf = obj.transform;
            tf.position = pos;
            tf.rotation = rot;
            if (!obj.activeSelf) obj.SetActive(true);
            m_Actives.Add(new Node(id, obj));
        }
#if UNITY_EDITOR
        else Debug.Log("Not " + (ProjectileHitEffect)id + " type effect");
#endif
    }

    public static void Static_CreateProjectileHitEffect(int id, Vector3 pos, Vector3 dir)
    {
        if (Can) m_I.CreateProjectileHitEffect(id, pos, dir);
#if UNITY_EDITOR
        else Debug.LogError(typeof(BulletHitPoolController) + " is null");
#endif
    }

    public static void Static_CreateProjectileHitEffect(RaycastHit hit, Vector3 dir)
    {
        if (Can) m_I.CreateProjectileHitEffect(hit, dir);
#if UNITY_EDITOR
        else Debug.LogError(typeof(BulletHitPoolController) + " is null");
#endif
    }

    public void CreateProjectileHitEffect(int id, Vector3 pos, Vector3 dir)
    {
        CreateProjectileHitEffect(id, pos, (dir.sqrMagnitude > 1e-5f ? Quaternion.LookRotation(-dir) : Quaternion.identity));
    }

    public void CreateProjectileHitEffect(RaycastHit hit, Vector3 dir)
    {
        var HitObj = hit.transform.gameObject;
        int id;
        if (Enum.IsDefined(typeof(ProjectileHitEffect), HitObj.tag))
            id = (int)Enum.Parse(typeof(ProjectileHitEffect), HitObj.tag);
        else id = (int)m_DefaultHit;
        CreateProjectileHitEffect(id, hit.point, dir);
    }
}
