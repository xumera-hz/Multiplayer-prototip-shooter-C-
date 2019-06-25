using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[Serializable]
public abstract class TouchControl
{
    public const int NULL_ID = -100;
    public abstract void Init();
    public abstract void EndTouches();
    public abstract void SetActive(bool state);
    public abstract bool IsActive { get; }
    public abstract bool IsTouch { get; }
    protected abstract void EndTouch(int id, Vector2 pos);
    protected abstract void StartTouch(int id, Vector2 pos);
    protected abstract void UpdateTouch(int id, Vector2 pos);
    protected abstract bool CanTouch(int id);
    public abstract bool Block { get; }
    [SerializeField]
    protected TypeTouch m_TypeTouch = new TypeTouch();
    [SerializeField]
    protected bool m_BlockOnPause = true;
    [Serializable]
    protected class TypeTouch
    {
        [SerializeField] int m_IDMouse = 1;

        public int IdMouse { get { return m_IDMouse; } }

        bool m_IsMouse =
#if UNITY_EDITOR || UNITY_STANDALONE
        true;
#else
        false;
#endif
        public bool IsMouse { get { return m_IsMouse; } }
    }
    
    public void ManualUpdate()
    {
        if (m_BlockOnPause && TimeManager.TimeScaleTime < 1e-5f)
        {
            EndTouches();
            return;
        }
        if (m_TypeTouch.IsMouse) MouseUpdate();
        else TouchUpdate();
    }

    protected virtual void TouchUpdate()
    {
        if (Block)
        {
            EndTouches();
            return;
        }
        var count = Input.touchCount;
        if (count == 0)
        {
            EndTouches();
            return;
        }
        for (int i = 0; i < count; i++)
        {
            var touch = Input.GetTouch(i);
            int id = touch.fingerId;
            var phase = touch.phase;
            if (CanTouch(id))
            {
                if (phase == TouchPhase.Canceled || phase == TouchPhase.Ended) EndTouch(id, touch.position);
                else if (phase == TouchPhase.Began) StartTouch(id, touch.position);
                else UpdateTouch(id, touch.position);
            }
        }
    }

    protected virtual void MouseUpdate()
    {
        int res = 0;
        int id = m_TypeTouch.IdMouse;
        if (!CanTouch(id))
        {
            EndTouches();
            return;
        }
        if (Input.GetMouseButtonDown(id))
        {
            res++;
            StartTouch(id, Input.mousePosition);
        }
        if (Input.GetMouseButtonUp(id))
        {
            res++;
            EndTouch(id, Input.mousePosition);
        }
        if (Input.GetMouseButton(id))
        {
            res++;
            UpdateTouch(id, Input.mousePosition);
        }
        if (res == 0) EndTouches();
    }
}

public abstract class WrapTouchControl : MonoBehaviour
{
    protected abstract TouchControl TouchControl { get; }

    void Update() { TouchControl.ManualUpdate(); }

    protected virtual void Start() { TouchControl.Init(); }

    void OnEnable() { TouchControl.EndTouches(); }
    void OnDisable() { TouchControl.EndTouches(); }

    public TouchControl GetTouchControl()
    {
        return TouchControl;
    }

    public T GetTouchControl<T>() where T : class
    {
        return TouchControl as T;
    }

    public void Active(bool state)
    {
        enabled = state;
    }
}

[Serializable]
public class TouchPad : TouchControl
{
    public const int START_CONST = 1;
    public const int UPDATE_CONST = 2;
    public const int END_CONST = 3;

    public struct Args
    {
        public Vector2 StartPoint;
        public Vector2 UpdatePoint;
        public Vector2 EndPoint;
        public Vector2 EndMoveDir;
        public int State;
        public bool IsStart { get { return State == START_CONST; } }
        public bool IsUpdate { get { return State == UPDATE_CONST; } }
        public bool IsEnd { get { return State == END_CONST; } }

