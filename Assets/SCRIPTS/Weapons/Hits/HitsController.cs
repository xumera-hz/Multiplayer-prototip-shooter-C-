using UnityEngine;
using System.Collections.Generic;
using System;

//public enum TypeIgnoreHit {
//    None,//не игнорирует
//    AfterFirstStop,//Проверка прерывается на первом игнорируемом объекте, т.е. игнорируемый объект поглощает патроны
//    Through,//Летит насквозь через любое кол-во игнорируемых объектов
//}

/*public interface IHitDealer
{
    void AddEffect(MyEffect newEffect);
    MyEffect[] GetEffects();
}*/

/*public interface IReceiver
{
    IHitReceiver HitReceiver();
    IDamageReceiver DamageReceiver();
    IPhysicsForceReceiver PhysicsForceReceiver();
}*/

/*public interface IDealer
{
    IHitDealer HitDealer();
    IDamageDealer DamageDealer();
    IPhysicsForceDealer PhysicsForceDealer();
}*/

public interface IPhysicsForceReceiver
{
    bool CanSetForce { get; }
    void SetForce(Vector3 force, ForceMode mode);
    void SetRelativeForce(Vector3 force, ForceMode mode);
    void SetForceAtPosition(Vector3 force, Vector3 position, ForceMode mode);
    void SetExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier, ForceMode mode);
}

/*public interface IPhysicsForceDealer
{
    bool GetForce(out Vector3 force, out ForceMode mode);
    bool GetRelativeForce(out Vector3 force, out ForceMode mode);
    bool GetForceAtPosition(out Vector3 force, out Vector3 position, ForceMode mode);
    bool GetExplosionForce(out float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier, ForceMode mode);
}*/

/*public interface IHitReceiver
{
    bool CheckRangeHitAbsorbLogicImpulse(ref RaycastHit hit, ref Vector3 dir, ref float impulse);
    bool SetRangeHitAbsorbLogicImpulse(ref RaycastHit hit, ref Vector3 dir, ref float impulse);

    bool CheckMeleeHit(ref RaycastHit hit, ref Vector3 dir);
    bool SetMeleeHit(ref RaycastHit hit, ref Vector3 dir);

    bool CheckCollisionHit(Collision hit, ref Vector3 dir);
    bool SetCollisionHit(Collision hit, ref Vector3 dir);
}*/

public interface IReceiver
{
    IMeleeHitReceiver MeleeHitReceiver { get; }
    IRangeHitReceiver RangeHitReceiver { get; }
    ICollisionReceiver CollisionReceiver { get; }
    //IEffectReceiver EffectReceiver { get; }
}

public interface IMeleeHitReceiver
{
    bool SetMeleeHit(ref RaycastHit hit, ref Vector3 dir);
}

public interface IRangeHitReceiver
{
    //bool CheckRangeHitAbsorbLogicImpulse(ref RaycastHit hit, ref Vector3 dir, ref float impulse);
    //void GetTypeEffect();

    bool SetRangeHitAbsorbLogicImpulse(Vector3 pos, Vector3 dir, ref float impulse);
}

public interface ICollisionReceiver
{
    bool SetCollisionHit(Collision hit);
}


//public interface IEffectReceiver
//{
//    IOwner Owner { get; }
//    bool CanTakeEffects { get; }
//    void SetEffects(MyEffect[] effects,GameObject target);
//    void SetEffect(MyEffect effect, GameObject target);
//}

//public interface IEffectDealer
//{
//    IOwner Owner { get; }
//    MyEffect[] Effects { get; }
//}

//public enum TypeDefaultEffect { Damage, Force }

//public abstract class MyEffect: IEffectType
//{
//    public readonly static int CountTypeEffects = System.Enum.GetValues(typeof(TypeDefaultEffect)).Length;
//    public int Type;
//    public Vector3 Position;
//    public Vector3 Direction;
//    public IOwner Owner;
//    public GameObject Subject;

//    public MyEffect Set(GameObject subject, IOwner owner)
//    {
//        Subject = subject; Owner = owner;
//        return this;
//    }

//    public int EffectType { get { return Type; } }

//    public MyEffect Set(ref Vector3 pos,ref Vector3 dir)
//    {
//        Position = pos; Direction = dir;
//        return this;
//    }


//}

//public enum WeaponHitEffect { None=-1,Bullet, Rocket }

//public class DamageEffect : MyEffect
//{
//    public float Damage;
//    public float WaitTime;
//    public int TypeEffect;


//    public IncomeDamageInfo GetInfo()
//    {
//        IncomeDamageInfo dmgInfo = new IncomeDamageInfo();
//        dmgInfo.Damage = Damage;
//        //dmgInfo.ReceivedDmg = Damage;
//        dmgInfo.Source = Subject;
//        dmgInfo.Owner = Owner;
//        return dmgInfo;
//    }

