using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

public enum TypeWorldUpdate { SyncPlayersInfo, Player, Hits }
public enum ServerCommands : byte { Register, Verify, UpdateWorld, ConnectOtherPlayer, DisconnectPlayer }
public enum ClientState : byte { None = 0, Connection = 1, Verification = 2, Register = 4, Connected = 8, Disconnected = 16 }

public class GameServer : MonoSingleton<GameServer>, INetEventListener
{
    #region MainData

    NetManager m_Server;
    Players m_Players;
    readonly NetDataWriter m_Writer = new NetDataWriter(true, GameConstants.DEFAULT_SIZE_WRITER);
    //readonly NetDataReader m_Reader = new NetDataReader();

    public const string SERVER_KEY = GameConstants.SERVER_KEY;

    #endregion

    protected override void OnAwake()
    {
        m_ConnectControl = new ProcessConnect(this);
        m_Players = new Players(GameConstants.MAX_PLAYERS_IN_GAME);
        m_Server = new NetManager(this);
        m_Server.UpdateTime = GameConstants.SERVER_SEND_DATA_RATE_MILLISEC;
        m_Server.DisconnectTimeout = GameConstants.TIMEOUT_TIME_MILLISEC;
        DontDestroyOnLoad(gameObject);
    }

    #region Public

    public bool IsStarted { get { return m_Server != null && m_Server.IsRunning; } }

    public bool Create(int port)
    {
        if (m_Server.IsRunning) return false;
        bool res = m_Server.Start(port);
        if (res) DontDestroyOnLoad(gameObject);
        return res;
    }

    public void Stop()
    {
        if (m_FakeClient == null) return;
        Log("StopServer");
        m_Server.DisconnectAll();
        m_Players.Clear();
        m_FakeClient = null;
        m_ConnectControl.StopAllConnect();
        //m_Server.Stop();
    }

    #endregion

    #region FakeClient

    Player m_FakeClient;

    internal void AddFakeClient(Player pl)
    {
        //Debug.LogError("AddFakeClient=" + m_FakeClient);
        if (pl == null || m_FakeClient != null) return;
        m_FakeClient = pl;
        pl.ID = GenericPlayerID();
        m_Players.AddPlayer(pl);
        StartCoroutine(WaitFakeClient());
    }
    //TODO: в стейте создается юнит, у него старт не успевается отработать
    //дратути Exceptions
    IEnumerator WaitFakeClient()
    {
        yield return new WaitForEndOfFrame();
        m_FakeClient.State = ClientState.Connected;
    }
    internal static void Static_AddFakeClient(Player pl)
    {
        if (Can) m_I.AddFakeClient(pl);
    }

    #endregion

    #region Time

    float m_CurrentTimerUpdate;
    float m_PeriodTimeUpdate = 1f / GameConstants.SERVER_GET_DATA_RATE;

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

        //обработка событий
        m_Server.PollEvents();

        if (!IsStarted) return;

        //синхронизируем игроков на клиентах
        SendSyncPlayers();

        //данные игрока
        for (int i = m_Players.Count - 1; i >= 0; i--)
        {
            var pl = m_Players[i];
            if (!pl.IsConnected) continue;
            m_Writer.Reset();
            ReaderGameHelper.AddCommand(m_Writer, ServerCommands.UpdateWorld);
            if (!pl.GetData(m_Writer)) continue;
            //if (m_IsLog) Log(string.Format("SendData ID={0} DataLen={1}", pl.ID, m_Writer.Length));
            SendToAllWithState(m_Writer, DeliveryMethod.Unreliable, MiscHelper.MASK_REGISTRY_OR_CONNECTED);
        }
        //данные о зарегистрированных попаданиях
        if (GetHitsData())
        {
            SendToAllWithState(m_Writer, DeliveryMethod.Unreliable, MiscHelper.MASK_REGISTRY_OR_CONNECTED);
        }

