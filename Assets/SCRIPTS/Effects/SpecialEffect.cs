using UnityEngine;
using System.Collections.Generic;
using System;

#region AudioEffects
/*
public class AudioEffectWithAS : MonoBehaviour
{
    public AudioSource AS;
    public bool UnscaleTime;
    int id;

    void Awake()
    {
        if (AS == null)
        {
            id = AudioController.RegisterFreeSource(out AS, UnscaleTime);
            if (AS != null) AS.transform.SetParent(transform, true);
#if UNITY_EDITOR
            else Debug.LogError(GetType() + " error: AudioSource is null on" + gameObject);
#endif
        }
        else id = AudioController.RegisterSource(AS, UnscaleTime);
    }
    void Start()
    {
        AS.Play(0);
    }
    void OnDestroy()
    {
        AudioController.UnRegisterSource(id);
    }
}

public class AudioEffect : MonoBehaviour
{
    public AudioClip Sound;

    void OnEnable()
    {
        if (Sound != null) AudioController.PlaySoundAtPosition(Sound, transform.position, AudioController.AS_3D);
#if UNITY_EDITOR
        else Debug.LogError(GetType() + " error: Sound clip is null on " + gameObject);
#endif
    }
}
*/
#endregion

public interface ISpecialEffect
{
    void Begin();
    void FullReset();
    void Init();
    //void ManualUpdate();
    bool CheckEnd();
    void Stop();
    //void FullStop();
    void VisualStop();
    TypeSpecialEffect Type { get; }
}

public enum TypeSpecialEffect { Audio, Particle }

public interface ISpecialEffectControl
{
    bool Register(ISpecialEffect effect);
    void UnRegister(ISpecialEffect effect);
}

public class SpecialEffect : MonoBehaviour, ISpecialEffectControl
{
    public static readonly int CountTypeEffects = System.Enum.GetValues(typeof(TypeSpecialEffect)).Length;
    public enum TypeLiveTime { EndLive, ByTimer, UnlimitLoop }
    //public enum TypeAudioEffect { None, Default, Register, FreeRegister }
    //public enum TypeVisualEffect { None, TimedLoop, EndLive }
    public enum TypePriority { None, Max, Particle, Audio }
    [SerializeField] TypeLiveTime LiveTime;
    ISpecialEffect[] specEffects;
    //public TypeAudioEffect AudioEffect;
    //public TypeVisualEffect VisualEffect;
    [SerializeField] TypePriority Priority;
    //public AudioClip Sound;
    //public AudioSource AS;
    //public ParticleSystem PS;
    [SerializeField] float Timer;
    [SerializeField] bool Destroyed = false;
    [SerializeField] bool AutoStartOnEnable = false;
    //public bool AudioUnscaleTime;
    //int id;
    float curTimer;
    Transform rootParent;
    Action curState=()=> { };

    #region Monobehaviour

    void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        if (AutoStartOnEnable) Active();
    }

    /*void OnDisable()
    {
        if (AutoStartOnEnable) Deactive();
    }*/

    void Update()
    {
        ManualUpdate();
    }

    void OnValidate()
    {
        if (LiveTime == TypeLiveTime.UnlimitLoop)
        {
            enabled = false;
//#if UNITY_EDITOR
//            Debug.LogError(GetType() + " error: " + LiveTime.GetType() + " not release(switch on default) on" + gameObject);
//#endif
//            LiveTime = 0;
        }
        Priority = LiveTime == TypeLiveTime.ByTimer ? TypePriority.None : Priority;
    }

    #endregion

    #region Private

    void EffectVisualStop(TypeSpecialEffect type)
    {
        for (int i = specEffects.Length - 1; i >= 0; i--)
        {
            if (specEffects[i].Type == type) specEffects[i].VisualStop();
        }
    }

    static List<ISpecialEffect> m_CacheList = new List<ISpecialEffect>(20);

    void Init()
    {
        OnValidate();
        specEffects = new ISpecialEffect[CountTypeEffects];
        m_CacheList.Clear();
        GetComponentsInChildren<ISpecialEffect>(true, m_CacheList);
#if UNITY_EDITOR
        if (m_CacheList.Count > specEffects.Length) Debug.LogError(GetType() + " error: Count register effects more than possible(can rewrite) on" + gameObject);
#endif
        for (int i = 0; i < m_CacheList.Count; i++)
        {
            m_CacheList[i].Init();
            Register(m_CacheList[i]);
        }
        //Debug.Log(specEffects[0]);
        //Debug.Log(specEffects[1]);
#if UNITY_EDITOR
        CheckData();
#endif
        SetMainAction();
    }

    void ByTimer()
    {
        if (curTimer <= 0f) Deactive();
        else curTimer -= TimeManager.TimeDeltaTime;
    }

    void SetMainAction()
    {
        switch (LiveTime)
        {
            case TypeLiveTime.EndLive: SetPriority(); break;
            case TypeLiveTime.ByTimer: curState = ByTimer; break;
            case TypeLiveTime.UnlimitLoop:
#if UNITY_EDITOR
                //Debug.LogError(GetType() + " error: " + TypeLiveTime.UnlimitLoop.ToString() + " not release on " + gameObject);
#endif
                curState = () => { };
                break;
        }

    }

    #region Priority

    void MaxPriority()
    {
        bool res = true;
        for (int i = 0; i < specEffects.Length; i++)
        {
            if (specEffects[i] != null) res &= specEffects[i].CheckEnd();
        }
        if (res) Deactive();
    }

    void ParticlePriority()
    {
        CheckEndEffect((int)TypeSpecialEffect.Particle);
    }
    void AudioPriority()
    {
        CheckEndEffect((int)TypeSpecialEffect.Audio);
    }

    void CheckEndEffect(int type)
    {
        if (specEffects[type] != null)
        {
            if (specEffects[type].CheckEnd()) Deactive();
        }
        else Deactive();
    }

    void SetPriority()
    {
        switch (Priority)
        {
            case TypePriority.Max: curState = MaxPriority; break;
            case TypePriority.Particle: curState = ParticlePriority; break;
            case TypePriority.Audio: curState = AudioPriority; break;
        }
    }

    #endregion

    #region CheckEditor