//    public void SetDamage(IDamageReceiver dr)
//    {
//        if (dr == null) return;
//        if (dr.CanDamage)
//        {
//            IncomeDamageInfo dmgInfo = new IncomeDamageInfo();
//            dmgInfo.Damage = Damage;
//            //dmgInfo.ReceivedDmg = Damage;
//            dmgInfo.Source = Subject;
//            dmgInfo.Owner = Owner;
//            dr.SetDamage(dmgInfo);
//        }
//    }
//}

//public sealed class PhysicsForceEffect : MyEffect
//{
//    public float Force;
//    public ForceMode Mode;

//    public void SetForce(IPhysicsForceReceiver receiver)
//    {
//        if (receiver != null && receiver.CanSetForce)
//        {
//            receiver.SetForceAtPosition(Direction * Force, Position, Mode);
//        }
//    }
//}

public class HitsUtility : IComparer<RaycastHit>
{
    static HitsUtility()
    {
        CompareByDistance = new HitsUtility();
    }
    public static IComparer<RaycastHit> CompareByDistance;
    public int Compare(RaycastHit x, RaycastHit y) { return x.distance.CompareTo(y.distance); }
}

public struct HitInfo
{
    public RaycastHit Hit;
    public Vector3 Direction;
    public Vector3 Position;
    public float LogicImpulse;
    public GameObject Target;
    public GameObject Subject;
    public GameObject PartTarget;

    public HitInfo(GameObject subject, ref RaycastHit hit, Vector3 dir, float impulse)
    {
        Hit = hit; Direction = dir; LogicImpulse = impulse;
        Position = hit.point; Target = hit.transform.gameObject;
        Subject = subject; PartTarget = hit.collider.gameObject;
    }
}

public static class ProjectileUtility
{
    public static HitInfo GetProjectileHitInfo(this Projectile Proj, ref RaycastHit hit)
    {
        return new HitInfo(Proj.GO, ref hit, Proj.Direction, Proj.Data.Impulse);
    }
}

public class HitsController : MonoBehaviour
{
    public static event Action<HitInfo> CreateHit;

    void OnDestroy()
    {
        CreateHit = null;
    }
    //TODO: Простенько сделаем нанесение урона
    void OnCreateHit(HitInfo projHit)
    {
        if (projHit.Subject == null || projHit.Target == null) return;
        var proj = projHit.Subject.GetComponentInChildren<IProjectile>();
        if (proj == null) return;
        var receiver = projHit.Target.GetComponentInChildren<IDamageReceiver>();
        if (receiver == null) return;
        var dmg = proj.GetData.Damage;
        if (dmg == 0f) return;
        //Если надо будет узнать кто кого убил, надо протягивать Owner из Projectile Data
        var dmgInfo = new IncomeDamageInfo()
        {
            Damage = dmg,
            Owner = proj.GetData.Owner,
            Source = proj
        };
        receiver.SetDamage(dmgInfo);

    }

    void OnEnable()
    {
        Projectile.RegistryHit += projModule.OnProjectileRegistryHits;
        HitsController.CreateHit += OnCreateHit;
    }

    void OnDisable()
    {
        Projectile.RegistryHit -= projModule.OnProjectileRegistryHits;
        HitsController.CreateHit -= OnCreateHit;
    }

    public static int CompareHitsByDistance(RaycastHit x, RaycastHit y)
    {
        return x.distance.CompareTo(y.distance);
    }

    //public int Priority { get { return Info.Priority; } }
    //public bool IsUsed { get { return Info.Use; } }
    //[SerializeField]
    //PriorityInfo Info;

    // Update is called once per frame
    public void ManualUpdate()
    {
        if (projModule.IsUpdate()) projModule.ManualUpdate();
    }


    ProjectileHitModule projModule = new ProjectileHitModule();

    class ProjectileHitModule
    {
        public ProjectileHitModule()
        {
            pool = new ObjectsPool<ProjectileHitInfo>(factory, 10);
        }

        ObjectsPool<ProjectileHitInfo> pool;

        bool factory(out ProjectileHitInfo hitInfo)
        {
            hitInfo = new ProjectileHitInfo();
            return hitInfo != null;
        }

        class ProjectileHitInfo
        {
            Projectile Proj;
            CastHitsInfo HitsInfo;

            static bool log = false;

            public ProjectileHitInfo Init(Projectile proj, CastHitsInfo hitsInfo)
            {
                Proj = proj; HitsInfo = hitsInfo;
                return this;
            }

