using UnityEngine;

public abstract class PhysicsInfo<T>
{
    protected const int DEFAULT_COUNT = 20;
    protected T[] m_Array;
    protected int m_Count = 0;

    public T this[int index] { get { return m_Array[index]; } }

    public int Count { get { return m_Count; } }

    public PhysicsInfo(int capacity)
    {
        m_Array = new T[capacity > 0 ? capacity : DEFAULT_COUNT];
    }
}

public class OverlapCollidersInfo : PhysicsInfo<Collider>
{
    public Collider[] Colliders { get { return m_Array; } }

    public OverlapCollidersInfo(int capacity = -1) : base(capacity) { }

    public void Cast(PhysicsCastData castData)
    {
        if (castData == null)
        {
            Debug.LogError(GetType() + " error: CastData is null");
            m_Count = 0;
            return;
        }
        switch (castData.PhysicsCast)
        {
            case PhysicsCast.Ray:
                Debug.LogError("Overlap не работает для Raycast");
                break;
            case PhysicsCast.Sphere:
                SphereOverlap(castData.Position1, castData.Radius, castData.LayerMask, castData.QueryTrigger);
                break;
            case PhysicsCast.Box:
                BoxOverlap();
                break;
            case PhysicsCast.Capsule:
                CapsuleOverlap(castData.Position1, castData.Position2, castData.Radius, castData.LayerMask, castData.QueryTrigger);
                break;
        }
        //m_Count = 0;
    }

    void CapsuleOverlap(Vector3 pos1, Vector3 pos2, float rad, int layers, QueryTriggerInteraction trigger)
    {
        m_Count = Physics.OverlapCapsuleNonAlloc(pos1, pos2, rad, m_Array, layers, trigger);
        if (m_Count == m_Array.Length)
        {
            m_Array = Physics.OverlapCapsule(pos1, pos2, rad, layers, trigger);
            m_Count = m_Array.Length;
        }
    }

    void SphereOverlap(Vector3 pos, float rad, int layers, QueryTriggerInteraction trigger)
    {
        m_Count = Physics.OverlapSphereNonAlloc(pos, rad, m_Array, layers, trigger);
        if (m_Count == m_Array.Length)
        {
            m_Array = Physics.OverlapSphere(pos, rad, layers, trigger);
            m_Count = m_Array.Length;
        }
    }

    void BoxOverlap()
    {
#if UNITY_EDITOR
        Debug.LogError("BoxOverlap is not released");
#endif
    }
}

public class CastHitsInfo : PhysicsInfo<RaycastHit>
{
    public RaycastHit[] Hits { get { return m_Array; } }

    static CheckIncludeCollidersFromCastHit m_CheckIncludeColliders = new CheckIncludeCollidersFromCastHit();
    
    public CastHitsInfo() : base(-1) { }
    public CastHitsInfo(int capacity) : base(capacity) { }

    public void Cast(PhysicsCastData castData)
    {
#if UNITY_EDITOR
        Debug.Assert(castData.Direction != Vector3.zero, ("Direction is zero ="+ castData.Direction));
#endif
        switch (castData.CountCast)
        {
            case PhysicsCountCast.One: CastOne(castData); break;
            case PhysicsCountCast.All: CastAll(castData); break;
        }
        m_Count = m_CheckIncludeColliders.CheckIncludeColliders(castData.CheckIncludeColliders, m_Array, 0, m_Count, castData.AvgPosition, castData.Direction);
    }

    void CastAll(PhysicsCastData castData)
    {
        switch (castData.PhysicsCast)
        {
            case PhysicsCast.Ray:
                RaycastAll(castData.Position1, castData.Direction, castData.Distance, castData.LayerMask, castData.QueryTrigger);
                break;
            case PhysicsCast.Sphere:
                SphereCastAll(castData.Position1, castData.Radius, castData.Direction, castData.Distance, castData.LayerMask, castData.QueryTrigger);
                break;
            case PhysicsCast.Box:
                BoxCastAll();
                break;
            case PhysicsCast.Capsule:
                CapsuleAllCast(castData.Position1, castData.Position2, castData.Radius, castData.Direction, castData.Distance, castData.LayerMask, castData.QueryTrigger);
                break;
        }
    }

    void CastOne(PhysicsCastData castData)
    {
#if UNITY_EDITOR
        Debug.LogError("CastOne is not released");
#endif
    }

    void CapsuleAllCast(Vector3 pos1, Vector3 pos2, float rad, Vector3 dir, float dist, int layers, QueryTriggerInteraction trigger)
    {
        m_Count = Physics.CapsuleCastNonAlloc(pos1, pos2, rad, dir, m_Array, dist, layers, trigger);
        if (m_Count == m_Array.Length)
        {
            m_Array = Physics.CapsuleCastAll(pos1, pos2, rad, dir, dist, layers, trigger);
            m_Count = m_Array.Length;
        }
    }

    void SphereCastAll(Vector3 pos, float rad, Vector3 dir, float dist, int layers, QueryTriggerInteraction trigger)
    {
        m_Count = Physics.SphereCastNonAlloc(pos, rad, dir, m_Array, dist, layers, trigger);
        if (m_Count == m_Array.Length)
        {
            m_Array = Physics.SphereCastAll(pos, rad, dir, dist, layers, trigger);
            m_Count = m_Array.Length;
        }
    }

