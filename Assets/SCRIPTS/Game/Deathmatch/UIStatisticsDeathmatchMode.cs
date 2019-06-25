using System.Collections.Generic;
using UnityEngine;

public class UIStatisticsDeathmatchMode : MonoBehaviour
{
    #region Test
#if UNITY_EDITOR
    [SerializeField] bool m_Test;
    [ContextMenu("TestAdd")]
    void TestAdd()
    {
        Add();
    }
    [ContextMenu("TestRemove")]
    void TestRemove()
    {
        int count = m_Elements.Count;
        if (count <= 0) return;
        int ind = UnityEngine.Random.Range(0, count);
        Debug.LogError("TestRemove ind=" + ind);
        Remove(ind);
    }
    [ContextMenu("TestChangeIndex")]
    void TestChangeIndex()
    {
        int count = m_Elements.Count;
        if (count <= 0) return;
        int ind1 = UnityEngine.Random.Range(0, count);
        int ind2 = UnityEngine.Random.Range(0, count);
        //if (ind1 == ind2) ind2 = count - 1;
        Debug.LogError("TestChangeIndex start=" + ind1 + " end=" + ind2);
        ChangeIndex(ind1, ind2);
    }
#endif

    #endregion

    UIDeathmatchStatsPlayer Create()
    {
        var elem = m_Prefab.Instantiate();
#if UNITY_EDITOR
        if (m_Test)
        {
            var text = elem.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text) text.text = elem.GetInstanceID().ToString();
        }
#endif
        return elem;
    }

    public bool IsActive { get { return gameObject.activeInHierarchy; } }
    public event System.Action<bool> Activity;

    private void OnEnable()
    {
        if (Activity != null) Activity(true);
    }

    private void OnDisable()
    {
        if (Activity != null) Activity(false);
    }

    [SerializeField] UIDeathmatchStatsPlayer m_Prefab = null;
    [SerializeField] RectTransform m_Parent = null;
    [SerializeField] RectTransform m_StartPosFirst = null;
    [SerializeField] float m_WidthBorderBetweenElements;
    List<UIDeathmatchStatsPlayer> m_Elements = new List<UIDeathmatchStatsPlayer>(10);
    
    public void ChangeIndex(int start, int end)
    {
        if (start == end) return;
        var elem = m_Elements[start];
        bool shiftUp = start < end;
        //int shiftStart = shiftUp ? (start + 1) : end;
        //int shiftEnd = shiftUp ? end : start - 1;
        int shiftStart = shiftUp ? (start) : end;
        int shiftEnd = shiftUp ? end : start;
        var pos = m_Elements[end].transform.localPosition;
        ShiftElements(shiftStart, shiftEnd, shiftUp ? -1 : 1);
        elem.SetLocalPos(pos);
        m_Elements.RemoveAt(start);
        m_Elements.Insert(end, elem);
    }

    public void ChangeCount(int ind, int count)
    {
        var elem = m_Elements[ind];
        elem.SetCount(count.ToString());
    }

    public UIDeathmatchStatsPlayer Add()
    {
        var elem = Create();//pool maybe
        if (elem == null) return null;
        var tf = elem.transform;
        tf.SetParent(m_Parent);
        tf.localScale = Vector3.one;
        elem.SetLocalPos(GetNewPos(m_Elements.Count));
        m_Elements.Add(elem);
        return elem;
    }

    public void Remove(int index)
    {
        if (index < 0 || index >= m_Elements.Count) return;
        var elem = m_Elements[index];
        ShiftElements(index, m_Elements.Count - 1, -1);
        m_Elements.RemoveAt(index);
        elem.DestroyGO();//pool maybe
    }

    Vector3 GetNewPos(int index)
    {
        if (index <= 0)
        {
            return m_StartPosFirst.localPosition;
        }
        else
        {
            var target = m_Elements[index - 1].transform as RectTransform;
            var pos = target.localPosition;
            pos.y = pos.y - (target.rect.height + m_WidthBorderBetweenElements);
            return pos;
        }
    }

    void ShiftElements(int startIndex, int endIndex, int dir)
    {
        if (dir == 0) return;
        int count = m_Elements.Count;
        if (dir < 0)
        {
            for (int i = endIndex; i >= startIndex; i--)
            {
                int ind = i + dir;
                if (ind < 0 || ind >= count) continue;
                m_Elements[i].SetLocalPos(m_Elements[ind].transform.localPosition);
            }
        }
        else
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                int ind = i + dir;
                if (ind < 0 || ind >= count) continue;
                m_Elements[i].SetLocalPos(m_Elements[ind].transform.localPosition);
            }
        }
    }
}
