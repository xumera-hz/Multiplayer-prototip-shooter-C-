using UnityEngine;

public class MainMenu : MonoSingleton<MainMenu> {

    [SerializeField] Fun m_FunExit;
    [SerializeField] ConnectController m_ConnectControl;
    [SerializeField] GameObject m_Misc;
    [SerializeField] UIMessageBox m_DialogMessageUI;
    [SerializeField] UIMessageBox m_ConnectionStatusUI;
    [SerializeField] GameObject m_MenuObject;

    private void Start()
    {
        Application.targetFrameRate = GameConstants.GAME_FPS;
        if (m_DialogMessageUI) m_DialogMessageUI.ClickEvent += OnClickUIButton;
    }

    void OnClickUIButton()
    {
        ActiveMenu(true);
    }

    public void CreateServer()
    {
        m_ConnectControl.Server();
    }

    public void ConnectToServer()
    {
        m_ConnectControl.Client();
    }

    public void ExitFromGame()
    {
        m_FunExit.ExitGame();
    }

    public void Active(bool state)
    {
        if (m_Misc) m_Misc.SetActive(state);
        gameObject.SetActive(state);
    }

    public void ActiveMenu(bool state)
    {
        m_MenuObject.SetActive(state);
        ActiveConnectionStatusMessage(false);
        ActiveDialogMessage(false);
    }

    public void ActiveConnectionStatusMessage(bool state)
    {
        if (m_ConnectionStatusUI.IsNullOrDestroy())
        {
            Debug.LogError(GetType() + " error: ConnectionStatus UI is null");
            return;
        }
        m_ConnectionStatusUI.Active(state);
    }

    public void ShowConnectionStatusMessage(string str)
    {
        ActiveMenu(false);
        ActiveDialogMessage(false);
        ActiveConnectionStatusMessage(true);
        m_ConnectionStatusUI.SetMessage(str);
    }

    public void ActiveDialogMessage(bool state)
    {
        if (m_DialogMessageUI.IsNullOrDestroy())
        {
            Debug.LogError(GetType() + " error: DialogMessage UI is null");
            return;
        }
        m_DialogMessageUI.Active(state);
    }

    public void ShowDialogMessage(string str)
    {
        ActiveMenu(false);
        ActiveConnectionStatusMessage(false);
        ActiveDialogMessage(true);
        m_DialogMessageUI.SetMessage(str);
    }

}
