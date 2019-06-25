using System;
using UnityEngine;

public sealed class Bullet : Projectile
{
    //IHit<CastHitInfo> managerHit;
    const float speedScale = 8f;
    //ProjectileTrail trail;
    bool useTrail;
    //CastHitsInfo hitsInfo;

#if UNITY_EDITOR
    void OnValidate()
    {
        ProjectileType = ProjectileType.Bullet;
    }
#endif

    void UpdateView()
    {
        if (m_TF.localScale.z != 1f)
        {
            Vector3 v3 = m_TF.localScale;
            v3.z += TimeManager.TimeDeltaTime * speedScale;
            if (v3.z > 1f) v3.z = 1f;
            m_TF.localScale = v3;
        }
    }
    void ResetView()
    {
        Vector3 v3 = m_TF.localScale;
        v3.z = 0f;
        m_TF.localScale = v3;
    }

    protected override void Init()
    {
        //trail = GetComponentInChildren<ProjectileTrail>(true);
    }

    public override void Move()
    {
        //Debug.Log("AA="+isEnd);
        UpdateView();
        Vector3 newPos = m_TF.position + m_Direction * Data.Speed * TimeManager.TimeDeltaTime;
        m_TF.position = newPos;
        //if (useTrail) trail.UpdateTrailPos(newPos);
    }

    public static int CompareHitsByDistance(RaycastHit x, RaycastHit y)
    {
        return x.distance.CompareTo(y.distance);
    }
    //public override MyEffect[] Effects
    //{
    //    get
    //    {
    //        //Debug.Log("Data.Damage="+Data.Damage);
    //        return new MyEffect[]
    //        {
    //            new WeaponDamageEffect((WeaponsKinds)Data.TypeWeapon,WeaponHitEffect.Bullet,Data.Damage).Set(gameObject,Owner)//,
    //            //new WeaponDamageEffect((WeaponsKinds)data.TypeWeapon,data.Damage)
    //        };
    //    }
    //}

    public override bool CheckHit()
    {
        //Debug.LogError("CheckHit");
        //m_CastData.Position1 = TF.position;
        float dist = (Data.Instantly ? Data.MaxDistance : (Data.Speed * TimeManager.TimeDeltaTime));
        m_CastData.Distance = dist;
        Cast();
        //hitsInfo.RaycastAll(TF.position, direction, (Data.Instantly ? Data.MaxDistance : (Data.Speed * TimeManager.TimeDeltaTime)), -1, QueryTriggerInteraction.Collide);
#if UNITY_EDITOR
        //Debug.Log("Data.MaxDistance=" + Data.MaxDistance);
        Debug.DrawRay(m_TF.position, m_Direction * dist, Color.blue, 5f);

        if (m_CastData.Target1 != null)
        {
            Debug.DrawRay(m_CastData.Target1.position, m_Direction * dist, Color.red, 5f);
        }
        if (m_CastData.Target2 != null)
        {
            Debug.DrawRay(m_CastData.Target2.position, m_Direction * dist, Color.red, 5f);
        }
#endif
        bool res = hitsInfo.Count > 0;
        if (res) CallRegistryHit();
        return res;
    }

    protected override void OnSet()
    {
        int rand = UnityEngine.Random.Range(0, 2);
        useTrail = rand == 1;
        //if (useTrail) trail.InitTrail(m_TF.position);
        //else trail.SetActive(false);
        ResetView();
    }

    public override void SetHit(float impulse, Vector3 pos)
    {
        if (Data.Impulse <= 0f) return;
        //Debug.LogError("SetHit=" + impulse);
        Data.Impulse = impulse < 0f ? 0f : impulse;
        if (impulse <= 0f)
        {
            isEndCheckHit = true;
            EndMove = true;
            //Debug.LogError("isEndCheckHit=" + isEndCheckHit);
        }
    }
}
