using UnityEngine;
using System;

public class LifeArgs
{
    public bool ChangeHealth { get; private set; }
    public bool ChangeArmor { get; private set; }
    public FloatValue PrevHealth { get; private set; }
    public FloatValue PrevArmor { get; private set; }
    public FloatValue Health { get; private set; }
    public FloatValue Armor { get; private set; }

    public void SetHealth(FloatValue prev, FloatValue health)
    {
        Health = health;
        PrevHealth = prev;
        ChangeHealth = true;
    }

    public void SetArmor(FloatValue prev, FloatValue armor)
    {
        Armor = armor;
        PrevArmor = prev;
        ChangeArmor = true;
    }

    public void Reset()
    {
        ChangeHealth = ChangeArmor = false;
    }
}

public enum TypeDamage { Default, Bullet, Explosion }

[System.Serializable]
public struct IncomeDamageInfo
{
    public bool IgnoreArmor;
    public float Damage;//входимый урон
    
    public TypeDamage DamageType;
    //Если стоит true, то в Damage кладется суммарное значение текущих хп и брони
    //(если стоит IgnoreArmor = true, то в Damage значение брони не кладется)
    public bool Kill;

    public object Source;
    public IOwner Owner;
}

public struct OutDamageInfo
{
    //public float ReceivedDmg;//Получаемый урон(например с множителями уменьшения урона)
    public float ReceiverDmg;//Полученный урон(конечный урон(например хп =1, а получаемый урон был 100), конечный равен 1)
    public float DmgToArmor;//Урон по броне
    public float DmgToHealth;//Урон по жизням(мяску :D)

    public void SetSpecificDamage(float dmgArmor, float dmgHealth)
    {
        DmgToArmor = dmgArmor; DmgToHealth = dmgHealth;
    }
}

public interface IDamageMisc
{
    bool Immortal { get; set; }
    bool Invulnerable { get; set; }
}

public interface IDamageTypeMisc
{
    bool DamageExplosionImmune { get; set; }
}

public delegate void GameObjectEventHandler<T>(GameObject sender, T args);
public delegate void ComponentEventHandler<T>(Component sender, T args);
public delegate void TemplateEventHandler<T>(T sender);
public delegate void TemplateEventHandler<Sender, Args>(Sender sender, Args args);
public delegate void MyEventHandler<T>(object sender, T args);
public interface ILifeEvents
{
    event TemplateEventHandler<Component, DeathArgs> DeathEvent;
    event TemplateEventHandler<Component, ResurrectionArgs> ResurrectionEvent;
    event TemplateEventHandler<Component, LifeArgs> Change;
    event TemplateEventHandler<FloatValue> ChangeHealth;
    event TemplateEventHandler<FloatValue> ChangeArmor;
}

public interface IDamageEvents
{
    event TemplateEventHandler<Component, DamageArgs> PreTakenDamage;
    event TemplateEventHandler<Component, DamageArgs> AfterTakenDamage;
}

public interface IDamageReceiver : IDamageMisc, IDamageEvents, IDamageTypeMisc
{
    bool CanDamage { get; }
    void SetDamage(IncomeDamageInfo income);
    void SetDamage(float dmg);
    bool Kill(GameObject source = null, bool force = false);
}

public interface ILifeControl : IDamageReceiver, ILifeEvents, ILife
{
    void SetDefault();
}

public struct DeathArgs
{
    public readonly DamageArgs Damage;

    public DeathArgs(DamageArgs dmg, GameObject victim)
    {
        Damage = dmg;
        Victim = victim;
    }

    public GameObject Killer { get { return Damage.Owner == null ? null : Damage.Owner.GameObj; } }
    public GameObject Victim { get; private set; }
}

public class ResurrectionArgs : EventArgs { }

public class LifeControl : Life, ILifeControl
{
    DamageArgs dmgArgs = new DamageArgs();
    //DeathArgs deathArgs = new DeathArgs();
    [SerializeField] bool m_DamageExplosionImmune;
    [SerializeField] bool m_Immortal;
    [SerializeField] bool m_Invulnerable;
    bool m_DirtyIsLived;
    public event TemplateEventHandler<Component, DamageArgs> PreTakenDamage = delegate { };
    public event TemplateEventHandler<Component, DamageArgs> AfterTakenDamage = delegate { };
    public event TemplateEventHandler<Component, DeathArgs> DeathEvent = delegate { };
    public event TemplateEventHandler<Component, ResurrectionArgs> ResurrectionEvent = delegate { };

    #region Public

    public bool Immortal { get { return m_Immortal; } set { m_Immortal = value; } }
    public bool Invulnerable { get { return m_Invulnerable; } set { m_Invulnerable = value; } }
    public bool DamageExplosionImmune { get { return m_DamageExplosionImmune; } set { m_DamageExplosionImmune = value; } }

    public bool CanDamage { get { return !m_Invulnerable; } }

    public void SetDamage(float dmg)
    {
        SetDamage(new IncomeDamageInfo() { Damage = dmg });
    }

    public void SetDamage(IncomeDamageInfo income)
    {
        if (m_Invulnerable) return;
        if (income.DamageType == TypeDamage.Explosion && m_DamageExplosionImmune) income.Damage = 0f;
        else if (m_Immortal || income.Damage < 0f) income.Damage = 0f;
        SetIncomeDamage(ref income);
    }

