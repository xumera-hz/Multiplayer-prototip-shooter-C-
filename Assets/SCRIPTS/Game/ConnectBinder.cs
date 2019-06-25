using UnityEngine;

public class ConnectBinder : MonoBehaviour {

    [SerializeField] MainMenu m_UI;
    //[SerializeField] UIMessageBox m_DialogMessageUI;
    //[SerializeField] UIMessageBox m_ConnectionStatusUI;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ConnectController.Events += OnConnectEvents;
        //if(m_DialogMessageUI) m_DialogMessageUI.ClickEvent += OnClickUIButton;
    }

    private void OnDestroy()
    {
        ConnectController.Events -= OnConnectEvents;
    }

    void OnClickUIButton()
    {

    }

    TypeUnit RandomUnit()
    {
        var arr = System.Enum.GetValues(typeof(TypeUnit));
        if (arr.Length <= 0) return 0;
        return (TypeUnit)arr.GetValue(UnityEngine.Random.Range(0, arr.Length));
    }

    void OnConnectEvents(ConnectController.Args args)
    {
        if (args.Error != ConnectController.TypeError.None)
        {
            BadConnectInfo(args);
            return;
        }
        if (args.Event == ConnectController.TypeEvent.ClientConnect)
        {
            ClientConnect(new ClientConnectInfo() { ConnectInfo = args.ConnectInfo, PlayerName = args.PlayerName, TypeUnit = RandomUnit() });
            return;
        }
        if (args.Event == ConnectController.TypeEvent.ServerConnect)
        {
            ServerConnectWithFakeClient(new ClientConnectInfo() { ConnectInfo = args.ConnectInfo, PlayerName = args.PlayerName, TypeUnit = RandomUnit() });
            return;
        }
    }

    void BadConnectInfo(ConnectController.Args args)
    {
        m_UI.ShowDialogMessage(args.Error.ToString());
    }

    #region Client

    void ClientConnect(ClientConnectInfo info)
    {
        var client = GameClient.I;
        if (client.IsNullOrDestroy())
        {
            BadGameClient("Client does not exist.");
            return;
        }
        //if(client.State != GameClient.ProcessConnection.None)
        //{
        //    client.State = GameClient.ProcessConnection.None;
        //}
        client.ConnectEvents -= OnClientConnectEvents;
        client.ConnectEvents += OnClientConnectEvents;
        client.Connect(info);
    }

    void BadGameClient(string reason)
    {
        m_UI.ShowDialogMessage(reason);
    }

    void OnClientConnectEvents(GameClient.Args args)
    {
        //Debug.LogError(args.ToString());
        if (args.IsConnection)
        {
            string msg = null;
            switch (args.StateConnection)
            {
                //case GameClient.ConnectionState.None: break;
                case GameClient.ConnectionState.Start: msg = "Connection starting..."; break;
                case GameClient.ConnectionState.LoadLevel: msg = "Load Level..."; break;
                case GameClient.ConnectionState.LoadData: msg = "Load Data..."; break;
                case GameClient.ConnectionState.End: msg = "Complete"; break;
            }
            m_UI.ShowConnectionStatusMessage(msg);
        }
        else if(args.IsError/* || args.IsDisconnected*/)
        {
            m_UI.ShowDialogMessage(args.ToString());
        }
        else if(args.IsConnected)
        {
            SetMainState(MainGameController.State.Lobby);
            //m_UI.ActiveConnectionStatusMessage(false);
            //m_UI.ActiveDialogMessage(false);
        }
        else if (args.IsDisconnection)
        {
            SetMainState(MainGameController.State.MainMenu);
            string msg = null;
            switch (args.StateDisconnection)
            {
                //case GameClient.ConnectionState.None: break;
                case GameClient.DisconnectionState.Start: msg = "Disconnection starting..."; break;
                case GameClient.DisconnectionState.LoadLevel: msg = "Load Level..."; break;
                case GameClient.DisconnectionState.End: msg = "Complete"; break;
            }
            m_UI.ShowConnectionStatusMessage(msg);
        }
        else if (args.IsDisconnected)
        {
            SetMainState(MainGameController.State.MainMenu);
            m_UI.ShowDialogMessage(args.ToString());
            var server = GameServer.I;
            if (!server.IsNullOrDestroy()) server.Stop();
        }
    }

    void SetMainState(MainGameController.State state)
    {
        MainGameController.Static_SetState(state);
    }

    #endregion

    #region Server

    bool ServerConnect(int port)
    {
        var server = GameServer.I;
        if (server.IsNullOrDestroy())
        {
            BadGameServer("Server does not exist.");
            return false;
        }
        if (!server.IsStarted)
        {
            if (!server.Create(port))
            {
                BadGameServer("Server create is bad. Look logs.");
                return false;
            }
        }
        return true;
    }

    void ServerConnectWithFakeClient(ClientConnectInfo info)
    {
        if (!ServerConnect(info.Port)) return;
        var client = GameClient.I;
        if (client.IsNullOrDestroy())
        {
            BadGameClient("Client does not exist. Fucking develop's bug.");
            return;
        }
        client.ConnectEvents -= OnClientConnectEvents;
        client.ConnectEvents += OnClientConnectEvents;
        client.FakeConnect(GameServer.I, info);
    }

    void BadGameServer(string reason)
    {
        m_UI.ShowDialogMessage(reason);
    }

    #endregion

}