    void RaycastAll(Vector3 pos, Vector3 dir, float dist, int layers, QueryTriggerInteraction trigger)
    {
        m_Count = Physics.RaycastNonAlloc(pos, dir, m_Array, dist, layers, trigger);
        if (m_Count == m_Array.Length)
        {
            m_Array = Physics.RaycastAll(pos, dir, dist, layers, trigger);
            m_Count = m_Array.Length;
        }
    }

    void BoxCastAll()
    {
#if UNITY_EDITOR
        Debug.LogError("BoxCastAll is not released");
#endif
    }

    void BoxCastAll(Vector3 center, Vector3 halfExtens, Vector3 dir, Quaternion orient, float dist, int layers, QueryTriggerInteraction trigger)
    {
        m_Count = Physics.BoxCastNonAlloc(center, halfExtens, dir, m_Array, orient, dist, layers, trigger);
        if (m_Count == m_Array.Length)
        {
            m_Array = Physics.BoxCastAll(center, halfExtens, dir, orient, dist, layers, trigger);
            m_Count = m_Array.Length;
        }
    }
}

public class CheckIncludeCollidersFromCastHit
{
    /// <summary>
    ///Дополнительная проверка на случай
    ///Когда Cast находится ввнутри других коллайдеров
    ///Попадание обнаруживается, но точка равна Vector3.zero и нормаль приблизительная
    /// </summary>
    public enum Type
    {
        None,//Нет проверки
        Ignore,//Удаляет из списка
        PointReplaceToCastPoint, //Замена точки попадания(zero) на точку место откуда происходит каст и его обратного направления
        ClosePoint,//Ищет ближайшую точку(сейчас на основе bounds самого коллайдера)
        AdditionalCast // Кидает дополн. райкаст в коллайдер для поиска точки
    }

    System.Collections.Generic.List<int> m_IndexIgnore = new System.Collections.Generic.List<int>(100);

    public int CheckIncludeColliders(Type type, RaycastHit[] hits, int start, int count, Vector3 posCast = default(Vector3), Vector3 dirCast = default(Vector3))
    {
        if (hits == null) return 0;
        if (count <= 0) return 0;
        if (start >= count || start < 0)
        {
#if UNITY_EDITOR
            Debug.LogError("Start or Count is BAD");
#endif
            return 0;
        }
        if (count > hits.Length || start >= hits.Length)
        {
#if UNITY_EDITOR
            Debug.LogError("Start or Count is BAD");
#endif
            return count;
        }
        if (type == Type.None) return count;
        switch (type)
        {
            case Type.Ignore: return Ignore(start, count, hits);
            case Type.PointReplaceToCastPoint: return PointReplaceToCastPoint(start, count, hits, posCast, dirCast.normalized);
            case Type.ClosePoint: return ClosePoint(start, count, hits, posCast, dirCast.normalized);
            case Type.AdditionalCast: return AdditionalCast(start, count, hits, posCast, dirCast.normalized);
        }
        return count;
    }


    int Ignore(int start, int count, RaycastHit[] hits)
    {
        for (int i = start; i < count; i++)
        {
            var hit = hits[i];
            if (!hit.point.IsAbsoluteZero()) continue;
            m_IndexIgnore.Add(i);
        }
        int countIndex = m_IndexIgnore.Count;
        for (int i = 0; i < countIndex; i++)
        {
            for (int j = m_IndexIgnore[i] - i; j < count - 1; j++) hits[j] = hits[j + 1];
        }
        m_IndexIgnore.Clear();
        return count - countIndex;
    }

    int PointReplaceToCastPoint(int start, int count, RaycastHit[] hits, Vector3 posCast, Vector3 dirCast)
    {
        dirCast = -dirCast;
        for (int i = start; i < count; i++)
        {
            var hit = hits[i];
            if (!hit.point.IsAbsoluteZero()) continue;
            hit.point = posCast;
            hit.normal = dirCast;
            hits[i] = hit;
        }
        return count;
    }

    int ClosePoint(int start, int count, RaycastHit[] hits, Vector3 posCast, Vector3 dirCast)
    {
        for (int i = start; i < count; i++)
        {
            var hit = hits[i];
            if (!hit.point.IsAbsoluteZero()) continue;
            var bounds = hit.collider.bounds;
            var point = bounds.ClosestPoint(posCast);
            hit.point = point;
            var normal = (posCast - point).normalized;
            if (normal.IsAbsoluteZero()) normal = -dirCast;
            hit.normal = normal;
            hits[i] = hit;
        }
        return count;
    }

    int AdditionalCast(int start, int count, RaycastHit[] hits, Vector3 posCast, Vector3 dirCast)
    {
        Ray ray = new Ray();
        for (int i = start; i < count; i++)
        {
            var hit = hits[i];
            if (!hit.point.IsAbsoluteZero()) continue;
            var cld = hit.collider;
            var bounds = cld.bounds;
            var point = bounds.ClosestPoint(posCast);//ближайщая точка на коробке bounds
            var center = bounds.center;
            var dir = center - point; // вектор от центра коробки до ближайшей точки
            float dist = dir.magnitude * 2f;
            var dirN = dir.normalized;
            ray.direction = dirN;
            ray.origin = point - dirN * 1.2f;
            RaycastHit hit2;
            Vector3 normal;
            //Получаем более точную точку
            if (cld.Raycast(ray, out hit2, dist)) point = hit2.point;
            hit.point = point;
            normal = (posCast - point).normalized;
            if (normal.IsAbsoluteZero()) normal = -dirCast;
            hit.normal = normal;
            hits[i] = hit;
        }
        return count;
    }
}
