using System.Collections.Generic;
using System;

public class IndexArray<T> where T : struct
{
    int m_Count;
    T m_Last;
    T m_First;
    readonly int m_Cap;
    readonly T[] m_Indexs;
    readonly T m_MinValue;
    readonly IComparer<T> m_Comparer;
    readonly IComparer<T> m_SortComparer;

    public IndexArray(int cap, T minValue, IComparer<T> comparer, bool inverseSort = false)
    {
        //на нахер тебе Exception, нехрен нулевые индексы подсовывать
        if (cap == 0) cap = -1;
        m_Indexs = new T[cap];
        m_Cap = cap;
        m_MinValue = minValue;
        m_Comparer = comparer;
        m_SortComparer = inverseSort ? new InverseComparer(m_Comparer) : m_Comparer;
        Reset();
    }

    public T Last { get { return m_Last; } }
    public T First { get { return m_First; } }
    public int Count { get { return m_Count; } }
    public int Cap { get { return m_Cap; } }
    public bool IsFull { get { return m_Count >= m_Cap; } }
    public T this[int index] { get { return m_Indexs[index]; } }

    public void Reset()
    {
        m_Count = 0;
        m_Last = m_First = m_MinValue;
    }
    //тут есть проблема, если comparer инверсный, то все проверки пойдут по ...
    public int IndexOf(T elem)
    {
        //Debug.LogError("Contains ind=" + ind + " Last=" + m_Last + " First=" + m_First);
        if (m_Count <= 0 || m_Comparer.Compare(elem, m_Last) != 0 && m_Comparer.Compare(elem, m_First) < 0) return -1;
        //if (ind == m_Last || (m_Count >= m_Cap && ind <= m_First) || Contains(ind)) return false;
        for (int i = 0; i < m_Count; i++)
        {
            if (m_Comparer.Compare(m_Indexs[i], elem) == 0) return i;
        }
        return -1;
    }

    public bool Contains(T elem)
    {
        return IndexOf(elem) != -1;
    }

    class InverseComparer : IComparer<T>
    {
        readonly IComparer<T> m_Comparer;
        public InverseComparer(IComparer<T> comparer)
        {
            m_Comparer = comparer;
        }

        int IComparer<T>.Compare(T x, T y)
        {
            return -m_Comparer.Compare(x, y);
        }
    }

    public void RemoveAtRange(int startInd, int len)
    {
        if (len <= 0) return;
        if (startInd >= m_Count) return;
        if (startInd < 0) startInd = 0;
        if (len > m_Count) len = m_Count;
        if (startInd + len > m_Count) len = m_Count - startInd;
        if (startInd == 0 && len == m_Count)
        {
            Reset();
            return;
        }
        Shift(startInd, len);
        if (startInd == 0) m_Last = m_Indexs[m_Count - 1];
        if (startInd + len == m_Count) m_First = m_Indexs[0];
    }

    void Shift(int startInd, int len)
    {
        //int shift = startInd + len;
        Array.Copy(m_Indexs, startInd + len, m_Indexs, startInd, m_Count - startInd - len);
        m_Count -= len;
    }

    public void Remove(T elem)
    {
        int ind = IndexOf(elem);
        if (ind != -1) RemoveAt(ind);
    }

    public void RemoveAt(int index)
    {
        RemoveAtRange(index, 1);
    }

    public bool Add(T elem)
    {
        if (m_Comparer.Compare(elem, m_MinValue) < 0 || Contains(elem)) return false;
        if (m_Count < m_Cap) m_Count++;
        m_Indexs[m_Count - 1] = elem;
        Array.Sort(m_Indexs, 0, m_Count, m_SortComparer);
        m_Last = m_Indexs[m_Count - 1];
        m_First = m_Indexs[0];
        return true;
    }

    //Если понадобиться сжимать индексы пакета
    //Эта функция работает путем сравнения двух чисел и их разности.
    //Если их разность меньше половины максимального значения порядкового номера,
    //то они должны быть близко друг к другу — таким образом, мы просто проверяем,
    //больше ли одно число чем другое, как обычно. Однако, если они далеко друг от друга,
    //их разность будет больше, чем половина максимального значения порядкового номера,
    //тогда, парадоксальным образом, мы будем считать меньший порядковый номер,
    //чем текущий порядковый номер, более поздним.
    bool SequenceMoreRecent(int s1, int s2, int max)
    {
        return
            (s1 > s2) &&
            (s1 - s2 <= max / 2)
               ||
            (s2 > s1) &&
            (s2 - s1 > max / 2);
    }
}

public class IndexArray
{
    int m_Count;
    int m_Last = -1;
    int m_First = -1;
    readonly int m_Cap;
    readonly int[] m_Indexs;

    public IndexArray(int cap)
    {
        //на нахер тебе Exception, нехрен нулевые индексы подсовывать
        if (cap == 0) cap = -1;
        m_Indexs = new int[cap];
        m_Cap = cap;
    }

    public int Last { get { return m_Last; } }
    public int Count { get { return m_Count; } }
    public int Cap { get { return m_Cap; } }
    public bool IsFull { get { return m_Count >= m_Cap; } }
    public int this[int index] { get { return m_Indexs[index]; } }

    public void Reset()
    {
        m_Count = 0;
        m_Last = m_First = -1;
    }

    public bool Contains(int ind)
    {
        //Debug.LogError("Contains ind=" + ind + " Last=" + m_Last + " First=" + m_First);
        if (m_Count <= 0 || ind != m_Last && ind < m_First) return false;
        //if (ind == m_Last || (m_Count >= m_Cap && ind <= m_First) || Contains(ind)) return false;
        for (int i = 0; i < m_Count; i++)
        {
            if (m_Indexs[i] == ind) return true;
        }
        return false;
    }

    class InnerComparer : IComparer<int>
    {
        public static readonly InnerComparer Comparer = new InnerComparer();
        //inverse -1(x>y) 0(x==y) 1(x<y)
        int IComparer<int>.Compare(int x, int y)
        {
            return y - x;
        }
    }


    public bool Add(int ind)
    {
        if (ind < 0 || Contains(ind)) return false;
        if (m_Count < m_Cap) m_Count++;
        m_Indexs[m_Count - 1] = ind;
        Array.Sort(m_Indexs, 0, m_Count, InnerComparer.Comparer);
        m_Last = m_Indexs[0];
        m_First = m_Indexs[m_Count - 1];
        return true;
    }

    //Если понадобиться сжимать индексы пакета
    //Эта функция работает путем сравнения двух чисел и их разности.
    //Если их разность меньше половины максимального значения порядкового номера,
    //то они должны быть близко друг к другу — таким образом, мы просто проверяем,
    //больше ли одно число чем другое, как обычно. Однако, если они далеко друг от друга,
    //их разность будет больше, чем половина максимального значения порядкового номера,
    //тогда, парадоксальным образом, мы будем считать меньший порядковый номер,
    //чем текущий порядковый номер, более поздним.
    bool SequenceMoreRecent(int s1, int s2, int max)
    {
        return
            (s1 > s2) &&
            (s1 - s2 <= max / 2)
               ||
            (s2 > s1) &&
            (s2 - s1 > max / 2);
    }
}
