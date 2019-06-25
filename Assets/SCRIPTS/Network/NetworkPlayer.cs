using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System;
using System.Runtime.InteropServices;

#region PlayerData

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MoveData
{
    public Vector3 Pos;
    public Vector3 MoveDir;
    public Vector3 LookDir;
    public int RespawnIndex;

    public override string ToString()
    {
        return string.Format("Pos={0} MoveDir={1} LookDir={2}", Pos, MoveDir, LookDir);
    }
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AttackData
{
    public int AttackID;
    //public bool IsAttack;
    public Vector3 AttackDir;

    public override string ToString()
    {
        return string.Format("AttackID={0} AttackDir={1}", AttackID, AttackDir);
    }
}

public class PlayerDataBuffer : IndexArray<PlayerData>
{
    public PlayerDataBuffer() : base(GameConstants.LENGTH_BUFFER_SAVE_PLAYER_DATA, PlayerData.MIN_DATA, PlayerData.COMPARER) { }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ServerData
{
    public float Time;
    public int Tick;

    public override string ToString()
    {
        return string.Format("Time={0} Tick={1}", (float)(((int)(Time * 100f)) / 100f), Tick);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FloatValueData
{
    public float Value;
    public float Max;
    public float Min;

    public override string ToString()
    {
        return string.Format("Health: value={0} Min={1} Max={2}", Value, Min, Max);
    }

}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerData
{
    public static readonly int SIZE = Marshal.SizeOf(typeof(PlayerData));
    public static readonly PlayerData MIN_DATA = new PlayerData();
    public static readonly IComparer<PlayerData> COMPARER = new Comparer();
    class Comparer : IComparer<PlayerData>
    {
        int IComparer<PlayerData>.Compare(PlayerData x, PlayerData y)
        {
            return x.ServerData.Tick - y.ServerData.Tick;
        }
    }

    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    //public Vector3[] Dirs;
    public ServerData ServerData;
    public int LifeID;
    public MoveData Move;
    public AttackData Attack;
    public FloatValueData HealthData;
    public int CountKills;

    public FloatValue Health
    {
        get { return new FloatValue() { Min = HealthData.Min, Max = HealthData.Max, Value = HealthData.Value };  }
        set { HealthData = new FloatValueData() { Min = value.Min, Max = value.Max, Value = value.Value }; }

    }

    public override string ToString()
    {
        //return string.Format("\n{0}\n{1}\n{2}\n", Move, Attack, Health);
        return string.Format("\nLifeID={0}\n{1}\n{2}\n", LifeID, ServerData, Health);
    }
}

#endregion

#region Player

public class ClientPlayer : Player
{
    public ClientPlayer(string id, NetPeer peer) : base(id, peer, true) { }
    public ClientPlayer(NetPeer peer) : base(null, peer, true) { }

    //TODO: вынести
    protected override void InitUnit()
    {
        //Debug.LogError("Client_InitUnit=" + ID);
        if (!m_UnitControl.IsNullOrDestroy()) return;
        if (!GameController.Can)
        {
            Log(typeof(GameController) + " is null");
            return;
        }
        m_UnitControl = GameController.I.PlayerControl;
        if (m_UnitControl.IsNullOrDestroy())
        {
            Log("UnitControl is null");
            return;
        }
        var unit = GameController.I.AddUnit(this, TypePlayer.Self, TypeUnit);
        m_UnitControl.Init(unit);
    }

    protected override void DestroyUnit()
    {
        //Debug.LogError("Client_DestroyUnit=" + ID);
        if (GameController.Can) GameController.I.RemoveUnit(this);
    }

    public override void Update(NetDataWriter writer)
    {
        if (m_State != ClientState.Connected) return;
        ReaderGameHelper.AddClientState(writer, m_State);
        int index = m_DataSend.Last + 1;
        m_DataSend.Add(index);
        writer.Put(index);
        var data = m_UnitControl.GetData();
        if (writer.PutInWriter(data))
        {
            //шоб наверняка) засрем канал))
            int count = MiscHelper.CountPackets(DeliveryMethod.Unreliable);
            for (int i = 0; i < count; i++) m_Peer.Send(writer, DeliveryMethod.Unreliable);
        }
    }

    public override bool AddData(NetDataReader reader)
    {
        //Debug.LogError("Player_AddData_Begin " + this);
        int index = reader.GetInt();

        if (m_DataReceive.Add(index)) return false;
        
        PlayerData d;
        bool res = reader.FromReader(out d);
        if (res)
        {
            m_UnitControl.AddData(d);
        }
        return res;
    }

    public override bool GetData(NetDataWriter writer)
    {
        //Debug.LogError("Client_GetData ID=" + this);
        PrepareSend(writer);
        var data = m_UnitControl.GetData();
        return writer.PutInWriter(data);
    }
}

public class ProxyPlayer : Player
{
    public ProxyPlayer(string id, NetPeer peer) : base(id, peer) { }

    public override bool AddData(NetDataReader reader)
    {
        //Debug.LogError("Proxy_AddData_Begin ID=" + this);
        int index = reader.GetInt();

        if (m_DataReceive.Add(index)) return false;
        PlayerData d;
        bool res = reader.FromReader(out d);
        if (res)
        {
            m_UnitControl.AddData(d);
        }
        return res;
    }
    public override bool GetData(NetDataWriter writer)
    {
        //Debug.LogError("Proxy_GetData ID=" + this);
        PrepareSend(writer);
        var data = m_UnitControl.GetData();
        return writer.PutInWriter(data);
    }

    protected override void InitUnit()
    {
        //Debug.LogError("Proxy_InitUnit=" + ID);
        if (!m_UnitControl.IsNullOrDestroy()) return;
        m_UnitControl = new GameObject("UnitController_" + ID).AddComponent<UnitController>();

        if (m_UnitControl.IsNullOrDestroy())
        {
            Log("UnitControl is null");
            return;
        }
        if (!GameController.Can)
        {
            Log(typeof(GameController) + " is null");
            return;
        }
        var unit = GameController.I.AddUnit(this, TypePlayer.Enemy, TypeUnit);
        m_UnitControl.Init(unit);
    }
    protected override void DestroyUnit()
    {
        //Debug.LogError("Proxy_DestroyUnit="+ID);
        m_UnitControl.DestroyGO();
        if (GameController.Can) GameController.I.RemoveUnit(this);
    }
}

public abstract class Player
{
    //public const int MAX_LENGTH_PLAYER_ID = GameConstants.MAX_LENGTH_PLAYER_ID;
    protected NetPeer m_Peer;
    protected UnitNetworkController m_UnitControl;

    public Action<object, bool> Logger { get; set; }

    protected void PrepareSend(NetDataWriter writer)
    {
        ReaderGameHelper.AddWorldUpdate(writer, TypeWorldUpdate.Player);
        ReaderGameHelper.AddClientState(writer, State);
        writer.Put(ID);
        writer.Put(PlayerName);
        writer.Put((byte)TypeUnit);
        int index = m_DataSend.Last + 1;
        writer.Put(index);
        m_DataSend.Add(index);
    }

    public override string ToString()
    {
        return string.Format("PlayerID = {0} PlayerName = {1} State = {2}", ID, PlayerName, m_State);
    }

    public Player(string id, NetPeer peer, bool isMine)
    {
        ID = id;
        m_Peer = peer;
        IsMine = isMine;
    }

    public Player(string id, NetPeer peer) : this(id, peer, false) { }

    protected IndexArray m_DataReceive = new IndexArray(GameConstants.LENGTH_BUFFER_SAVE_PLAYER_DATA);
    protected IndexArray m_DataSend = new IndexArray(GameConstants.LENGTH_BUFFER_SAVE_PLAYER_DATA);

    public UnitControlBase Controller { get { return m_UnitControl; } }
    public int LastIndexDataSend { get { return m_DataSend.Last; } }
    public int LastIndexDataReceive { get { return m_DataReceive.Last; } }
    public bool IsConnected { get { return m_State == ClientState.Connected; } }
    //public bool IsClient { get { return !IsServer; } }
    public bool IsFakeClient { get { return IsMine && IsServer; } }
    //TODO: заменить на ConnectInfo
    public bool IsServer { get { return ConnectController.IsServer; } }
    public bool IsMine { get; private set; }
    protected ClientState m_State;
    public string ID { get; set; }
    public string PlayerName { get; set; }
    public NetPeer Peer { get { return m_Peer; } }
    public IPEndPoint IPInfo { get { return m_Peer == null ? null : m_Peer.EndPoint; } }

    //TODO: выпилить вместо с InitUnit
    public TypeUnit TypeUnit { get; set; }
    public ClientState State
    {
        get { return m_State; }
        set
        {
            //Debug.LogError("SET_STATE "+ value+" for " + this);
            if (m_State == value) return;
            m_State = value;
            if (m_State == ClientState.Connected)
            {
                InitUnit();
            }
            else if (m_State == ClientState.Disconnected)
            {
                DestroyUnit();
            }
        }
    }

    public abstract bool AddData(NetDataReader reader);
    public abstract bool GetData(NetDataWriter writer);
    public virtual void Update(NetDataWriter writer) { }

    protected void Log(object obj, bool error = false)
    {
        try
        {
            if (Logger != null) Logger(obj, error);
        }
        catch (Exception e) { }
    }

    //TODO: костыльки пока что)
    protected abstract void InitUnit();

    protected abstract void DestroyUnit();



    #region OLD
    //TODO: доделать
    class IndexArray2
    {
        int m_FirstIndex = -1;
        int m_LastIndex = -1;
        int m_Current;
        int m_Count;
        readonly int m_Cap;
        //int m_OptimiseCap;
        readonly int[] m_Indexs;

        ///// <param name="cap"></param>
        ///// <param name="optimiseCap">
        ///// определяет проверку последних элементов на наличие дубля
        ///// Если не найдет, то проверяет весь массив
        ///// По дефолту 3 ( если tick 33ms, то 3 будет учитывать 100 ms)
        ///// </param>
        public IndexArray2(int cap/*, int optimiseCap = 3*/)
        {
            //на нахер тебе Exception, нехрен нулевые индексы подсовывать
            if (cap == 0) cap = -1;
            m_Indexs = new int[cap];
            m_Current = cap - 1;
            m_Cap = cap;
            //OptimiseCap = optimiseCap;
        }

        //public int OptimiseCap { get { return m_OptimiseCap; } set { m_OptimiseCap = value < 0 ? 0 : (value > m_Cap ? m_Cap : value); } }
        public int Last { get { return m_LastIndex; } }
        public int Count { get { return m_Count; } }
        public bool IsFull { get { return m_Count >= m_Cap; } }


        void Shift(int ind)
        {
        }

        void AddIndex(int ind)
        {
            int delta = ind - m_LastIndex;
            //ВАААААЙП
            if (delta >= m_Cap)
            {
                m_Current = m_Cap - 1;
                m_Count = 1;
                m_Indexs[m_Current] = ind;
                m_FirstIndex = m_Current;
            }
            else
            {
                for (int i = 0; i < delta; i++)
                {
                    m_Current--;
                    m_FirstIndex--;
                    if (m_Current < 0) m_Current = m_Cap - 1;
                    if (m_FirstIndex < 0) m_FirstIndex = m_Cap - 1;
                    m_Indexs[m_Current] = -1;
                }
                m_Indexs[m_Current] = ind;
                m_LastIndex = ind;
                if (m_Count < m_Cap)
                {
                    if (m_FirstIndex == -1) m_FirstIndex = ind;
                    m_Count++;
                }
                else
                {

                }
            }
        }

        public bool Add(int ind)
        {
            if (ind == m_LastIndex) return false;
            if (ind > m_LastIndex)
            {
                AddIndex(ind);
                return true;
            }
            int first = m_Indexs[m_FirstIndex];
            if (ind <= first) return false;
            //проверяем что значение входит в доверительный диапозон
            //затем перебираем массив на поиск пропуска значения

            int deltaCount = m_Cap - m_Count;
            if (deltaCount < 0) deltaCount = 0;
            for (int i = m_Cap - 2; i >= deltaCount; i--)
            {
                int id = m_Indexs[i];
                if (id == ind) return false;
                if (id == -1 || id > ind) continue;

            }
            return false;
        }
        //Если понадобиться сжимать индексы пакета
        //Эта функция работает путем сравнения двух чисел и их разности.
        //Если их разность меньше половины максимального значения порядкового номера,
        //то они должны быть близко друг к другу — таким образом, мы просто проверяем,
        //больше ли одно число чем другое, как обычно. Однако, если они далеко друг от друга,
        //их разность будет больше, чем половина максимального значения порядкового номера,
        //тогда, парадоксальным образом, мы будем считать меньший порядковый номер,
        //чем текущий порядковый номер, более поздним.
        bool SequenceMoreRecent(int s1, int s2, int max)
        {
            return
                (s1 > s2) &&
                (s1 - s2 <= max / 2)
                   ||
                (s2 > s1) &&
                (s2 - s1 > max / 2);
        }
    }
    #endregion

}

public class Players
{
    readonly List<Player> m_Players;
    readonly Dictionary<string, Player> m_PlayersToIDs;

    public Players(int maxPlayers)
    {
        maxPlayers = maxPlayers < 0 ? 0 : maxPlayers;
        MaxCount = maxPlayers;
        m_Players = new List<Player>(maxPlayers);
        m_PlayersToIDs = new Dictionary<string, Player>(maxPlayers);
    }

    public Player this[int id]
    {
        get { return m_Players[id]; }
    }

    public void Clear()
    {
        m_Players.Clear();
        m_PlayersToIDs.Clear();
    }

    //public void AddPlayerData(string id, NetDataReader reader)
    //{
    //    Player pl;
    //    bool res = m_PlayersToIDs.TryGetValue(id, out pl);
    //    if (res)
    //    {
    //        pl.AddData(reader);
    //    }
    //}

    public byte[] PlayersData { get { return null; } }

    public int Count { get { return m_Players.Count; } }

    public int MaxCount { get; private set; }

    public bool IsMax { get { return Count >= MaxCount; } }

    //public bool AddPlayer(string id, NetPeer peer = null)
    //{
    //    if (ContainsPlayer(id)) return false;
    //    var pl = CreateNewPlayer(id, peer);
    //    m_PlayersToIDs.Add(id, pl);
    //    m_Players.Add(pl);
    //    return true;
    //}

    public void ResolveDublicateName(Player player)
    {
        int count = 0;
        string defName = player.PlayerName;
        string name = defName;
        var id = player.ID;
        for (int i = m_Players.Count - 1; i >= 0; i--)
        {
            var pl = m_Players[i];
            if (pl.ID == id) continue;
            if (pl.PlayerName == name)
            {
                name = defName +  "(" + count + ")";
                count++;
                player.PlayerName = name;
                i = m_Players.Count;
            }
        }
        player.PlayerName = name;
    }

    public bool AddPlayer(Player player)
    {
        if (player == null || ContainsPlayer(player.ID)) return false;
        ResolveDublicateName(player);
        m_PlayersToIDs.Add(player.ID, player);
        m_Players.Add(player);
        return true;
    }

    public bool ContainsPlayer(string id)
    {
        return m_PlayersToIDs.ContainsKey(id);
    }

    public Player GetPlayer(string id)
    {
        Player pl;
        bool res = m_PlayersToIDs.TryGetValue(id, out pl);
        return res ? pl : null;
    }

    public bool RemovePlayer(string id)
    {
        Player pl;
        bool res = m_PlayersToIDs.TryGetValue(id, out pl);
        if (res)
        {
            m_PlayersToIDs.Remove(id);
            m_Players.Remove(pl);
            pl.State = ClientState.Disconnected;
        }
        return res;
    }

    public Player GetPlayerByPeer(NetPeer peer)
    {
        for (int i = m_Players.Count - 1; i >= 0; i--)
        {
            if (m_Players[i].Peer == peer) return m_Players[i];
        }
        return null;
    }

    public string GetPlayerIDByPeer(NetPeer peer)
    {
        var pl = GetPlayerByPeer(peer);
        return pl == null ? string.Empty : pl.ID;
    }

    //Player CreateNewPlayer(string id, NetPeer peer)
    //{
    //    return new Player(id, peer);
    //}
}

#endregion
