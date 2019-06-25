using System;
using UnityEngine;

public abstract class Projectile : MonoBehaviour, IProjectile
{
    public const int DEACTIVE_STATUS = 0;
    public const int ACTIVE_STATUS = 1;
    public const int ACTIVE_IMMEDIATE_STATUS = 2;

    public event Action<MonoBehaviour, int> EventActive;
    //int - состояние: 0-деактивация, 1 - активация, 2 - активация мгновенного патрона
    public static Action<Projectile, int> Active;
    public static Action<Projectile, CastHitsInfo> RegistryHit;
    public ProjectileType ProjectileType;
    protected float m_LifeTime;
    [SerializeField] protected PhysicsCastData m_CastData = new PhysicsCastData();
    public readonly ProjectileData Data = new ProjectileData();

    protected GameObject m_GO;
    protected Transform m_TF;
    protected Vector3 m_Direction;

    public virtual Vector3 Direction { get { return m_Direction; } }

    public GameObject GO { get { return m_GO; } }

    MeshRenderer model;
    protected bool isEndMove, isEndCheckHit, isEnd;
    protected CastHitsInfo hitsInfo;

    void Awake()
    {
        m_GO = gameObject;
        m_TF = GO.transform;
        m_Direction = m_TF.forward;
        model = GetComponentInChildren<MeshRenderer>(true);
        hitsInfo = new CastHitsInfo();
        Init();
    }

    protected virtual void Cast()
    {
        hitsInfo.Cast(m_CastData);
    }

    protected void CallRegistryHit()
    {
        if (RegistryHit != null) RegistryHit(this, hitsInfo);
    }

    protected abstract void Init();

    public ProjectileType Type { get { return ProjectileType; } }
    public abstract void Move();
    public abstract bool CheckHit();
    public abstract void SetHit(float impulse, Vector3 pos);
    public virtual void Reset()
    {
        isEnd = isEndCheckHit = isEndMove = false;
    }
    public bool Finish
    {
        get { return isEnd; }
        set
        {
            isEnd = value;
            if (value)
            {
                Release();
                CallActive(DEACTIVE_STATUS);
            }
        }
    }
    public bool EndCheckHit { get { return isEndCheckHit; } set { Debug.LogError("isEndCheckHit=" + value); isEndCheckHit = value; } }
    public bool EndMove { get { return isEndMove; } set { isEndMove = value; if (model != null) model.enabled = !value; } }
    public IOwner Owner { get { return Data.Owner; } }
    public float LifeTime { get { return m_LifeTime; } }

    public ProjectileData GetData
    {
        get { return Data; }
    }

    void CallActive(int state)
    {
        if (EventActive != null) EventActive(this, state);
        if (Active != null) Active(this, state);
    }

    protected virtual void ProjDataInit(ref ProjectileData data)
    {
        Data.Clone(data);
        Data.TypeProjectile = (int)ProjectileType;
        m_LifeTime = Data.LifeTime;
        //m_LifeTime = data.LifeTime;
    }

    public void Set(Vector3 pos, Vector3 dir, ProjectileData data)
    {
        ProjDataInit(ref data);
        EndMove = false;
        Activation(true);
        m_TF.position = pos;
        dir.Normalize();
        m_TF.forward = dir;
        m_Direction = dir;
        m_CastData.Direction = dir;
        EndMove = false;
        OnSet();


        OnActiveCall(data.Instantly ? ACTIVE_IMMEDIATE_STATUS : ACTIVE_STATUS);
    }

    protected abstract void OnSet();

    public void Activation(bool status) { GO.SetActive(status); }

    protected void OnActiveCall(int state)
    {
        if (Active != null) Active(this, state);
    }

    public void SetParent(Transform par) { m_TF.SetParent(par, true); }

    public void PreUpdate()
    {
        if (isEnd) return;
        if (!isEndCheckHit)
        {
            CheckHit();
            //Если выстрел мгновенный, то делаем только одну проверку и блкоируем
            if (Data.Instantly) isEndCheckHit = true;
        }
    }

    public void ManualUpdate()
    {
        if (isEnd)
        {
            OnActiveCall(DEACTIVE_STATUS);
            return;
        }
        if (!isEndMove) Move();
        if (m_LifeTime <= 0f) isEnd = true;
        else m_LifeTime -= TimeManager.TimeDeltaTime;
        if (isEnd)
        {
            Finish = true;
            return;
        }

    }

    protected virtual void Release() { }
}

sealed class EmptyProjectile : IProjectile
{
    public bool EndCheckHit
    {
        get
        {
            throw new NotImplementedException();
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public bool EndMove
    {
        get
        {
            throw new NotImplementedException();
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public bool Finish
    {
        get
        {
            throw new NotImplementedException();
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public ProjectileType Type
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public event Action<MonoBehaviour, int> EventActive;

    public void ManualUpdate()
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }

    public void Set(Vector3 pos, Vector3 dir, ProjectileData data)
    {
#if UNITY_EDITOR
        Debug.Log(GetType() + ": this empty logic create bullet");
#endif
    }

    public void SetParent(Transform par)
    {
    }

    public void Activation(bool status)
    {

    }

    public void PreUpdate()
    {
        throw new NotImplementedException();
    }

    public ProjectileData Data { get { throw new NotImplementedException(); } }

    public ProjectileData GetData
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    /*public void SetManagerHit(IHit<ProjectileHitInfo> manager)
    {

    }*/
}
