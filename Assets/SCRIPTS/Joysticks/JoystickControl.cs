using UnityEngine;
using System;

public class JoystickControl : WrapTouchControl
{
    protected override TouchControl TouchControl
    {
        get { return m_TouchControl; }
    }

    #region Data
    [SerializeField] protected MobileJoystick m_TouchControl = new MobileJoystick();

    #endregion

}
[Serializable]
public class MobileJoystick : TouchPad
{

    #region Data
    
    [SerializeField] TouchArea m_RectInnerStick = null, m_RectOuterStick = null, m_TouchArea = null;
    [SerializeField] TypeJoystick TJ;
    float clampRadius, invClampRadius;
    Vector3 m_DefaultPos;

    #endregion

    #region Public

    public TypeJoystick SetTJ
    {
        set
        {
            TJ = value;
            EndTouches();
            EndTouch();
            if (TJ == TypeJoystick.Static)
            {
                ShowHideImages(true);
                ChangeTouchArea(m_RectOuterStick);
            }
            else if (TJ == TypeJoystick.StaticDynamicStart)
            {
                ShowHideImages(true);
                ChangeTouchArea(m_TouchArea);
            }
            else
            {
                ChangeTouchArea(m_TouchArea);
            }
        }
    }

    public void Show()
    {
        SetActive(true);
    }
    public void Hide()
    {
        SetActive(false);
    }

    public void ShowHideImages(bool show)
    {
        m_RectInnerStick.ActiveImage = show;
        m_RectOuterStick.ActiveImage = show;
        //m_RectInnerStick.TargetImage.enabled = show;
        //m_RectOuterStick.TargetImage.enabled = show;
    }

    #endregion

    #region Private

    protected override void InnerEndTouch(Vector2 pos)
    {
        EndTouch();
    }

    protected override void InnerStartTouch(int id, Vector2 pos)
    {
        StartTouch(pos);
    }

    void EndTouch()
    {
        m_RectOuterStick.localPosition = m_DefaultPos;
        m_RectInnerStick.localPosition = m_DefaultPos;
        if (TJ == TypeJoystick.Dynamic) ShowHideImages(false);
    }

    void StartTouch(Vector2 pos)
    {
        Vector3 pos2 = new Vector3(pos.x, pos.y, 0f);
        if (TJ == TypeJoystick.Dynamic)
        {
            ShowHideImages(true);
            m_RectOuterStick.position = pos2;
        }
        else if (TJ == TypeJoystick.StaticDynamicStart)
        {
            m_RectOuterStick.position = pos2;
        }
        m_RectInnerStick.position = pos2;
    }

    protected override void InnerUpdateTouch()
    {
        Vector3 tmpPos = m_RectOuterStick.position;
        Vector2 rectPos = new Vector3(tmpPos.x, tmpPos.y);
        Vector2 newPos = m_LastPos;
        Vector2 dir = newPos;
        dir.x -= rectPos.x;
        dir.y -= rectPos.y;
        if ((dir.x * dir.x + dir.y * dir.y) > clampRadius * clampRadius)
        {
            dir.Normalize();
            m_MotionDir = dir;
            newPos.x = dir.x * clampRadius + rectPos.x;
            newPos.y = dir.y * clampRadius + rectPos.y;
        }
        else
        {
            m_MotionDir.x = dir.x * invClampRadius;
            m_MotionDir.y = dir.y * invClampRadius;
        }
        m_RectInnerStick.position = new Vector3(newPos.x, newPos.y, 0f);
    }

    public override void Init()
    {
        m_DefaultPos = m_RectInnerStick.localPosition;
        base.Init();
        if (m_RectInnerStick == null || m_RectOuterStick == null) Debug.LogError("MobileJoystick error");
        else
        {
            //clampRadius = ((m_RectOuterStick.localPosition - m_RectInnerStick.localPosition).magnitude) * (Screen.width / Resolution.x);
            clampRadius = ((m_RectOuterStick.rect.position - m_RectInnerStick.rect.position).magnitude) * (Screen.width / Resolution.x);
            if (clampRadius > 0.01f) invClampRadius = 1f / clampRadius; else invClampRadius = 100f;
            SetTJ = TJ;
        }
    }

    #endregion

    #region Enums

    public enum TypeJoystick
    {
        Dynamic,
        Static,
        StaticDynamicStart
    }

    #endregion
}