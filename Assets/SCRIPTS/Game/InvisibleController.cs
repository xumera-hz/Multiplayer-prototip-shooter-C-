using System.Collections.Generic;
using UnityEngine;

public class InvisibleController : MonoBehaviour
{
    [SerializeField]
    GrassController m_GrassControl;

    [SerializeField] float m_RadiusInvisible = 5f;

    UnitContainer m_Target;

    List<UnitContainer> m_Targets = new List<UnitContainer>(10);

    public void SetMainTarget(GameObject target)
    {
        m_Target = target.GetComponent<UnitContainer>();
    }

    void RemoveTarget(int index)
    {
        if (index != -1)
        {
            m_Targets.RemoveAt(index);
        }
    }

    public void AddTarget(GameObject target)
    {
        if (target == null) return;
        var targ = target.GetComponent<UnitContainer>();
        if (targ.IsNullOrDestroy()) return;
        if (!m_Targets.Contains(targ))
        {
            m_Targets.Add(targ);
        }
    }

    public void RemoveTarget(GameObject target)
    {
        if (target == null) return;
        var targ = target.GetComponent<UnitContainer>();
        if (targ.IsNullOrDestroy()) return;
        RemoveTarget(m_Targets.IndexOf(targ));
    }

    bool InGrass(Transform target) { return m_GrassControl.InGrass(target); }

    void SetInvisible(UnitVisible elem)
    {
        elem.State = UnitVisible.TypeVisible.Invisible;
    }

    void SetFade(UnitVisible elem)
    {
        elem.State = UnitVisible.TypeVisible.Fade;
    }

    void SetVisible(UnitVisible elem)
    {
        elem.State = UnitVisible.TypeVisible.Visible;
    }

    void CheckVisibleState()
    {
        if (m_Target.IsNullOrDestroy()) return;
        float sqrInvis = m_RadiusInvisible * m_RadiusInvisible;
        var targetPos = m_Target.TF.position;
        targetPos.y = 0f;

        //главная цель становится прозрачной как только попадает в траву
        if (InGrass(m_Target.TF)) SetFade(m_Target.VisibleControl);
        else SetVisible(m_Target.VisibleControl);

        for (int i = m_Targets.Count - 1; i >= 0; i--)
        {
            var elem = m_Targets[i];
            if (elem.IsNullOrDestroy())
            {
                RemoveTarget(i);
                continue;
            }
            bool inGrass = InGrass(elem.TF);
            if(!inGrass)
            {
                SetVisible(elem.VisibleControl);
                continue;
            }
            var pos = elem.TF.position;
            pos.y = 0f;
            float sqr = (targetPos - pos).sqrMagnitude;
            bool invis = sqr > sqrInvis;
            if(invis) SetInvisible(elem.VisibleControl);
            else SetFade(elem.VisibleControl);
        }
    }

    void Update ()
    {
        CheckVisibleState();
    }

    private void OnDestroy()
    {
        m_Target = null;
        m_Targets.Clear();
    }
}
