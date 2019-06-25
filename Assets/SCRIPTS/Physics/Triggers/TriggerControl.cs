using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TriggerControl : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField]
    protected bool m_IsLog;
#endif
    protected ColliderObjects m_TriggerObjects;
    bool m_IsInit;

    Coroutine m_CheckTriggerCoroutine;
    protected bool m_IgnoreTriggerEnter;

    public event Action<Collider, bool> EnterToTrigger, ExitFromTrigger;
    //цель, состояние, условие
    public event Action<Collider, bool, bool> EventTrigger;

    public int Count { get { return m_TriggerObjects.Count; } }
    public Collider this[int index] { get { return m_TriggerObjects.Objs[index]; } }

    protected void ImprovedStopCoroutine(Coroutine coroutine)
    {
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = null;
    }

    protected virtual bool CheckConditions(Collider cld)
    {
        return !cld.IsNullOrDestroy();
    }

    protected virtual void Init()
    {
        if (m_IsInit) return;
        m_TriggerObjects = new ColliderObjects();
        m_TriggerObjects.SetConditions(CheckConditions);
        m_TriggerObjects.SetTriggerControl(this);
        m_IsInit = true;
    }

    protected class ColliderObjects : SomeTriggerObject<Collider>
    {


        public void SetTriggerControl(TriggerControl control)
        {
            m_Control = control;
        }

        TriggerControl m_Control;
        public bool Validation()
        {
            for (int i = m_Objs.Count - 1; i >= 0; i--)
            {
                var cld = m_Objs[i];
                if (cld.IsNullOrDestroy())
                {
                    m_Objs.RemoveAt(i);
                    continue;
                }
                bool res = CheckValidation(cld);
                if (!res)
                {
                    m_Objs.RemoveAt(i);
                    m_Control.CallExitFromTrigger(cld, true);
                }
            }
            return true;
        }

        bool CheckValidation(Collider cld)
        {
            return cld.enabled && cld.gameObject.activeInHierarchy;
        }
    }

    //улучшенный ебанный костыль
    IEnumerator CheckTriggerObjects()
    {
        while (true)
        {
            m_TriggerObjects.Validation();
            yield return null;
        }
    }

    void CallExitFromTrigger(Collider cld, bool cond)
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("CallExitFromTrigger=" + cld + " cond=" + cond);
#endif
        if (cond)
        {
            if (m_TriggerObjects.Count <= 0) ImprovedStopCoroutine(m_CheckTriggerCoroutine);
        }
        OnExitFromTrigger(cld, cond);
        if (ExitFromTrigger != null) ExitFromTrigger(cld, cond);
        if (EventTrigger != null) EventTrigger(cld, false, cond);
    }
    void CallEnterToTrigger(Collider cld, bool cond)
    {
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("CallEnterToTrigger=" + cld + " cond=" + cond);
#endif
        if (cond)
        {
            if (m_CheckTriggerCoroutine == null) m_CheckTriggerCoroutine = StartCoroutine(CheckTriggerObjects());
        }
        OnEnterToTrigger(cld, cond);
        if (EnterToTrigger != null) EnterToTrigger(cld, cond);
        if (EventTrigger != null) EventTrigger(cld, true, cond);
    }

    protected void OnTriggerEnter(Collider cld) { if (m_IgnoreTriggerEnter) return; CallEnterToTrigger(cld, m_TriggerObjects.AddObject(cld)); }
    protected void OnTriggerExit(Collider cld) { CallExitFromTrigger(cld, m_TriggerObjects.RemoveObject(cld)); }

    protected virtual void OnEnterToTrigger(Collider sender, bool condition) { }
    protected virtual void OnExitFromTrigger(Collider sender, bool condition) { }
    protected virtual void Disable() { }
    protected virtual void OnAwake() { }
    protected void Clear()
    {
        m_TriggerObjects.Reset();
        ImprovedStopCoroutine(m_CheckTriggerCoroutine);
    }

    private void Awake()
    {
        Init();
        OnAwake();
    }

    void OnDisable()
    {
        Clear();
        Disable();
    }

    //Костыли
    #region Manual Funcs

    public void ManualExit(Collider cld)
    {
        if (cld.IsNullOrDestroy()) return;
        Init();
        OnTriggerExit(cld);
    }

    public void ManualExit(GameObject go)
    {
        if (go == null) return;
        ManualExit(go.GetComponent<Collider>());
    }

    public void ManualEnter(Collider cld)
    {
        if (cld.IsNullOrDestroy()) return;
        Init();
        OnTriggerEnter(cld);
    }

    public void ManualEnter(GameObject go)
    {
        if (go == null) return;
        ManualEnter(go.GetComponent<Collider>());
    }

    #endregion
}

public abstract class TriggerObjects<T>
{
    Predicate<T> m_Conditions;
    public abstract void Reset();
    //public abstract bool CheckConditions();
    public void SetConditions(Predicate<T> cond)
    {
        m_Conditions = cond;
    }
    public bool AddObject(T obj)
    {
        bool res = m_Conditions == null || m_Conditions(obj);
        return res && InnerAdd(obj);
    }
    public abstract bool RemoveObject(T obj);
    protected abstract bool InnerAdd(T obj);
    public TriggerObjects() { }
    public TriggerObjects(Predicate<T> cond)
    {
        SetConditions(cond);
    }
}

public class OneTriggerObject<T> : TriggerObjects<T> where T : class
{
    T m_Obj;
    //public T Obj { get { return m_Obj; } }
    public OneTriggerObject() { }
    public OneTriggerObject(Predicate<T> cond) : base(cond) { }

    public override void Reset()
    {
        m_Obj = null;
    }
    protected override bool InnerAdd(T obj)
    {
        m_Obj = obj;
        return true;
    }
    public override bool RemoveObject(T obj)
    {
        bool res = m_Obj == obj;
        if (res) m_Obj = null;
        return res;
    }

    //public override bool CheckConditions()
    //{
    //    return m_Conditions == null || m_Conditions(m_Obj);
    //}
}

public class SomeTriggerObject<T> : TriggerObjects<T> where T : class
{
    protected List<T> m_Objs;

    public List<T> Objs { get { return m_Objs; } }
    public int Count { get { return m_Objs.Count; } }
    public SomeTriggerObject() : this(10) { }
    public SomeTriggerObject(int cap) { m_Objs = new List<T>(cap); }
    public SomeTriggerObject(Predicate<T> cond) : this(10, cond) { }

    public SomeTriggerObject(int cap, Predicate<T> cond) : base(cond)
    {
        m_Objs = new List<T>(cap);
    }

    public bool Contains(T obj)
    {
        return m_Objs.Contains(obj);
    }

    public override void Reset()
    {
        m_Objs.Clear();
    }
    protected override bool InnerAdd(T obj)
    {
        bool res = !m_Objs.Contains(obj);
        if (res) m_Objs.Add(obj);
        return res;
    }
    public override bool RemoveObject(T obj)
    {
        int index = m_Objs.IndexOf(obj);
        if (index != -1) m_Objs.RemoveAt(index);
        return index != -1;
    }

    //public override bool CheckConditions()
    //{
    //    if (m_Conditions == null) return true;
    //    for (int i = m_Objs.Count - 1; i >= 0; i--)
    //    {
    //        bool res = m_Conditions(m_Objs[i]);
    //        if (!res) return false;
    //    }
    //    return true;
    //}
}

public struct ColliderTriggerArgs
{
    public Collider Object;
    public bool EnterConditions;
    public bool TriggerConditions;
}
