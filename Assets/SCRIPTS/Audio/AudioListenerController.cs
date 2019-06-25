using UnityEngine;
public class AudioListenerController : MonoSingleton<AudioListenerController>
{
    Transform m_TF;
    protected override void OnAwake()
    {
        m_TF = transform;

    }

    ITransform m_Target;

    public void SetTarget(ITransform target)
    {
        m_Target = target;
        enabled = m_Target != null;
    }

    void LateUpdate()
    {
        if (m_Target == null)
        {
            enabled = false;
            return;
        }
        Vector3 pos;
        Quaternion rot;
        m_Target.GetInfo(out pos, out rot, true);
        m_TF.position = pos;
        m_TF.rotation = rot;
    }
}