        //if (m_ClientPlayer != null) m_ClientPlayer.Update(m_Writer);
    }

    void SendSyncPlayers()
    {
        //Log("SendSyncPlayers");
        m_Writer.Reset();
        ReaderGameHelper.AddCommand(m_Writer, ServerCommands.UpdateWorld);
        ReaderGameHelper.AddWorldUpdate(m_Writer, TypeWorldUpdate.SyncPlayersInfo);
        int count = m_Players.Count;
        m_Writer.Put(count);
        for (int i = 0; i < count; i++)
        {
            //Log("Sync " + m_Players[i]);
            m_Writer.Put(m_Players[i].ID);
        }
        SendToAllWithState(m_Writer, DeliveryMethod.Unreliable, MiscHelper.MASK_REGISTRY_OR_CONNECTED);
    }

    bool GetHitsData()
    {
        if (!NetworkHitsController.Can) return false;
        m_Writer.Reset();
        ReaderGameHelper.AddCommand(m_Writer, ServerCommands.UpdateWorld);
        ReaderGameHelper.AddWorldUpdate(m_Writer, TypeWorldUpdate.Hits);
        bool res = NetworkHitsController.I.GetData(m_Writer);
        return res;
    }

    #endregion

    #region Logging

    [SerializeField] bool m_IsLog;

    static void Log(object obj, bool error = false)
    {
        const string Tag = "[SERVER] ";
        if (error) Debug.LogError(Tag + obj.ToString());
        else Debug.Log(Tag + obj.ToString());
    }

    #endregion

    #region Send

    void SendToAllWithStateExcept(NetDataWriter writer, DeliveryMethod method, int indexExcept, int maskState)
    {
        int count = MiscHelper.CountPackets(method);
        //Посылаем много раз, в надежде что хоть 1 дойдет)

        for (int i = m_Players.Count - 1; i >= 0; i--)
        {
            if(i == indexExcept) continue;
            var pl = m_Players[i];
            bool res = MiscHelper.CheckIncludeInMask(pl.State, maskState);
            if (!res) continue;
            Send(pl.Peer, writer, method, count);
        }
    }
    /// <summary>
    /// Отправляем данные только тем, что удолетворяют маске состояний(одному и более сразу)
    /// Исключая одного клиента
    /// </summary>
    void SendToAllWithStateExcept(NetDataWriter writer, DeliveryMethod method, NetPeer peer, int maskState)
    {
        if (peer == null)
        {
            SendToAllWithState(writer, method, maskState);
            return;
        }
        int count = MiscHelper.CountPackets(method);
        //Посылаем много раз, в надежде что хоть 1 дойдет)

        for (int i = m_Players.Count - 1; i >= 0; i--)
        {
            var pl = m_Players[i];
            var playerPeer = pl.Peer;
            bool res = playerPeer == peer;
            if (res) continue;
            res = MiscHelper.CheckIncludeInMask(pl.State, maskState);
            if (!res) continue;
            Send(playerPeer,writer, method, count);
        }
    }

    void Send(NetPeer peer, NetDataWriter writer, DeliveryMethod method, int count)
    {
        if (peer == null) return;
        for (int j = 0; j < count; j++) peer.Send(writer, method);
    }

    /// <summary>
    /// Отправляем данные только тем, что удолетворяют маске состояний(одному и более сразу)
    /// </summary>
    void SendToAllWithState(NetDataWriter writer, DeliveryMethod method, int maskState)
    {
        int count = MiscHelper.CountPackets(method);
        //Посылаем много раз, в надежде что хоть 1 дойдет)

        for (int i = m_Players.Count - 1; i >= 0; i--)
        {
            var pl = m_Players[i];
            bool res = MiscHelper.CheckIncludeInMask(pl.State, maskState);
            if (!res) continue;
            Send(pl.Peer, writer, method, count);
        }
    }
    /// <summary>
    /// Отправляет данные всем без проверки на состояние, исключая одного клиента
    /// </summary>
    void SendToAllExcept(NetDataWriter writer, DeliveryMethod method, NetPeer peer)
    {
        if (peer == null)
        {
            SendToAll(writer, method);
            return;
        }
        int count = MiscHelper.CountPackets(method);

        for (int i = m_Players.Count - 1; i >= 0; i--)
        {
            var playerPeer = m_Players[i].Peer;
            bool res = playerPeer == peer;
            if (res) continue;
            //Посылаем много раз, в надежде что хоть 1 дойдет)
            for (int j = 0; j < count; j++) playerPeer.Send(writer, method);
        }
    }
    /// <summary>
    /// Отправляет данные всем без проверки на состояние
    /// </summary>
    void SendToAll(NetDataWriter writer, DeliveryMethod method)
    {
        int count = MiscHelper.CountPackets(method);

        //Посылаем много раз, в надежде что хоть 1 дойдет)
        for (int i = 0; i < count; i++)
        {
            m_Server.SendToAll(writer, method);
        }
    }
    
    void SendConcrete(NetPeer peer, NetDataWriter writer, DeliveryMethod method)
    {
        if (peer == null) return;
        int count = MiscHelper.CountPackets(method);

        //Посылаем много раз, в надежде что хоть 1 дойдет)
        for (int i = 0; i < count; i++)
        {
            peer.Send(writer, method);
        }
    }

    #endregion

    #region Misc

    int m_IndexPlayers;
    string GenericPlayerID()
    {
        m_IndexPlayers++;
        return "C_" + m_IndexPlayers;
    }

    #endregion

    #region ConnectProcess

    class ProcessConnect
    {
        GameServer m_Server;
        Dictionary<Player, Coroutine> m_ListConnects;

        public ProcessConnect(GameServer server)
        {
            m_Server = server;
            m_ListConnects = new Dictionary<Player, Coroutine>(10);
        }

        public bool Connect(Player player)
        {
            if (player == null || m_ListConnects.ContainsKey(player)) return false;
            m_ListConnects.Add(player, m_Server.StartCoroutine(ConnectProcess(player)));
            return true;
        }

        public void StopConnect(Player player)
        {
            Coroutine cor;
            if (m_ListConnects.TryGetValue(player, out cor))
            {
                if (cor != null) m_Server.StopCoroutine(cor);
            }
            m_ListConnects.Remove(player);
        }

        public void StopAllConnect()
        {
            foreach(var key in m_ListConnects.Keys)
            {
                var cor = m_ListConnects[key];
                if (cor != null) m_Server.StopCoroutine(cor);
            }
            m_ListConnects.Clear();
        }

        IEnumerator ConnectProcess(Player player)
        {
            var players = m_Server.m_Players;
            player.State = ClientState.Connection;
            players.AddPlayer(player);
            SendVerificationData(player);
            Log("ConnectProcess=" + ClientState.Verification);
            yield return WaitAccept(player, ClientState.Verification);
            SendRegisterData(player);
            Log("ConnectProcess=" + ClientState.Register);
            yield return WaitAccept(player, ClientState.Register);
            //SendGameData(player);
            Log("ConnectProcess="+ ClientState.Connected);
            yield return WaitAccept(player, ClientState.Connected);
            //m_Server.ConnectPlayer(player.ID);
            StopConnect(player);
        }

        IEnumerator WaitAccept(Player player, ClientState state)
        {
            while (player.State != state)
            {
                yield return null;
            }
        }

        void SendVerificationData(Player player)
        {
            Log("SendVerificationData on " + player.IPInfo);
            var writer = m_Server.m_Writer;
            writer.Reset();
            ReaderGameHelper.AddCommand(writer, ServerCommands.Verify);
            //writer.Put(player.ID);
            player.Peer.Send(writer, DeliveryMethod.ReliableUnordered);
        }

        void SendRegisterData(Player player)
        {
            Log("SendRegisterData on " + player.IPInfo);
            var writer = m_Server.m_Writer;
            var players = m_Server.m_Players;
            writer.Reset();
            ReaderGameHelper.AddCommand(writer, ServerCommands.Register);
            var playerID = player.ID;
            //players.AddPlayer(player);
            writer.Put(playerID);
            writer.Put(player.PlayerName);
            writer.Put(players.Count - 1);
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var otherPlayer = players[i];
                if (!otherPlayer.IsConnected) continue;
                var id = otherPlayer.ID;
                if (id == playerID) continue;
                writer.Put(id);
                //writer.Put(otherPlayer.PlayerName);
            }
            player.Peer.Send(writer, DeliveryMethod.ReliableUnordered);
        }
    }

    ProcessConnect m_ConnectControl;

    #endregion

    #region EventsHandler

    public void OnPeerConnected(NetPeer peer)
    {
        if (m_IsLog) Log("OnPeerConnected " + peer.EndPoint);

        m_ConnectControl.Connect(new ProxyPlayer(GenericPlayerID(), peer));
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (m_IsLog) Log("OnPeerDisconnected " + peer.EndPoint + " reason=" + disconnectInfo.Reason);
        var id = m_Players.GetPlayerIDByPeer(peer);
        if (m_Players.RemovePlayer(id))
        {
            //DisconnectPlayer(id);
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        if (m_IsLog) Log("OnNetworkError " + endPoint + " error=" + socketError);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        try
        {
            //if (m_IsLog) Log("OnNetworkReceive " + peer.EndPoint + " method=" + deliveryMethod);
            var player = m_Players.GetPlayerByPeer(peer);
            if (player == null) return;
            var state = ReaderGameHelper.GetClientState(reader);
            //var prevState = player.State;

            //TODO: добавить действия которые присылает клиент(как ответы на команды от сервера)
            if (state == ClientState.Verification)
            {
                player.PlayerName = reader.GetString();
                player.TypeUnit = (TypeUnit)reader.GetByte();
                m_Players.ResolveDublicateName(player);
            }

            if(state!= ClientState.Connected)
            {
                Log("OnNetworkReceive " + player + " setState=" + state + " frame=" + Time.frameCount);
            }

            player.State = state;
            //if (m_IsLog) Log("OnNetworkReceive " + player);
            if (player.IsConnected) player.AddData(reader);
            //if (MiscHelper.CheckIncludeInMask(state, MiscHelper.MASK_REGISTRY_OR_CONNECTED))
            //{
            //    player.AddData(reader);
            //}
        }
        catch (System.Exception e)
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
        if (m_Players.IsMax)
        {
            if (m_IsLog) Log("OnConnectionRequest " + request.RemoteEndPoint +" is reject. Full server!");
            request.Reject();
        }
        else
        {
            if (m_IsLog) Log("OnConnectionRequest " + request.RemoteEndPoint + " is accept");
            request.AcceptIfKey(SERVER_KEY);
        }
    }

    #endregion
}