#if UNITY_EDITOR
    void CheckData()
    {
        bool res = false;
        for (int i = 0; i < specEffects.Length; i++) res |= (specEffects[i] != null);
        if (!res) Debug.Log(GetType() + " error: specEffects is empty(non-registers) on" + gameObject);
        if (LiveTime == TypeLiveTime.ByTimer)
        {
            if (Timer <= 0f) Debug.Log(GetType() + " error: Timer is bad(<=0) on" + gameObject);
            if (Priority == TypePriority.Audio && specEffects[(int)TypeSpecialEffect.Audio] == null)
                Debug.Log(GetType() + " error: With Priority(audio) no register audio effect on" + gameObject);
            if (Priority == TypePriority.Particle && specEffects[(int)TypeSpecialEffect.Particle] == null)
                Debug.Log(GetType() + " error: With Priority(particle) no register particle effect on" + gameObject);
            if (Priority == TypePriority.Max && (specEffects[(int)TypeSpecialEffect.Particle] == null && specEffects[(int)TypeSpecialEffect.Audio] == null))
                Debug.Log(GetType() + " error: With Priority(Max) no register particle or audio effect on" + gameObject);
        }
        if (LiveTime == TypeLiveTime.EndLive)
        {
            if (specEffects[(int)TypeSpecialEffect.Particle] == null && specEffects[(int)TypeSpecialEffect.Audio] == null)
                Debug.Log(GetType() + " error: No register particle or audio effect on" + gameObject);
        }
    }
#endif

    #endregion

    #endregion

    #region Public

    public void Active()
    {
        curTimer = Timer;
        for (int i = 0; i < specEffects.Length; i++)
            if (specEffects[i] != null) specEffects[i].Begin();
        gameObject.SetActive(true);
    }

    public void Deactive()
    {
        //transform.SetParent(rootParent, true);
        for (int i = 0; i < specEffects.Length; i++)
            if (specEffects[i] != null) specEffects[i].Stop();
        gameObject.SetActive(false);
    }

    public void SetRootParent(Transform par)
    {
        rootParent = par;
    }

    public void StopAudio()
    {
        EffectVisualStop(TypeSpecialEffect.Audio);
    }
    public void StopVisual()
    {
        EffectVisualStop(TypeSpecialEffect.Particle);
    }

    public void ManualUpdate()
    {
        curState();
    }

    #region SpecControl

    public bool Register(ISpecialEffect effect)
    {
        if (effect == null) return false;
        int type = (int)effect.Type;
        if (type < 0 && type > specEffects.Length) return false;
        if (specEffects[type] != null) return false;
        specEffects[type] = effect;
        return true;
    }

    public void UnRegister(ISpecialEffect effect)
    {
        if (effect == null) return;
        int type = (int)effect.Type;
        if (type < 0 && type > specEffects.Length) return;
        specEffects[type] = null;
    }

    #endregion

    #endregion

    #region Comments

    /*void GetAudio()
    {
        if (AS == null)
        {
            id = AudioController.RegisterFreeSource(out AS, AudioUnscaleTime);
            if (AS != null)
            {
                AS.transform.SetParent(transform, true);
                AS.transform.position = TransEx.V3Zero;
                AS.transform.rotation = TransEx.RotZero;
                AudioEffect = TypeAudioEffect.FreeRegister;
            }
#if UNITY_EDITOR
            else
            {
                AudioEffect = TypeAudioEffect.None;
#if UNITY_EDITOR
                Debug.LogError(GetType() + " error: AudioSource is null on" + gameObject);
#endif
            }
        }
        else id = AudioController.RegisterSource(AS, AudioUnscaleTime);
    }*/

    /*void OnEnable()
    {
        AudioController.PlaySoundAtPosition(Sound, transform.position, AudioController.AS_3D);
    }*/

    #endregion
}
