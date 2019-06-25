using UnityEngine;
using System.Collections;

public class UnitVisible : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] TypeVisible m_TestType;
    [ContextMenu("TestVisible")]
    void TestVisible()
    {
        State = m_TestType;
    }
#endif

    UnitContainer m_Unit;

    [SerializeField] GameObject m_ImmortalView;
    Renderer[] m_Renders;
    TypeVisible m_State;

    public enum TypeVisible { Visible, Fade, Invisible, Immortal }

    public bool Invisible { get { return m_State == TypeVisible.Invisible; } }
    public bool Immortal { get { return m_State == TypeVisible.Immortal; } }

    public bool BlockInvisible { get; set; }

    public event System.Action<TypeVisible> ChangeVisibleState;

    public TypeVisible State
    {
        get { return m_State; }
        set
        {
            if (m_State == value) return;
            if (BlockInvisible && value == TypeVisible.Invisible) return;
            m_State = value;
            m_ImmortalView.SetActive(value == TypeVisible.Immortal);
            switch (value)
            {
                case TypeVisible.Visible: SetAlpha(1f); break;
                case TypeVisible.Fade: SetAlpha(0.5f); break;
                case TypeVisible.Invisible: SetAlpha(0f); break;
                case TypeVisible.Immortal: SetAlpha(1f); break;
            }
            if (ChangeVisibleState != null) ChangeVisibleState(m_State);
        }
    }

    public void SetDefault()
    {
        StopAllCoroutines();
        BlockInvisible = false;
        State = TypeVisible.Visible;
    }

    void SetAlpha(float value)
    {
        for (int i = 0; i < m_Renders.Length; i++)
        {
            var mat = m_Renders[i].material;
            if (mat.HasProperty("_Color"))
            {
                var color = mat.color;
                color.a = value;
                mat.color = color;
                m_Renders[i].material = mat;
            }
#if UNITY_EDITOR
            else Debug.LogError(mat.name +" У материала нет цвета");
#endif
        }
    }

    void ActiveUnitWeaponEffects(bool state)
    {
        var view = m_Unit.WeaponControl.Weapon.View;
        var settings = view.Settings;
        settings.Fire = state;
        view.Settings = settings;
    }

    //TODO: в другое место
    #region BlockInvisbleOnAttack

    void OnAttackEvent()
    {
        if (Invisible) State = TypeVisible.Visible;
        StartCoroutine(WaitBlockStateAfterAttack());
    }

    IEnumerator WaitBlockStateAfterAttack()
    {
        BlockInvisible = true;
        yield return new WaitForSeconds(1f);
        BlockInvisible = false;
    }

    #endregion

    void Awake ()
    {
        m_Unit = GetComponentInChildren<UnitContainer>();
        m_Renders = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        m_Unit.UnitControl.AttackEvent += OnAttackEvent;
        m_Unit.LifeControl.DeathEvent += (sender,args)=> { State = TypeVisible.Visible; BlockInvisible = true;  }; ;
        m_Unit.LifeControl.ResurrectionEvent += (sender, args) => { BlockInvisible = false; }; ;
    }

    private void OnDisable()
    {
        BlockInvisible = false;
    }
}
