using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

//Прототипный аудиоконтроллер(разработка Песчанский С.)

/*public interface IAudioController
{
    bool SoundEnabled { get; set; }
    bool MusicEnabled { get; set; }
    float SoundVolume { get; set; }
    float MusicVolume { get; set; }
}*/

public static class AudioUtillity
{
    public static bool IsPlayingAccurate(this AudioSource source)
    {
        return source.isPlaying || AudioController.CheckPause(source.ignoreListenerPause);
    }
}

public class AudioController : MonoBehaviour//, IAudioController
{
#if UNITY_EDITOR
    [ContextMenu("ActiveSound")]
    void ActiveSound()
    {
        SoundEnabled = true;
        MusicEnabled = true;
    }
#endif


    static float musicVolume, soundVolume;
    public static Action Change;
    static bool dirtyChanged;

    //Множители звука, если извне наназвачать volume, то нужно перемножать на данное
    //public static float MusicVolumeMulti, SoundVolumeMulti;

    class FactorySource : IFactoryElement<AudioSourceData>
    {
        Transform parentSource;
        public FactorySource(Transform par = null)
        {
            parentSource = par;
        }

        public bool CreateElement(out AudioSourceData elem)
        {
            GameObject go = new GameObject("ASD");
            if (go != null)
            {
                elem = new AudioSourceData();
                elem.GO = go;
                elem.TF = elem.GO.transform;
                elem.TF.SetParent(parentSource, true);
                elem.AS = go.AddComponent<AudioSource>();
                elem.AS.playOnAwake = false;
                elem.AS.rolloffMode = AudioRolloffMode.Linear;
                /*elem.AS.volume = settingsSource.Volume;
                elem.AS.spatialBlend = settingsSource.Blend3DSound;
                elem.AS.loop = settingsSource.Loop;
                elem.AS.pitch = settingsSource.Pitch;
                elem.AS.minDistance = settingsSource.MinDist;
                elem.AS.maxDistance = settingsSource.MaxDist;*/
                return true;
            }
            else elem = null;
            return false;
        }
    }

    #region AudioControl

    public static void SetVolume(float _music, float _sound)
    {
        MusicVolume = _music;
        SoundVolume = _sound;
    }

    public static bool MusicEnabled
    {
        get { return musicVolume > 1e-5f; }
        set { MusicVolume = value ? 1f : 0f; }
    }

    public static float MusicVolume
    {
        get { return musicVolume; }
        set
        {
            musicVolume = value > 1f ? 1f : (value < 1e-5f ? 0f : value);
            musicAS.AS.volume = musicVolume;
            dirtyChanged = true;
            //if (Change != null) Change();
        }
    }

    public static bool SoundEnabled
    {
        get { return soundVolume > 1e-5f; }
        set { SoundVolume = value ? 1f : 0f; }
    }

    public static float SoundVolume
    {
        get { return soundVolume; }
        set
        {
            soundVolume = value > 1f ? 1f : (value < 1e-5f ? 0f : value);
            AudioListener.volume = soundVolume;
            dirtyChanged = true;
            //if (Change != null) Change();
        }
    }

    #endregion

    #region Data

    public readonly static AudioData DefaultAudioData;
    readonly static AudioSourceInfo DefaultAudioSourceInfo;
    static ObjectsPool<AudioSourceData> ASDFree;
    static List<AudioSourceData> ActiveElements, registeredElements;
    static AudioSourceData musicAS;
    static bool paused, deepPaused;
    static int maxID = 0;
    static bool isInit;

    #endregion

    #region OptimizationData
#pragma warning disable 649
#pragma warning disable 414
    class OptimisePoolNode
    {
        public AudioSourceData Elem;
        public float LastTimeUsed;
    }
#pragma warning restore 649
#pragma warning restore 414
    //static List<OptimisePoolNode> listInActiveElems;
    public float CheckTime = 60f, TimeToDelete = 180f;
    float curCheckTime;
    #endregion

