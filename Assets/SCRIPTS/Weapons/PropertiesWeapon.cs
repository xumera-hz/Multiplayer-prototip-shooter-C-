using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

//нужно ли
#region HZ

public enum TypeAttack { Strike, Shoot }

public interface IInitWeapon<T>
{
    void Initialize(IOwner owner, T info);
}

public interface IWeapon
{
    bool IsReady { get; }
    bool IsAttack { get; }
    bool Attack();
}

public interface IAnchor
{
    Transform GetAnchor(int slot);
}

public interface IRangeWeapon: IWeapon
{
    bool Reload();
    bool InstantlyReload();
    bool CanReload { get; }
    bool IsReload { get; }
    FloatValue Dispersion { get; }
    IAmmo Ammo { get; }
    IRangeWeaponInfo Info { get; }
    IViewFireArm View { get; }
    IOwner Owner { get; }
}

public interface IAmmo
{
    bool IsFullAmmo { get; }
    bool IsFullEmpty { get; }
    bool IsEmptyCage { get; }
    bool IsFullCage { get; }
    void AddCountAmmo(int count);
    void AddCageAmmo(int count);
    void ReloadCage();
    AmmoInfo Ammo { get; }
}

public enum ProjectileType { Bullet, Rocket, Grenade }

public interface IProjectile
{
    void Set(Vector3 pos, Vector3 dir, ProjectileData data);
    void SetParent(Transform par);
    void ManualUpdate();
    void PreUpdate();
    void Activation(bool status);
    void Reset();
    ProjectileData GetData { get; }
}

public interface IWeaponInfo : IIdentifier
{
    float AttackTime { get; }
}

public interface IRangeWeaponInfo : IWeaponInfo
{
    float ReloadTime { get; }
    float FireTime { get; }
}

#endregion

#region EmptyClasses

sealed class EmptyFactoryProjectile : IGetObject<IProjectile>
{
    public IProjectile Get()
    {
        throw new NotImplementedException();
    }

    //public IProjectile Get()
    //{
    //    return new EmptyProjectile();
    //}

    public bool TryGet(out IProjectile elem)
    {
        elem = Get();
        return elem != null;
    }
}

#endregion

public class PropertiesWeapon : MonoBehaviour, IInitWeapon<GlobalWeapon>
{
    public IViewFireArm View { get { return m_ViewControl; } }
    public IOwner Owner { get { return m_ProjData.Owner; } }
    public IAmmo Ammo { get { return m_WeaponInfo; } }
    public GlobalWeapon Info { get { return m_WeaponInfo; } }

    [SerializeField] TypeReload m_ReloadType;
    protected GlobalWeapon m_WeaponInfo;
    protected ProjectileData m_ProjData = new ProjectileData();
    protected float m_CurDispersion;
    protected IViewFireArm m_ViewControl;
    protected IGetObject<IProjectile> m_BulletFactory;

    public FloatValue Dispersion { get { return new FloatValue() { Value = m_CurDispersion, Min = m_WeaponInfo.DispersionMin, Max = m_WeaponInfo.DispersionMax }; } }

    #region HelpData
    protected enum StateGun { Busy, Ready, Reload, BetweenShots, PrepareReady }
    public enum TypeReload { State, Stream }
    protected YieldInstruction YI_ttbs, YI_ttra, YI_disp;
    protected StateGun m_State;
    protected Coroutine m_CurrentAction;
    #endregion

    #region MaybeNeed

    public void SetBusy()
    {
        DisableActionsGun();
        m_State = StateGun.Busy;
    }
    //Использовать очень аккуратно
    public void SetForceState(int NewState)
    {
        StateGun newState = (StateGun)NewState;
        if (m_State == newState) return;
        DisableActionsGun();
        //if (SG == StateGun.Reload) SetAmmoInCage();
        //if (newState == StateGun.Reload) SetAmmoInCage();
        m_State = newState;
    }

    public void SetDefaultState(bool completeCurrent) { m_State = StateGun.Ready; }

    #endregion

    #region Fire

