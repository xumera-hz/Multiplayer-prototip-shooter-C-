using UnityEngine;
using System;
using System.Collections.Generic;

public struct AnimationData
{
    bool isPlay;
    string name;
    float transDurat;
    public int layer;
    float time;
    bool isFixed;
    public bool isHash;
    public int hash;

    public AnimationData(int _hash, float _transDurat, int _layer, float _time, bool _isFixed)
    {
        if (_layer < 0) _layer = 0;
        hash = _hash; transDurat = _transDurat;
        isHash = true; layer = _layer; isFixed = _isFixed;
        time = _time; name = null;
        isPlay = true;
    }
    public AnimationData(string _name, float _transDurat, int _layer, float _time, bool _isFixed)
    {
        if (_layer < 0) _layer = 0;
        hash = 0; transDurat = _transDurat;
        isHash = false; layer = _layer; isFixed = _isFixed;
        time = _time; name = _name;
        isPlay = true;
    }

    public void Set(Animator anim)
    {
        if (!isPlay)
        {
#if UNITY_EDITOR
           // Debug.LogWarning(GetType() + " You call cross disabled animation on " + anim.gameObject);
#endif
            return;
        }
        //if(!anim.gameObject.IsPlayer()) Debug.LogError(Time.frameCount+" Layer="+layer+" PlayAnim=" + hash + " " + Animator.StringToHash("Down.AnyAction"));
        if (isFixed)
        {
            if (isHash) anim.CrossFadeInFixedTime(hash, transDurat, layer, time);
            else anim.CrossFadeInFixedTime(name, transDurat, layer, time);
        }
        else
        {
            if (isHash) anim.CrossFade(hash, transDurat, layer, time);
            else anim.CrossFade(name, transDurat, layer, time);
        }
    }

    public bool Play { get { return isPlay; } set { isPlay = value; } }
}

public interface AnimatorStateCallback
{
    void AnimationStart(int ID, AnimatorStateInfo state, int layer);
    void AnimationExit(int ID, AnimatorStateInfo state, int layer);
    void AnimationEnd(int ID, AnimatorStateInfo state, int layer);
}

public class AnimatorWrapper : MonoBehaviour, AnimatorStateCallback
{
    public event Action<int> AnimStart, AnimExit, AnimEnd;//with hash and layer
    [NonSerialized] AnimatorOverrideController m_AnimOverride;
    [SerializeField] protected Animator m_Anim;

    //LocalAnimatorStateInfo[] m_LayerInfo, m_PrevLayerInfo;
    FullAnimatorStateInfo m_LayerInfo;
    //LocalAnimatorStateInfo[] m_LayerInfo;

#if UNITY_EDITOR
    public bool IsLog = false;
#endif

    class FullAnimatorStateInfo
    {
        public LocalAnimatorStateInfo[] Prev;
        public LocalAnimatorStateInfo[] Cur;

        public FullAnimatorStateInfo(int count)
        {
            Prev = new LocalAnimatorStateInfo[count];
            Cur = new LocalAnimatorStateInfo[count];
            for(int i=0;i< Prev.Length; i++)
            {
                Prev[i] = new LocalAnimatorStateInfo();
                Cur[i] = new LocalAnimatorStateInfo();
            }
        }

        public void CheckError(Animator anim)
        {
            //проверить нужное состояние с текущим, и если они разные, то перезапустить анимации
            Debug.LogError("Не сделано");
        }

        public void GetPrevLayersInfo(Animator anim)
        {
            for (int i = 0; i < Prev.Length; i++) Prev[i].Get(anim, i);
        }
        public void GetCurLayersInfo(Animator anim)
        {
            for (int i = 0; i < Cur.Length; i++) Cur[i].Get(anim, i);
        }

        public void SetPrevLayersInfo(Animator anim)
        {
            for (int i = 0; i < Prev.Length; i++) Prev[i].Set(anim, i);
        }
        public void SetCurLayersInfo(Animator anim)
        {
            for (int i = 0; i < Cur.Length; i++) Cur[i].Set(anim, i);
        }
    }

    class LocalAnimatorStateInfo
    {
        public AnimatorStateInfo CurInfo;
        public AnimatorStateInfo NextInfo;
        public bool IsNext;
        public float Weight;

