using UnityEngine;
using System.Collections;
using System;

public interface IPoint<T>
{
    void GetInfo(out Vector3 pos, out Quaternion rot, bool isWorld);
    T GetPoint(); 
}

public interface ITransform : IPoint<Transform> { }

public interface IViewFireArm
{
    ViewGunSettings Settings { get; set; }
    void PlayShot();
    void PlayReload();
    void PlayEmptyShot();
    void PlayBetweenShots();
    void FullStopView();
    void ActiveView(bool state);
    void StopAudio();
    ITransform FirePoint { get; set; }
    ITransform AimPoint { get; set; }
    void SetParent(Transform tf);
    void LinkToAnchor(Transform tf);
    void ChangeSkin(Material mat);
}

public struct ViewGunSettings
{
    public bool Audio, Fire, Bullet, Shell;
}

[Serializable]
public class ParticleSystemObjs
{
    //[NonSerialized][HideInInspector]public Transform TF;
    public GameObject GO;
    //AutoDeactiveOrDestroyParticleSystem psControl;
    //ParticleSystem PS;
    bool IsInit;
    public void Init()
    {
        if (GO == null) return;
        //psControl = GO.GetComponentInChildren<AutoDeactiveOrDestroyParticleSystem>(true);
        IsInit = true;
    }

    public void SetActive(bool state)
    {
        if (!IsInit) Init();
        if (!IsInit) return;
        if(state && GO.activeSelf) GO.SetActive(false);
        GO.SetActive(state);
        //if(psControl!=null) psControl.SetActive(state);
    }
}
public class ViewGun : MonoBehaviour, IViewFireArm
{
#if UNITY_EDITOR
    [ContextMenu("InitPoints")]
    void InitPoints()
    {
        var firePoint = transform.FindTF("FirePoint", true);
        m_FirePoint = firePoint;
        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint").transform;
            var fire = transform.FindTF("Fire", true);
            firePoint.SetParent(fire);
            firePoint.localPosition = Vector3.zero;
            firePoint.localRotation = Quaternion.identity;
        }
        m_FirePoint = firePoint;
        var aim = transform.FindTF("Aim", true);
        m_AimPoint = aim;
    }
#endif

    public ViewGun()
    {
        FirePoint = null;
        AimPoint = null;
    }
#if UNITY_EDITOR
    //public bool CreateDebugLaser = false;
    //public GameObject DebugLaser;
#endif
    public const string Anchor = "Anchor";
    protected ViewGunSettings settings;
    public AudioClip AC_Shot, AC_Reload, AC_Empty, AC_AfterShoot;
    bool isBetweenAudio;//есть ли звук между выстрелами
    [SerializeField]
    GameObject m_Visual;
    [SerializeField] ParticleSystemObjs Fire, Shell;
    [SerializeField] Transform m_FirePoint;
    [SerializeField] Transform m_AimPoint;
    MeshRenderer[] Model;
    YieldInstruction yi_timeAudioAfterFire;
    protected AudioSource AS;
    Coroutine cor;
    Transform innerAnchor;

    public void SetTimeAudioAfterFire(float timer) { yi_timeAudioAfterFire = new WaitForSeconds(timer > 0f ? timer : 0f); }

    void Awake()
    {
        Init();
    }

    void Start()
    {
        InitAudio();
        InitParticles();
    }

#if UNITY_EDITOR
    /*const string laser = "Laser";
    void Update()
    {
#if UNITY_EDITOR
        if (CreateDebugLaser)
        {
            if (DebugLaser == null) return;
            Transform child = Aim.FindChild(laser);
            if(child==null)
            {
                GameObject go = Instantiate(DebugLaser);
                Vector3 pos = go.transform.localPosition;
                go.transform.SetParent(Aim, false);
            }
        }
#endif
    }*/
