using System;
using UnityEngine;

public class UnitController : UnitNetworkController
{
    bool m_IsAttack;
    protected override bool IsAttack { get { return m_IsAttack; } set { m_IsAttack = value && m_Unit.LifeControl.Lived; } }
    //{
    //    get { return m_CurrentData.Attack.IsAttack; }
    //    set { /*m_Data.Attack.IsAttack = value;*/ }
    //}

    protected override Vector3 GetMoveDirection() { return m_ReceiveData.Move.MoveDir; }
    protected override Vector3 GetAttackDirection() { return m_ReceiveData.Attack.AttackDir; }


    void ServerReceive(PlayerData data)
    {
        IsAttack = data.Attack.AttackID > m_ReceiveData.Attack.AttackID;
        if (m_Unit.LifeControl.Lived)
        {
            var move = m_Unit.TF;
            move.forward = data.Move.LookDir;
            move.position = data.Move.Pos;
            //move.Apply();
        }
        m_ReceiveData = data;
    }

    private void ServerSend()
    {
        //m_SendData = m_ReceiveData;
        m_SendData.Health = m_Unit.LifeControl.HealthPoints;
        m_SendData.CountKills = OnGetKills();
        m_SendData.Move.LookDir = m_Unit.TF.forward;
        m_SendData.Move.Pos = m_Unit.TF.position;
        m_SendData.Move.MoveDir = m_ReceiveData.Move.MoveDir;
        //if (!m_Unit.LifeControl.Lived)
        //{
        //    //m_SendData.Move.MoveDir = m_ReceiveData.Move.MoveDir;
        //    m_SendData.Move.LookDir = m_Unit.MoveControl.forward;
        //    m_SendData.Move.Pos = m_Unit.TF.position;
        //}
    } 

    void ClientReceive(PlayerData data)
    {
        OnSetKills(m_ReceiveData.CountKills, data.CountKills);
        IsAttack = data.Attack.AttackID > m_ReceiveData.Attack.AttackID;
        m_Unit.LifeControl.SetHealth(data.Health);
        var move = m_Unit.TF;
        move.forward = data.Move.LookDir;
        move.position = data.Move.Pos;
        //move.Apply();
        m_ReceiveData = data;
    }

    private void ClientSend()
    {

    }

    void ApplyReceiveData()
    {
        if (!IsData) return;

        var data = GetActualData();

        //if (!m_IsReceive)
        //{
        //    m_ReceiveData = data;
        //    m_IsReceive = true;
        //}
        //else
        //{
        //    if (m_ReceiveData.ServerData.Tick == data.ServerData.Tick) return;
        //}
        if (m_ReceiveData.ServerData.Tick == data.ServerData.Tick) return;

        if (IsServer)
        {
            ServerReceive(data);
        }
        else
        {
            ClientReceive(data);
        }
    }

    void SetSendData()
    {
        if (IsServer) ServerSend();
        else ClientSend();
    }
    [Obsolete("ApplyReceiveData2")]
    void ApplyReceiveData2()
    {
        if (!IsData) return;

        var data = GetActualData();

        if (!m_IsReceive)
        {
            m_ReceiveData = data;
            m_IsReceive = true;
        }
        else
        {
            if (m_ReceiveData.ServerData.Tick == data.ServerData.Tick) return;
        }

        //статистика убийств
        if (!IsServer) OnSetKills(m_ReceiveData.CountKills, data.CountKills);

        //если игрок на сервере умирал(или мертв)
        //То если приходят команды когда он был жив
        //Они бракуются
        if (m_ReceiveData.LifeID > data.LifeID)
        {
            //m_ReceiveData = data;
            return;
        }
        var move = m_Unit.MoveControl;
        var pos = move.position;
        var pos1 = data.Move.Pos;
        if ((pos - pos1).sqrMagnitude > 1e-4f)
        {
            move.position = pos1;
            move.Apply();
        }

        if (data.Move.MoveDir.sqrMagnitude < 1e-4f)
        {
            move.forward = data.Move.LookDir;
        }

        IsAttack = data.Attack.AttackID > m_ReceiveData.Attack.AttackID;

        if (IsServer)
        {

        }
        else
        {
            m_Unit.LifeControl.SetHealth(data.Health);
        }
        m_ReceiveData = data;
    }
    [Obsolete("SetSendData2")]
    void SetSendData2()
    {
        if (IsServer)
        {
            m_SendData.Health = m_Unit.LifeControl.HealthPoints;
            m_SendData.CountKills = OnGetKills();
        }
        m_SendData.Move.MoveDir = m_ReceiveData.Move.MoveDir;
        m_SendData.Move.Pos = m_Unit.TF.position;
    }

