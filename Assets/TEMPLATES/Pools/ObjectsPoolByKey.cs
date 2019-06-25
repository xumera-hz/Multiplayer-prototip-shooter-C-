using System;
using System.Collections.Generic;

public delegate bool FactoryMethod<U,T>(U key, out T obj);
public delegate void EmptyMethod<U, T>(U key, T obj);
public class NodeByID<T>
{
    public int ID;
    public T Value;

    public NodeByID<T> Set(int id, T value)
    {
        ID = id; Value = value;
        return this;
    }

    public NodeByID<T> Clear()
    {
        ID = 0; Value = default(T);
        return this;
    }
}

public class Container<T> where T : class
{
    static bool CreateNode(out NodeByID<T> Node) { Node = new NodeByID<T>(); return true; }
    static ObjectsPool<NodeByID<T>> nodePool;
    static Container() { nodePool = new ObjectsPool<NodeByID<T>>(CreateNode, 10); }
    List<NodeByID<T>> elems;

    public List<NodeByID<T>> Objects { get { return elems; } }
    int ids = int.MinValue;
    public int Add(T elem)
    {
        int id = 0;
        if (elem != null)
        {
            id = ++ids;
            elems.Add(nodePool.Get().Set(id, elem));
        }
        return id;
    }

    public void Remove(T listener)
    {
        if (listener == null) return;
        for (int i = elems.Count - 1; i >= 0; i--)
        {
            if (elems[i].Value == listener) InnerRemoveAt(elems[i], i);
        }
    }

    public void Remove(int ID)
    {
        for (int i = elems.Count - 1; i >= 0; i--)
        {
            if (elems[i].ID == ID) InnerRemoveAt(elems[i], i);
        }
    }

    void InnerRemoveAt(NodeByID<T> node, int index)
    {
        if (node.ID == ids) ids--;
        elems.RemoveAt(index);
        nodePool.Return(node.Clear());
    }

    public void RemoveAt(int index)
    {
        if (index >= elems.Count) return;
        InnerRemoveAt(elems[index], index);
    }
}

public interface IPool<U, T> : IReturnObjectByKey<U, T>
{
    void Clear();
    void Clear(U key);
    int Count(U key);
    void SetFactoryMethod(FactoryMethod<U, T> _factory);
    T Get(U key);
    bool TryGet(U key, out T elem);
    bool Remove(U key, T elem);
    bool RemoveWithReference(U key, T elem);
    void RemoveCount(U key, int count);
}

public class ObjectsPoolByKey<TKey, TValue> : IPool<TKey, TValue> where TValue : class
{
    protected const int countDefCapacity = 10;
    FactoryMethod<TKey, TValue> factory;

    protected Dictionary<TKey, List<TValue>> elems;
    //public ObjectsPoolByKey() : this(null, 0, null) { }

    public ObjectsPoolByKey(FactoryMethod<TKey, TValue> _factory, int capacity, IEqualityComparer<TKey> compare)
    {
        factory = _factory;
        elems = new Dictionary<TKey, List<TValue>>(capacity < 0 ? 0 : capacity, compare);
        //if (compare == null)
        //{
        //    elems = new Dictionary<TKey, List<TValue>>(capacity < 0 ? 0 : capacity);
        //}
        //else
        //{
        //    elems = new Dictionary<TKey, List<TValue>>(capacity < 0 ? 0 : capacity, compare);
        //}
    }

    public ObjectsPoolByKey(FactoryMethod<TKey, TValue> _factory, int capacity) : this(_factory, capacity, null) {  }

    public int Count(TKey key)
    {
        List<TValue> tmp;
        if (elems.TryGetValue(key, out tmp)) return tmp.Count;
        return 0;
    }

    public virtual bool Return(TKey key, TValue elem)
    {
        if (elem == null) return false;
        List<TValue> tmp;
        if (!elems.TryGetValue(key, out tmp)) return false;
        tmp.Add(elem);
        return true;
    }

    public TValue Get(TKey key)
    {
        TValue elem;
        return TryGet(key, out elem) ? elem : null;
    }

    public bool TryGet(TKey key, out TValue elem)
    {
        List<TValue> tmp;
        if (elems.TryGetValue(key, out tmp))
        {
            for (int i = tmp.Count - 1; i >= 0; i--)
            {
                elem = tmp[i];
                tmp.RemoveAt(i);
                if (elem == null) continue;
                RemoveCallBack(key, elem);
                return true;
            }
        }
        bool res = factory(key, out elem);
        if (res)
        {
            if(tmp==null) elems.Add(key, new List<TValue>(countDefCapacity));
            GetCallBack(key, elem);
        }
        return res;
    }

    public bool Remove(TKey key, TValue elem)
    {
        if (elem == null) return false;
        List<TValue> tmp;
        if (!elems.TryGetValue(key, out tmp)) return false;
        for (int i = tmp.Count - 1; i >= 0; i--)
        {
            if (tmp[i] == elem)
            {
                tmp.RemoveAt(i);
                RemoveCallBack(key, elem);
                return true;
            }
        }
        return false;
    }

    public bool RemoveWithReference(TKey key, TValue elem)
    {
        bool res = Remove(key, elem);
        if(res) elem = null;
        return res;
    }

    protected virtual void RemoveCallBack(TKey key, TValue elem) { }

    protected virtual void GetCallBack(TKey key, TValue elem) { }

    public virtual void Clear(TKey key)
    {
        List<TValue> tmp;
        if (elems.TryGetValue(key, out tmp)) tmp.Clear();
    }

    public virtual void Clear()
    {
        elems.Clear();
    }

    public void SetFactoryMethod(FactoryMethod<TKey,TValue> _factory)
    {
        if (_factory != null) factory = _factory;
    }

    public void RemoveCount(TKey key, int count)
    {
        if (count <= 0) return;
        List<TValue> list;
        if (!(elems.TryGetValue(key, out list))) return;
        int _count = list.Count;
        if (count >= _count)
        {
            Clear(key);
            return;
        }
        count = _count - count;
        TValue elem;
        for (int i = _count - 1; i >= count; i--)
        {
            elem = list[i];
            list.RemoveAt(i);
            RemoveCallBack(key, elem);
        }
    }
}
