using UnityEngine;
using System.Collections.Generic;

public class ManagerSpawnPoints : MonoSingleton<ManagerSpawnPoints> {

    [SerializeField] Transform m_RespawnPointsParent;
    Transform[] m_RespawnPoints;
    int m_LastPoint = -1;
    List<int> m_ListPoints;

    public Vector3 GetRespawnPoint(int ind)
    {
        int len = m_RespawnPoints.Length;
        if (len <= 0) return Vector3.zero;
        if (ind < 0 || ind >= len) ind = 0;
        return m_RespawnPoints[ind].position;
    }

    //TODO: сделать проверку, что точка не занята
    public Vector3 GetRandomRespawnPoint()
    {
        //if (m_RespawnPoints.Length <= 0) return Vector3.zero;
        //int rand = UnityEngine.Random.Range(0, m_RespawnPoints.Length);
        if (m_RespawnPoints.Length <= 0) return Vector3.zero;
        if (m_RespawnPoints.Length == 1) return m_RespawnPoints[0].position;
        if (m_LastPoint != -1) m_ListPoints.RemoveAt(m_LastPoint);
        int rand = UnityEngine.Random.Range(0, m_ListPoints.Count);
        if (m_LastPoint != -1) m_ListPoints.Add(m_LastPoint);
        m_LastPoint = rand;
        return m_RespawnPoints[rand].position;
    }

    protected override void OnAwake()
    {
        int count = m_RespawnPointsParent.childCount;
        m_RespawnPoints = new Transform[count];
        m_ListPoints = new List<int>(count);
        for (int i = count - 1; i >= 0; i--)
        {
            m_RespawnPoints[i] = m_RespawnPointsParent.GetChild(i);
            m_ListPoints.Add(i);
        }
    }
}
