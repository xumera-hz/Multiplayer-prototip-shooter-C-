using UnityEngine;
using System;

public class AttackController : MonoBehaviour
{
    [SerializeField] AimController m_AimControl;

    const float ATTACK_MOVEDIR_SQR = 0.4f * 0.4f;
    const float AIM_MOVEDIR_SQR = 0.4f * 0.4f;

    public struct Args
    {
        public bool IsAttack;
        public bool ClickAttack;
        public Vector3 AttackDir;
    }

    public event Action<Args> AttackEvents;

    void CallEvent(Args args)
    {
        if (AttackEvents != null) AttackEvents(args);
    }

    Args m_DeferredArgs;
    bool m_DeferredEvent;

    void OnAttackTouchEvents(TouchPad.Args args)
    {
        if(args.IsStart)
        {
            m_BeginAttack = true;
        }
        else if (args.IsEnd)
        {
            var dir = args.EndMoveDir;
            bool clickAttack = dir.sqrMagnitude < ATTACK_MOVEDIR_SQR;
            //прицельный выстрел или быстрый тап
            bool attack = (!clickAttack && m_BeginAim) || (clickAttack && !m_BeginAim);
            SetDefault();
            m_DeferredEvent = true;
            m_DeferredArgs.ClickAttack = clickAttack;
            m_DeferredArgs.IsAttack = attack;
            if (!clickAttack)
            {
                m_DeferredArgs.AttackDir = RotateByCamera(dir);
            }
        }
    }
    bool m_BeginAttack;
    bool m_BeginAim;

    public void SetDefault()
    {
        m_BeginAttack = false;
        m_BeginAim = false;
        m_DeferredEvent = false;
        m_AimControl.Active(false);
    }

    TouchPad Joy { get { return JoysticksManager.AttackJoystick; } }

    public void Init(Transform target)
    {
        m_AimControl.SetTarget(target);
        Joy.TouchEvent -= OnAttackTouchEvents;
        Joy.TouchEvent += OnAttackTouchEvents;
    }

    protected Vector3 RotateByCamera(Vector2 move)
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

    public void ManualUpdate ()
    {
        if (m_DeferredEvent)
        {
            m_DeferredEvent = false;
            CallEvent(m_DeferredArgs);
        }
        if (!m_BeginAttack) return;
        var dir = Joy.MotionDir;
        bool aim = dir.sqrMagnitude >= AIM_MOVEDIR_SQR;
        if (aim) m_BeginAim = true;
        m_AimControl.Active(aim);

        if (aim)
        {
            m_AimControl.ManualUpdate(RotateByCamera(dir));
        }
    }
}