    public virtual bool Attack()
    {
        if (m_State != StateGun.Ready) return false;

        int addBulletForTimeError = IsAddBulletsForTimeError;

        m_CurrentAction = StartCoroutine(TimeToBetweenShot());

        int countAmmo = 0;
        //Debug.LogError("m_WeaponInfo.IsEmptyCage=" + m_WeaponInfo.IsEmptyCage);
        //bool r = CreateBullet();
        //Debug.LogError("CreateBullet=" + r);
        if (!m_WeaponInfo.IsEmptyCage && CreateBullet())
        {
            countAmmo = -1;
        }

        //накапливаем погрешность, если она превысит
        //время между выстрелами, то выполняет дополнительный выстрел,
        //но только в том случае, если время между последними двумя выстрелами меньше 0.3
        if (addBulletForTimeError > 0)
        {
            if (!m_WeaponInfo.IsEmptyCage && CreateBullet())
            {
                countAmmo = -addBulletForTimeError - 1;
            }
        }

        if (countAmmo < 0)
        {
            m_WeaponInfo.AddCageAmmo(countAmmo);
            m_ViewControl.PlayShot();
        }
        else m_ViewControl.PlayEmptyShot();

        return true;
    }

    static YieldInstruction m_WaitEndFrame = new WaitForEndOfFrame();
    //static YieldInstruction waitFixedUpdate = new WaitForFixedUpdate();

    float m_TimeError;
    float m_TimeLastCreateBullet;

    int IsAddBulletsForTimeError
    {
        get
        {
            bool res2 = Mathf.Abs(TimeManager.TimeTime - m_TimeLastCreateBullet) < 0.3f;
            if (!res2)
            {
                m_TimeError = 0f;
                m_TimeLastCreateBullet = 0f;
                return 0;
            }
            float fireTime = m_WeaponInfo.FireTime;
            bool res = m_TimeError >= fireTime;
            int count = 0;
            if (res)
            {
                count = (int)(m_TimeError / fireTime);
                m_TimeError = m_TimeError - fireTime * count;
                if (m_TimeError < 0f) m_TimeError = 0f;
            }
            return count;
        }
    }

#if UNITY_EDITOR
    float m_Time;
    bool gg;
#endif

    protected IEnumerator TimeToBetweenShot()
    {
        m_State = StateGun.BetweenShots;
        float currentTime = m_WeaponInfo.FireTime;

        //драчка с погрешностями времени
        if (currentTime <= TimeManager.TimeDeltaTime)
        {
            m_TimeError += (TimeManager.TimeDeltaTime - m_WeaponInfo.FireTime);
            yield return m_WaitEndFrame;
            m_TimeError += TimeManager.TimeDeltaTime;
            yield return m_WaitEndFrame;
        }
        else
        {
            while (currentTime > 0f)
            {
                yield return null;
                currentTime -= TimeManager.TimeDeltaTime;
                //драчка с погрешностями времени
                if (currentTime < 0f) m_TimeError -= currentTime;
            }
        }
        m_State = StateGun.Ready;
    }

    #endregion

    #region Reload

    Coroutine m_StreamReload;

    void ActiveStreamReload(bool state)
    {
        if (m_ReloadType != TypeReload.Stream) return;
        if (state)
        {
            if (m_WeaponInfo != null)
            {
                if (m_StreamReload == null) m_StreamReload = StartCoroutine(StreamReload());
            }
        }
        else
        {
            if (m_StreamReload != null)
            {
                StopCoroutine(m_StreamReload);
                m_StreamReload = null;
            }
        }
    }

    protected IEnumerator StreamReload()
    {
        float m_Timer = 0f;

        while (true)
        {
            while (m_WeaponInfo.IsFullCage)
            {
                m_Timer = m_WeaponInfo.ReloadTime;
                yield return null;
            }
            if (m_Timer < 0f)
            {
                m_Timer = m_WeaponInfo.ReloadTime;
                m_WeaponInfo.AddCageAmmo(m_WeaponInfo.Info.ReloadAmmoInCage);
            }
            else m_Timer -= TimeManager.TimeDeltaTime;
            yield return null;
        }
    }

