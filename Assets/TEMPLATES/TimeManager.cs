using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public const int FixFPS = 30;
    public const bool UseFixFPS = true;
    public static float TimeScaleTime;
    public static float TimeDeltaTime;
    public static float UnscaledDeltaTime;
    public static float TimeFixedDeltaTime;
    public static float UnscaleFixedDeltaTime;
    public static float InverseTimeDeltaTime;
    public static float InverseFixedTimeDeltaTime;
    public static int Frame;
    public static float RealtimeSinceStartup;

    //--------------------------------------------
    public static float TimeUnscaleTime;
    public static float TimeTime;
    //--------------------------------------------
    public static int FPS;
    static float m_TimeCheckFPS = 0.5f;
    static float m_LastTimeScale=1f;
    const float SO_CLOSE_ZERO = 1e-05f;

    int m_Frames = 0;
    float m_TimeLeftForCheckFPS;

    static TimeManager m_I;

    public static void InstanceInit()
    {
        if (m_I != null) return;
        new GameObject(typeof(TimeManager).ToString());
    }

    void Awake()
    {
        if (m_I != null)
        {
            Destroy(gameObject);
            return;
        }
        m_I = this;
        if (transform.root == null) DontDestroyOnLoad(gameObject);
        Init();
    }

    void Init()
    {
        if(UseFixFPS) Application.targetFrameRate = FixFPS;
        Time.timeScale = 1f;
        TimesUpdate();
    }

    void TimesUpdate()
    {
        Frame = Time.frameCount;
        RealtimeSinceStartup = Time.realtimeSinceStartup;
        TimeScaleTime = Time.timeScale;
        TimeDeltaTime = Time.deltaTime;
        m_TimeLeftForCheckFPS = m_TimeCheckFPS;
        TimeScaleTime = Time.timeScale;
        UnscaledDeltaTime = Time.unscaledDeltaTime;
        TimeTime = Time.time;
        TimeUnscaleTime = Time.unscaledTime;
        TimeFixedDeltaTime = Time.fixedDeltaTime;
        UnscaleFixedDeltaTime = Time.fixedUnscaledDeltaTime;
        InverseTimeDeltaTime = TimeDeltaTime > SO_CLOSE_ZERO ? (1f / TimeDeltaTime) : 0f;
        InverseFixedTimeDeltaTime = TimeFixedDeltaTime > SO_CLOSE_ZERO ? (1f / TimeFixedDeltaTime) : 0f;
    }


    //Использовать с осторожностью, а то получишь паузу
    public static void SetScaleTime(float scale)
    {
        Time.timeScale = scale;
        TimeScaleTime = Time.timeScale;
    }

    public static void SetTimeToCheckFPS(float value)
    {
        m_TimeCheckFPS = value <= SO_CLOSE_ZERO ? SO_CLOSE_ZERO : value;
    }

    public static void Pause(bool state)
    {
        m_LastTimeScale = state ? TimeScaleTime : m_LastTimeScale;
        Time.timeScale = state ? 0f : m_LastTimeScale;
        TimeScaleTime = Time.timeScale;
    }

    void Update () {
        TimesUpdate();

        m_TimeLeftForCheckFPS -= UnscaledDeltaTime;
        m_Frames++;

        if (m_TimeLeftForCheckFPS <= 0f)
        {
            FPS = (int)(m_Frames / m_TimeCheckFPS);
            m_TimeLeftForCheckFPS = m_TimeCheckFPS;
            m_Frames = 0;
        }
    }
}
