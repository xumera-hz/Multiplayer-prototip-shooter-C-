using System.Collections;
using UnityEngine;

public class DeathController : MonoBehaviour {

    [SerializeField] UIRespawn m_RespawnUI;
    [SerializeField] float m_TimeToRespawn = 4f;

    GameController m_GameControl;
    YieldInstruction m_CacheRespawnTime;
    YieldInstruction m_CacheOneSecond;

    private void Start()
    {
        m_GameControl = GameController.I;
        m_GameControl.PlayerDeathEvent += OnDeathUnit;
        m_CacheRespawnTime = new WaitForSeconds(m_TimeToRespawn);
        m_CacheOneSecond = new WaitForSeconds(1f);
    }

    void OnDeathUnit(PlayerMainControl unit, DeathArgs args)
    {
        if (unit.Player.IsMine)
        {
            StartCoroutine(WaitRespawnPlayer(unit));
        }
        else if(unit.Player.IsServer)
        {
            StartCoroutine(WaitRespawnUnit(unit));
        }
    }

    void RespawnAction(PlayerMainControl control)
    {
        control.Respawn();
    }

    void ActiveControlUI(bool state)
    {
        JoysticksManager.AttackJoystick.SetActive(state);
        JoysticksManager.MoveJoystick.SetActive(state);
    }

    IEnumerator WaitRespawnUnit(PlayerMainControl control)
    {
        yield return m_CacheRespawnTime;
        RespawnAction(control);
    }

    IEnumerator WaitRespawnPlayer(PlayerMainControl control)
    {
        ActiveControlUI(false);
        float timer = m_TimeToRespawn - 1f;
        yield return m_CacheOneSecond;
        m_RespawnUI.IsActive = true;
        while (timer > 0f)
        {
            m_RespawnUI.SetTimer(((int)timer).ToString());
            yield return m_CacheOneSecond;
            timer -= 1f;
        }
        m_RespawnUI.IsActive = false;
        RespawnAction(control);
        if(control.Player.IsConnected) ActiveControlUI(true);
    }
}
