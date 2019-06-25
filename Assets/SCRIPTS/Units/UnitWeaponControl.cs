using System.Collections.Generic;
using UnityEngine;

public class UnitWeaponControl : MonoBehaviour
{
    public PropertiesWeapon Weapon { get; private set; }

    UnitContainer m_Unit;
    
	void Awake ()
    {
        m_Unit = GetComponentInChildren<UnitContainer>();
        Weapon = GetComponentInChildren<PropertiesWeapon>();
    }

    private void Start()
    {
        InitWeapon();
    }

    public bool ReadyAttack { get { return Weapon.IsReady && !Weapon.IsEmpty; } }

    public bool IsAttack { get { return Weapon.IsAttack; } }

    public bool Attack(Vector3 dir)
    {
        bool canAttack = ReadyAttack;
        if (canAttack)
        {
            dir.Normalize();
            if (dir.sqrMagnitude > 1e-5f)
            {
                m_Unit.MoveControl.forward = dir;
                m_Unit.MoveControl.Apply();
            }
            return Weapon.Attack();
        }
        return false;
    }

    //TODO: temp init weapon
    #region Temp
    void InitWeapon()
    {
        GlobalWeapon weapon = new GlobalWeapon(
            new WeaponInfo(0, 0, "Shotgun")
                .SetBaseData(3, 3, 1, 2f, 0.5f)
                .SetDispersion(0f, 0f, 0f, 0f)
                .SetDamage(20f * 1)
                .SetProjectile((int)ProjectileType.Bullet, 100f, 70f, 7f, 1)
                .SetMisc(0, instantProjectile: true)
                );

        weapon.SetUnlimitAmmo(true);
        weapon.ReloadCage();
        //Weapon.GenerateBulletDirections = GenerateBulletDirections;
        Weapon.Initialize(m_Unit, weapon);
    }

    void GenerateBulletDirections(Vector3 startPos, Vector3 lookDir, int count, List<Vector3> outDirections)
    {
        //outDirections.Clear();
        //if (count <= 0) return;
        //float angleStep = m_AngleBetweetBorders / count;
        //float angle=
        //for (int i = 0; i < count; i++)
        //{
        //    outDirections.Add(MathUtils.RandomVectorInsideCone2(m_CurDispersion));
        //}
    }

    #endregion
}