        public int CheckRelativeSwipeLength(float dist)
        {
            // проверка - свайп если смещение больше dist, dist в см.
            float x = UpdatePoint.x;
            float y = UpdatePoint.y;
            //это обратная величина квадрата дюйма
            const float inverseSqrInch = 1f / (2.54f * 2.54f);//=0.155f
            if ((x * x + y * y) >= (dist * dist) * inverseSqrInch)
            {
                x = x < 0f ? -x : x;
                y = y < 0f ? -y : y;
                if (x >= y) return (int)((UpdatePoint.x > 0) ? SWIPE_DIRECTION.RIGHT : SWIPE_DIRECTION.LEFT);
                else return (int)((UpdatePoint.y > 0) ? SWIPE_DIRECTION.UP : SWIPE_DIRECTION.DOWN);
            }
            return (int)SWIPE_DIRECTION.NONE;
        }
    }

    #region Misc

    public enum AxisOption
    {
        Both, // Use both
        OnlyHorizontal, // Only horizontal
        OnlyVertical // Only vertical
    }
    public enum ControlStyle
    {
        Absolute, // operates from teh center of the image
        Relative, // operates from the center of the initial touch
        Swipe, // swipe to touch touch no maintained center
    }

    public enum SWIPE_DIRECTION : int { NONE = 0, RIGHT = 1, LEFT = 2, UP = 3, DOWN = 4 }

    #endregion

    #region Data

    public event Action<Args> TouchEvent, TouchUpdateEvent;
    //Args args = new Args();
    //Костылек
    public Vector2 Resolution { get { return m_Scaler.referenceResolution; } }
    [SerializeField] TouchArea m_DefaultArea = null;
    [SerializeField] CanvasScaler m_Scaler;

    [SerializeField] AxisOption m_AxesToUse = AxisOption.Both; // The options for the axes that the still will use
    [SerializeField] ControlStyle m_ControlStyle = ControlStyle.Absolute; // control style to use
    [SerializeField] float m_XSensitivity = 1f, m_YSensitivity = 1f;
    [SerializeField] GameObject m_MainObject;
    bool m_UseX, m_UseY;
    int m_Id = NULL_ID;
    protected Vector2 m_MotionDir;
    protected Vector2 m_DefPos, m_LastPos;
    TouchArea m_CurrentArea;
    bool m_Stop;

    #endregion

    #region Misc(DPI, RATIO)

    float m_DPI;
    Vector2 m_ScaleRatio;

    void OnChangeScreenDPI()
    {
        m_DPI = Screen.dpi;
        if (m_DPI < 1f)
        {
            m_DPI = m_Scaler.fallbackScreenDPI;
            if (m_DPI < 1f) m_DPI = 1f;
        }
        OnChangeScaleRatio();
    }

    void OnChangeScaleRatio()
    {
        m_ScaleRatio.x = m_DPI / m_XSensitivity;
        m_ScaleRatio.y = m_DPI / m_XSensitivity;
    }

    #endregion

    #region Private

    #region Base

    protected override bool CanTouch(int id)
    {
        return m_Id == NULL_ID || m_Id == id;
    }

    protected override void EndTouch(int id, Vector2 pos)
    {
        if (m_Id != id) return;
        m_LastPos = pos;
        EndTouches();
    }

    protected virtual void InnerStartTouch(int id, Vector2 pos) { }
    protected virtual void InnerEndTouch(Vector2 pos) { }

    protected override void StartTouch(int id, Vector2 pos)
    {
        if (m_Id == id) return;
        if (!Contains(pos)) return;
        m_Id = id;
        if (m_ControlStyle == ControlStyle.Relative) m_DefPos = pos;
        else if (m_ControlStyle == ControlStyle.Swipe) m_DefPos = pos;
        InnerStartTouch(id, pos);
        Args args = new Args() { State = START_CONST, StartPoint = pos };
        CallEvent(args);
    }

    protected override void UpdateTouch(int id, Vector2 pos)
    {
        if (m_Id != id) return;
        m_LastPos = pos;
        InnerUpdateTouch();
#if UNITY_EDITOR
        //Debug.LogError("m_MotionPoint.x=" + m_MotionDir.x);
        //Debug.LogError("m_MotionPoint.y=" + m_MotionDir.y);
#endif
        if (m_ControlStyle == ControlStyle.Swipe) m_DefPos = m_LastPos;
        Args args = new Args() { State = UPDATE_CONST, UpdatePoint = pos };
        CallEvent(args, true);
    }

