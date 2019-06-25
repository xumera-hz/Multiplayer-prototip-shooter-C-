using System.Collections.Generic;
using UnityEngine;
using System;

public class GrassController : MonoSingleton<GrassController> {

    [SerializeField] GameObject m_GamePlace;
    [SerializeField] float m_RadiusFade = 5f;
    List<Transform> m_Targets = new List<Transform>(10);
    List<TargetInGrassControl> m_TargetsInGrass = new List<TargetInGrassControl>(10);
    Grass[] m_GrassElems;

    //public static event Action<Transform> TargetInGrass;

    public static bool Static_InGrass(Transform target)
    {
        return Can && m_I.InGrass(target);
    }

    public bool InGrass(Transform target)
    {
        if (target.IsNullOrDestroy()) return false;
        int ind = m_Targets.IndexOf(target);
        if (ind == -1) return false;
        var elem = m_TargetsInGrass[ind];
        if (!elem.Validate) return false;
        return elem.InGrass;
    }

    public void AddTarget(Transform target, bool checkFade)
    {
        if (!m_Targets.Contains(target))
        {
            m_Targets.Add(target);
            m_TargetsInGrass.Add(new TargetInGrassControl() { Target = target, IsCheckFade = checkFade });
        }
    }

    public void RemoveTarget(Transform target)
    {
        RemoveTarget(m_Targets.IndexOf(target));
    }

    void RemoveTarget(int index)
    {
        if (index != -1)
        {
            m_Targets.RemoveAt(index);
            m_TargetsInGrass.RemoveAt(index);
        }
    }

    static void SetFade(Grass elem, bool fade)
    {
        SetFade(elem, fade ? 1 : 0, fade ? 0.5f : 1f);
    }

    static void SetFade(Grass elem, int state, float value)
    {
        elem.SetFade(state, value);
    }

    void CheckDistanceTargetsToGrass()
    {
        float sqrFade = m_RadiusFade * m_RadiusFade;
        for (int i = m_TargetsInGrass.Count - 1; i >= 0; i--)
        {
            var elem = m_TargetsInGrass[i];
            if (!elem.Validate)
            {
                RemoveTarget(i);
                continue;
            }
            var pos = elem.Target.position;
            pos.y = 0f;
            for (int j = 0; j < m_GrassElems.Length; j++)
            {
                var grass = m_GrassElems[j];
                if (elem.IsCheckFade)
                {
                    var pos2 = grass.Position;
                    pos2.y = 0f;
                    float sqr = (pos2 - pos).sqrMagnitude;
                    bool fade = sqr <= sqrFade;
                    SetFade(grass, fade);
                }
                bool inGrass = grass.ContainsXZ(pos);
                int index = elem.ListElements.IndexOf(grass);
                if (inGrass)
                {
                    if (index == -1) elem.ListElements.Add(grass);
                }
                else if (index != -1) elem.ListElements.RemoveAt(index);
            }
        }
    }

    void Test_CheckTargetsInGrass()
    {
        for (int i = m_TargetsInGrass.Count - 1; i >= 0; i--)
        {
            var elem = m_TargetsInGrass[i];
            if (!elem.Validate)
            {
                m_TargetsInGrass.RemoveAt(i);
                continue;
            }
            Debug.LogError("Target=" + elem.Target + " in grass=" + elem.InGrass);
        }
    }

    private void Start()
    {
        //TODO: хрупкое место
        if (m_GamePlace == null)
        {
            m_GrassElems = FindObjectsOfType<Grass>();
        }
        else
        {
            var tf = m_GamePlace.transform.FindTF(CreateLevel.GAME_LEVEL_NAME, true);
            if (tf == null) m_GrassElems = FindObjectsOfType<Grass>();
            else m_GrassElems = tf.GetComponentsInChildren<Grass>();
        }
    }

    protected override void Destroy()
    {
        m_Targets.Clear();
        m_TargetsInGrass.Clear();
    }

    private void Update()
    {
        CheckDistanceTargetsToGrass();
#if UNITY_EDITOR
        //Test_CheckTargetsInGrass();
#endif
    }

    class TargetInGrassControl
    {
        public Transform Target;
        public List<Grass> ListElements = new List<Grass>(10);

        public bool IsCheckFade { get; set; }

        public bool Validate { get { return !Target.IsNullOrDestroy(); } }

        public bool InGrass
        {
            get { return ListElements.Count > 0; }
        }
    }
}
