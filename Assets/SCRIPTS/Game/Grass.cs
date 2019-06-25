using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//[RequireComponent(typeof(TriggerControl))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class Grass : MonoBehaviour
{
    MeshRenderer m_Renderer;
    //TriggerControl m_Trigger;
    Bounds m_Bounds;
    Vector3 m_Pos;
    int m_State = -1;

    //public static event Action<Grass, GameObject, bool> GrassEvent;

    public Vector3 Position { get { return m_Pos; } }

    public bool ContainsXZ(Vector3 pos)
    {
        pos.y = m_Bounds.center.y;
        return Contains(pos);
    }

    public bool Contains(Vector3 pos)
    {
        return m_Bounds.Contains(pos);
    }

    private void Awake()
    {
        //m_Trigger = GetComponent<TriggerControl>();
        m_Renderer = GetComponent<MeshRenderer>();
        m_Bounds = GetComponent<Collider>().bounds;
        //m_Trigger.EventTrigger += OnEnter;
    }

    private void Start()
    {
        m_Pos = transform.position;
    }

    public void SetFade(int state, float value)
    {
        if (m_State == state) return;
        m_State = state;
        var mat = m_Renderer.material;
        var color = mat.color;
        color.a = value;
        mat.color = color;
    }

    //private void OnEnter(Collider cld, bool state, bool condition)
    //{
    //    if (GrassEvent != null) GrassEvent(this, cld.GameObject(), state && condition);
    //}
}
