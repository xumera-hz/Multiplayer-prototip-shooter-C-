using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] TypeState m_TestState;
    [ContextMenu("TestState")]
    void TestState()
    {
        SetState(m_TestState);
    }
#endif

    UnitContainer m_Container;
    Vector3 m_MoveDir;
    Vector3 m_AttackDir;
    protected readonly States m_States = new States();

    public enum TypeState { Idle, Move, Attack, Death }

    private void Start()
    {
        m_Container = GetComponent<UnitContainer>();
        m_Container.LifeControl.DeathEvent += (arg1, arg2) => { SetState(TypeState.Death); };
        m_Container.LifeControl.ResurrectionEvent += (arg1, arg2) => { SetState(TypeState.Idle); };
        InitStates();
        SetLayer(1, 0f);
    }

    private void OnEnable()
    {
        if (m_Container != null) SetLayer(1, 0f);
    }

    public void ManualUpdate(float deltaTime)
    {
        m_States.Update(deltaTime);
        m_AttackState.Update(deltaTime);
    }

    void SetState(TypeState state)
    {
        m_States.SetState((int)state);
    }

    void SetLayer(int layer, float value)
    {
        m_Container.Anim.SetLayerWeight(layer, value);
    }

    void PlayAnim(int hash, int layer = 0)
    {
        m_Container.Anim.CrossFade(hash, 0f, layer);
    }

    void InitStates()
    {
        m_States.AddState((int)TypeState.Idle, new EasyStateWrapper(() => { PlayAnim(CharAnimHashes.Idle); }, null, IdleUpdate));
        m_States.AddState((int)TypeState.Move, new EasyStateWrapper(()=> { PlayAnim(CharAnimHashes.Move); }, null, MoveUpdate));
        m_States.AddState((int)TypeState.Death, new EasyStateWrapper(
            () => {
                m_AttackState.ExitState();
                PlayAnim(CharAnimHashes.Death);
                m_Container.CharacterControl.enabled = false;
            }
            , () => 
            {
                m_Container.CharacterControl.enabled = true;
            }
            , DeathUpdate));

        m_States.SetState((int)TypeState.Idle);
        m_AttackState = new EasyStateWrapper(AttackStart, AttackStop, AttackUpdate);
    }

    public void SetDefault()
    {
        m_States.SetState((int)TypeState.Idle);
        m_Container.CharacterControl.enabled = true;
        m_AttackState.ExitState();
    }

    public void SetMoveDirection(Vector3 dir)
    {
        m_MoveDir = dir;
    }

    public void SetAttackDirection(Vector3 dir)
    {
        m_AttackDir = dir;
    }

    void IdleUpdate(float deltaTime)
    {
        if (m_MoveDir.sqrMagnitude > 1e-5f) SetState(TypeState.Move);
    }

    void MoveUpdate(float deltaTime)
    {
        var dir = m_MoveDir.normalized;
        if (dir.sqrMagnitude < 1e-5f) SetState(TypeState.Idle);
        else
        {
            var move = m_Container.MoveControl;
            move.Move(dir, deltaTime);
            move.forward = dir;
            move.Apply();
        }
    }

    void DeathUpdate(float deltaTime)
    {
        m_AttackState.ExitState();
    }

    #region Attack
    //TODO: синхронизировать с анимацией и оружием
    //float m_AttackTime = 1f;
    float m_CurrentAttacKTime;

    ITimedState m_AttackState;

    void AttackStart()
    {
        if (IsAttack) return;
        bool attack = m_Container.WeaponControl.Attack(m_AttackDir);
        if (attack)
        {
            m_CurrentAttacKTime = m_Container.WeaponControl.Weapon.Info.AttackTime;
            IsAttack = true;
            m_AttackDir = Vector3.zero;
            SetLayer(1, 1f);
            PlayAnim(CharAnimHashes.Attack, 1);
            if (AttackEvent != null) AttackEvent();
        }
    }

    void AttackStop()
    {
        SetLayer(1, 0f);
        PlayAnim(CharAnimHashes.Idle, 1);
        IsAttack = false;
    }

    void AttackUpdate(float deltaTime)
    {
        if (!IsAttack) return;
        m_Container.MoveControl.forward = m_AttackDir;
        m_Container.MoveControl.Apply();
        if (m_CurrentAttacKTime <= 0f) m_AttackState.ExitState();
        else m_CurrentAttacKTime -= deltaTime;
    }

    public void SetAttack()
    {
        m_AttackState.StartState();
    }

    public bool IsAttack { get; private set; }

    public event Action AttackEvent;

    #endregion
}

public class States : TimedStateManager<int, ITimedState>
{
    public States() : base(10, null, -1, null) { }
}

public class EasyStateWrapper : ITimedState
{
    Action m_Start;
    Action m_Exit;
    Action<float> m_Update;

    public EasyStateWrapper(Action start, Action exit, Action<float> update)
    {
        m_Start = start == null ? () => { } : start;
        m_Exit = exit == null ? () => { } : exit;
        m_Update = update == null ? (arg) => { } : update;
    }

    void IFixedState.StartState()
    {
        m_Start();
    }

    void IFixedState.ExitState()
    {
        m_Exit();
    }

    void ITimedState.Update(float deltaTime)
    {
        m_Update(deltaTime);
    }


}

/*

public class StateInfo
{
    public int ID;
    public int Priority;
    public int[] CanTransition;
    //public Action ActionStart, ActionExit;
    //public Func<int> CheckActionEnd;
    public StateInfo(int id, int p, int count) : this(id, p)
    {
        CanTransition = new int[count];
    }
    public StateInfo(int id, int p)
    {
        ID = id;
        Priority = p;
    }
}

public class FSM
{
    protected StateInfo[] m_StatesInfo;

    protected byte[] m_CurrentActiveStates;

    protected byte[] m_DeactiveStates;

    StateInfo GetState(int id)
    {
        return m_StatesInfo[id];
    }

    void SetState(int id)
    {
        var state = GetState(id);
        
    }

    #region Узнаем, что состояние может работать с другим состоянием

    byte[,] m_StateTable;
    bool CanStateByState(int state1, int state2)
    {
        return m_StateTable[state1, state2] == 1;
    }

    #endregion
}

    */
