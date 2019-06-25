using UnityEngine;

public interface IType
{
    int TYPE { get; }
}
public interface ISubType
{
    int SUBTYPE { get; }
}
public interface IIdentifier : IType, ISubType { }

[System.Serializable]
public class IdentifierClass : IIdentifier
{
    //public int[] Array_ID;
    public IdentifierClass(/*int count = 0*/)
    {
        //Array_ID = new int[count];
    }
    public IdentifierClass(int id, int type, string _name/*, int count=0*/)
    {
        ID = id; Type = type; Name = _name;
        //Array_ID = new int[count];
    }
    public IdentifierClass(IdentifierClass ic) { Clone(ic); }

    public void Clone(IdentifierClass ic)
    {
        ID = ic.ID; Type = ic.Type; Name = ic.Name;
        DisplayName = ic.DisplayName;
        //Array_ID = new int[ic.Array_ID.Length];
        //for (int i = 0; i < Array_ID.Length; i++) Array_ID[i] = ic.Array_ID[i];
    }
    public string Name;
    public string DisplayName;
    public int ID, Type;
    public int SUBTYPE { get { return Type; } }
    public int TYPE { get { return ID; } }

    public bool Check(int category, int kind)
    {
        return Type == category && ID == kind;
    }
}
