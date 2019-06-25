using System.Collections.Generic;

public interface IFixedState
{
    void StartState();
    void ExitState();
}

public interface ITimedState : IFixedState
{
    void Update(float deltaTime);
}

public class StateManager<Key, TState> : IFixedState where TState : IFixedState
{
    protected TState m_CurrentState;
    Key m_TypeCurrentState;
    //Key m_DefaultState;
    Dictionary<Key, TState> m_States;
    IEqualityComparer<Key> m_Compare;

    public StateManager() : this(default(Key), default(TState)) { }
    public StateManager(Key defaultKey, TState defaultState) : this(10, null, defaultKey, defaultState) { }
    public StateManager(int cap, IEqualityComparer<Key> comparer) : this(cap, comparer, default(Key), default(TState)) { }
    public StateManager(IEqualityComparer<Key> comparer, Key defaultKey, TState defaultState) : this(10, comparer, defaultKey, defaultState) { }
    public StateManager(int cap, IEqualityComparer<Key> comparer, Key defaultKey, TState defaultState)
    {
        m_States = new Dictionary<Key, TState>(cap, comparer);
        //m_DefaultState = defaultKey;
        m_TypeCurrentState = defaultKey;
        AddState(defaultKey, defaultState);
    }

    public Key TypeCurrentState
    {
        get { return m_TypeCurrentState; }
    }

    public TState CurrentState
    {
        get { return m_CurrentState; }
    }

    public void StartState()
    {
        if (m_CurrentState != null) m_CurrentState.StartState();
    }

    public void ExitState()
    {
        if (m_CurrentState != null) m_CurrentState.ExitState();
    }

    public void AddState(Key type, TState state)
    {
        if (m_States.ContainsKey(type)) return;
        m_States.Add(type, state);
    }

    public void RemoveState(Key type)
    {
        m_States.Remove(type);
        //if (CheckKeyEquals(m_DefaultState, type))
        //{
        //    m_TypeCurrentState = m_DefaultState;
        //    m_CurrentState = default(TState);
        //}
    }

    bool CheckKeyEquals(Key type1, Key type2)
    {
        if (m_Compare != null) return m_Compare.Equals(type1, type2);
        if (type1 != null) return type1.Equals(type2);
        if (type2 != null) return type2.Equals(type1);
        return true;
    }

    public virtual bool SetState(Key type)
    {
        if (CheckKeyEquals(m_TypeCurrentState, type)) return false;
        if (!m_States.ContainsKey(type))
        {
            UnityEngine.Debug.LogError(GetType()+ " error: Not exist state=" + type);
            return false;
        }
        if (m_CurrentState != null) m_CurrentState.ExitState();
        m_TypeCurrentState = type;
        m_CurrentState = m_States[type];
        if (m_CurrentState != null) m_CurrentState.StartState();
        return true;
    }
}

public class TimedStateManager<Key, TState> : StateManager<Key, TState>, ITimedState where TState : ITimedState
{
    public void Update(float deltaTime)
    {
        if (m_CurrentState != null) m_CurrentState.Update(deltaTime);
    }

    public TimedStateManager() : this(default(Key), default(TState)) { }
    public TimedStateManager(int cap, IEqualityComparer<Key> comparer) : base(cap, comparer) { }
    public TimedStateManager(Key defaultKey, TState defaultState) : base(10, null, defaultKey, defaultState) { }
    public TimedStateManager(IEqualityComparer<Key> comparer, Key defaultKey, TState defaultState) : base(10, comparer, defaultKey, defaultState) { }
    public TimedStateManager(int cap, IEqualityComparer<Key> comparer, Key defaultKey, TState defaultState) : base(cap, comparer, defaultKey, defaultState) { }

}
