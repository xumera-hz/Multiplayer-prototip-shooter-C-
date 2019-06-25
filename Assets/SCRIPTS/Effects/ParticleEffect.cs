using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleEffect : MonoBehaviour, ISpecialEffect
{
    ParticleSystem[] PS;
    TypeSpecialEffect type = TypeSpecialEffect.Particle;
    bool isInit;

    public TypeSpecialEffect Type { get { return type; } }

    public bool CheckEnd()
    {
        bool res = true;
        for (int i = 0; i < PS.Length; i++)
        {
            res = !PS[i].IsAlive(true);
            if (!res) return false;
        }
        return res;
    }

    public void Stop()
    {
        for (int i = 0; i < PS.Length; i++)
        {
            PS[i].Stop();
        }
    }

    void Awake()
    {
        if(!isInit) Init();
    }

    public void Init()
    {
        PS = GetComponentsInChildren<ParticleSystem>();
#if UNITY_EDITOR
        CheckEditor();
#endif
        isInit = true;
    }
#if UNITY_EDITOR
    void CheckEditor()
    {
        if (PS == null) Debug.LogError(GetType() + " error: " + PS.GetType() + " is NULL on " + gameObject);
    }
#endif

    public void VisualStop()
    {
        throw new NotImplementedException();
    }

    /*void OnEnable()
    {
        if (Sound != null) AudioController.PlaySoundAtPosition(Sound, transform.position, AudioController.AS_3D);
#if UNITY_EDITOR
        else Debug.LogError(GetType() + " error: Sound clip is null on " + gameObject);
#endif
    }*/

    public void Begin()
    {
        for (int i = 0; i < PS.Length; i++)
        {
            PS[i].Stop();
            PS[i].Play(true);
        }
    }

    public void FullReset()
    {
        Begin();
    }
}