#endif

    void Init()
    {
        innerAnchor = transform.Find(Anchor);
        var _model = transform.Find("Model");
        if (_model != null) Model = GetComponentsInChildren<MeshRenderer>(true);
        //InitAudio();
    }

    void InitParticles()
    {
        Fire.Init();
        Fire.SetActive(false);
        Shell.Init();
    }

    class InnerFirePoint : ITransform
    {
        ViewGun m_View;
        public InnerFirePoint(ViewGun view)
        {
            m_View = view;
        }
        void IPoint<Transform>.GetInfo(out Vector3 pos, out Quaternion rot, bool isWorld)
        {
            if (isWorld)
            {
                pos = m_View.m_FirePoint.position;
                rot = m_View.m_FirePoint.rotation;
            }
            else
            {
                pos = m_View.m_FirePoint.localPosition;
                rot = m_View.m_FirePoint.localRotation;
            }
        }

        Transform IPoint<Transform>.GetPoint()
        {
            return m_View.m_FirePoint;
        }
    }

    class InnerAimPoint : ITransform
    {
        ViewGun m_View;
        public InnerAimPoint(ViewGun view)
        {
            m_View = view;
        }
        void IPoint<Transform>.GetInfo(out Vector3 pos, out Quaternion rot, bool isWorld)
        {
            if (isWorld)
            {
                pos = m_View.m_AimPoint.position;
                rot = m_View.m_AimPoint.rotation;
            }
            else
            {
                pos = m_View.m_AimPoint.localPosition;
                rot = m_View.m_AimPoint.localRotation;
            }
        }

        Transform IPoint<Transform>.GetPoint()
        {
            return m_View.m_AimPoint;
        }
    }

    ITransform m_FireTrans;
    ITransform m_AimTrans;

    public ITransform FirePoint
    {
        get
        {
            return m_FireTrans;
        }

        set
        {
            m_FireTrans = value == null ? new InnerFirePoint(this) : value;
        }
    }

    public ITransform AimPoint
    {
        get
        {
            return m_AimTrans;
        }

        set
        {
            m_AimTrans = value == null ? new InnerAimPoint(this) : value;
        }
    }

    #region Audio
    void InitAudio()
    {
        AS = gameObject.GetComponent<AudioSource>();
        isBetweenAudio = AC_AfterShoot != null;
        if (isBetweenAudio) yi_timeAudioAfterFire = new WaitForSeconds(AC_AfterShoot.length);
        AudioController.RegisterSource(AS);
    }
    IEnumerator TimeToAudioAfterFire()
    {
        yield return yi_timeAudioAfterFire;
        PlayAudio(AC_AfterShoot);
    }

    void PlayAudio(AudioClip ac)
    {
        //StopAudio();
        if (!settings.Audio) return;
        AS.PlayOneShot(ac);
        //AS.clip = ac;
        //AS.Play(0);
    }

    public void PlayShot() {
        if (settings.Audio)
        {
            AS.PlayOneShot(AC_Shot);
            if (isBetweenAudio)
            {
                if (cor != null)
                {
                    StopCoroutine(cor);
                    cor = null;
                }
                cor = StartCoroutine(TimeToAudioAfterFire());
            }
        }
        Fire.SetActive(settings.Fire);
    }
    public void PlayReload() { PlayAudio(AC_Reload); }
    public void PlayEmptyShot() { if (settings.Audio) AS.PlayOneShot(AC_Empty); }
    public void PlayBetweenShots() { PlayAudio(AC_AfterShoot); }

    public void StopAudio()
    {
        if (cor != null)
        {
            StopCoroutine(cor);
            cor = null;
        }
        AS.Stop();
    }
    #endregion

    public void FullStopView()
    {
        Fire.SetActive(false);
        StopAudio();
    }

    public void ActiveView(bool state)
    {
        m_Visual.SetActive(state);
    }

    public void Visual(bool audio, bool fire, bool bullet, bool shell)
    {
        Settings = new ViewGunSettings() { Audio = audio, Fire = fire, Bullet = bullet, Shell = shell };
    }

    public ViewGunSettings Settings
    {
        get { return settings; }
        set
        {
            settings = value;
            if(!settings.Fire) Fire.SetActive(false);
            if (!settings.Audio) StopAudio();
            if (!settings.Shell) { }
            if (!settings.Bullet) { }
        }
    }

    public void SetParent(Transform tf)
    {
        transform.SetParent(tf, true);
    }

    public void LinkToAnchor(Transform tf)
    {
        SetParent(tf);
        if (tf == null) return;
        if (innerAnchor != null)
        {
            transform.localPosition = innerAnchor.localPosition;
            transform.localRotation = innerAnchor.localRotation;
        }
    }

    public void ChangeSkin(Material mat)
    {
        if (mat != null && Model != null && Model.Length > 0)
        {
            for (int i = Model.Length - 1; i >= 0; --i)
            {
                Model[i].material = mat;
            }
        }
    }
}


class EmptyViewFireArm : IViewFireArm, ITransform
{

    Transform m_TF;
    bool m_IsLog;

    public void SetLog(bool log)
    {
        m_IsLog = log;
    }

    public EmptyViewFireArm()
    {
    }
    public EmptyViewFireArm(Transform tf)
    {
        m_TF = tf;
    }

    public void FullStopView()
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("FullStopView Empty");
#endif
    }

    public void PlayBetweenShots()
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("PlayBetweenShots Empty");
#endif
    }

    public void PlayEmptyShot()
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("PlayEmptyShot Empty");
#endif
    }

    public void PlayReload()
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("PlayReload Empty");
#endif
    }

    public void PlayShot()
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("PlayShot Empty");
#endif
    }

    public ViewGunSettings Settings
    {
        get
        {
#if UNITY_EDITOR
            if (m_IsLog) Debug.LogError("SettingsGet Empty");
#endif
            return new ViewGunSettings();
        }
        set
        {
#if UNITY_EDITOR
            if (m_IsLog) Debug.LogError("SettingsSet Empty");
#endif
        }
    }

    public ITransform FirePoint
    {
        get
        {
#if UNITY_EDITOR
            if (m_IsLog) Debug.LogError("AimPointGet Empty");
#endif
            return this;
        }

        set
        {
#if UNITY_EDITOR
            if (m_IsLog) Debug.LogError("AimPointSet Empty");
#endif
        }
    }

    public ITransform AimPoint
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

    public void StopAudio()
    {
#if UNITY_EDITOR
        Debug.LogError("StopAudio Empty");
#endif
    }

    public void SetParent(Transform tf)
    {
        if (m_TF) m_TF.SetParent(tf, true);
    }

    public void LinkToAnchor(Transform tf)
    {
        SetParent(tf);
        if (tf == null) return;
        if (m_TF)
        {
            m_TF.localPosition = Vector3.zero;
            m_TF.localRotation = Quaternion.identity;
        }
    }

    public void ChangeSkin(Material mat)
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("ChangeSkin Empty");
#endif
    }

    public void GetInfo(out Vector3 pos, out Quaternion rot, bool world = true)
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("GetInfo Empty");
#endif
        pos = Vector3.zero;
        rot = Quaternion.identity;
    }

    public Transform GetPoint()
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("GetPoint Empty");
#endif
        return null;
    }

    public void ActiveView(bool state)
    {
    }
}

