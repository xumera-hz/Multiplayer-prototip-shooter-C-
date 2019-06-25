using System.Collections.Generic;

//Пул можно обмануть, если FactoryMethod будет возвращать один(два) и тот(те) же элемент(ы)
//Дополнительные проверки на эту проблему сильно замедлят пул, поэтому будте совестными :D
public delegate bool FactoryMethod<T>(out T obj);

public interface IPool<T> : IReturnObject<T>, IGetObject<T>
{
    void Clear();
    int Count { get; }
    void SetFactoryMethod(FactoryMethod<T> _factory);
    void Remove(T elem);
    void RemoveWithReference(T elem);
    void RemoveCount(int count);
}

public class ObjectsPool<T> : IPool<T> where T : class
{
    protected FactoryMethod<T> factory;

    protected List<T> elems;

    public virtual void Clear() { elems.Clear(); }

    public int Count { get { return elems.Count; } }

    public void SetFactoryMethod(FactoryMethod<T> _factory)
    {
        if(_factory!=null) factory = _factory;
    }

    public ObjectsPool(FactoryMethod<T> _factory, int capacity)
    {
        factory = _factory;
        elems = new List<T>(capacity < 0 ? 0 : capacity);
    }

    public virtual bool Return(T elem)
    {
        if (elem == null) return false;
        elems.Add(elem);
        return true;
    }
    public T Get()
    {
        T elem;
        TryGet(out elem);
        return elem;
    }
    public bool TryGet(out T elem)
    {
        for (int i = elems.Count - 1; i >= 0; i--)
        {
            elem = elems[i];
            elems.RemoveAt(i);
            if (elem == null) continue;
            RemoveCallBack(elem);
            return true;
        }
        return factory(out elem);
    }

    public void Remove(T elem)
    {
        if (elem == null) return;
        for (int i = elems.Count - 1; i >= 0; i--)
        {
            if (elems[i] == elem)
            {
                elems.RemoveAt(i);
                RemoveCallBack(elem);
                return;
            }
        }
    }

    public void RemoveWithReference(T elem)
    {
        Remove(elem);
        elem = null;
    }

    protected virtual void RemoveCallBack(T elem) { }

    public void RemoveCount(int count)
    {
        if (count <= 0) return;
        int _count = elems.Count;
        if (count >= _count)
        {
            Clear();
            return;
        }
        count = _count - count;
        T elem;
        for (int i = _count - 1; i >= count; i--)
        {
            elem = elems[i];
            elems.RemoveAt(i);
            RemoveCallBack(elem);
        }
    }


}
