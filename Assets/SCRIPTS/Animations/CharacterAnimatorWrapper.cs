using UnityEngine;

public class CharAnimHashes
{
    public readonly static int Idle, Move, Attack, Death;
    public readonly static int X, Y, Z;

    struct ActionNode
    {
        public int Hash;
        public string Name;
    }

    readonly static ActionNode[,] AnyActions;

    static int StringToHash(string str)
    {
        return Animator.StringToHash(str);
    }

    static CharAnimHashes()
    {
        Idle = StringToHash("Idle");
        Move = StringToHash("Move");
        Attack = StringToHash("Attack");
        Death = StringToHash("Death");

        AnyActions = new ActionNode[2, 3]
        {
            {
                new ActionNode()
                {
                    Hash = StringToHash("Down.AnyAction"),
                    Name = "ActionDown1"
                },
                new ActionNode()
                {
                    Hash = StringToHash("Down.SecondAnyAction"),
                    Name = "ActionDown2"
                },
                new ActionNode()
                {
                    Hash = StringToHash("Down.ThirdAnyAction"),
                    Name = "ActionDown3"
                }
            },
            {
                new ActionNode()
                {
                    Hash = StringToHash("Up.AnyAction"),
                    Name = "ActionUp1"
                },
                new ActionNode()
                {
                    Hash = StringToHash("Up.SecondAnyAction"),
                    Name = "ActionUp2"
                },
                new ActionNode()
                {
                    Hash = StringToHash("Up.ThirdAnyAction"),
                    Name = "ActionUp3"
                }
            }
        };
    }

    public static int GetActionHashByLayer(int layer, int num)
    {
        return AnyActions[layer, num].Hash;
    }

    public static string GetNameActionByHash(int layer, int num)
    {
        return AnyActions[layer, num].Name;
    }
}

public class CharacterAnimatorWrapper : AnimatorWrapper {

    byte[,] m_FreeAction;

    string GetFreeActionName(int layer = 0)
    {
        if (m_FreeAction[layer, 0] != 0) return CharAnimHashes.GetNameActionByHash(layer,0);
        if (m_FreeAction[layer, 1] != 0) return CharAnimHashes.GetNameActionByHash(layer, 1);
        if (m_FreeAction[layer, 2] != 0) return CharAnimHashes.GetNameActionByHash(layer, 2);
#if UNITY_EDITOR
        Debug.LogError(GetType() + " GetFreeActionName все очень плохо( нет свободных хэшей анимаций )");
#endif
        return string.Empty;
    }

    int GetFreeAction(int layer = 0)
    {
        if (m_FreeAction[layer, 0] != 0) return CharAnimHashes.GetActionHashByLayer(layer, 0);
        if (m_FreeAction[layer, 1] != 0) return CharAnimHashes.GetActionHashByLayer(layer, 1);
        if (m_FreeAction[layer, 2] != 0) return CharAnimHashes.GetActionHashByLayer(layer, 2);
#if UNITY_EDITOR
        Debug.LogError(GetType() + " GetFreeAction все очень плохо( нет свободных хэшей анимаций )");
#endif
        return 0;
    }

    public void SetFreeAction(int hash, byte state, int layer = 0)
    {
        if (hash == CharAnimHashes.GetActionHashByLayer(layer, 0)) m_FreeAction[layer,0] = state;
        else if (hash == CharAnimHashes.GetActionHashByLayer(layer, 1)) m_FreeAction[layer,1] = state;
        else if (hash == CharAnimHashes.GetActionHashByLayer(layer, 2)) m_FreeAction[layer,2] = state;
#if UNITY_EDITOR
        //else Debug.LogWarning(GetType() + " GetFreeAction все очень плохо( нет такого хэша " + hash + " )");
#endif
    }

    void DD()
    {
        #region TimeKostil
#if UNITY_EDITOR
        // Debug.LogError("CheckFallState");
#endif
        //костыль дефает проблему, когда перс запустил другую анимацию отличной от нужной в заданном состоянии
        //Например GameObject выкл/вкл, все данные сбросятся
        var state = Anim.GetCurrentAnimatorStateInfo(0);
        var nextState = Anim.GetNextAnimatorStateInfo(0);
        int cur = state.shortNameHash;
        int next = nextState.shortNameHash;
        bool res = false;
        //for (int i = 0; i < CharAnimHashes.FallStates.Length; i++)
        //{
        //    if ((CharacterFallState)i == CharacterFallState.None) continue;
        //    if (cur != CharAnimHashes.FallStates[i] || (next != 0 && next != CharAnimHashes.FallStates[i]))
        //    {
        //        res = true;
        //        break;
        //    }
        //}
        if (!res)
        {
#if UNITY_EDITOR
            Debug.Log("TODO: Kostil srabotal");
#endif
            //ChangeState();
        }
        #endregion
    }

    protected override void Init()
    {
        m_FreeAction = new byte[m_Anim.layerCount, 3];
        for (int i = 0; i < m_FreeAction.GetLength(0); i++)
        {
            m_FreeAction[i, 0] = 1;
            m_FreeAction[i, 1] = 1;
            m_FreeAction[i, 2] = 1;
        }
    }

    protected override void OnAnimationExit(int ID, AnimatorStateInfo state, int layer)
    {
        if (layer < 0) layer = 0;
        //if (!this.IsPlayer()) Debug.LogError("OnAnimationExit=" + state.fullPathHash);
        SetFreeAction(state.fullPathHash, 1, layer);
    }

    protected override void OnAnimationStart(int ID, AnimatorStateInfo state, int layer)
    {
        if (layer < 0) layer = 0;
        //if (!this.IsPlayer()) Debug.LogError("OnAnimationStart="+ state.fullPathHash);
        SetFreeAction(state.fullPathHash, 0, layer);
    }

    public void AnyCrossFadeInFixedTime(float TransDurat, int _layer = -1, float fixedTime = 0f)
    {
        if (_layer < 0) _layer = 0;
        int hash = GetFreeAction(_layer);
        //if (!this.IsPlayer()) Debug.LogError("AnyCrossFadeInFixedTime=" + _layer +" Hash="+ hash+ " frame=" +Time.frameCount);
        SetAnimationData(new AnimationData(hash, TransDurat, _layer, fixedTime, true));
    }

    public void AddOverrideAnyAnim(AnimationClip clip, int layer = 0)
    {
        string name = GetFreeActionName(layer);
        //if (!this.IsPlayer()) Debug.LogError("AddOverrideAnyAnim=" + layer + " Name=" + name + " frame=" + Time.frameCount);
        AddOverrideAnim(name, clip);
    }
}
