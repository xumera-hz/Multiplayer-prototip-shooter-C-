public interface IGetObject<T>: IInstanceGetObject<T>, ITryGetObject<T> { }

public interface IInstanceGetObject<T>
{
    T Get();
}

public interface ITryGetObject<T>
{
    bool TryGet(out T elem);
}