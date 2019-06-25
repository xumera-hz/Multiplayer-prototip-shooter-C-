using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum TypeUnit { German1, German2, British1 }

public class ManagerUnits : MonoSingleton<ManagerUnits>
{
    [SerializeField] Node[] m_Units = null;
    Transform m_RootUnits;

    [Serializable]
    class Node
    {
        public TypeUnit Type = TypeUnit.British1;
        public GameObject Prefab = null;
    }

    protected override void OnAwake()
    {
        m_RootUnits = new GameObject("ROOT_UNITS").transform;
    }

    protected override void Destroy()
    {
        m_RootUnits.DestroyGO();
    }

    public static GameObject CreateRandom()
    {
        if (!Can) return null;
        var units = m_I.m_Units;
        if (units == null || units.Length <= 0) return null;
        int rand = UnityEngine.Random.Range(0, units.Length);
        return CreateUnit((TypeUnit)rand);
    }

    public static GameObject CreateUnit(TypeUnit type)
    {
        return Can ? m_I.InnerCreateUnit(type) : null;
    }

    GameObject InnerCreateUnit(TypeUnit type)
    {
        GameObject go = null;
        var units = m_Units;
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i].Type == type)
            {
                go = units[i].Prefab.Instantiate();
                break;
            }
        }
        if (go == null && units.Length > 0) go = units[0].Prefab.Instantiate();
        if (go != null) go.transform.SetParent(m_RootUnits);
        return go;
    }

    //List<UnitContainer> m_Units = new List<UnitContainer>(10);

}
