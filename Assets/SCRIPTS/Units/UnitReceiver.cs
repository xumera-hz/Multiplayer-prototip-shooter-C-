using UnityEngine;

public class UnitReceiver : MonoBehaviour, IReceiver, IRangeHitReceiver
{
    public ICollisionReceiver CollisionReceiver
    {
        get
        {
            return null;
        }
    }

    public IMeleeHitReceiver MeleeHitReceiver
    {
        get
        {
            return null;
        }
    }

    public IRangeHitReceiver RangeHitReceiver
    {
        get
        {
            return this;
        }
    }

    UnitContainer m_Unit;
    [SerializeField] ProjectileHitEffect m_HitEffect = ProjectileHitEffect.Blood;

    private void Awake()
    {
        m_Unit = GetComponentInChildren<UnitContainer>();
    }

    public bool SetRangeHitAbsorbLogicImpulse(Vector3 pos, Vector3 dir, ref float impulse)
    {
        if (!m_Unit.LifeControl.Lived) return false;
        BulletHitPoolController.Static_CreateProjectileHitEffect((int)m_HitEffect, pos, dir);
        return true;
    }
}
