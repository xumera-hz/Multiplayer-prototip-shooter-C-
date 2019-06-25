using System;
using System.Runtime.InteropServices;

[Serializable]
public struct IntValue
{
    public int Value;
    public int Max;
    public int Min;

    //public int Random { get { return Value + MasterScript.rand.Next(Min, Max); } }

    public float RatioValueMax { get { return Max < 1e-5f ? 0f : ((float)Value) / Max; } }

    public bool IsMax { get { return Value == Max; } }

    public void Set(int value)
    {
        if (value < Min) value = Min; else if (value > Max) value = Max;
        Value = value;
    }

    public bool Set(ref IntValue info)
    {
        int value = info.Value;
        bool res = value != Value || info.Max != Max || info.Min != Min;
        if (res)
        {
            Max = info.Max;
            Min = info.Min;
            Set(value);
        }
        return res;
    }
}

[Serializable]
public struct FloatRange
{
    public float Max;
    public float Min;
}

[Serializable]
public struct FloatValue
{
    public float Value;
    public float Max;
    public float Min;
    const float epsEquals = 1e-5f;
    const float negEpsEquals = -1e-5f;

    public override string ToString()
    {
        return string.Format("Health: value={0} Min={1} Max={2}", Value, Min, Max);
    }

    public float RatioValueMinMax { get { return Clamp01((Max - Min) < epsEquals ? 0f : (Value - Min) / (Max - Min)); } }
    public float RatioValueZeroMax { get { return Clamp01(Max < epsEquals ? 0f : Value / Max); } }
    //public float RatioValueMax { get { return Max < epsEquals ? 0f : Value / Max; } }

    public bool IsMax { get { return (Value - Max) < epsEquals && (Value - Max) > negEpsEquals; } }
    public bool IsMin { get { return (Value - Min) < epsEquals && (Value - Min) > negEpsEquals; } }

    public void Set(float value)
    {
        Value = Clamp(value);
    }

    public float Clamp(float value)
    {
        if (value < Min) value = Min; else if (value > Max) value = Max;
        return value;
    }

    public bool PercentLessOrEqualValue(float percentAbs)
    {
        percentAbs = Math.Abs(percentAbs) * 0.01f;
        float delta = Math.Abs(Max - Min);
        float val = delta * percentAbs;
        return val >= Value || ValuesEqual(val, Value);
    }

    public float Clamp01(float value)
    {
        if (value < 0f) value = 0f; else if (value > 1f) value = 1f;
        return value;
    }

    public float InverseClamp01(float value)
    {
        return 1f - Clamp01(value);
    }

    public float ClosestClamp01(float value)
    {
        if (value < Min) return 1f;
        if (value > Max) return 0f;
        float delta = Max - Min;
        if (ValuesEqual(delta, 0f))
        {
            if (ValuesEqual(value, 0f)) return 1f;
            return 0f;
        }
        return (Max - value) / (Max - Min);
    }


    bool ValuesEqual(float val1, float val2)
    {
        return (val1 - val2) <= epsEquals && (val1 - val2) >= negEpsEquals;
    }

    public bool ValueEqual(float value)
    {
        return ValuesEqual(value, Value);
    }

    public static implicit operator float(FloatValue value)
    {
        return value.Value;
    }

    public float Normalize()
    {
        float delta = Max - Min;
        if (delta < epsEquals && epsEquals > negEpsEquals) return 0f;
        return Value / delta;
    }

    public bool Set(ref FloatValue info)
    {
        float value = info.Value;
        bool res =
        (value - Value) >= epsEquals || (value - Value) <= negEpsEquals ||
        (info.Max - Max) >= epsEquals || (info.Max - Max) <= negEpsEquals ||
        (info.Min - Min) >= epsEquals || (info.Min - Min) <= negEpsEquals;
        if (res)
        {
            Max = info.Max;
            Min = info.Min;
            Set(value);
        }
        return res;
    }
}
