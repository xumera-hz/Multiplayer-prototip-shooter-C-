using UnityEngine;
using System.Collections;
using System;

public class GlobalWeapon : IIdentifier, IAmmo, IRangeWeaponInfo
{
    public event Action<GlobalWeapon> Change;
    public event Action ChangeSkin;
    public event Action<AmmoInfo> ChangeAmmo;
    AmmoInfo ammo;
    int skinID;
    public AmmoInfo Ammo { get { return ammo; } }
    public readonly WeaponInfo Info;
    //bool m_InstantAttack;

    //public bool IsInstantAttack { set { m_InstantAttack = value; } get { return Info.InstantProjectile && m_InstantAttack; } }

    public GlobalWeapon(WeaponInfo info)
    {
        if (info == null)
        {
#if UNITY_EDITOR
            Debug.LogError(GetType() + " error: INIT IS BAD");
#endif
            info = new WeaponInfo();
        }
        Info = info;
        ammo = new AmmoInfo(Info);
    }
    public int SkinID
    {
        get { return skinID; }
        set
        {
            skinID = value;
            if (ChangeSkin != null) ChangeSkin();
        }
    }
    //Тут добавлять модификации
    public float ReloadTime { get { return Info.ReloadTime * 1f; } }
    public float AttackTime { get { return Info.AttackTime * 1f; } }
    public float FireTime { get { return Info.AttackTime * 1f; } }
    public float DispersionStep { get { return Info.DispersionStep * 1f; } }
    public float DispersionMin { get { return Info.MinDispersion * 1f; } }
    public float DispersionMax { get { return Info.MaxDispersion * 1f; } }
    public float DispersionDamp { get { return Info.DispersionDamp * 1f; } }
    //public FloatValue Dispersion { get { return new FloatValue() { Value= }} }

    public void SetUnlimitAmmo(bool state) { ammo.UnlimitAmmo = state; CallEventChange(); }

    public int TYPE { get { return Info.ID; } }

    public int SUBTYPE { get { return Info.Type; } }

    #region IAmmo

    public bool IsEmptyCage { get { return ammo.CurCountAmmoInCage <= 0; } }
    public bool IsFullCage { get { return ammo.CurCountAmmoInCage == ammo.MaxCountAmmoInCage; } }
    /// <summary>
    /// Суммарно все патроны
    /// </summary>
    public bool IsFullAmmo { get { return (ammo.CountAmmo + ammo.CurCountAmmoInCage) >= ammo.MaxCountAmmo; } }
    public bool IsFullEmpty { get { return (ammo.CountAmmo + ammo.CurCountAmmoInCage) <= 0 && !ammo.UnlimitAmmo; } }

    public float RatioCurToMaxCage { get { return ammo.MaxCountAmmoInCage == 0 ? 0f : (ammo.CurCountAmmoInCage / (float)ammo.MaxCountAmmoInCage); } }


    void CallEventChange()
    {
        if (Change != null) Change(this);
    }

    void CallEventChangeAmmo()
    {
        if (ChangeAmmo != null) ChangeAmmo(ammo);
        CallEventChange();
    }

    public void AddCountAmmo(int count)
    {
        if (count == 0) return;
        int prev = ammo.CountAmmo;
        ammo.AddCountAmmo(count);
        if (prev != ammo.CountAmmo) CallEventChangeAmmo();
    }
    public void SetCountAmmo(int count)
    {
        int prev = ammo.CountAmmo;
        ammo.AddCountAmmo(count);
        if (prev != ammo.CountAmmo) CallEventChangeAmmo();
    }




    public void AddCageAmmo(int count)
    {
        if (count == 0) return;
        int prev = ammo.CurCountAmmoInCage;
        ammo.AddCageAmmo(count);
        if (prev != ammo.CurCountAmmoInCage) CallEventChangeAmmo();
    }

    public void ReloadCage()
    {
        //Debug.LogError("ammo.CurCountAmmoInCage1=" + ammo.CurCountAmmoInCage);
        int prev = ammo.CurCountAmmoInCage;
        //Debug.LogError("ammo.CurCountAmmoInCage2=" + ammo.CurCountAmmoInCage);
        ammo.TrySetFullCage();
        if (prev != ammo.CurCountAmmoInCage) CallEventChangeAmmo();
    }

    public void AddCages(int count)
    {
        if (count == 0) return;

        AddCountAmmo(count * ammo.MaxCountAmmoInCage);
    }

    #endregion
}
