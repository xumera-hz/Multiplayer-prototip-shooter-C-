using UnityEngine;
using System;


public enum TypeEnterTriggerZone
{
    None, Unit, Unique
}

public interface IContactEnter
{
    event Action Enter;
    void Active(bool state);
}

public class TriggerZone : TriggerElement, IContactEnter
{
    #region Data

    const string View = "View";
    [SerializeField] TypeEnterTriggerZone m_TypeTriggerZone;
    [Tooltip("Нужно ли искать обьект View, отвечающий за визуализацию")]
    [SerializeField] bool m_IsVisual = true;
    [Tooltip("Вырубает объект с именем View в детях, когда зашел в зону")]
    [SerializeField] bool m_OffVisualOnEnter = true;
    Predicate<Collider> m_ConditionsUnique;
    Transform m_VisualAnchor;
    [SerializeField] bool m_CallEvent = true;
    public bool StopCallEvent { set { m_CallEvent = !value; } get { return !m_CallEvent; } }

    #endregion

    #region Public

    //public event Action Enter, Exit, WrongEnter;
    public event Action<Collider> EnterSender;

    protected override void OnSetDefault()
    {
        //Enter = Exit = WrongEnter = null;
        EnterSender = null;
        m_ConditionsUnique = null;
        m_CallEvent = true;
        VisualActiveOnEnterOrExit(true);
    }

    public void SetUniqueCondition(Predicate<Collider> cond)
    {
        m_ConditionsUnique = cond;
    }

    public void SetVisualSettings(bool isVisual, bool offVisualOnEnter)
    {
        m_IsVisual = isVisual;
        m_OffVisualOnEnter = offVisualOnEnter;
    }

    public Transform VisualAnchor {
        get
        {
            if (m_VisualAnchor == null) InitAnchor();
            return m_VisualAnchor;
        }
    }

    public TypeEnterTriggerZone TypeTriggerZone
    {
        get { return m_TypeTriggerZone; }
        set
        {
            m_TypeTriggerZone = value;
            if (m_TypeTriggerZone == TypeEnterTriggerZone.None) m_Conditions = null;
            if (m_TypeTriggerZone == TypeEnterTriggerZone.Unique) m_Conditions = ManualConditionsUnique;
            else m_Conditions = GetConditionByType(m_TypeTriggerZone);
            //ChangeTypeTriggerZone(value);
        }
    }

    //protected virtual void ChangeTypeTriggerZone(TypeEnterTriggerZone type) { }

    public void Active(bool active)
    {
        if (gameObject.activeSelf != active) gameObject.SetActive(active);
    }

    protected override void OnEnterToTrigger(Collider sender, bool condition)
    {
        base.OnEnterToTrigger(sender, condition);
        if (condition)
        {
            VisualActiveOnEnterOrExit(false);
            if (EnterSender != null) EnterSender(sender);
        }
    }

    protected override void OnExitFromTrigger(Collider sender, bool condition)
    {
        base.OnExitFromTrigger(sender, condition);
        if(condition) VisualActiveOnEnterOrExit(true);
    }

    #endregion

    #region MonoBehaviour

    protected override void Disable()
    {
        base.Disable();
        VisualActiveOnEnterOrExit(true);
    }

    protected override void OnAwake()
    {
        InitAnchor();
    }

    void Start()
    {
        TypeTriggerZone = m_TypeTriggerZone;
#if UNITY_EDITOR
        if (GetComponent<Collider>() == null) Debug.LogError(GetType() + " error: " + typeof(Collider) + " is null");
#endif
    }

    #endregion

    #region Private

    void InitAnchor()
    {
        if (m_IsVisual)
        {
            m_VisualAnchor = transform.FindTF(View, true);
            if (m_IsVisual && m_VisualAnchor == null)
            {
                m_IsVisual = false;
#if UNITY_EDITOR
                Debug.LogWarning(GetType() + " error: " + View + " gameObject maybe not exist");
#endif
            }
        }
    }

    protected void VisualActiveOnEnterOrExit(bool state)
    {
        if (!m_IsVisual || !m_OffVisualOnEnter) return;
        var anchor = VisualAnchor;
        if (anchor != null)
        {
            var go = anchor.gameObject;
            if (go != null && go.activeSelf != state) go.SetActive(state);
        }
    }

    //protected override void EnterToTrigger(GameObject sender) { UseEnter(sender); }

    //protected override void ExitFromTrigger() { UseExit(); }

    //protected override void WrongEnterFromTrigger(GameObject sender)
    //{
    //    if (WrongEnter != null) WrongEnter();
    //    if (WrongEnterSender != null) WrongEnterSender(sender);
    //}

    #endregion

    #region Conditions

    bool ManualConditionsUnique(Collider sender)
    {
        return m_ConditionsUnique == null || m_ConditionsUnique(sender);
    }

    public static Predicate<Collider> GetConditionByType(TypeEnterTriggerZone type)
    {
        switch (type)
        {
            case TypeEnterTriggerZone.Unit: return ConditionsPredicates.ConditionsEnterPlayer;
        }
        return null;
    }

    #endregion
}