    public const float DefaultMaxDistance = 100f, DefaultMinDistance = 1f;
    public const float DefaultLifeTime = -1f;
    public const int DefaultCapacity = 10;
    public const int AS_3D = 1, AS_Loop = 2, AS_Unscale = 4;
    public const int NULL_ID = -1;

    void InitStatic()
    {
        //Debug.Log("InitStatic");
        ActiveElements = new List<AudioSourceData>(DefaultCapacity);
        registeredElements = new List<AudioSourceData>(DefaultCapacity);
        //listInActiveElems = new List<OptimisePoolNode>(DefaultCapacity);
        Transform par = transform;
#if UNITY_EDITOR
        //GameObject go = new GameObject("ROOT_AUDIO");
        //if (go != null) par = go.transform;
#endif
        ASDFree = new ObjectsPool<AudioSourceData>(new FactorySource(par).CreateElement, DefaultCapacity);
        ASDFree.TryGet(out musicAS);
        musicAS.AS.ignoreListenerVolume = true;
        musicAS.AS.spatialBlend = 0f;
        maxID = NULL_ID;
        paused = deepPaused = false;
        isInit = true;
    }

    static AudioController()
    {
        DefaultAudioData = new AudioData()
        {
            Blend3DSound = 1f,
            MaxDist = DefaultMaxDistance,
            MinDist = DefaultMinDistance,
            Pitch = 1f,
            Volume = 1f
        };
        DefaultAudioSourceInfo = new AudioSourceInfo();
    }


    static void InitAudioSourceData(AudioSourceData asd, AudioSourceInfo info)
    {
        asd.TF.position = info.Pos;
        asd.AS.clip = info.Clip;

        asd.AS.volume = info.SettingsSource.Volume;
        asd.AS.spatialBlend = info.SettingsSource.Blend3DSound;
        asd.AS.loop = (info.SettingsSource.Type & AS_Loop) != 0;
        asd.AS.pitch = info.SettingsSource.Pitch;
        asd.AS.minDistance = info.SettingsSource.MinDist;
        asd.AS.maxDistance = info.SettingsSource.MaxDist;
        asd.AS.ignoreListenerVolume = false;//пока такой костыль для звука(не музыки)
        asd.AS.ignoreListenerPause = (info.SettingsSource.Type & AS_Unscale) != 0;
        float lt = info.SettingsSource.LifeTime;
        asd.UnLimit = lt < 0f && ((info.SettingsSource.Type & AS_Loop) != 0);
        asd.CurLifeTime = asd.LifeTime = lt > 0f ? lt : info.Clip.length;
        asd.UnscaleTime = (info.SettingsSource.Type & AS_Unscale) != 0;
        asd.ID = (maxID + 1) == NULL_ID ? 0 : (++maxID);
#if UNITY_EDITOR
        asd.GO.name = info.Clip.name;
#endif
    }

    #region PlaySound

    public static void Stop(ref int id)
    {
        if (id == NULL_ID) return;
        for (int i = ActiveElements.Count - 1; i >= 0; i--)
        {
            var elem = ActiveElements[i];
            if (elem.ID == id)
            {
                if (elem.AS != null) elem.AS.Stop();
                ReturnSource(elem, ActiveElements, i);
                break;
            }
        }
        id = NULL_ID;
    }

    public static int PlaySound(AudioSourceInfo info)
    {
        if (!isInit && info == null || info.Clip == null) return NULL_ID;
        AudioSourceData asd;
        if (!ASDFree.TryGet(out asd))
        {
            //CheckElements need
            return NULL_ID;
        }
        InitAudioSourceData(asd, info);
        asd.pool = ASDFree;
        asd.IsPool = true;
        //asd.IsMusic = info.IsMusic;
        //ActiveIDs.Add(maxID);
        //countIDs++;
        //Debug.LogError("GetInstanceID=" + asd.AS.GetInstanceID());
        ActiveElements.Add(asd);
        //if (asd.GO.name == "RUS_M1_D1_S1") Debug.Log("YES");
        asd.AS.Play(0);
        if (CheckPause(asd.UnscaleTime)) asd.AS.Pause();
        //if (!asd.UnscaleTime && (paused || deepPaused)) asd.AS.Pause();
        return asd.ID;
    }

