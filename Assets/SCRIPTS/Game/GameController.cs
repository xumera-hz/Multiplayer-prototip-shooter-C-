using System.Collections.Generic;
using UnityEngine;
using System;

public enum TypePlayer { Self, Ally, Enemy }

public class GameController : MonoSingleton<GameController>
{
    [SerializeField] PlayerController m_PlayerControl;
    [SerializeField] ManagerSpawnPoints m_SpawnControl;
    [SerializeField] InvisibleController m_InvisibleControl;
    [SerializeField] GrassController m_GrassControl;
    [SerializeField] DeathmatchMode m_Mode;

    public PlayerController PlayerControl { get { return m_PlayerControl; } }

    List<PlayerMainControl> m_PlayerControls = new List<PlayerMainControl>(10);

    #region Add_Remove_Unit

    public UnitContainer AddUnit(Player player, TypePlayer typePlayer, TypeUnit typeUnit)
    {
        return AddUnit(player, typePlayer, ManagerUnits.CreateUnit(typeUnit));
    }

    public UnitContainer AddUnit(Player player, TypePlayer typePlayer)
    {
        return AddUnit(player, typePlayer, ManagerUnits.CreateRandom());
    }

    public UnitContainer AddUnit(Player player, TypePlayer typePlayer, GameObject unitObject)
    {
        var pl = GetPlayerControl(player.ID);
        if (pl != null) return pl.Unit;
        var control = Create(player, typePlayer, unitObject.GetComponent<UnitContainer>());
        m_PlayerControls.Add(control);
        //unit.Init();
        ExternInit(control);
        return control.Unit;
    }

    #endregion

    #region Misc

    public void RemoveUnit(Player player)
    {
        for (int i = m_PlayerControls.Count - 1; i >= 0; i--)
        {
            var pl = m_PlayerControls[i];
            if (pl.Player == player)
            {
                pl.Destroy();
                ExternRemove(pl);
                m_PlayerControls.RemoveAt(i);
                break;
            }
        }
    }

    Transform FindClosestTarget(Transform target)
    {
        if (target == null) return null;
        float minDist = float.MaxValue;
        Transform outTarget = null;
        var pos = target.position;
        for (int i = m_PlayerControls.Count - 1; i >= 0; i--)
        {
            var control = m_PlayerControls[i];
            if (!control.Player.IsConnected || control.Player.IsMine) continue;
            var unit = control.Unit;
            if (!unit.LifeControl.Lived || unit.VisibleControl.Invisible) continue;
            var pos2 = unit.TF.position;
            float sqr = (pos - pos2).sqrMagnitude;
            if (sqr < minDist)
            {
                minDist = sqr;
                outTarget = unit.TF;
            }
        }
        return outTarget;
    }

    void ExternInit(PlayerMainControl control)
    {
        control.DeathUnit += OnDeathUnit;
        control.SpawnPosition += m_SpawnControl.GetRandomRespawnPoint;
        var unit = control.Unit;
        var player = control.Player;
        m_Mode.Add(player);

        //TODO: костыльки
        //Передаем изменение статистики по убийствам
        var unitControl = player.Controller as UnitNetworkController;
        if (unitControl != null)
        {
            if (player.IsServer) unitControl.GetKills += (arg) => { return m_Mode.CountKills(GetPlayer(arg)); };
            else unitControl.SetKills += (arg, value) => { m_Mode.SetKills(GetPlayer(arg), value); };
        }

        if (player.IsMine)
        {
            m_InvisibleControl.SetMainTarget(unit.GO);
            var playerControl = player.Controller as PlayerController;
            if (playerControl != null)
            {
                //Подбор цели для авто-атаки
                playerControl.ClosestAttackTarget += FindClosestTarget;
            }
        }
        else m_InvisibleControl.AddTarget(unit.GO);
        m_GrassControl.AddTarget(unit.TF, player.IsMine);
    }

    void ExternRemove(PlayerMainControl control)
    {
        var player = control.Player;
        m_Mode.Remove(player);
    }

    #endregion

    #region Getters

    public Player GetPlayer(GameObject go)
    {
        if (go == null) return null;
        return GetPlayer(go.GetComponentInChildren<UnitContainer>(true));
    }

