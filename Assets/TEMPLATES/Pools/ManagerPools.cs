using System;
using System.Collections.Generic;

public interface IManagerPools<T1,T2> : IReturnObjectByKey<T1, T2>, IGetObjectByKey<T1,T2>
{
    bool Add(T1 key, IPool<T2> elem);
    bool TryGet(T1 key, out IPool<T2> elem);
}


public class ManagerPools<T> : IManagerPools<int, T> where T : class
{
    Dictionary<int, IPool<T>> pools;
    IPool<T> cachePool;
    int cacheId;
    public ManagerPools(int capacity = 10)
    {
        pools = new Dictionary<int, IPool<T>>(capacity);
    }

    public T Get(int id)
    {
        T elem = default(T);
        TryGet(id, out elem);
        return elem;
    }

    public bool TryGet(int id, out IPool<T> pool)
    {
        if (cacheId == id)
        {
            if (cachePool != null || pools.TryGetValue(id, out cachePool))
            {
                cacheId = id;
                pool = cachePool;
                return true;
            }
        }
        if (pools.TryGetValue(id, out cachePool))
        {
            cacheId = id;
            pool = cachePool;
            return true;
        }
        pool = null;
        return false;
    }

    public bool TryGet(int id, out T elem)
    {
        if (cacheId == id)
        {
            if (cachePool != null || pools.TryGetValue(id, out cachePool))
            {
                cacheId = id;
                return cachePool.TryGet(out elem);
            }
        }
        else
        {
            if (pools.TryGetValue(id, out cachePool))
            {
                cacheId = id;
                return cachePool.TryGet(out elem);
            }
        }
        elem = null;
        return false;
    }

    public bool Add(int id, IPool<T> pool)
    {
        if (pool == null || pools.ContainsKey(id)) return false;
        pools.Add(id, pool);
        return true;
    }

    public bool Return(int id, T elem)
    {
        if (cacheId == id)
        {
            if (cachePool != null || pools.TryGetValue(id, out cachePool))
            {
                cacheId = id;
                cachePool.Return(elem);
                return true;
            }
        }
        else
        {
            if (pools.TryGetValue(id, out cachePool))
            {
                cacheId = id;
                cachePool.Return(elem);
                return true;
            }
        }
        return false;
    }
}