    public static int PlaySoundAtPosition(AudioClip clip, Vector3 pos, int type = 0, float volume = 1f, float lifeTime = DefaultLifeTime)
    {
        DefaultAudioSourceInfo.Clip = clip;
        DefaultAudioSourceInfo.Pos = pos;
        DefaultAudioSourceInfo.SettingsSource = DefaultAudioData;
        DefaultAudioSourceInfo.SettingsSource.Type = type;
        DefaultAudioSourceInfo.SettingsSource.LifeTime = lifeTime;
        DefaultAudioSourceInfo.SettingsSource.Volume = volume < 0f ? 0f : volume;
        DefaultAudioSourceInfo.SettingsSource.Blend3DSound = type & AS_3D;
        return PlaySound(DefaultAudioSourceInfo);
    }

    public static void PlayMusic(AudioClip clip, int type, float volume = 1f, float lifeTime = DefaultLifeTime)
    {
        if (clip == null) return;
        musicAS.AS.Stop();
        musicAS.AS.clip = clip;
        musicAS.AS.volume = volume < 0f ? 0f : volume;
        musicAS.UnscaleTime = (type & AS_Unscale) != 0;
        musicAS.AS.ignoreListenerPause = (type & AS_Unscale) != 0;
        float lt = lifeTime;
        musicAS.CurLifeTime = musicAS.LifeTime = lt > 0f ? lt : clip.length;
        if (CheckPause(musicAS.UnscaleTime)) musicAS.AS.Pause();
    }

    public static void StopMusic()
    {
        musicAS.AS.Stop();
    }

    #endregion

    public static bool IsPause { get { return paused; } }
    public static bool IsDeepPause { get { return deepPaused; } }
    public static bool IsPlaying(int id)
    {
        for (int i = ActiveElements.Count - 1; i >= 0; --i)
        {
            if (id == ActiveElements[i].ID)
            {
                return ActiveElements[i].AS.IsPlayingAccurate();
            }
        }
        return false;
    }

    public static bool CheckPause(bool unscale = false)
    {
        return (unscale && deepPaused) || (!unscale && (paused || deepPaused));
    }

    public int GetSourceID(AudioSource auSo)
    {
        if (!(auSo == null))
        {
            int hashcode = auSo.GetInstanceID();
            for (int i = 0; i < registeredElements.Count; i++)
            {
                if (registeredElements[i].AS.GetInstanceID() == hashcode) return registeredElements[i].ID;
            }
        }
        return NULL_ID;//not found
    }

    #region Register and Return/remove

    public static void RemoveSource(int source)
    {
        if (source == NULL_ID) return;
        for (int i = ActiveElements.Count - 1; i >= 0; i--)
        {
            AudioSourceData asd = ActiveElements[i];
            if (asd.ID == source)
            {
                ReturnSource(asd, ActiveElements, i);
                return;
            }
        }
#if UNITY_EDITOR
        Debug.LogError(typeof(AudioController) + " error: Sound's ID not found");
#endif
    }

    public static void RemoveSource(AudioSource auSo)
    {
        if (auSo == null)
        {
#if UNITY_EDITOR
            Debug.LogError(typeof(AudioController) + " error: RemoveSource AudioSource is null");
#endif
            return;
        }
        for (int i = ActiveElements.Count - 1; i >= 0; i--)
        {
            AudioSourceData asd = ActiveElements[i];
            if (asd.AS == auSo)
            {
                ReturnSource(asd, ActiveElements, i);
                return;
            }
        }
#if UNITY_EDITOR
        Debug.LogError(typeof(AudioController) + " error: Sound's ID not found");
#endif
    }