    public Player GetPlayer(UnitContainer unit)
    {
        if (unit.IsNullOrDestroy()) return null;
        var control = GetPlayerControl(unit);
        return control == null ? null : control.Player;
    }

    public PlayerMainControl GetPlayerControl(UnitContainer unit)
    {
        if (unit.IsNullOrDestroy()) return null;
        for (int i = m_PlayerControls.Count - 1; i >= 0; i--)
        {
            if (m_PlayerControls[i].Unit == unit) return m_PlayerControls[i];
        }
        return null;
    }

    public PlayerMainControl GetPlayerControl(GameObject go)
    {
        if (go == null) return null;
        return GetPlayerControl(go.GetComponentInChildren<UnitContainer>(true));
    }

    public PlayerMainControl GetPlayerControl(string ID)
    {
        if (string.IsNullOrEmpty(ID)) return null;
        for (int i = m_PlayerControls.Count - 1; i >= 0; i--)
        {
            if (m_PlayerControls[i].Player.ID == ID) return m_PlayerControls[i];
        }
        return null;
    }

    #endregion

    #region Death_Kill_Events

    public struct PlayerKillArgs
    {
        public Player Killer { get { return m_Killer.Player; } }
        public Player Victim { get { return m_Victim.Player; } }

        PlayerMainControl m_Killer;
        PlayerMainControl m_Victim;

        public PlayerKillArgs(PlayerMainControl killer, PlayerMainControl victim)
        {
            m_Killer = killer;
            m_Victim = victim;
        }
    }

    public event Action<PlayerMainControl, DeathArgs> PlayerDeathEvent;
    public event Action<PlayerKillArgs> PlayerKillEvent;

    void OnDeathUnit(PlayerMainControl unit, DeathArgs args)
    {
        if (PlayerDeathEvent != null) PlayerDeathEvent(unit, args);

        if (PlayerKillEvent != null)
        {
            var killer = GetPlayerControl(args.Killer);
            PlayerKillEvent(new PlayerKillArgs(killer, unit));
        }
    }

    #endregion

    private void Update()
    {
        for (int i = m_PlayerControls.Count - 1; i >= 0; i--)
        {
            m_PlayerControls[i].Update();
        }
    }

    PlayerMainControl Create(Player player, TypePlayer type, UnitContainer unit)
    {
        return new PlayerMainControl(player, unit, ManagerUnitUI.GetUnitUI(), ManagerUnitUI.GetPersistentDataUI(type));
    }
}

public class PlayerMainControl
{
    TypePlayer m_TypePlayer;
    UnitContainer m_Unit;
    Player m_Player;
    UIControl m_UI;
    bool m_IsInit;

    public void Destroy()
    {
        DeathUnit = null;
        m_Unit.DestroyGO();
        m_UI.Destroy();
    }

    public UnitContainer Unit { get { return m_Unit; } }
    public Player Player { get { return m_Player; } }

    public event Action<PlayerMainControl, DeathArgs> DeathUnit;

    public event Func<Vector3> SpawnPosition;

    public PlayerMainControl(Player pl, UnitContainer Unit, UIUnitInfo unitUI, PersistentDataUnitUI dataUI)
    {
        m_Player = pl;
        m_Unit = Unit;
        m_UI = new UIControl(unitUI, dataUI);
        m_UI.Target = () => { return m_Unit.TF; };
    }

    public void Update()
    {
        Init();
        if (m_TypePlayer == TypePlayer.Enemy)
        {
            m_UI.IsActive = !m_Unit.VisibleControl.Invisible;
        }
        m_UI.Update();
    }

    public void Respawn()
    {
        if (!Player.IsConnected) return;
        if (Player.IsServer)
        {
            Unit.LifeControl.SetDefault();
            SetSpawnPosition();
        }
    }

    void SetSpawnPosition()
    {
        if (SpawnPosition == null) return;
        m_Unit.TF.position = SpawnPosition();
        //m_Unit.MoveControl.Apply();
    }

    public void Init()
    {
        if (m_IsInit) return;
        m_UI.Init(m_Player.PlayerName);
        OnChangeHealth(m_Unit.LifeControl.HealthPoints);
        if (m_Player.IsMine) OnChangeAmmo(m_Unit.WeaponControl.Weapon.Info.Ammo);
        SetState(1);
        //if(m_Player.IsServer)
        SetSpawnPosition();
        m_IsInit = true;
    }

