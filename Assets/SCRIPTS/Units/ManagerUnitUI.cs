using UnityEngine;
using System;

public class ManagerUnitUI : MonoSingleton<ManagerUnitUI> {

    [SerializeField] PersistentDataUnitUI[] m_PersistentUnitUI;
    [SerializeField] UIUnitInfo m_PrefabUnitUI;
    [SerializeField] RectTransform m_ParentUnitUI;
    
    public static UIUnitInfo GetUnitUI()
    {
        if (!Can) return null;
        var obj = m_I.m_PrefabUnitUI.Instantiate();
        var tf = obj.transform;
        tf.SetParent(m_I.m_ParentUnitUI);
        tf.localScale = Vector3.one;
        //tf.AutoAnchors();
        return obj;
    }

    public static PersistentDataUnitUI GetPersistentDataUI(TypePlayer type)
    {
        if (!Can) return null;
        for (int i = 0; i < m_I.m_PersistentUnitUI.Length; i++)
        {
            if (m_I.m_PersistentUnitUI[i].Type == type) return m_I.m_PersistentUnitUI[i];
        }
        return null;
    }
}

[Serializable]
public class PersistentDataUnitUI
{
    public TypePlayer Type;
    public Color ColorName;
    public ColorHP[] ColorSliderHP;
    [Serializable]
    public struct ColorHP
    {
        public float Value;
        public Color Color;
    }

    public Color GetColorByValue(float value)
    {
        Color color = Color.green;
        float prevVal = 0f;
        for (int i = 0; i < ColorSliderHP.Length; i++)
        {
            float nextValue = ColorSliderHP[i].Value * 0.01f;
            if (prevVal <= value && value <= nextValue)
            {
                return ColorSliderHP[i].Color;
            }
            prevVal = nextValue;
        }
        return Color.green;
    }
}
