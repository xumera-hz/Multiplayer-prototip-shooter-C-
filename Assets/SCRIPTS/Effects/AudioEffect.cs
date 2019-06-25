using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(AudioSource))]
public class AudioEffect : MonoBehaviour, ISpecialEffect
{
    //public AudioClip Sound;
    //public enum TypeAudioEffect { None, Default, Register, FreeRegister }
    //public TypeAudioEffect TypeAudio;
    [SerializeField] bool UnscaleTime;
    AudioSource AS;
    int id;
    bool isInit;
    TypeSpecialEffect type = TypeSpecialEffect.Audio;

    [SerializeField] AudioClip[] Clips;

    public TypeSpecialEffect Type { get { return type; } }

    void OnDestroy()
    {
        AudioController.UnRegisterSource(ref id);
    }

    void Awake()
    {
        if(!isInit) Init();
    }


    public bool CheckEnd()
    {
        return !AS.isPlaying;
    }

    public void Stop()
    {
        AS.Stop();
    }

    public void Init()
    {
        AS = GetComponent<AudioSource>();
        if (AS == null) AS = gameObject.AddComponent<AudioSource>();
        id = AudioController.RegisterSource(AS, UnscaleTime);
        //AS.clip = Sound;
#if UNITY_EDITOR
        CheckEditor();
#endif
        isInit = true;
    }
#if UNITY_EDITOR
    void CheckEditor()
    {
        if (AS == null) Debug.Log(GetType() + " error: AudioSource is NULL(maybe manual add) on " + gameObject);
        //else if (AS.clip == null) Debug.Log(GetType() + " error: Audioclip is NULL on " + gameObject);
        if (id == AudioController.NULL_ID) Debug.Log(GetType() + " error: AudioSource is NULL on " + gameObject);
    }
#endif

    public void VisualStop()
    {
        throw new NotImplementedException();
    }

//   void Start()
//    {
//        //Debug.Log("EXP POS"+transform.position);
//        if (Sound != null) { AS.clip = Sound; AS.Play(); }
//#if UNITY_EDITOR
//        else Debug.LogError(GetType() + " error: Sound clip is null on " + gameObject);
//#endif
//    }

    public void Begin()
    {
        AS.Stop();
        if(Clips.Length>0) AS.clip = Clips[UnityEngine.Random.Range(0, Clips.Length)];
        AS.Play(0);
    }

    public void FullReset()
    {
        Begin();
    }
}
