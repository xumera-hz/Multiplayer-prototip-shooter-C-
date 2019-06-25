using UnityEngine;
using System;

public class TriggerElement : TriggerControl
{
    protected Predicate<Collider> m_Conditions;
    protected enum TriggerState { Deactive, Active };
    protected TriggerState m_State;
    protected TriggerState State
    {
        set
        {
            m_State = value;
            if (value == TriggerState.Deactive)
            {
                if (DeactiveTrigger != null) DeactiveTrigger();
            }
            else if (value == TriggerState.Active)
            {
                if (ActiveTrigger != null) ActiveTrigger();
            }
            if (Activity != null) Activity(value != TriggerState.Deactive);
        }
    }
    public event Action<bool> Activity;
    public event Action ActiveTrigger, DeactiveTrigger;
    public event Action Enter, Exit;

    protected override bool CheckConditions(Collider cld)
    {
        return m_Conditions == null || m_Conditions(cld);
    }

    protected virtual void OnSetDefault() { }
    protected virtual void Enable() { }

    public void SetDefault()
    {
        Enter = Exit = null;
        m_Conditions = null;
        m_IgnoreTriggerEnter = false;
        Clear();
        StopAllCoroutines();
        OnSetDefault();
        bool active = enabled && gameObject.activeInHierarchy;
        if (m_State == TriggerState.Active)
        {
            if (!active) m_State = TriggerState.Deactive;
        }
        else
        {
            if (active) m_State = TriggerState.Active;
        }
    }
    protected override void Disable()
    {
        m_IgnoreTriggerEnter = false;
        State = TriggerState.Deactive;
    }
    private void OnEnable()
    {
        State = TriggerState.Active;
        Enable();
    }

    protected override void OnEnterToTrigger(Collider sender, bool condition)
    {
        if (condition)
        {
            m_IgnoreTriggerEnter = true;
            if (Enter != null) Enter();
        }
    }
    protected override void OnExitFromTrigger(Collider sender, bool condition)
    {
        if (condition)
        {
            m_IgnoreTriggerEnter = false;
            if (Exit != null) Exit();
        }
    }

}