    public static int RegisterSource(AudioSource source, bool unscaleTime = false)
    {
        if (source == null)
        {
#if UNITY_EDITOR
            Debug.LogError(typeof(AudioController) + " error: RegisterSource Register Audio is null");
#endif
            return NULL_ID;
        }
        AudioSourceData asd = new AudioSourceData();
        asd.AS = source;
        asd.GO = source.gameObject;
        asd.TF = asd.GO.transform;
        asd.UnscaleTime = unscaleTime;
        asd.AS.ignoreListenerVolume = false;//пока такой костыль для звука(не музыки)
        asd.AS.ignoreListenerPause = unscaleTime;
        asd.ID = (maxID + 1) == NULL_ID ? 0 : (++maxID);
        asd.pool = ASDFree;
        asd.IsPool = true;
        registeredElements.Add(asd);
        return asd.ID;
    }

    public static int RegisterFreeSource(out AudioSource source, bool unscaleTime = false)
    {
        AudioSourceData asd;
        if (!ASDFree.TryGet(out asd))
        {
#if UNITY_EDITOR
            Debug.LogError(typeof(AudioController) + " error: RegisterFreeSource GetElement is null");
#endif
            source = null;
            return NULL_ID;
        }
        source = asd.AS;
        asd.UnscaleTime = unscaleTime;
        asd.AS.ignoreListenerVolume = false;//пока такой костыль для звука(не музыки)
        asd.AS.ignoreListenerPause = unscaleTime;
        asd.AS.loop = false;
        asd.ID = (maxID + 1) == NULL_ID ? 0 : (++maxID);
        asd.pool = ASDFree;
        asd.IsPool = true;
        //asd.IsMusic = isMusic;
        registeredElements.Add(asd);
        return asd.ID;
    }

    public static void UnRegisterSource(ref int source)
    {
        if (source == NULL_ID) return;
        for (int i = registeredElements.Count - 1; i >= 0; i--)
        {
            AudioSourceData asd = registeredElements[i];
            if (asd.ID == source)
            {
                registeredElements.RemoveAt(i);
                source = NULL_ID;
                //ReturnSource(asd, registeredElements, ref countRegElems, i);
                return;
            }
        }
        source = NULL_ID;
#if UNITY_EDITOR
        Debug.LogError(typeof(AudioController) + " error: Sound's ID not found");
#endif
    }

    public static int UnRegisterSource(AudioSource auSo)
    {
        if (auSo == null) return NULL_ID;
        int hasCode = auSo.GetInstanceID();
        for (int i = registeredElements.Count - 1; i >= 0; i--)
        {
            AudioSourceData asd = registeredElements[i];
            if (asd.AS.GetInstanceID() == hasCode)
            {
                registeredElements.RemoveAt(i);
                //ReturnSource(asd, registeredElements, ref countRegElems, i);
                return NULL_ID;
            }
        }
#if UNITY_EDITOR
        Debug.LogError(typeof(AudioController) + " error: AudioSource not found");
#endif
        return NULL_ID;
    }

    static void ReturnSource(AudioSourceData asd, List<AudioSourceData> list, int localInd)
    {
        list.RemoveAt(localInd);

        #region AudioSourceIsNull
        bool isDestroy = asd.GO == null;
        if (isDestroy || asd.AS == null)
        {
            if (!isDestroy) GameObject.Destroy(asd.GO, 0f);
            asd = null;
#if UNITY_EDITOR
            Debug.LogError(typeof(AudioController) + " error: ReturnSource is null");
#endif
            return;
        }
        #endregion

        asd.AS.Stop();
        if (asd.IsPool) asd.pool.Return(asd);
        else
        {
            if (asd.GO != null) GameObject.Destroy(asd.GO, 0f);
            asd = null;
        }
    }

    #endregion

    #region Pause
    static void DeepPauseList(List<AudioSourceData> list, bool pauseState)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            AudioSourceData asd = list[i];

            #region AudioSourceIsNull
            if (asd.AS == null)
            {
                list.RemoveAt(i);
                if (asd.GO != null) GameObject.Destroy(asd.GO);
                asd = null;
#if UNITY_EDITOR
                Debug.LogError(typeof(AudioController) + " error: ReturnSource is null");
#endif
                continue;
            }
            #endregion

