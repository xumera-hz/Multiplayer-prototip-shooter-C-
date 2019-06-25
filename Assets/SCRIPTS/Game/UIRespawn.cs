using UnityEngine;
using UnityEngine.UI;

public class UIRespawn : MonoBehaviour {

    [SerializeField] Text m_TimerLabel;
    [SerializeField] string m_AdditionalMessage;

    public bool IsActive
    {
        get { return gameObject.activeInHierarchy; }
        set { gameObject.SetActive(value); }
    }

    public void SetTimer(string value)
    {
        m_TimerLabel.text = m_AdditionalMessage +" " +value +" сек";
    }

}
