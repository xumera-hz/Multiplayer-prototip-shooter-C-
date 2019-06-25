using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimController : MonoBehaviour
{
    [SerializeField] Transform m_AimElement;
    [SerializeField] Vector3 m_Offset = new Vector3(0f, 0.05f, 0f);
    Transform m_Target;
    
    public void SetTarget(Transform target)
    {
        m_Target = target;
    }

    public void Active(bool state)
    {
        m_AimElement.gameObject.SetActive(state);
    }
	
	public void ManualUpdate (Vector3 dir)
    {
        if (!m_AimElement.gameObject.activeInHierarchy) return;
        if (m_Target.IsNullOrDestroy()) return;
        float sqr = dir.sqrMagnitude;
        if (sqr < 1e-2f) dir = m_Target.forward;
        var pos = m_Target.position + m_Offset;
        m_AimElement.position = pos;
        m_AimElement.forward = dir;
    }
}
