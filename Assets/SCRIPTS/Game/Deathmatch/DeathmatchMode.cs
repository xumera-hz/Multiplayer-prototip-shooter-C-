using System.Collections.Generic;
using UnityEngine;
using System;

public class DeathmatchMode : MonoBehaviour {

    #region Statistics
    [SerializeField] Statistics m_Statistics;

    [Serializable]
    class Statistics
    {
        [SerializeField] UIStatisticsDeathmatchMode m_UIStatistics;

        List<StatisticsPlayer> m_StatisticsMatch = new List<StatisticsPlayer>(GameConstants.MAX_HITS_FOR_ONE_PLAYER);

        public int CountKills(Player player)
        {
            StatisticsPlayer elem;
            if (!TryGetValue(player, out elem)) return -1;
            return elem.CountKills;
        }

        public void Add(Player player)
        {
            StatisticsPlayer elem;
            if (TryGetValue(player, out elem)) return;
            elem = new StatisticsPlayer(player);
            m_StatisticsMatch.Add(elem);
            OnEventPlayer(player, true);
        }
        public void Remove(Player player)
        {
            StatisticsPlayer elem;
            int index;
            if (!TryGetValue(player, out elem, out index)) return;
            m_StatisticsMatch.RemoveAt(index);
            m_UIStatistics.Remove(index);
            OnEventPlayer(player, false);
        }

        bool TryGetValue(Player pl, out StatisticsPlayer elem, out int index)
        {
            for (int i = m_StatisticsMatch.Count - 1; i >= 0; i--)
            {
                if (m_StatisticsMatch[i].Player == pl)
                {
                    elem = m_StatisticsMatch[i];
                    index = i;
                    return true;
                }
            }
            elem = null;
            index = -1;
            return false;
        }

        bool TryGetValue(Player pl, out StatisticsPlayer elem)
        {
            int index = -1;
            return TryGetValue(pl, out elem, out index);
        }

        public void AddKills(Player player, int count)
        {
            if (count == 0) return;
            StatisticsPlayer elem;
            int index = -1;
            if (!TryGetValue(player, out elem, out index)) return;
            elem.CountKills += count;
            Sort(index, elem);
        }

        public void SetKills(Player player, int count)
        {
            StatisticsPlayer elem;
            int index = -1;
            if (!TryGetValue(player, out elem, out index)) return;
            if (elem.CountKills == count) return;
            elem.CountKills = count;
            Sort(index, elem);
        }

        void Sort(int ind, StatisticsPlayer elem)
        {
            if (ind == -1) return;
            int count = elem.CountKills;
            int indStart = ind;
            while (ind > 0)
            {
                var el = m_StatisticsMatch[ind - 1];
                if (el.CountKills < count)
                {
                    ind--;
                    continue;
                }
                break;
            }
            m_UIStatistics.ChangeCount(indStart, count);
            if (indStart != ind)
            {
                m_StatisticsMatch.RemoveAt(indStart);
                m_StatisticsMatch.Insert(ind, elem);
                m_UIStatistics.ChangeIndex(indStart, ind);
            }
        }

        void OnEventPlayer(Player player, bool added)
        {
            if (added)
            {
                var elem = m_UIStatistics.Add();
                elem.SetName(player.PlayerName, player.IsMine);
                elem.SetCount("0");
            }
        }


        public void Init()
        {
            m_UIStatistics.Activity -= OnActivity;
            m_UIStatistics.Activity += OnActivity;
        }

        void OnActivity(bool state)
        {
            if (!state) return;
            //TODO: добавить изменение UI тока при ее активации
        }

        [Serializable]
        class StatisticsPlayer
        {
            public StatisticsPlayer(Player player)
            {
                Player = player;
            }

            public Player Player { get; private set; }
            public string GetName() { return Player.PlayerName; }
            public int CountKills { get; set; }

            public override bool Equals(object obj)
            {
                return (obj is StatisticsPlayer) && Player == (obj as StatisticsPlayer).Player;
            }

            public override int GetHashCode()
            {
                return Player.ID.GetHashCode();
            }

        }

    }

    public void Add(Player player) { m_Statistics.Add(player); }
    public void Remove(Player player) { m_Statistics.Remove(player); }
    public int CountKills(Player player) { return m_Statistics.CountKills(player); }
    public void SetKills(Player player, int count) { m_Statistics.SetKills(player, count); }

    #endregion


    private void Start()
    {
        m_Statistics.Init();
        //TODO: сделать хитрее отключение подсчета убийств на клиенте
        if (!ConnectController.IsServer) return;
        if (GameController.Can)
        {
            GameController.I.PlayerKillEvent += OnKillEvent;
        }
    }

    void OnKillEvent(GameController.PlayerKillArgs args)
    {
        m_Statistics.AddKills(args.Killer, 1);
    }

    void OnDeathUnit(PlayerMainControl control, DeathArgs args)
    {
    }



}