            public void Set()
            {
#if UNITY_EDITOR
                if (log) Debug.LogError("Выстрел");
#endif
                //TODO: игнорируем проверку попадания на стороне клиента
                //заменить на ConnectInfo
                if (!ConnectController.IsServer) return;


                int count = HitsInfo.Count;
                if (count <= 0)
                {
#if UNITY_EDITOR
                    if (log) Debug.LogError("Нет попаданий");
#endif
                    return;
                }
                var hits = HitsInfo.Hits;
                if (count > 1) Array.Sort(hits, 0, count, HitsUtility.CompareByDistance);
#if UNITY_EDITOR
                //Debug.Log("First="+HitsInfo.Hits[0].distance.CompareTo(HitsInfo.Hits[1].distance));
                //if(HitsInfo.Hits.Length>1) Debug.Log("Last="+HitsInfo.Hits[HitsInfo.Hits.Length-2].distance.CompareTo(HitsInfo.Hits[HitsInfo.Hits.Length-1].distance));
#endif
                IReceiver iHit;
                IRangeHitReceiver receiver;
                Transform cldTF;
                Transform mainTF;
                Collider cld;
                for (int i = 0; i < count; i++)
                {
                    if (Proj.EndCheckHit)
                    {
#if UNITY_EDITOR
                        if (log) Debug.LogError("Патрон исчерпал свой лимит, break");
#endif
                        break;
                    }
                    //Debug.LogError("Iter=" + i);
                    cld = hits[i].collider;
                    cldTF = cld.transform;
                    mainTF = hits[i].transform;
#if UNITY_EDITOR
                    //Debug.DrawRay(hits[i].point, hits[i].normal * 15f+new Vector3(0f,0.01f,0f), Color.green, 5f);
#endif
                    var owner = Proj.Data.Owner;
                    if (owner != null && (owner.CheckIgnoreObject(cldTF) || owner.CheckIgnoreObject(mainTF)))
                    {
#if UNITY_EDITOR
                        if (log) Debug.LogError("CheckIgnoreObject is true");
#endif
                        continue;
                    }
                    iHit = mainTF.GetComponentInChildren<IReceiver>();
                    float impulse = Proj.Data.Impulse;
                    if (iHit == null)
                    {
                        //попал во что-то плотное
                        if (!cld.isTrigger)
                        {
                            if (SetCommonHit(ref hits[i], Proj.Direction, ref impulse)) Proj.SetHit(impulse, hits[i].point);
                            else Proj.EndCheckHit = true;
                            if (Proj.EndCheckHit) break;
                        }
#if UNITY_EDITOR
                        if (log) Debug.LogError("IReceiver is null on " + mainTF.gameObject);
#endif
                        continue;
                    }
                    receiver = iHit.RangeHitReceiver;
                    if (receiver == null)
                    {
#if UNITY_EDITOR
                        if (log) Debug.LogError("RangeHitReceiver is null on " + mainTF.gameObject);
#endif
                        continue;
                    }

                    if (receiver.SetRangeHitAbsorbLogicImpulse(hits[i].point, Proj.Direction, ref impulse))
                    {
#if UNITY_EDITOR
                        if(log) Debug.LogError("Попал");
#endif
                        Proj.SetHit(impulse, hits[i].point);
                        if (CreateHit != null) CreateHit(Proj.GetProjectileHitInfo(ref hits[i]));
                        if (Proj.EndCheckHit) break;
                    }
                    else
                    {
#if UNITY_EDITOR
                        if (log) Debug.LogError("RangeHitReceiver не смог обработать патрон " + mainTF.gameObject);
#endif
                    }
                }
            }

            public ProjectileHitInfo() { }
            public ProjectileHitInfo(Projectile proj, CastHitsInfo hitsInfo)
            {
                Proj = proj; HitsInfo = hitsInfo;
            }

            bool SetCommonHit(ref RaycastHit hit, Vector3 dir, ref float impulse)
            {
                //отсекание какое-нибудь сделать по тэгам
                impulse = 0f;
                if (Proj.Data.VisualHitIgnoreOnDistance)
                {
                    if (hit.distance < Proj.Data.VisualHitDistance)
                    {
                        BulletHitPoolController.Static_CreateProjectileHitEffect(hit, dir);
                    }
                }
                return true;
            }

        }

        List<ProjectileHitInfo> hits = new List<ProjectileHitInfo>(100);
        public bool IsUpdate() { return hits.Count > 0; }
        public void ManualUpdate()
        {
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                hits[i].Set();
                pool.Return(hits[i]);
            }
            hits.Clear();
        }

        public void OnProjectileRegistryHits(Projectile proj, CastHitsInfo hitsInfo)
        {
            if (proj == null || hitsInfo.Count == 0) return;
            //Мгновенная обработка попадания
            if (proj.Data.Instantly)
            {
                var hitInfo = pool.Get();
                hitInfo.Init(proj, hitsInfo).Set();
                pool.Return(hitInfo);
            }
            else hits.Add(pool.Get().Init(proj, hitsInfo));
        }
    }

}
