using UnityEngine;
using System.Collections.Generic;

abstract class SimplePrefabFactory<Key, Value> : IFactoryByKey<Key, Value>, IEqualityComparer<Key> where Value : UnityEngine.Object
{
    protected Dictionary<Key, Value> objs;

    public SimplePrefabFactory(int capacity = 10)
    {
        objs = new Dictionary<Key, Value>(capacity);
    }

    public Value Create(Key key)
    {
        Value value;
        return TryCreate(key, out value) ? value : null;
    }

    protected abstract string GetPath(Key key);
    protected abstract bool CheckKey(Key key);

    public bool TryCreate(Key key, out Value value)
    {
        Value obj;
        if (CheckKey(key))
        {
            value = default(Value);
            return false;
        }
        if (!objs.TryGetValue(key, out obj))
        {
            obj = Resources.Load(GetPath(key)) as Value;
            if (obj != null) objs.Add(key, obj);
        }
        value = obj == null ? null : GameObject.Instantiate(obj);
        return value != null;
    }

    public abstract Key[] GetKeys();

    public abstract bool Equals(Key x, Key y);

    public int GetHashCode(Key obj) { return obj.GetHashCode(); }
}