    protected override void OnUpdate()
    {
        ApplyReceiveData();
    }

    protected override void InnerInit(UnitContainer unit)
    {
        base.InnerInit(unit);
        m_Unit.UnitControl.AttackEvent += ()=> { if (IsServer) m_SendData.Attack.AttackID++; };
        m_Unit.LifeControl.DeathEvent += (sender, arg) => { if (IsServer) m_SendData.LifeID++; };
        m_Unit.MoveControl.BlockPos = true;
        m_Unit.MoveControl.BlockRot = true;
    }

    public override void AddData(PlayerData data)
    {
        //Debug.LogError("Proxy_AddData " + data);
        base.AddData(data);
    }

    public override PlayerData GetData()
    {
        SetSendData();
        var data = base.GetData();
        //Debug.LogError("Proxy_GetData " + m_SendData);
        return data;
    }
}

public abstract class UnitNetworkController : UnitControlBase
{

    public bool IsServer { get { return ConnectController.IsServer; } }

    protected bool IsData { get { return m_Buffer.Count > 0; } }
    protected PlayerData m_ReceiveData;
    protected bool m_IsReceive;
    protected PlayerData m_SendData;
    readonly PlayerDataBuffer m_Buffer = new PlayerDataBuffer();

    protected PlayerData GetActualData()
    {
        if (!m_IsReceive)
        {
            m_IsReceive = true;
            return m_Buffer[0];
        }

        int count = m_Buffer.Count;

        float deltaTime = TimeManager.UnscaledDeltaTime;
        float lastTime = m_ReceiveData.ServerData.Time;
        float newTime = lastTime + deltaTime - GameConstants.INTERPOLATION_TIME;

        //Debug.LogError("GetActualData " + this + " CountBuffer=" + count+" newTime="+newTime);
        m_ReceiveData.ServerData.Time += deltaTime;
        if (count <= 0 || newTime <= 0f) return m_ReceiveData;


        //Debug.LogError("GetActualData " + this + " CountBuffer=" + count);

        //for (int i = 0; i < count; i++)
        //{
        //    float time = m_Buffer[i].ServerData.Time;
        //    //Debug.LogError("Buffer[" + i + "]=" + time);
        //    if (newTime >= time)
        //    {
        //        int ind = time == newTime ? i : i - 1;
        //        if (ind < 0) ind = 0;
        //        var data = m_Buffer[ind];
        //        m_Buffer.RemoveAtRange(0, ind + 1);
        //        return data;
        //    }
        //}

        for (int i = 0; i < count; i++)
        {
            float time = m_Buffer[i].ServerData.Time;
            //Debug.LogError("Buffer[" + i + "]=" + time);
            if (newTime > time) continue;
            int ind = time == newTime ? i : i - 1;
            if (ind < 0) ind = 0;
            var data = m_Buffer[ind];
            m_Buffer.RemoveAtRange(0, ind + 1);
            return data;
        }
        return m_ReceiveData;
    }

    public virtual void AddData(PlayerData data)
    {
        //Если пришли данные старые(разница во времени больше N сек, игнорим их)
        if (m_Buffer.Count > 0)
        {
            if ((m_Buffer.Last.ServerData.Time - data.ServerData.Time) > GameConstants.IGNORE_RECEIVE_DATA_TIME) return;
        }
        m_Buffer.Add(data);
    }

    public virtual PlayerData GetData()
    {
        var data = m_SendData;
        data.ServerData.Tick = TimeManager.Frame;
        data.ServerData.Time = TimeManager.RealtimeSinceStartup;
        return data;
    }


    //TODO: переделать
    #region Kills
    public event Action<GameObject, int> SetKills;
    public event Func<GameObject, int> GetKills;
    protected void OnSetKills(int prevCount, int count)
    {
        //Debug.LogError("prevCount=" + prevCount + " count=" + count);
        if (prevCount == count) return;
        if (SetKills != null) SetKills(m_Unit.GO, count);
    }
    protected int OnGetKills()
    {
        return GetKills == null ? 0 : GetKills(m_Unit.GO);
    }
    #endregion
}

