using UnityEngine;
using System;


public interface IHealth
{
    FloatValue HealthPoints { get; }
    void SetMaxHealth();
    void SetMinHealth();
    void SetValueHealth(float value);

}

public interface IArmor
{
    FloatValue ArmorPoints { get; }
    void SetMaxArmor();
    void SetMinArmor();
    void SetValueArmor(float value);
}

public interface ILife : IHealth, IArmor
{
    void SetHealth(float hp = -1f, float min = -1f, float max = -1f);
    void SetArmor(float armor = -1f, float min = -1f, float max = -1f);
    bool Lived { get; }
}

//public interface ILife : IHealth, IArmor
//{
//    //bool Lived { get; }
//    event EventHandler<LifeArgs> Change;
//    bool GetPoints(int id, out ValueInfo points);
//    bool SetPoints(int id, ref ValueInfo points);
//}
