using System.Collections.Generic;

public interface IFactoryElement<T>
{
    bool CreateElement(out T elem);
}

public interface IFactoryByKey<T1,T2>
{
    bool TryCreate(T1 id, out T2 elem);
    T2 Create(T1 id);
    T1[] GetKeys();

}

