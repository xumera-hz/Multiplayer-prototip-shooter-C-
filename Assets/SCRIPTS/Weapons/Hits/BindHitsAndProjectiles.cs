using UnityEngine;

public class BindHitsAndProjectiles : MonoBehaviour
{
    [SerializeField]
    HitsController m_Hits;
    [SerializeField]
    ManagerProjectile m_Projectiles;

    void Update()
    {
        m_Projectiles.PreUpdate();
        m_Hits.ManualUpdate();
        m_Projectiles.ManualUpdate();
    }
}
