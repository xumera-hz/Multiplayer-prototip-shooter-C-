using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameController : MonoSingleton<MainGameController> {

    #region States

    public enum State { MainMenu, Lobby, LobbyMenu }

    StateManager<int, IFixedState> m_States;

    public static void Static_SetState(State state)
    {
        if (Can) m_I.SetState(state);
    }

    public void SetState(State state)
    {
        m_States.SetState((int)state);
    }

    public State GetState { get { return (State)m_States.TypeCurrentState; } }

    void InitStates()
    {
        m_States = new StateManager<int, IFixedState>(3, null, -1, null);
        m_States.AddState((int)State.MainMenu, new EasyStateWrapper(() => { MainMenuState(true); }, () => { MainMenuState(false); }, null));
        m_States.AddState((int)State.Lobby, new EasyStateWrapper(() => { LobbyState(true); }, () => { LobbyState(false); }, null));
        m_States.AddState((int)State.LobbyMenu, new EasyStateWrapper(() => { LobbyMenuState(true); }, () => { LobbyMenuState(false); }, null));
        m_States.SetState((int)State.MainMenu);
    }

    //TODO: singletons forever ;D

    void MainMenuState(bool state)
    {
        if (!MainMenu.Can) return;
        MainMenu.I.Active(state);
    }
    void LobbyState(bool state)
    {

    }
    void LobbyMenuState(bool state)
    {
        if (!LobbyMenu.Can) return;
        LobbyMenu.I.Active(state);
    }

    #endregion

    protected override void OnAwake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitStates();
    }

}