        public void Get(Animator anim, int layer)
        {
            //без этой строчки, при переключении снайперка - базука, выдает warning в лог
            //и косячит анимацию. С этой строчкой, лога нет, но также косячит анимацию
            //if(!anim.hasBoundPlayables) anim.Update(0f);

            CurInfo = anim.GetCurrentAnimatorStateInfo(layer);
            NextInfo = anim.GetNextAnimatorStateInfo(layer);
            Weight = anim.GetLayerWeight(layer);
            IsNext = NextInfo.fullPathHash != 0;
        }

        public void Set(Animator anim, int layer)
        {
            anim.SetLayerWeight(layer, Weight);
            anim.Play(CurInfo.fullPathHash, layer, CurInfo.normalizedTime);
            if (IsNext) anim.Play(NextInfo.fullPathHash, layer, NextInfo.normalizedTime);
        }
    }

    void InitAnimator()
    {
        if (m_Anim == null) m_Anim = GetComponentInChildren<Animator>(true);
        var animGO = m_Anim.gameObject;
        //если объекты разные, надо знать если вдруг вырубят объект в аниматором(очищает данные при выключении GO)
        //if (animGO != null && animGO != gameObject)
        //{
        //    var events = animGO.GetComponent<GameObjectEvents>();
        //    if (events == null) animGO.AddComponent<GameObjectEvents>();
        //}
    }

    protected virtual void Init() { }

    List<string> names;
    List<AnimationClip> clips;
    private void Awake()
    {
        InitAnimator();
        m_AnimOverride = new AnimatorOverrideController();
        m_AnimOverride.runtimeAnimatorController = m_Anim.runtimeAnimatorController;
        m_Anim.runtimeAnimatorController = m_AnimOverride;
        int layers = m_Anim.layerCount;
        m_LayerInfo = new FullAnimatorStateInfo(layers);
        m_CurrentAnimation = new AnimationData[layers];
        //#pragma warning disable 618
        //        int count = m_AnimOverride.clips.Length;
        //#if UNITY_EDITOR
        //        Debug.LogWarning(GetType() + " Obsolete");
        //#endif
        //#pragma warning restore 618
        int count = m_AnimOverride.overridesCount;
        clips = new List<AnimationClip>(count);
        names = new List<string>(count);
        Init();
    }

    #region OverrideAnim

    public void AddOverrideAnim(string _name, AnimationClip clip)
    {
        //throw new ArgumentNullException();
        if (clip == null || string.IsNullOrEmpty(_name)) return;
#if UNITY_EDITOR
        if(IsLog) Debug.LogError("AddOverrideAnim=" + _name + " " + clip.name);
#endif
        //if(!this.IsPlayer()) Debug.LogError("AddOverrideAnim="+ _name+" "+ clip.name);
        clip.name = _name;
        int ind = names.IndexOf(_name);
        if (ind == -1)
        {
            names.Add(_name);
            clips.Add(clip);
        }
        else
        {
            names[ind] = _name;
            clips[ind] = clip;
        }
        //OverrideAnim();
    }
    public void AddOverrideAnim(AnimationInfo info)
    {
        AddOverrideAnim(info.Name, info.Clip);
    }
    public void AddOverrideAnim(AnimationInfo[] info)
    {
        if (info == null) return;
        for (int i = 0; i < info.Length; i++) AddOverrideAnim(info[i]);
    }

    //Проверяем что щас играет нужна анимация
    void CheckErrorInAnimator()
    {
        m_LayerInfo.CheckError(m_Anim);
    }

    public void OverrideAnim()
    {
        int count = names.Count;
        if (count == 0) return;
        m_LayerInfo.GetCurLayersInfo(m_Anim);
        // Do swap clip in override controller here
        for (int i = count - 1; i >= 0; i--) m_AnimOverride[names[i]] = clips[i];
        names.Clear();
        clips.Clear();
        // Force an update
        m_Anim.Update(0.01f);

        // Push back state
        m_LayerInfo.SetCurLayersInfo(m_Anim);
    }

    #endregion

    public Animator Anim { get { return m_Anim; } }

