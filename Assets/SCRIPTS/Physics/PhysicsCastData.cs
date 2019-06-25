using System;
using UnityEngine;

public enum TypePhysicsCheck { Cast, Overlap }
public enum PhysicsCast { Ray, Box, Sphere, Capsule }
public enum PhysicsOverlap { Box, Sphere, Capsule }
public enum PhysicsCountCast { One, All }

[Serializable]
public class PhysicsCastData
{
    public TypePhysicsCheck PhysicsCheck;
    public PhysicsCast PhysicsCast;
    public PhysicsCountCast CountCast;
    public QueryTriggerInteraction QueryTrigger;
    public LayerMask LayerMask = -1;
    [Tooltip("Если коллайдер оказался внутри Cast или Overlap, то его позицию контакта считается 0. Эта штука решает эту проблему")]
    public CheckIncludeCollidersFromCastHit.Type CheckIncludeColliders = CheckIncludeCollidersFromCastHit.Type.Ignore;
    public float Radius;
    public float Distance;
    //[NonSerialized] public Vector3 Position1, Position2;
    [NonSerialized]
    public Vector3 Direction;
    [NonSerialized]
    public Quaternion Rotation;
    [NonSerialized]
    public Vector3 BoxHalfExtends;
    //Цели для кастов
    //Например для капсулы
    //Расстояние между двумя целями это высота цилиндра капсулы
    //А вектор между целями, задает поворот капсулы
    public Transform Target1, Target2;

    Vector3 m_Position1;
    Vector3 m_Position2;

    public void SetFromRay(Ray ray)
    {
        Position1 = ray.origin;
        Direction = ray.direction;
    }

    public Vector3 AvgPosition
    {
        get { return (Position1 + Position2) * 0.5f; }
    }

    public Vector3 Position1
    {
        set { m_Position1 = value; if (Target1 != null) Target1.position = value; }
        get { return Target1 == null ? m_Position1 : Target1.position; }
    }
    public Vector3 Position2
    {
        set { m_Position2 = value; if (Target2 != null) Target2.position = value; }
        get { return Target2 == null ? m_Position2 : Target2.position; }
    }
    public Vector3 Center { get { return Position1; } }


}
