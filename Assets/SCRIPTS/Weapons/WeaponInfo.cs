using System;

public class WeaponInfo : IdentifierClass
{
    //BaseData
    public int MaxCountAmmoInCage, MaxCountAmmo;
    public int ReloadAmmoInCage;//для потоковой перезарядки
    public float ReloadTime, AttackTime;
    public bool InstantProjectile;

    //Damage
    public float Damage;

    //Dispersion
    public float MinDispersion = 0f;
    public float MaxDispersion = 1f;
    public float DispersionStep;
    public float DispersionDamp;

    //Projectile
    public int ProjsPerShot;
    public int TypeProjectile;
    public float Impulse;
    public float SpeedProj;
    public float MaxDistance;

    public int SlotType;
    public bool NoAmmo;//Если у оружия нет патроны

    //Misc
    public float PreAttackDelay = 0f, PostAttackDelay = 0f;
    int m_AnimationType;

    public int AnimationType { get { return m_AnimationType > 0f ? m_AnimationType : Type; } }


    public WeaponInfo SetBaseData(int ammoInCage, int commonAmmo, int reloadAmmo, float reloadTime, float attackTime)
    {
        MaxCountAmmoInCage = ammoInCage; MaxCountAmmo = commonAmmo; ReloadAmmoInCage = reloadAmmo;  ReloadTime = reloadTime; AttackTime = attackTime;
        return this;
    }

    public WeaponInfo SetDispersion(float minDisp, float maxDisp, float dispStep, float dispDamp)
    {
        MinDispersion = minDisp; MaxDispersion = maxDisp; DispersionStep = dispStep; DispersionDamp = dispDamp;
        return this;
    }

    public WeaponInfo SetProjectile(int type, float impulse, float speed, float dist, int count = 1)
    {
        TypeProjectile = type; Impulse = impulse; SpeedProj = speed; MaxDistance = dist; ProjsPerShot = count;
        return this;
    }

    public WeaponInfo SetDamage(float mainDamage)
    {
        Damage = mainDamage;
        return this;
    }

    public WeaponInfo SetMisc(int slotType, int animationType = -1, bool noAmmo = false, bool instantProjectile = true)
    {
        SlotType = slotType;
        NoAmmo = noAmmo;
        InstantProjectile = instantProjectile;
        if (animationType < 0) animationType = Type;
        m_AnimationType = animationType;
        return this;
    }

    public WeaponInfo SetDelays(float preAttackDelay, float postAttackDelay)
    {
        PreAttackDelay = preAttackDelay;
        PostAttackDelay = postAttackDelay;
        return this;
    }

    public WeaponInfo(int id, int type, string name, string displayName) : base(id, type, name)
    {
        DisplayName = displayName;
    }
    public WeaponInfo(int id, int type, string name) : this(id, type, name, name) { }

    public WeaponInfo(WeaponInfo clone) : base(clone) { Clone(clone); }

    public void Clone(WeaponInfo clone)
    {
        SetBaseData(clone.MaxCountAmmoInCage, clone.MaxCountAmmo, clone.ReloadAmmoInCage, clone.ReloadTime, clone.AttackTime)
        .SetDispersion(clone.MinDispersion, clone.MaxDispersion, clone.DispersionStep, clone.DispersionDamp)
        .SetDamage(clone.Damage)
        .SetProjectile(clone.TypeProjectile, clone.Impulse, clone.SpeedProj, clone.MaxDistance, clone.ProjsPerShot)
        .SetMisc(clone.SlotType, clone.m_AnimationType, clone.NoAmmo, clone.InstantProjectile)
        .SetDelays(clone.PreAttackDelay, clone.PostAttackDelay);
    }


    public WeaponInfo() { }
}
