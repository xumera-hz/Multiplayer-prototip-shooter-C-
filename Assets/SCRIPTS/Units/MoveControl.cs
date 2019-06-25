using UnityEngine;

public class MoveControl : MonoBehaviour {

    [SerializeField] protected Vector3 m_Speed = Vector3.one * 5f;
    [SerializeField] protected Vector3 m_RotateSpeed = Vector3.one * 5f;
    protected Transform m_TF;
    Vector3 m_Pos;
    Vector3 m_LocalPos;
    Quaternion m_Rot;
    Quaternion m_LocalRot;

    Space m_PositionSpace;
    Space m_RotationSpace;

    protected bool m_DirtyPos;
    protected bool m_DirtyRot;
    bool m_BlockPos;
    bool m_BlockRot;

    protected virtual void Awake()
    {
        m_TF = transform;
    }

    public Vector3 Speed { get { return m_Speed; } set { m_Speed = value; } }
    public Vector3 RotateSpeed { get { return m_RotateSpeed; } set { m_RotateSpeed = value; } }
    public bool BlockPos { get { return m_BlockPos; } set { m_BlockPos = value; } }
    public bool BlockRot { get { return m_BlockRot; } set { m_BlockRot = value; } }

    public void Apply()
    {
        ApplyRotation();
        ApplyPosition();
    }

    public void ApplyPosition()
    {
        if (m_BlockPos)
        {
            if (m_PositionSpace == Space.World) m_Pos = m_TF.position;
            else if (m_PositionSpace == Space.Self) m_LocalPos = m_TF.localPosition;
            m_DirtyPos = false;
            return;
        }
        if (!m_DirtyPos) return;
        if (m_PositionSpace == Space.World) m_TF.position = GetPosition();
        else if (m_PositionSpace == Space.Self) m_TF.localPosition = GetLocalPosition();
        m_DirtyPos = false;
    }

    public void ApplyRotation()
    {
        if (m_BlockRot)
        {
            if (m_RotationSpace == Space.World) m_Rot = m_TF.rotation;
            else if (m_RotationSpace == Space.Self) m_LocalRot = m_TF.localRotation;
            m_DirtyRot = false;
            return;
        }
        if (!m_DirtyRot) return;
        if (m_RotationSpace == Space.World) m_TF.rotation = GetRotation();
        else if (m_RotationSpace == Space.Self) m_TF.localRotation = GetLocalRotation();
        m_DirtyRot = false;
    }

    #region Rotation

    public Vector3 forward
    {
        get { return GetRotation() * Vector3.forward; }
        set { if (value.sqrMagnitude > 1e-5f) SetRotation(Quaternion.LookRotation(value)); }
    }

    public Quaternion rotation
    {
        get { return GetRotation(); }
        set { SetRotation(value); }
    }

    public Quaternion localRotation
    {
        get { return GetLocalRotation(); }
        set { SetLocalRotation(value); }
    }

    public Quaternion GetRotation()
    {
        if (m_RotationSpace == Space.Self && m_DirtyRot)
        {
            m_DirtyRot = false;
            m_Rot = m_TF.TransformRotation(m_LocalRot);
        }
        return m_Rot;
    }
    public Quaternion GetLocalRotation()
    {
        if (m_RotationSpace == Space.World && m_DirtyRot)
        {
            m_DirtyRot = false;
            m_LocalRot = m_TF.InverseTransformRotation(m_Rot);
        }
        return m_LocalRot;
    }
    public void SetRotation(Quaternion rot)
    {
        if (m_BlockRot) return;
        m_RotationSpace = Space.World;
        m_Rot = rot;
        m_DirtyRot = true;
    }
    public void SetLocalRotation(Quaternion rot)
    {
        if (m_BlockRot) return;
        m_RotationSpace = Space.Self;
        m_LocalRot = rot;
        m_DirtyRot = true;
    }

    public void Rotate(Vector3 euler, float deltaTime)
    {
        Quaternion rot = GetRotation();
        euler.x = euler.x + (m_RotateSpeed.x * deltaTime);
        euler.y = euler.y + (m_RotateSpeed.y * deltaTime);
        euler.z = euler.z + (m_RotateSpeed.z * deltaTime);
        rot = rot * Quaternion.Inverse(rot) * Quaternion.Euler(euler) * rot;
        SetRotation(rot);
    }

    public void LocalRotate(Vector3 euler, float deltaTime)
    {
        Quaternion rot = GetLocalRotation();
        euler.x = euler.x + (m_RotateSpeed.x * deltaTime);
        euler.y = euler.y + (m_RotateSpeed.y * deltaTime);
        euler.z = euler.z + (m_RotateSpeed.z * deltaTime);
        SetLocalRotation(rot * Quaternion.Euler(euler));
    }

    #endregion

    #region Position

    public Vector3 position
    {
        get { return GetPosition(); }
        set { SetPosition(value); }
    }

    public Vector3 localPosition
    {
        get { return GetLocalPosition(); }
        set { SetLocalPosition(value); }
    }

    public Vector3 GetPosition()
    {
        if (m_PositionSpace == Space.Self && m_DirtyPos)
        {
            m_DirtyPos = false;
            //if (m_DirtyRot)
            //{
            //    if (m_RotationSpace == Space.Self) m_TF.localRotation = GetLocalRotation();
            //    else if (m_RotationSpace == Space.World) m_TF.rotation = GetRotation();
            //}
            m_Pos = m_TF.TransformPoint(m_LocalPos);
        }
        return m_Pos;
    }
    public Vector3 GetLocalPosition()
    {
        if (m_PositionSpace == Space.World && m_DirtyPos)
        {
            m_DirtyPos = false;
            m_LocalPos = m_TF.InverseTransformPoint(m_Pos);
        }
        return m_LocalPos;
    }
    public void SetPosition(Vector3 pos)
    {
        if (m_BlockPos) return;
        m_PositionSpace = Space.World;
        m_Pos = pos;
        m_DirtyPos = true;
    }
    public void SetLocalPosition(Vector3 pos)
    {
        if (m_BlockPos) return;
        m_PositionSpace = Space.Self;
        m_LocalPos = pos;
        m_DirtyPos = true;
    }

    public virtual void Move(Vector3 dir, float deltaTime)
    {
        Vector3 pos = GetPosition();
        pos.x = pos.x + dir.x * m_Speed.x * deltaTime;
        pos.y = pos.y + dir.y * m_Speed.y * deltaTime;
        pos.z = pos.z + dir.z * m_Speed.z * deltaTime;
        SetPosition(pos);
    }

    public virtual void LocalMove(Vector3 dir, float deltaTime)
    {
        Vector3 pos = GetLocalPosition();
        pos.x = pos.x + dir.x * m_Speed.x * deltaTime;
        pos.y = pos.y + dir.y * m_Speed.y * deltaTime;
        pos.z = pos.z + dir.z * m_Speed.z * deltaTime;
        SetLocalPosition(pos);
    }

    #endregion
}