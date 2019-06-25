using UnityEngine;
using System.Collections;

public struct AmmoInfo
{
    public AmmoInfo(WeaponInfo info)
    {
        MaxCountAmmoInCage = info.MaxCountAmmoInCage;
        MaxCountAmmo = info.MaxCountAmmo;
        MaxCountAmmoInCage = info.MaxCountAmmoInCage;
        UnlimitAmmo = false;
        FullCageOnSwitch = false;
        CurCountAmmoInCage = 0;
        CountAmmo = 0;
    }

    public int MaxCountAmmoInCage;
    public int MaxCountAmmo;
    public int CurCountAmmoInCage;//текущее кол-во патронов в обойме
    public int CountAmmo;//общее кол-во патронов

    //Misc
    public bool UnlimitAmmo;//Безлимитные патроны
    public bool FullCageOnSwitch;//Устанавливает в обойму возможное максимальное кол-во патронов при смене оружия

    public int FullAmmo { get { return CountAmmo + CurCountAmmoInCage; } }
    public int CountFullAmmoCages { get { return MaxCountAmmoInCage == 0 ? 0 : ((CountAmmo + CurCountAmmoInCage) / MaxCountAmmoInCage); } }

    public bool IsEmptyCage { get { return CurCountAmmoInCage <= 0; } }
    public bool IsFullCage { get { return CurCountAmmoInCage == MaxCountAmmoInCage; } }
    public bool IsFullAmmo { get { return UnlimitAmmo||(CountAmmo + CurCountAmmoInCage) >= MaxCountAmmo; } }
    public bool IsFullEmpty { get { return (CountAmmo + CurCountAmmoInCage) <= 0 && !UnlimitAmmo; } }

    public void AddCountAmmo(int count)
    {
        if (count == 0) return;
        int common = CountAmmo + count;
        CountAmmo = common < 0 ? 0 : ((common+ CurCountAmmoInCage) > MaxCountAmmo ? (MaxCountAmmo - CurCountAmmoInCage) : common);
    }

    public void AddCageAmmo(int count)
    {
        if (count == 0) return;
        if (count < 0) DecreaseAmmoInCage(-count);
        else IncreaseAmmoInCage(count);
    }

    public void IncreaseAmmoInCage(int count)
    {
        if (!UnlimitAmmo && CountAmmo == 0) return;
        int limit = MaxCountAmmoInCage - CurCountAmmoInCage;
        count = count < 0 ? 0 : (count > limit ? limit : count);
        if (count == 0) return;
        int common = UnlimitAmmo ? count : (count > CountAmmo ? CountAmmo : count);
        CurCountAmmoInCage += common;
        if (!UnlimitAmmo) CountAmmo -= common;
    }

    public void DecreaseAmmoInCage(int count)
    {
        if (count < 0) count = 0;
        if (count == 0) return;
        int common = CurCountAmmoInCage - count;
        CurCountAmmoInCage = common < 0 ? 0 : (common > MaxCountAmmoInCage ? MaxCountAmmoInCage : common);
    }

    public void TrySetFullCage()//если общего количества патронов хватает
    {
        IncreaseAmmoInCage(MaxCountAmmoInCage);
    }

}
