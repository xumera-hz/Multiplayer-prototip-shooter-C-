
public class ProjectileData
{
    public const float DefaultLifeTime = 10f;
    public int TypeProjectile;
    public float Speed;
    public float Impulse;
    public float CurrentImpulse;
    public float MaxDistance;
    public float LifeTime;
    //public float Radius;
    public bool Instantly;
    public int TypeWeapon;
    public int SubtypeWeapon;
    public float Damage;

    //Для оптимизации
    public bool VisualHitIgnoreOnDistance = true;//включает игнор на расстоянии
    public float VisualHitDistance = 50f;//само расстояние


    public IOwner Owner;

    public void Clone(ProjectileData data)
    {
        TypeProjectile = data.TypeProjectile;
        Speed = data.Speed;
        Impulse = data.Impulse;
        CurrentImpulse = data.CurrentImpulse;
        MaxDistance = data.MaxDistance;
        LifeTime = data.LifeTime;
        Instantly = data.Instantly;
        TypeWeapon = data.TypeWeapon;
        SubtypeWeapon = data.SubtypeWeapon;
        Damage = data.Damage;
        VisualHitIgnoreOnDistance = data.VisualHitIgnoreOnDistance;
        VisualHitDistance = data.VisualHitDistance;
        Owner = data.Owner;
    }

    public ProjectileData() { }

    public ProjectileData(int type, float speed, float impulse, float dist, bool instant = false)
    {
        Init(type, speed, impulse, dist, instant);
    }

    public ProjectileData(WeaponInfo info, bool instant = false)
    {
        Init(info, instant);
    }

    public void Init(int type, float speed, float impulse, float dist, bool instant = false)
    {
        TypeProjectile = type; Speed = speed; Impulse = impulse;
        MaxDistance = dist;
        LifeTime = speed > 1e-5f ? (dist / speed) : DefaultLifeTime;
        Instantly = instant;
        TypeWeapon = SubtypeWeapon = -1;
    }

    public void Init(WeaponInfo info, bool instant = false)
    {
        TypeProjectile = info.TypeProjectile; Speed = info.SpeedProj; Impulse = info.Impulse;
        MaxDistance = info.MaxDistance;
        LifeTime = info.SpeedProj > 1e-5f ? (info.MaxDistance / info.SpeedProj) : DefaultLifeTime;
        Instantly = instant;
        TypeWeapon = info.ID;
        SubtypeWeapon = info.Type;
        float count = info.ProjsPerShot > 0 ? (float)info.ProjsPerShot : 1f;
        Damage = info.Damage / count;
        //Debug.LogError("TypeProjectile=" + (ProjectileType)TypeProjectile);
        //Debug.LogError("Damage=" + Damage);
    }
}