    protected virtual void InnerUpdateTouch()
    {
        m_MotionDir.x = m_UseX ? (m_LastPos.x - m_DefPos.x) / (m_ScaleRatio.x) : 0f;
        m_MotionDir.y = m_UseY ? (m_LastPos.y - m_DefPos.y) / (m_ScaleRatio.y) : 0f;
    }

    #endregion

    bool Contains(Vector3 pos)
    {
        return m_CurrentArea.Contain(pos);// RectTransformUtility.RectangleContainsScreenPoint(m_CurrentArea, pos, GameMenu.UICamera);
    }

    #endregion

    #region Public

    public override bool IsTouch { get { return m_Id != NULL_ID; } }
    public Vector2 PrevPoint { get { return m_DefPos; } }

    public void ChangeTouchArea(TouchArea area)
    {
#if UNITY_EDITOR
        if(area == null) Debug.LogWarning("TouchArea is null");
#endif
        if (m_CurrentArea != null) m_CurrentArea.Active(false);
        m_CurrentArea = area == null ? m_DefaultArea : area;
        if (m_CurrentArea != null) m_CurrentArea.Active(true);
    }
    public Vector2 MotionDir { get { return m_MotionDir; } set { m_MotionDir = value; } }
    public float SensitivityX { get { return m_XSensitivity; } set { m_XSensitivity = Mathf.Abs(value == 0f ? 1f : value); OnChangeScaleRatio(); } }
    public float SensitivityY { get { return m_YSensitivity; } set { m_YSensitivity = Mathf.Abs(value == 0f ? 1f : value); OnChangeScaleRatio(); } }
    public AxisOption Axis
    {
        get { return m_AxesToUse; }
        set
        {
            m_AxesToUse = value;
            m_UseX = (m_AxesToUse == AxisOption.Both || m_AxesToUse == AxisOption.OnlyHorizontal);
            m_UseY = (m_AxesToUse == AxisOption.Both || m_AxesToUse == AxisOption.OnlyVertical);
        }
    }

    public void ChangeControlStyle(ControlStyle style)
    {
        if (style == m_ControlStyle) return;
        EndTouches();
        if (style == ControlStyle.Absolute) m_DefPos = m_CurrentArea.position;
        m_ControlStyle = style;
    }

    void CallEvent(Args args, bool update = false)
    {
        if (update)
        {
            if (TouchUpdateEvent != null) TouchUpdateEvent(args);
        }
        else
        {
            if (TouchEvent != null) TouchEvent(args);
        }
    }

    public override bool Block
    {
        get
        {
            return
                m_Stop
                || TimeManager.TimeScaleTime <= 1e-02f //костылек
                ;
        }
    }

    public override void EndTouches()
    {
        if (m_Id != NULL_ID)
        {
            m_Id = NULL_ID;
            var dir = m_MotionDir;
            m_MotionDir.x = m_MotionDir.y = 0f;
            InnerEndTouch(m_LastPos);
            Args args = new Args() { State = END_CONST, EndPoint = m_LastPos, EndMoveDir = dir };
            CallEvent(args);
        }
    }

    public override void Init()
    {
        Axis = m_AxesToUse;
        OnChangeScreenDPI();
        ChangeTouchArea(m_DefaultArea);
    }

    public bool Stop { get { return m_Stop; } set { m_Stop = value; if (value) EndTouches(); } }

    public override void SetActive(bool state)
    {
        if (m_MainObject != null) m_MainObject.SetActive(state);
    }

    public override bool IsActive
    {
        get { return m_MainObject != null && m_MainObject.activeSelf && m_MainObject.activeInHierarchy; }
    }

    #endregion
}

public class TouchPadControl : WrapTouchControl
{
    protected override TouchControl TouchControl
    {
        get { return m_TouchControl; }
    }
    #region Data
    [SerializeField]
    protected TouchPad m_TouchControl = new TouchPad();

    //[SerializeField] CanvasScaler ThisCanvas;
    [SerializeField] List<TouchArea> m_Areas = null;

    #endregion

}