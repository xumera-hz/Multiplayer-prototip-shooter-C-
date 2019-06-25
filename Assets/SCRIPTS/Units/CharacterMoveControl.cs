using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMoveControl : MoveControl
{
    CharacterController m_CharacterControl;

    protected override void Awake()
    {
        base.Awake();
        m_CharacterControl = GetComponent<CharacterController>();
    }

    public override void Move(Vector3 dir, float deltaTime)
    {
        dir.Normalize();
        dir.x = dir.x * m_Speed.x * deltaTime;
        dir.y = dir.y * m_Speed.y * deltaTime;
        dir.z = dir.z * m_Speed.z * deltaTime;
        m_CharacterControl.Move(dir);
        SetPosition(m_TF.position);
        m_DirtyPos = false;
    }

    public override void LocalMove(Vector3 dir, float deltaTime)
    {
        dir = m_TF.TransformDirection(dir);
        Move(dir, deltaTime);
    }
}
