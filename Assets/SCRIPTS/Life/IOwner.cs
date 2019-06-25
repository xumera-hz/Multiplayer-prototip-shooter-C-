using UnityEngine;

public enum TypeOwner { Character }

public interface IOwner {

    TypeOwner GetTypeOwner { get; }
    bool CheckIgnoreObject(Transform obj);
    GameObject GameObj { get; }
    IAnchor Anchor { get; }
}