    protected IEnumerator TimeToReload()
    {
        m_State = StateGun.Reload;
        yield return YI_ttra;
        yield return m_WaitEndFrame;
        m_State = StateGun.Ready;
        SetAmmoInCage();
    }

    public virtual bool Reload()
    {
        if (m_ReloadType != TypeReload.State) return false;
        if ((m_State != StateGun.Ready && m_State!= StateGun.PrepareReady) || m_WeaponInfo.IsFullCage) return false;
        m_ViewControl.PlayReload();
        m_CurrentAction = StartCoroutine(TimeToReload());
        return true;
    }

    public bool InstantlyReload()
    {
        if (m_State == StateGun.Busy || m_WeaponInfo.IsFullCage) return false;
        DisableActionsGun();
        SetAmmoInCage();
        m_State = StateGun.Ready;
        return true;
    }



    #endregion

    #region Property
    public bool CanReload { get { return m_State != StateGun.Reload && !m_WeaponInfo.IsFullCage; } }
    public bool IsReady { get { return m_State == StateGun.Ready; } }
    public bool IsReload { get { return m_State == StateGun.Reload; } }
    public bool IsAttack { get { return m_State == StateGun.BetweenShots; } }
    public TypeReload ReloadType { get { return m_ReloadType; } }
    public bool IsEmpty { get { return m_WeaponInfo.IsEmptyCage; } }
    #endregion

    #region Ammo

    protected void SetAmmoInCage()
    {
        m_WeaponInfo.ReloadCage();
    }

    #endregion

    #region Dispersion
    Coroutine dispCycle;
    bool isDispCycle;
    IEnumerator DispersionCycle()
    {
        isDispCycle = true;
        while (m_CurDispersion > m_WeaponInfo.Info.MinDispersion)
        {
            //Debug.LogError("ITer=" + CurDispersion);
            yield return YI_disp;
            //if (SG == StateGun.Ready)
                AddCurDispersion(-m_WeaponInfo.DispersionDamp * TimeManager.TimeDeltaTime);
        }
        yield return m_WaitEndFrame;
        isDispCycle = false;
        dispCycle = null;
    }

    protected void AddCurDispersion(float addDisp)
    {
        if (addDisp == 0f) return;
        m_CurDispersion += addDisp;
        //Debug.LogError("Dispersion=" + CurDispersion);
        if (addDisp > 0f)
        {
            float maxDisp = m_WeaponInfo.Info.MaxDispersion;
            if (m_CurDispersion > maxDisp) m_CurDispersion = maxDisp;
        }
        else
        {
            float minDisp = m_WeaponInfo.Info.MinDispersion;
            if (m_CurDispersion < minDisp) m_CurDispersion = minDisp;
        }
        if (!isDispCycle) StartCoroutine(DispersionCycle());
    }

    #endregion

    public static event Action<MonoBehaviour> ShootEvent;
    public static void ClearEvents()
    {
        ShootEvent = null;
    }

    List<Vector3> m_BulletDirections = new List<Vector3>(10);
    Action<Vector3, Vector3, int, List<Vector3>> m_GenerateFireDirections;

    void DefaultGenerateDirections(Vector3 startPos, Vector3 lookDir, int count, List<Vector3> outDirections)
    {
        outDirections.Clear();
        Quaternion rotLook = Quaternion.LookRotation(lookDir);
        for (int i = 0; i < count; i++)
        {
            outDirections.Add(rotLook * MathUtils.RandomVectorInsideCone2(m_CurDispersion));
        }
    }

    public Action<Vector3,Vector3,int,List<Vector3>> GenerateBulletDirections
    {
        private get { if (m_GenerateFireDirections == null) m_GenerateFireDirections = DefaultGenerateDirections; return m_GenerateFireDirections; }
        set { if (value == null) m_GenerateFireDirections = DefaultGenerateDirections; }
    }

