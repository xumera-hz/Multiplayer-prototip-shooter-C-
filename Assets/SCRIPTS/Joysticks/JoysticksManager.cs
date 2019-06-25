using UnityEngine;
using System;

public class JoysticksManager : MonoBehaviour
{
    public static TouchPad MoveJoystick { get; private set; }
    public static TouchPad AttackJoystick { get; private set; }

    [SerializeField] WrapTouchControl m_CharacterMoveJoy;
    [SerializeField] WrapTouchControl m_AttackJoy;


    void Awake()
    {
        MoveJoystick = m_CharacterMoveJoy.GetTouchControl<TouchPad>();
        AttackJoystick = m_AttackJoy.GetTouchControl<TouchPad>();
    }

    void OnDestroy()
    {
        MoveJoystick = null;
        AttackJoystick = null;
    }

}