    void SetState(int state)
    {
        if (m_Unit.IsNullOrDestroy())
        {
            Debug.LogError(GetType() + " error: SetState unit is null");
            return;
        }
        if (state == 1)
        {
            m_Unit.LifeControl.ChangeHealth += OnChangeHealth;
            m_Unit.LifeControl.DeathEvent += OnDeath;
            m_Unit.LifeControl.ResurrectionEvent += OnResurrectionEvent;
            m_Unit.VisibleControl.ChangeVisibleState += OnChangeVisibleState;
            if (m_Player.IsMine) m_Unit.WeaponControl.Weapon.Info.ChangeAmmo += OnChangeAmmo;
            else m_UI.ActiveAmmoInfo(false);
        }
        else if(state == 0)
        {
            m_Unit.LifeControl.ChangeHealth -= OnChangeHealth;
            m_Unit.LifeControl.DeathEvent -= OnDeath;
            m_Unit.LifeControl.ResurrectionEvent -= OnResurrectionEvent;
            m_Unit.VisibleControl.ChangeVisibleState -= OnChangeVisibleState;
            if (m_Player.IsMine) m_Unit.WeaponControl.Weapon.Info.ChangeAmmo -= OnChangeAmmo;
        }
    }

    #region Handlers

    void OnChangeVisibleState(UnitVisible.TypeVisible state)
    {
        if (m_Unit.LifeControl.Lived)
        {
            m_UI.IsActive = state != UnitVisible.TypeVisible.Invisible;
        }
    }

    void OnChangeAmmo(AmmoInfo ammo)
    {
        m_UI.OnChangeAmmo(ammo);
    }

    void OnResurrectionEvent(Component sender, ResurrectionArgs args)
    {
        m_UI.OnRessurect();
    }

    void OnDeath(Component sender, DeathArgs args)
    {
        m_UI.OnDeath();
        if (DeathUnit != null) DeathUnit(this, args);
    }

    void OnChangeHealth(FloatValue hp)
    {
        m_UI.OnChangeHealth(hp);
    }

    #endregion

    #region UI

    class UIControl
    {

        UIUnitInfo m_UnitUI;
        PersistentDataUnitUI m_UnitUIData;

        public UIControl(UIUnitInfo unitUI, PersistentDataUnitUI dataUI)
        {
            m_UnitUI = unitUI;
            m_UnitUIData = dataUI;
        }

        public void Destroy()
        {
            m_UnitUI.DestroyGO();
        }

        Vector3 WorldToScreenPoint(Vector3 position)
        {
            return ManagerCameras.GetMainCamera().WorldToScreenPoint(position);
        }

        Vector3 GetUIPosition(Vector3 pos, Vector3 offset)
        {
            return WorldToScreenPoint(pos + offset);
        }
        Func<Transform> m_Target;
        public Func<Transform> Target
        {
            set { m_Target = value; }
        }

        Vector3 GetUIPosition()
        {
            return GetUIPosition(m_Target().position, Offset);
        }
        //TODO:заменить на вычисляемый
        static Vector3 Offset = Vector3.up * 3f;

        public void Init(string name)
        {
            IsActive = true;
            m_UnitUI.SetName(name, m_UnitUIData.ColorName);
        }

        public void ActiveAmmoInfo(bool state)
        {
            m_UnitUI.ActiveAmmoInfo(state);
        }

        public void OnChangeAmmo(AmmoInfo ammo)
        {
            m_UnitUI.SetCountAmmo(ammo.CurCountAmmoInCage);
        }

        public bool IsActive { get { return m_UnitUI.IsActive; } set { m_UnitUI.IsActive = value; } }

        public void OnDeath()
        {
            IsActive = false;
        }

        public void OnRessurect()
        {
            IsActive = true;
        }

        public void OnChangeHealth(FloatValue hp)
        {
            float ratio = hp.RatioValueMinMax;
            m_UnitUI.SetSlider((int)hp.Value, ratio, m_UnitUIData.GetColorByValue(ratio));
        }

        public void Update()
        {
            if (!IsActive) return;
            m_UnitUI.SetPosition(GetUIPosition());
        }
    }

    #endregion
}
