using UnityEngine;

public class Life : MonoBehaviour, ILife
{
    [SerializeField] bool m_RegisterDeferredEvent;
    [SerializeField] protected bool m_IsLog;
    public event TemplateEventHandler<Component, LifeArgs> Change = delegate { };
    public event TemplateEventHandler<FloatValue> ChangeHealth = delegate { };
    public event TemplateEventHandler<FloatValue> ChangeArmor = delegate { };

    LifeArgs lifeArgs = new LifeArgs();
    [SerializeField] protected FloatValue m_Health;
    [SerializeField] protected FloatValue m_Armor;

    protected void CallChangeEvent()
    {
        if (m_RegisterDeferredEvent)
        {
#if UNITY_EDITOR
            Debug.LogError(GetType() + " NON RELEASE CODE");
#endif
        }
        else
        {
            Change(this, lifeArgs);
            lifeArgs.Reset();
        }
    }

    protected void HealthEventCall(FloatValue prev)
    {
        lifeArgs.SetHealth(prev, m_Health);
        if (ChangeHealth != null) ChangeHealth(m_Health);
        //UnitStaticEvents.ChangeHealth(this, m_Health);
    }

    protected void ArmorEventCall(FloatValue prev)
    {
        lifeArgs.SetArmor(prev, m_Armor);
        if (ChangeArmor != null) ChangeArmor(m_Armor);
        //UnitStaticEvents.ChangeHealth(this, m_Armor);
    }

    protected void HealthEventStack(FloatValue prev)
    {
        InnerBeforeChangeHealth();
        HealthEventCall(prev);
        InnerAfterChangeHealth();
    }

    protected void ArmorEventStack(FloatValue prev)
    {
        InnerBeforeChangeArmor();
        ArmorEventCall(prev);
        InnerAfterChangeArmor();
    }

    protected virtual void InnerBeforeChangeHealth() { }
    protected virtual void InnerAfterChangeHealth() { }
    protected virtual void InnerBeforeChangeArmor() { }
    protected virtual void InnerAfterChangeArmor() { }

    public void AddHealth(float hp)
    {
        if (hp == 0f) return;
        var info = m_Health;
        info.Set(info.Value + hp);
        HealthPoints = info;
    }
    public void AddArmor(float armor)
    {
        if (armor == 0f) return;
        var info = m_Armor;
        info.Set(info.Value + armor);
        ArmorPoints = info;
    }

    public void SetHealth(FloatValue hp)
    {
        SetHealth(hp.Value, hp.Min, hp.Max);
    }

    public void SetHealth(float hp = -1f, float min = -1f, float max = -1f)
    {
        var info = m_Health;
        if (min >= 0f) info.Min = min;
        if (max >= 0f) info.Max = max;
        info.Set(hp >= 0f ? hp : info.Value);
        HealthPoints = info;
    }
    public void SetArmor(float armor = -1f, float min = -1f, float max = -1f)
    {
        var info = m_Armor;
        if (min >= 0f) info.Min = min;
        if (max >= 0f) info.Max = max;
        info.Set(armor >= 0f ? armor : info.Value);
        ArmorPoints = info;
    }

    public void SetMaxHealth()
    {
        var newValue = m_Health;
        newValue.Value = newValue.Max;
        HealthPoints = newValue;
    }

    public void SetMinHealth()
    {
        var newValue = m_Health;
        newValue.Value = newValue.Min;
        HealthPoints = newValue;
    }

    public void SetValueHealth(float value)
    {
        var newValue = m_Health;
        newValue.Set(value);
        HealthPoints = newValue;
    }

    public void SetMaxArmor()
    {
        var newValue = m_Armor;
        newValue.Value = newValue.Max;
        ArmorPoints = newValue;
    }

    public void SetMinArmor()
    {
        var newValue = m_Armor;
        newValue.Value = newValue.Min;
        ArmorPoints = newValue;
    }

    public void SetValueArmor(float value)
    {
        var newValue = m_Armor;
        newValue.Set(value);
        ArmorPoints = newValue;
    }

    public float MaxArmor { get { return ArmorPoints.Max; } }
    public float CurrentArmor { get { return ArmorPoints.Value; } }
    public float MinArmor { get { return ArmorPoints.Min; } }

    public float MaxHealth { get { return HealthPoints.Max; } }
    public float CurrentHealth { get { return HealthPoints.Value; } }
    public float MinHealth { get { return HealthPoints.Min; } }

    public FloatValue HealthPoints
    {
        get { return m_Health; }
        protected set
        {
            value.Min = 0f;
            var prev = m_Health;
            if (m_Health.Set(ref value))
            {
                HealthEventStack(prev);
                CallChangeEvent();
            }
        }
    }

    public FloatValue ArmorPoints
    {
        get { return m_Armor; }
        protected set
        {
            value.Min = 0f;
            var prev = m_Armor;
            if (m_Armor.Set(ref value))
            {
                ArmorEventStack(prev);
                CallChangeEvent();
            }
        }
    }

    public bool Lived { get { return !m_Health.IsMin; } }


    protected void Awake()
    {
        //LimitMinValue();
        Init();
    }

    protected void OnEnable()
    {
        Enable();
    }

    protected virtual void Enable() { }

    protected virtual void Init() { }

    protected void OnValidate()
    {
        LimitMinValue();
    }

    protected void LimitMinValue()
    {
        if (m_Health.Min != 0f)
        {
            m_Health.Min = 0f;
#if UNITY_EDITOR
            Debug.Log("MinHealth cannot is zero");
#endif
        }
        if (m_Armor.Min != 0f)
        {
            m_Armor.Min = 0f;
#if UNITY_EDITOR
            Debug.Log("MinArmor cannot is zero");
#endif
        }
    }
}
