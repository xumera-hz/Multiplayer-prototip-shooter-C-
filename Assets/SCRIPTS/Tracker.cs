using UnityEngine;

public class Tracker : MonoBehaviour
{
    [SerializeField] Transform m_Target;
    [SerializeField] bool m_X = false, m_Y = false, m_Z = false;
    [SerializeField] Vector3 m_Offset;
    Transform m_TF;

    void Awake()
    {
        m_TF = transform;
    }

    public void SetTarget(Transform target)
    {
        m_Target = target;
    }

    void LateUpdate()
    {
        if (m_Target == null) return;
        var pos = m_Target.position;
        if (m_X) pos.x += m_Offset.x;
        if (m_Y) pos.y += m_Offset.y;
        if (m_Z) pos.z += m_Offset.z;
        m_TF.position = pos;
    }
}