    public bool Kill(GameObject source = null, bool force = false)
    {
        if (!force && (Immortal || m_Invulnerable)) return false;
        var income = new IncomeDamageInfo() { Source = source };
        income.Kill = true;
        SetIncomeDamage(ref income);
        return true;
    }

    public virtual void SetDefault()
    {
        SetMaxHealth();
        SetMaxArmor();
        m_DirtyIsLived = Lived;
    }

    #endregion

    #region Private

    void SetIncomeDamage(ref IncomeDamageInfo income)
    {
        if (!Lived) return;
        OutDamageInfo outInfo;
        GetOutDamageInfo(ref income, out outInfo);
        dmgArgs.Output = outInfo;
        dmgArgs.Income = income;
        SetOutputDamage(ref outInfo);
    }

    

    void SetOutputDamage(ref OutDamageInfo dmg)
    {
        OnPreTakenDamage(ref dmg);
        bool isCallEvent = false;
        var prev = m_Armor;
        bool res = AddTo(ref m_Armor, -dmg.DmgToArmor);
        isCallEvent = res;
        if (res)
        {
            ArmorEventStack(prev);
        }
        prev = m_Health;
        res = AddTo(ref m_Health, -dmg.DmgToHealth);
        if (res)
        {
            InnerBeforeChangeHealth();
            HealthEventCall(prev);
        }
        isCallEvent = isCallEvent || res;
        if (isCallEvent) CallChangeEvent();
        OnAfterTakenDamage(ref dmg);
        OnCheckedDeathOrResurrect();
    }

    bool AddTo(ref FloatValue value, float newValue)
    {
        newValue = value.Value + newValue;
        bool res = !value.ValueEqual(newValue);
        if (res) value.Set(newValue);
        return res;
    }

    #region Monobehaviour

    protected override void Init()
    {
        m_DirtyIsLived = Lived;
#if UNITY_EDITOR
        if (m_IsLog) Debug.LogError("Awake_dirtyIsLived=" + m_DirtyIsLived);
#endif
    }
    protected override void Enable()
    {
        //dirtyIsLived = true;
    }

    #endregion

    void OnPreTakenDamage(ref OutDamageInfo outInfo)
    {
        PreTakenDamage(this, dmgArgs);
        //UnitStaticEvents.GlobalTakenDamage(this, dmgArgs);//Костыль

    }

    void OnAfterTakenDamage(ref OutDamageInfo outInfo)
    {
        AfterTakenDamage(this, dmgArgs);
        //UnitStaticEvents.GlobalTakenDamage(this, dmgArgs);//Костыль

    }

    void GetOutDamageInfo(ref IncomeDamageInfo inDmg, out OutDamageInfo outDmg)
    {
        float curArmor = CurrentArmor;
        float curHp = CurrentHealth;
        float armorReceiver = 0f;
        float hpReceiver = 0f;
        float receiver = 0f;//реально полученный общий урон
        if (inDmg.Kill)
        {
            float fullArmor = inDmg.IgnoreArmor ? 0f : curArmor;
            receiver = curHp + fullArmor;
            inDmg.Damage = receiver;
            armorReceiver = fullArmor;
            hpReceiver = curHp;
        }
        else
        {
            float dmg = inDmg.Damage;//Заменить на ReceivedDamage
            float deltaHpDmg = dmg;//урон котоырй пойдет, когда кончится броня
            float realDmg;//реальный урон для текущего типа(броня/хп)
            if (!inDmg.IgnoreArmor)
            {
                if (curArmor > 0f)
                {
                    realDmg = (dmg > curArmor ? curArmor : dmg);
                    receiver += realDmg;
                    deltaHpDmg = dmg - realDmg;
                    armorReceiver = realDmg;
                }
            }
            if (deltaHpDmg > 0f)
            {
                realDmg = (deltaHpDmg > curHp ? curHp : deltaHpDmg);
                hpReceiver = realDmg;
                receiver += realDmg;
            }
        }
        outDmg = new OutDamageInfo();
        outDmg.ReceiverDmg = receiver;
        outDmg.SetSpecificDamage(armorReceiver, hpReceiver);
    }


    protected override void InnerAfterChangeHealth()
    {
        OnCheckedDeathOrResurrect();
    }

    void OnCheckedDeathOrResurrect()
    {
        if (Lived)
        {
            if (!m_DirtyIsLived) OnResEvent();
        }
        else
        {
            if (m_DirtyIsLived) OnDeathEvent();
        }
    }

    void OnResEvent()
    {
        m_DirtyIsLived = true;
        //BeforeResurrection(this, null);
        //EmptyResurrection();
        ResurrectionEvent(this, null);

    }

    void OnDeathEvent()
    {
        m_DirtyIsLived = false;
        var deathArgs = new DeathArgs(dmgArgs, gameObject);
        //deathArgs.Damage = dmgArgs;


        //BeforeDeath(this, deathArgs);
        //EmptyDeath();


        DeathEvent(this, deathArgs);
        //UnitStaticEvents.Death(this, deathArgs);//Костыль

    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("KILL")]
    private void KillManually()
    {
        Kill();
    }
#endif
}
