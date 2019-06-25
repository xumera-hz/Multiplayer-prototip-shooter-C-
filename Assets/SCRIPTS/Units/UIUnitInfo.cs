using UnityEngine;
using UnityEngine.UI;

public class UIUnitInfo : MonoBehaviour {

    [SerializeField] Text m_NameLabel;
    [SerializeField] Slider m_SliderHP;
    [SerializeField] Image m_SliderHPFill;
    [SerializeField] Text m_HPValue;
    [SerializeField] Text m_CountAmmo;

    Transform m_TF;

    public bool IsActive
    {
        get { return gameObject.activeInHierarchy; }
        set { gameObject.SetActive(value); }
    }

    public void SetPosition(Vector3 pos)
    {
        if (m_TF == null) m_TF = transform;
        m_TF.position = pos;
    }

    public void SetName(string name, Color color)
    {
        //if (name!=null && name.Length > 6) name = name.Substring(0, 6);
        m_NameLabel.text = name;
        m_NameLabel.color = color;
    }

    public void ActiveAmmoInfo(bool state)
    {
        m_CountAmmo.gameObject.SetActive(state);
    }

    public void SetCountAmmo(int value)
    {
        m_CountAmmo.text = value.ToString();
        m_CountAmmo.color = value > 0 ? Color.green : Color.red;
    }

    public void SetSlider(int value, float ratio, Color color)
    {
        m_SliderHP.value = ratio;
        m_SliderHPFill.color = color;
        m_HPValue.text = value.ToString();
    }
}
