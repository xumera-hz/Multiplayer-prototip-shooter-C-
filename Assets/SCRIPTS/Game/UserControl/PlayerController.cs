using UnityEngine;
using System;

public class PlayerController : UnitNetworkController
{
    [SerializeField] AttackController m_AttackControl;

    public event Func<Transform, Transform> ClosestAttackTarget;

    void OnClickAttack(AttackController.Args args)
    {
        m_AutoAttack = args.ClickAttack;
        m_IsAttack = args.IsAttack;
        m_AttackDir = args.AttackDir;
    }

    bool m_AutoAttack;
    bool m_IsAttack;
    Vector3 m_AttackDir;

    protected override bool IsAttack { set { m_IsAttack = value; } get { return m_IsAttack && m_Unit.LifeControl.Lived; } }

    protected override Vector3 GetAttackDirection()
    {
        if (m_AutoAttack)
        {
            m_AutoAttack = false;
            if (ClosestAttackTarget == null)
            {
                m_AttackDir = m_Unit.MoveControl.forward;
            }
            else
            {
                var targ = ClosestAttackTarget(m_Unit.TF);
                if (targ == null) m_AttackDir = m_Unit.MoveControl.forward;
                else
                {
                    m_AttackDir = targ.position - m_Unit.TF.position;
                    m_AttackDir.y = 0f;
                }
            }
            m_AttackDir.Normalize();
        }
        m_SendData.Attack.AttackDir = m_AttackDir;
        return m_AttackDir;
    }

    protected override Vector3 GetMoveDirection()
    {
        Vector2 move = JoysticksManager.MoveJoystick.MotionDir;
        var dir = RotateByCamera(move);
        m_SendData.Move.MoveDir = dir;
        return dir;
    }

    Vector3 RotateByCamera(Vector2 move)
    {
        var rot = ManagerCameras.GetMainCameraTransform().rotation;
        Vector3 forw = rot * Vector3.forward;
        Vector3 right = rot * Vector3.right;
        Vector3 dir;
        dir.x = forw.x * move.y + right.x * move.x;
        dir.y = 0f;
        dir.z = forw.z * move.y + right.z * move.x;
        return dir;
    }

    void ServerReceive(PlayerData data)
    {
        if (m_Unit.LifeControl.Lived)
        {
            var move = m_Unit.MoveControl;
            move.forward = data.Move.LookDir;
            move.position = data.Move.Pos;
            move.Apply();
        }
    }

    private void ServerSend()
    {
        m_SendData.Health = m_Unit.LifeControl.HealthPoints;
        m_SendData.CountKills = OnGetKills();
        m_SendData.Move.LookDir = m_Unit.MoveControl.forward;
        m_SendData.Move.Pos = m_Unit.TF.position;
    }

    void ClientReceive(PlayerData data)
    {
        OnSetKills(m_ReceiveData.CountKills, data.CountKills);
        IsAttack = data.Attack.AttackID > m_ReceiveData.Attack.AttackID;
        if (!m_Unit.LifeControl.Lived)
        {
            var move = m_Unit.MoveControl;
            move.forward = data.Move.LookDir;
            move.position = data.Move.Pos;
            move.Apply();
        }
        m_Unit.LifeControl.SetHealth(data.Health);
    }

    private void ClientSend()
    {
        m_SendData.Move.LookDir = m_Unit.MoveControl.forward;
        m_SendData.Move.Pos = m_Unit.TF.position;
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

        m_ReceiveData = data;
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
            m_Unit.MoveControl.position = m_ReceiveData.Move.Pos;
            return;
        }

        //var pos = m_Unit.MoveControl.position;
        //var pos1 = data.Move.Pos;
        //if ((pos - pos1).sqrMagnitude > 1e-4f)
        //{
        //    m_Unit.MoveControl.position = pos1;
        //    m_Unit.MoveControl.Apply();
        //}

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
        m_SendData.Move.Pos = m_Unit.TF.position;
        m_SendData.Move.LookDir = m_Unit.TF.forward;
    }

    protected override void OnUpdate()
    {
        m_AttackControl.ManualUpdate();
        ApplyReceiveData();
    }

    public override PlayerData GetData()
    {
        SetSendData();
        var data = base.GetData();
        //Debug.LogError("Owner_GetData " + data);
        return data;
    }

    public override void AddData(PlayerData data)
    {
        //Debug.LogError("Owner_AddData " + data);
        base.AddData(data);
    }

    protected override void ExternInit()
    {
        base.ExternInit();
        ManagerCameras.GetMainCamera().GetComponent<Tracker>().SetTarget(m_Unit.TF);
    }

    void OnUnitAttackEvent()
    {
        m_ReceiveData.Attack.AttackID++;
    }

    protected override void InnerInit(UnitContainer unit)
    {
        base.InnerInit(unit);
        m_AttackControl.Init(m_Unit.TF);
        m_AttackControl.AttackEvents += OnClickAttack;
        m_Unit.UnitControl.AttackEvent += () => { m_SendData.Attack.AttackID++; };
        m_Unit.LifeControl.DeathEvent += (sender, arg) => { if(IsServer) m_SendData.LifeID++; };
    }
}

public abstract class UnitControlBase : MonoBehaviour
{
    [SerializeField]
    protected UnitContainer m_Unit;

    protected abstract Vector3 GetMoveDirection();
    protected abstract Vector3 GetAttackDirection();
    protected abstract bool IsAttack { get; set; }

    protected virtual void OnUpdate() { }

    void UnitUpdate(Unit unit)
    {
        float deltaTime = TimeManager.TimeDeltaTime;
        unit.SetMoveDirection(GetMoveDirection());
        unit.SetAttackDirection(GetAttackDirection());
        if (IsAttack) unit.SetAttack();
        IsAttack = false;
        unit.ManualUpdate(deltaTime);
    }

    private void Update()
    {
        if (m_Unit.IsNullOrDestroy()) return;
        UnitUpdate(m_Unit.UnitControl);


        OnUpdate();
    }

    public void Start()
    {
        if (!m_Unit.IsNullOrDestroy()) Init(m_Unit);
    }

    //TODO: вынести отсюда
    protected virtual void ExternInit()
    {
        //GrassController.AddTarget(m_Unit.TF, true);
    }

    protected virtual void InnerInit(UnitContainer unit)
    {
        m_Unit = unit;
    }

    public void Init(UnitContainer unit)
    {
        InnerInit(unit);
        ExternInit();
    }
}
