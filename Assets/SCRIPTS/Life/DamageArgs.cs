using System;

public class DamageArgs : EventArgs
{
    public IncomeDamageInfo Income;
    public OutDamageInfo Output;

    public IOwner Owner { get { return Income.Owner; } }
    public object Source { get { return Income.Source; } }
}