            if (asd.UnscaleTime)
            {
                if (pauseState) asd.AS.Pause();
                else asd.AS.UnPause();
            }
            else
            {
                if (pauseState)
                {
                    if (!paused) asd.AS.Pause();
                }
                else
                {
                    if (!paused) asd.AS.UnPause();
                }
            }
        }
    }


    public static void Pause(bool state)
    {
        paused = state;
        AudioListener.pause = state;
    }

    public static void DeepPause(bool state)
    {
        if (deepPaused == state) return;
        deepPaused = state;
        DeepPauseList(ActiveElements, state);
        DeepPauseList(registeredElements, state);
    }
    #endregion

    static OptimisePoolNode node;
    public static void ManualUpdate(float timeDelta, float timeUnscaleDelta)
    {
        if (dirtyChanged)
        {
            dirtyChanged = false;
            if (Change != null) Change();
        }
        if (deepPaused) return;
        AudioSourceData asd;
        for (int i = ActiveElements.Count - 1; i >= 0; i--)
        {
            asd = ActiveElements[i];
            if (asd.UnLimit || (!asd.UnscaleTime && paused)) continue;
            if (asd.CurLifeTime <= 0f) ReturnSource(asd, ActiveElements, i);
            else asd.CurLifeTime -= asd.UnscaleTime ? timeUnscaleDelta : timeDelta;
        }

        #region OptimizationSource
        /*float unscale = TimeManager.TimeUnscaleTime;
        if ((curCheckTime+ CheckTime) < unscale || TimeManager.TimeScaleTime==0f)
        {
            curCheckTime = unscale;
            for(int i = listInActiveElems.Count - 1; i >= 0; i--)
            {
                node = listInActiveElems[i];
                if (node.LastTimeUsed+ TimeToDelete< unscale)
                {
                    node.Elem.pool.RemoveElement(node.Elem);
                    listInActiveElems.RemoveAt(i);
                    Destroy(node.Elem.GO);
                }
            }
        }*/
        #endregion

    }

    #region MonoBehaviour

    void Awake()
    {
        if (isInit)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        InitStatic();
    }

    void OnDestroy()
    {
        isInit = false;
        dirtyChanged = false;
    }
    #endregion
}

public class AudioSourceInfo
{
    public AudioClip Clip;
    public AudioData SettingsSource;
    public Vector3 Pos;
    /*public bool UnscaleTime;
    public float LifeTime;*/
    //public bool IsMusic;//Если false то это звук, если true то музыка
}

public struct AudioData
{
    public float Volume;
    public float Pitch;
    public float MinDist, MaxDist;
    public float LifeTime;
    public float Blend3DSound;
    public int Type;
    /*public float Blend3DSound;
    public bool Loop;
    public bool UnscaleTime;*/
}

//Standart -2D sound
//Dynamic - 3D sound in static position
//ContinueDynamic - 3D sound in target position
/*public enum TypeAudio { Standart2D, Standart3D, Unique }
//Static -2D sound
//Dynamic - 3D sound in static position
//ContinueDynamic - 3D sound in target position
public enum TypePositionAudio { Static, Dynamic, ContinueDynamic }

public enum TypeAudioSourceData { Free, Register }*/

public class AudioSourceData
{
    public Transform TF;
    public GameObject GO;
    public AudioSource AS;
    public ObjectsPool<AudioSourceData> pool;

    public float LifeTime, CurLifeTime;
    public bool UnscaleTime, IsPool;
    public bool UnLimit;
    //public bool IsMusic;//Если false то это звук, если true то музыка
    public int ID;

}
#if UNITY_EDITOR
[CustomEditor(typeof(AudioController))]
public class AudioControllerEditor : Editor
{
    bool isPaused = false;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        //AudioController ta = (AudioController)target;

        if (GUILayout.Button("Pause"))
        {
            AudioController.Pause(!isPaused);
            isPaused = !isPaused;
            //			if (Time.timeScale > 0f)
            //			{
            //				Time.timeScale = 0f;
            //				ta.Pause(true);
            //			}
            //			else
            //			{
            //				Time.timeScale = 1f;
            //				ta.Pause(false);
            //			}
        }
    }
}
#endif
