public interface IGetObjectByKey<Key, Return> : IInstanceGetObjectByKey<Key, Return>, ITryGetObjectByKey<Key, Return> { }

public interface IInstanceGetObjectByKey<Key, Return>
{
    Return Get(Key key);
}

public interface ITryGetObjectByKey<Key, Return>
{
    bool TryGet(Key key, out Return elem);
}