    void AnimatorStateCallback.AnimationStart(int ID, AnimatorStateInfo state, int layer) { OnAnimationStart(ID, state, layer); if (AnimStart != null) AnimStart(ID); }
    void AnimatorStateCallback.AnimationExit(int ID, AnimatorStateInfo state, int layer) { OnAnimationExit(ID, state, layer); if (AnimExit != null) AnimExit(ID); }
    void AnimatorStateCallback.AnimationEnd(int ID, AnimatorStateInfo state, int layer) { OnAnimationEnd(ID, state, layer); if (AnimEnd != null) AnimEnd(ID); }

    protected virtual void OnAnimationStart(int ID, AnimatorStateInfo state, int layer) { }
    protected virtual void OnAnimationExit(int ID, AnimatorStateInfo state, int layer) { }
    protected virtual void OnAnimationEnd(int ID, AnimatorStateInfo state, int layer) { }


    #region PlayAnimation
    protected AnimationData[] m_CurrentAnimation;

    #region CrossFade
    public void CrossFadeInFixedTime(string _name, float TransDurat, int _layer = -1, float fixedTime = 0f)
    {
        //if(!this.IsPlayer()) Debug.LogError("CrossFadeInFixedTime="+ _layer + " frame=" + Time.frameCount);
        SetAnimationData(new AnimationData(_name, TransDurat, _layer, fixedTime, true));
    }
    public void CrossFadeInFixedTime(int hash, float TransDurat, int _layer = -1, float fixedTime = 0f)
    {
        //if (!this.IsPlayer()) Debug.LogError("CrossFadeInFixedTime=" + _layer + " frame=" + Time.frameCount);
        SetAnimationData(new AnimationData(hash, TransDurat, _layer, fixedTime, true));
    }
    public void CrossFade(string _name, float TransDurat, int _layer = -1, float normalTime = float.NegativeInfinity)
    {
        //if (!this.IsPlayer()) Debug.LogError("CrossFadeInFixedTime=" + _layer + " frame=" + Time.frameCount);
        SetAnimationData(new AnimationData(_name, TransDurat, _layer, normalTime, false));
    }
    public void CrossFade(int hash, float TransDurat, int _layer = -1, float normalTime = float.NegativeInfinity)
    {
        //if (!this.IsPlayer()) Debug.LogError("CrossFadeInFixedTime=" + _layer + " frame=" + Time.frameCount);
        SetAnimationData(new AnimationData(hash, TransDurat, _layer, normalTime, false));
    }

    protected void SetAnimationData(AnimationData data)
    {
        if (data.layer >= m_CurrentAnimation.Length) return;
        m_CurrentAnimation[data.layer] = data;
    }
    #endregion

    //Пока напрямую
    public void SetFloat(int hash, float value, float damp, float delta)
    {
        if (m_Anim.isActiveAndEnabled) m_Anim.SetFloat(hash, value, damp, delta);
    }
    public void SetFloat(string name, float value, float damp, float delta)
    {
        if (m_Anim.isActiveAndEnabled) m_Anim.SetFloat(name, value, damp, delta);
    }
    public void SetLayerWeight(int layer, float value)
    {
        if (m_Anim.isActiveAndEnabled) m_Anim.SetLayerWeight(layer, value);
    }
    public float GetLayerWeight(int layer)
    {
#if UNITY_EDITOR
        Debug.LogError("GetLayerWeight may be isActiveAndEnabled");
#endif
        return m_Anim.GetLayerWeight(layer);
    }

    public void CrossFadeManual(AnimationData info) { SetAnimationData(info); }
    public void StopPlayAnimation(int layer) { if (layer < 0) layer = 0; if(layer< m_CurrentAnimation.Length) m_CurrentAnimation[layer].Play = false; }

    public virtual void CallAnimationPlay()
    {
        //if (!this.IsPlayer()) Debug.LogError("CallAnimationPlay="+Time.frameCount);
        for (int i = 0; i < m_CurrentAnimation.Length; i++)
        {
            m_CurrentAnimation[i].Set(m_Anim);
            m_CurrentAnimation[i].Play = false;
        }
    }
    #endregion

    //void Update() {
    //    OverrideAnim();
    //}

    void LateUpdate()
    {
        if (m_Anim.isActiveAndEnabled && m_Anim.hasBoundPlayables)
        {
            OverrideAnim();
            CallAnimationPlay();
        }
    }
}

public enum StateOverrideAnimation { Override, None }

[Serializable]
public struct AnimationInfo
{
    public string Name;
    public StateOverrideAnimation State;
    public AnimationClip Clip;
}
