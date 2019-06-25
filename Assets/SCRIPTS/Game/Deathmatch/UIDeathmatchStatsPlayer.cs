using UnityEngine;
using UnityEngine.UI;

public class UIDeathmatchStatsPlayer : MonoBehaviour
{
    [SerializeField] Text m_NameLabel;
    [SerializeField] Text m_CountLabel;
    RectTransform m_TF;

    private void Awake()
    {
        m_TF = transform as RectTransform;
    }

    public void SetLocalPos(Vector3 localPos)
    {
        m_TF.localPosition = localPos;
        m_TF.AutoAnchors();
    }

    public void SetName(string name, bool isMine)
    {
        m_NameLabel.text = name;
        var color = isMine ? Color.green : Color.red;
        m_NameLabel.color = color;
        m_CountLabel.color = color;
    }

    public void SetCount(string count)
    {
        m_CountLabel.text = count;
    }
}