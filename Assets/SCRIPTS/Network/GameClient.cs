using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using LiteNetLib;
using LiteNetLib.Utils;

public class GameClient : MonoSingleton<GameClient>, INetEventListener
{
    public const int MAX_PLAYERS = GameConstants.MAX_PLAYERS_IN_GAME;

    #region MainData

    NetManager m_ClientManager;
    Players m_Players = new Players(MAX_PLAYERS);
    ClientPlayer m_ClientPlayer;
    readonly NetDataWriter m_Writer = new NetDataWriter(true, GameConstants.DEFAULT_SIZE_WRITER);
    //readonly NetDataReader m_Reader = new NetDataReader();

    #endregion

    #region Mono

    protected override void OnAwake()
    {
        m_ProcessConnect = new ProcessConnect(this);
        m_ClientManager = new NetManager(this);
        m_ClientManager.UpdateTime = GameConstants.CLIENT_SEND_DATA_RATE_MILLISEC;
        m_ClientManager.DisconnectTimeout = GameConstants.TIMEOUT_TIME_MILLISEC;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Public

    public void FakeConnect(GameServer server, ClientConnectInfo info)
    {
        if (server.IsNullOrDestroy()) return;
        m_ProcessConnect.FakeConnect(server, info);
    }

    public void Connect(ClientConnectInfo info)
    {
        m_ProcessConnect.Connect(info);
    }

    public void Disconnect(string reason = null)
    {
        m_ProcessConnect.Disconnect(reason);
    }

    #endregion

    #region Events

    public struct Args
    {
        public ProcessConnection Event;
        public ConnectionState StateConnection;
        public DisconnectionState StateDisconnection;
        public string Reason;

        public bool IsConnection { get { return Event == ProcessConnection.Connection; } }
        public bool IsError { get { return Event == ProcessConnection.Error; } }
        public bool IsDisconnected { get { return Event == ProcessConnection.Disconnected; } }
        public bool IsDisconnection { get { return Event == ProcessConnection.Disconnection; } }
        public bool IsConnected { get { return Event == ProcessConnection.Connected; } }

        public override string ToString()
        {
            //var str = "ClientConnectEvent = " + Event;
            var str = Event.ToString();
            if (!string.IsNullOrEmpty(Reason)) str = "\nReason = " + Reason;
            return str;
        }
    }

    public event Action<Args> ConnectEvents;

    void CallConnectEvents(Args args)
    {
        State = args.Event;
        ConnectState = args.StateConnection;
        DisconnectState = args.StateDisconnection;
        if (ConnectEvents != null) ConnectEvents(args);
    }

    #endregion

    #region Time

    float m_CurrentTimerUpdate;
    float m_PeriodTimeUpdate = 1f / GameConstants.CLIENT_GET_DATA_RATE;

    bool Tick()
    {
        if (m_CurrentTimerUpdate > 0f)
        {
            m_CurrentTimerUpdate -= TimeManager.UnscaleFixedDeltaTime;
            return true;
        }
        m_CurrentTimerUpdate = m_PeriodTimeUpdate + m_CurrentTimerUpdate;
        return false;
    }

    #endregion

    #region UpdateTick

    private void FixedUpdate()
    {
        if (Tick()) return;

        m_ClientManager.PollEvents();

        var state = State;
        if (state != ProcessConnection.Connected) return;

        //Debug.LogError("Update");

        if (!m_ClientPlayer.IsFakeClient && m_ClientPlayer.IsConnected)
        {
            m_Writer.Reset();
            //ReaderGameHelper.AddCommand(m_Writer, ServerCommands.UpdateWorld);
            m_ClientPlayer.Update(m_Writer);
        }
    }

    #endregion

    #region Logging

    [SerializeField] bool m_IsLog;

    static void Log(object obj, bool error = false)
    {
        const string Tag = "[CLIENT] ";
        if (error) Debug.LogError(Tag + obj.ToString());
        else Debug.Log(Tag + obj.ToString());
    }

    static float m_PeriodLogTime = 5f;
    static float m_CurrentTimer = -1f;
    static void PeriodLog(object obj, bool error = false)
    {
        float time = Time.time;
        if (m_CurrentTimer < 0f) m_CurrentTimer = time + m_PeriodLogTime;
        if (time > m_CurrentTimer)
        {
            m_CurrentTimer = time + m_PeriodLogTime;
            Log(obj, error);
        }
    }

    #endregion

    #region ProcessConnect

    class ProcessConnect
    {
        GameClient m_Client;

        public ProcessConnect(GameClient client)
        {
            m_Client = client;
        }

        #region Public

        public void Disconnect(string reason)
        {
            if (m_Client.State == ProcessConnection.None) return;
            m_Client.StopAllCoroutines();
            m_Client.StartCoroutine(ProcessDisconnection(reason));
        }

        public void FakeConnect(GameServer server, ClientConnectInfo info)
        {
            if (m_Client.State != ProcessConnection.None) return;
            m_Client.StopAllCoroutines();
            m_Client.StartCoroutine(StartFakeConnection(server, info));
        }

        public void Connect(ClientConnectInfo info)
        {
            if (m_Client.State != ProcessConnection.None) return;
            m_Client.StopAllCoroutines();
            StartConnection(info);
        }

        public void Verification(NetPacketReader reader)
        {
            Log("Verification");
            var clientPlayer = m_Client.m_ClientPlayer;
            if (clientPlayer == null || clientPlayer.State != ClientState.Connection)
            {
                SendErrorEvent("Bad Verification client " + clientPlayer);
                return;
            }
            clientPlayer.State = ClientState.Verification;
            SendVerifyData(clientPlayer);
        }

        public void Register(NetPacketReader reader)
        {
            Log("Registration");
            var clientPlayer = m_Client.m_ClientPlayer;
            if (clientPlayer == null || clientPlayer.State != ClientState.Verification) return;
            var id = m_Client.GetIDPlayer(reader);
            clientPlayer.ID = id;
            var playerName = reader.GetString();
            clientPlayer.PlayerName = playerName;
            int countPlayers = reader.GetInt();
            for (int i = 0; i < countPlayers; i++)
            {
                m_Client.AddNewPlayer(reader);
            }
            clientPlayer.State = ClientState.Register;
        }

        #endregion
        bool m_SceneLoadStart;
        bool m_SceneUnloadStart;

        IEnumerator ProcessDisconnection(string reason)
        {
            Log("ProcessDisconnection");
            SetDisconnectionState(DisconnectionState.Start);
            if (m_Client.m_ClientPlayer != null) m_Client.m_ClientPlayer.State = ClientState.Disconnected;
            SetDisconnectionState(DisconnectionState.LoadLevel);
            //if(m_Client.DisconnectState == DisconnectionState.LoadLevel || )
            yield return WaitUnLoadGameLevel();
            m_Client.m_ClientPlayer = null;
            m_Client.m_Players.Clear();
            m_Client.m_ClientManager.Stop();
            StopProcessConnection();
            SetDisconnectionState(DisconnectionState.End);
            m_Client.CallConnectEvents(new Args() { Event = ProcessConnection.Disconnected, Reason = reason });
            m_Client.CallConnectEvents(new Args() { Event = ProcessConnection.None });
            m_Client.DisconnectState = DisconnectionState.None;
            m_Client.ConnectState = ConnectionState.None;
            m_Client.State = ProcessConnection.None;
            m_Client.StopAllCoroutines();
        }

        void SetConnectionState(ConnectionState state)
        {
            m_Client.CallConnectEvents(new Args() { Event = ProcessConnection.Connection, StateConnection = state });
        }

        void SetDisconnectionState(DisconnectionState state)
        {
            m_Client.CallConnectEvents(new Args() { Event = ProcessConnection.Disconnection, StateDisconnection = state });
        }

        IEnumerator StartFakeConnection(GameServer server, ClientConnectInfo info)
        {
            Log("FakeConnectProcess");
            SetConnectionState(ConnectionState.Start);
            SetConnectionState(ConnectionState.LoadLevel);
            yield return WaitLoadGameLevel();
            m_Client.CreateClientPlayer(null, info);
            server.AddFakeClient(m_Client.m_ClientPlayer);
            SetConnectionState(ConnectionState.End);
            m_Client.CallConnectEvents(new Args() { Event = ProcessConnection.Connected });
        }

        void SendErrorEvent(string reason)
        {
            m_Client.CallConnectEvents(new Args() { Event = ProcessConnection.Error, Reason = reason });
            Disconnect(reason);
        }

        void StartConnection(ClientConnectInfo info)
        {
            var manager = m_Client.m_ClientManager;
            if (!manager.IsRunning)
            {
                if (!manager.Start())
                {
                    SendErrorEvent("Client is not running. Look logs.");
                    return;
                }
            }
            var peer = manager.Connect(info.IP, info.Port, GameServer.SERVER_KEY);
            if (peer == null)
            {
                SendErrorEvent("Client connect is bad. Look logs.");
                return;
            }
            m_Client.CreateClientPlayer(peer, info);
            StartProcessConnection();
        }

        IEnumerator WaitAccept(Player player, ClientState state)
        {
            while (player.State != state)
            {
                yield return null;
            }
        }

        void SendRegisterAccept(Player player)
        {
            Log("SendRegisterAccept on " + player.IPInfo);
            var writer = m_Client.m_Writer;
            writer.Reset();
            ReaderGameHelper.AddClientState(writer, player.State);
            player.Peer.Send(writer, DeliveryMethod.ReliableUnordered);
        }

        void SendVerifyData(Player player)
        {
            Log("SendVerifyData on " + player.IPInfo);
            var writer = m_Client.m_Writer;
            writer.Reset();
            ReaderGameHelper.AddClientState(writer, player.State);
            writer.Put(player.PlayerName);
            writer.Put((byte)player.TypeUnit);
            player.Peer.Send(writer, DeliveryMethod.ReliableUnordered);
        }

        IEnumerator ConnectProcess(Player player)
        {
            Log("ConnectProcess=" + ConnectionState.Start);
            SetConnectionState(ConnectionState.Start);
            player.State = ClientState.Connection;
            yield return WaitAccept(player, ClientState.Verification);
            SetConnectionState(ConnectionState.LoadLevel);
            yield return WaitLoadGameLevel();
            yield return WaitAccept(player, ClientState.Register);
            SendRegisterAccept(player);
            SetConnectionState(ConnectionState.LoadData);
            yield return CheckAllPlayersConnected(player);
            Log("ConnectProcess=" + ConnectionState.End);
            SetConnectionState(ConnectionState.End);
            m_Client.AddPlayer(player);
            player.State = ClientState.Connected;
            //m_Client.AddPlayer(player);
            m_Client.CallConnectEvents(new Args() { Event = ProcessConnection.Connected });
        }
        //TODO: переделать подгрузку уровня
        const string LevelScene = "Deathmatch";
        //TODO: пока грузим уровень так
        IEnumerator WaitLoadGameLevel()
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(LevelScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        IEnumerator WaitUnLoadGameLevel()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(LevelScene);
            if (!scene.isLoaded) yield break;
            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(LevelScene);
        }

        IEnumerator CheckAllPlayersConnected(Player player)
        {
            var players = m_Client.m_Players;
            //var id = player.ID;
            while (true)
            {
                int count = players.Count - 1;
                int countConnected = 0;
                for (int i = 0; i < count; i++)
                {
                    var pl = players[i];
                    //if (pl.ID == id) continue;
                    var state = pl.State;
                    if (state == ClientState.Disconnected || state == ClientState.Connected) countConnected++;
                }
                if (countConnected >= count) yield break;
                yield return null;
            }
        }

        void StartProcessConnection()
        {
            if (m_Connect != null)
            {
                SendErrorEvent("StartProcessConnection was runned. Fucking develop bugs.");
                return;
            }
            m_Connect = m_Client.StartCoroutine(ConnectProcess(m_Client.m_ClientPlayer));
        }

        void StopProcessConnection()
        {
            if (m_Connect != null)
            {
                m_Client.StopCoroutine(m_Connect);
                m_Connect = null;
            }
        }
        Coroutine m_Connect;
    }

    public enum ProcessConnection { None, Connection, Disconnection, Connected, Disconnected, Error }
    public enum ConnectionState { None, Start, LoadLevel, LoadData, End }
    public enum DisconnectionState { None, Start, LoadLevel, End }

    public ProcessConnection State { get; private set; }
    public ConnectionState ConnectState { get; private set; }
    public DisconnectionState DisconnectState { get; private set; }

    ProcessConnect m_ProcessConnect;

    #endregion

    #region Misc

    string GetIDPlayer(NetDataReader reader)
    {
        return reader.GetString();
    }

    Player AddNewPlayer(string id, NetPeer peer = null)
    {
        return AddPlayer(CreatePlayer(id, peer));
    }

    Player AddNewPlayer(NetDataReader reader)
    {
        return AddPlayer(CreatePlayer(reader));
    }

    Player AddPlayer(Player player)
    {
        m_Players.AddPlayer(player);
        return player;
    }

    Player CreatePlayer(NetDataReader reader)
    {
        var id = GetIDPlayer(reader);
        var pl = CreatePlayer(id);
        return pl;
    }

    Player CreatePlayer(string id, NetPeer peer = null)
    {
        return new ProxyPlayer(id, peer);
    }

    void CreateClientPlayer(NetPeer peer, ClientConnectInfo info)
    {
        var clientPlayer = new ClientPlayer(peer);
        clientPlayer.Logger = Log;
        clientPlayer.PlayerName = info.PlayerName;
        clientPlayer.TypeUnit = info.TypeUnit;
        m_ClientPlayer = clientPlayer;
    }

    #endregion

    #region ReceiveHandlers

    void UpdateWorld(NetPeer peer, NetPacketReader reader)
    {
        var type = ReaderGameHelper.GetWorldUpdate(reader);
        switch (type)
        {
            case TypeWorldUpdate.SyncPlayersInfo: SyncPlayersInfo(reader); break;
            case TypeWorldUpdate.Player: UpdatePlayer(reader); break;
            case TypeWorldUpdate.Hits: SetHitsData(reader); break;
        }
    }

    System.Collections.Generic.List<string> m_IDPlayersCache = new System.Collections.Generic.List<string>(GameClient.MAX_PLAYERS);
    //выпиливаем игроков, которых нет на сервере( завалялись и(у)роды xD )
    void SyncPlayersInfo(NetDataReader reader)
    {
        //Log("SyncPlayersInfo");
        if (MiscHelper.CheckIncludeInMask(m_ClientPlayer.State, (int)ClientState.Connection)) return;
        m_IDPlayersCache.Clear();
        int countPlayers = reader.GetInt();
        for (int i = 0; i < countPlayers; i++)
        {
            var id = GetIDPlayer(reader);
            m_IDPlayersCache.Add(id);
        }
        for (int i = m_Players.Count - 1; i >= 0; i--)
        {
            var id = m_Players[i].ID;
            if (m_IDPlayersCache.Contains(id)) continue;
            //Log("Remove " + m_Players[i]);
            m_Players.RemovePlayer(id);
        }
        m_IDPlayersCache.Clear();
    }

    void UpdatePlayer(NetPacketReader reader)
    {
        //if (m_IsLog) Log(string.Format("UpdatePlayer ID={0} StartInd={1} DataLen={2}", peer.EndPoint, reader.Position, reader.AvailableBytes));
        if (!MiscHelper.CheckIncludeInMask(m_ClientPlayer.State, MiscHelper.MASK_REGISTRY_OR_CONNECTED)) return;
        var state = ReaderGameHelper.GetClientState(reader);
        var id = GetIDPlayer(reader);
        if (m_IsLog) PeriodLog(string.Format("UpdatePlayer={0} state={1}", id, state));

        bool isClient = m_ClientPlayer.ID == id;

        if (isClient && state == ClientState.Disconnected)
        {
            //CallConnectEvents(new Args() { Event = ProcessConnection.Disconnected, Reason = "Server kicked out" });
            return;
        }

        if (isClient && m_ClientPlayer.State != ClientState.Connected) return;
        var playerName = reader.GetString();
        var player = m_Players.GetPlayer(id);
        var typeUnit = (TypeUnit)reader.GetByte();
        if (player == null)
        {
            player = AddNewPlayer(id);
        }
        player.TypeUnit = typeUnit;
        player.PlayerName = playerName;
        player.State = state;
        player.AddData(reader);
    }
    //TODO: возможно и при registry тоже надо учитывать
    void SetHitsData(NetDataReader reader)
    {
        if (!MiscHelper.CheckIncludeInMask(m_ClientPlayer.State, (int)ClientState.Connected)) return;

        if (NetworkHitsController.Can)
        {
            if (!m_ClientPlayer.IsServer) NetworkHitsController.I.AddData(reader);
        }
    }

    #endregion

    #region EventsHandler

    public void OnPeerConnected(NetPeer peer)
    {
        if (m_IsLog) Log("OnPeerConnected " + peer.EndPoint);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (m_IsLog) Log("OnPeerDisconnected " + (peer == null ? "Empty" : peer.EndPoint.ToString()) + " reason=" + disconnectInfo.Reason);
        Disconnect(disconnectInfo.Reason.ToString());
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        if (m_IsLog) Log("OnNetworkError " + endPoint + " error=" + socketError);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        try
        {
            //if (m_IsLog) Log(string.Format("ReceiveData ID={0} DataLen={1}", peer.EndPoint, reader.AvailableBytes));
            //if (m_IsLog) Log("OnNetworkReceive " + peer.EndPoint + " method=" + deliveryMethod);

            var command = ReaderGameHelper.GetCommand(reader);
            switch (command)
            {
                case ServerCommands.UpdateWorld: UpdateWorld(peer, reader); break;
                case ServerCommands.Verify: m_ProcessConnect.Verification(reader); break;
                case ServerCommands.Register: m_ProcessConnect.Register(reader); break;
            }
        }
        catch(Exception e)
        {
            if (m_IsLog) Log("OnNetworkReceive bad data from " + (peer == null ? "Empty" : peer.EndPoint.ToString()) + " error=" + e, true);
            return;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (m_IsLog) Log("OnNetworkReceiveUnconnected " + remoteEndPoint + " messageType=" + messageType);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        //if (m_IsLog) Log("OnNetworkLatencyUpdate " + peer.EndPoint + " latency=" + latency);
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
    }

    #endregion
}