    #region Private
    [ContextMenu("CreateBullet")]
    protected bool CreateBullet()
    {
        IProjectile bullet;
        Vector3 pos;
        Quaternion rot;
        //Vector3 dir;
        m_ViewControl.FirePoint.GetInfo(out pos, out rot, true);
        bool res = false;
        int count = m_WeaponInfo.Info.ProjsPerShot;
        m_BulletDirections.Clear();
        GenerateBulletDirections(pos, rot * Vector3.forward, count, m_BulletDirections);
        for (int i = 0; i < count; i++)
        {
            bool res2 = m_BulletFactory.TryGet(out bullet);
            if (!res2) continue;
            res = true;
            //dir = rot * m_BulletDirections[i];
            bullet.Set(pos, m_BulletDirections[i], m_ProjData);
#if UNITY_EDITOR
            //Debug.DrawRay(pos, m_ViewControl.FirePoint.GetPoint().forward * (pd.Instantly ? pd.MaxDistance * 2f : (pd.Speed * TimeManager.TimeDeltaTime)), Color.red, 5f);
            Debug.DrawRay(pos, (bullet as MonoBehaviour).transform.forward * (m_ProjData.Instantly ? m_ProjData.MaxDistance : (m_ProjData.Speed * TimeManager.TimeDeltaTime)), Color.red, 5f);
#endif
        }
        if (res)
        {
            AddCurDispersion(m_WeaponInfo.DispersionStep);
            m_TimeLastCreateBullet = TimeManager.TimeTime;
            if (ShootEvent != null) ShootEvent(this);
        }
        return res;
    }

    #endregion

    #region Public

    public void Initialize(IOwner owner, GlobalWeapon wInfo)
    {
        m_WeaponInfo = wInfo;
        SetFactoryProjectile(ManagerProjectile.GetIPoolElement((ProjectileType)wInfo.Info.TypeProjectile));
        //if(owner.GameObj.IsPlayer()) Debug.LogError(wInfo.FireTime);
        YI_ttbs = new WaitForSeconds(wInfo.FireTime);
        YI_ttra = new WaitForSeconds(wInfo.ReloadTime);
        m_ProjData.Init(wInfo.Info, wInfo.Info.InstantProjectile);
        m_CurDispersion = wInfo.DispersionMin;
        ActiveStreamReload(true);
        if (owner != null)
        {
            if (owner.Anchor != null) m_ViewControl.LinkToAnchor(owner.Anchor.GetAnchor((int)wInfo.Info.SlotType));
            m_ProjData.Owner = owner;
        }
    }

    public void SetOwner(IOwner own)
    {
        if (own == null) gameObject.SetActive(false);
        m_ProjData.Owner = own;
    }

    public void SetFactoryProjectile(IGetObject<IProjectile> bltFctr)
    {
        m_BulletFactory = bltFctr == null ? new EmptyFactoryProjectile() : bltFctr;
    }

    #endregion

    #region MonoBehaviour

    protected void Awake()
    {
        m_ViewControl = GetComponent<IViewFireArm>();
        if (m_ViewControl == null) m_ViewControl = new EmptyViewFireArm();
        m_ViewControl.Settings = new ViewGunSettings() { Audio = true, Fire = true, Bullet = true };
    }

    protected virtual void DisableActionsGun()
    {
        if (m_ViewControl != null) m_ViewControl.FullStopView();
        if (m_CurrentAction != null)
        {
            StopCoroutine(m_CurrentAction);
            m_CurrentAction = null;
        }
        if (m_WeaponInfo != null) m_CurDispersion = m_WeaponInfo.DispersionMin;
        m_TimeError = 0f;
        m_TimeLastCreateBullet = 0f;
        if (dispCycle != null)
        {
            StopCoroutine(dispCycle);
            dispCycle = null;
        }
        isDispCycle = false;
    }

    void OnDisable()
    {
        DisableActionsGun();
        m_State = StateGun.Busy;
        if (m_StreamReload != null) StopCoroutine(m_StreamReload);
        m_StreamReload = null;
    }
    protected virtual void OnEnable()
    {
        m_State = StateGun.Ready;
        ActiveStreamReload(true);
    }

    #endregion

}
