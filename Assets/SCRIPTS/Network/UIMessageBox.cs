using UnityEngine;
using UnityEngine.UI;
using System;

public class UIMessageBox : MonoBehaviour {

    [SerializeField] Text m_MessageLabel;

    public event Action ClickEvent;

    public void Active(bool state)
    {
        gameObject.SetActive(state);
    }

    public void SetMessage(string msg)
    {
        if (m_MessageLabel) m_MessageLabel.text = msg;
    }

    public void ClickButton()
    {
        if (ClickEvent != null) ClickEvent();
    }
}
