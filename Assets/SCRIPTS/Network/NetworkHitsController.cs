using UnityEngine;
using System.Runtime.InteropServices;
using System;
using LiteNetLib.Utils;
using System.Collections.Generic;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NetworkHitData
{
    //public const int MAX_LENGTH_PLAYER_ID = GameConstants.MAX_LENGTH_PLAYER_ID;
    //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_LENGTH_PLAYER_ID)]
    //public string ObjectPlayerID;//над кем

    public byte IndexSubjectPlayer;//кто попал
    public byte IndexObjectPlayer;//в кого попали
    public Vector3 Pos;
    public Vector3 Dir;
    //public Vector3 ReflectDir;

    public override string ToString()
    {
        return IndexSubjectPlayer + " попал в " + IndexObjectPlayer;
    }

    #region Base

    public static readonly int SIZE = Marshal.SizeOf(typeof(NetworkHitData));
    public unsafe bool UnsafeCopyTo(byte[] array, int offset = 0)
    {
        var t = this;
        byte* p1 = (byte*)&t;
        return array.CopyTo(p1, SIZE, offset);
    }

    public unsafe bool UnsafeCopyFrom(byte[] array, int offset = 0)
    {
        var t = this;
        byte* p1 = (byte*)&t;
        bool res = array.CopyFrom(p1, SIZE, offset);
        if (res) this = t;
        return res;
    }

    #endregion
}

public class NetworkHits
{

    //public const int MAX_HITS = GameConstants.MAX_HITS_FOR_PLAYERS;
    //public const int MAX_LENGTH_PLAYER_ID = GameConstants.MAX_LENGTH_PLAYER_ID;

    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_HITS)]
    public List<NetworkHitData> Hits = new List<NetworkHitData>(GameConstants.MAX_HITS_FOR_ONE_PLAYER);

    //[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = MAX_LENGTH_PLAYER_ID * MAX_HITS)]
    public List<string> PlayerIDs = new List<string>(GameConstants.MAX_PLAYERS_IN_GAME);

    public void Clear()
    {
        Hits.Clear();
        PlayerIDs.Clear();
    }

    #region Pool

    static ObjectsPool<NetworkHits> m_PoolHits = new ObjectsPool<NetworkHits>((out NetworkHits arg) => { arg = new NetworkHits(); return true; }, 128);

    public static NetworkHits Get()
    {
        return m_PoolHits.Get();
    }

    public static void Return(NetworkHits hits)
    {
        hits.Clear();
        m_PoolHits.Return(hits);
    }

    #endregion

    #region ReadWrite

    /// <summary>
    /// Кладет структуру в Writer(обертка над массивом байт)
    /// </summary>
    public bool PutInWriter(NetDataWriter writer, bool reset = false)
    {
        int startLen = writer.Length;
        try
        {
            if (reset) writer.Reset();
            int count = Hits == null ? 0 : Hits.Count;
            if(count <= 0)
            {
                writer.Put(0);
                writer.Put(0);
                return true;
            }
            int count2 = PlayerIDs == null ? 0 : PlayerIDs.Count;
            int lenIDs = PlayerIDs.GetLen();
            if (count2 <= 0 || lenIDs <= -1)
            {
                writer.Put(0);
                writer.Put(0);
                return true;
            }
            writer.Put(count);
            int startP = writer.Length;
            int size = NetworkHitData.SIZE;
            int offset = 0;
            writer.ResizeIfNeed(startP + size * count + lenIDs);
            var bytes = writer.Data;
            for (int i = 0; i < count; i++)
            {
                if(!Hits[i].UnsafeCopyTo(bytes, startP + offset))
                {
                    throw new Exception("Не смог записать данные о " + typeof(NetworkHitData));
                }
                offset += size;
            }
            writer.AddOffset(offset);
            writer.Put(count2);
            startP = writer.Length;
            offset = 0;
            for (int i = 0; i < count2; i++)
            {
                var str = PlayerIDs[i];
                int res = str.ToBytes(bytes, startP + offset);
                if (res == -1)
                {
                    throw new Exception("Не смог записать данные о PlayerIDs");
                }
                offset += res;
            }
            writer.AddOffset(offset);

            #region OLD
            //if (count2 > 0)
            //{
            //    int res = PlayerIDs.ToBytes(bytes, startP + offset);
            //    if (res == -1 || res != lenIDs)
            //    {
            //        throw new Exception("Не смог записать данные о PlayerIDs");
            //    }
            //    offset += lenIDs;
            //    writer.AddOffset(offset);
            //}
            //else
            //{
            //    writer.Put(0);
            //}
            #endregion

            return true;
        }
        catch (Exception e) { Debug.LogError("PutInWriter is bad=" + e); }
        writer.Length = startLen;
        return false;
    }
    /// <summary>
    /// Считываем структуру в Reader(обертка над массивом байт)
    /// </summary>
    public bool FromReader(NetDataReader reader)
    {
        int startPos = reader.Position;
        try
        {
            int count = reader.GetInt();
            if (count <= 0)
            {
                reader.GetInt();
                return true;
            }
            //var hits = new NetworkHitData[count];
            var bytes = reader.RawData;
            int startP = reader.Position;
            int size = NetworkHitData.SIZE;
            int offset = 0;
            for (int i = 0; i < count; i++)
            {
                var data = new NetworkHitData();
                if (!data.UnsafeCopyFrom(bytes, startP + offset))
                {
                    throw new Exception("Не смог считать данные о " + typeof(NetworkHitData));
                }
                Hits.Add(data);
                //hits[i] = data;
                offset += size;
            }
            reader.AddOffset(offset);
            int count2 = reader.GetInt();
            startP = reader.Position;
            offset = 0;
            for (int i = 0; i < count2; i++)
            {
                string str;
                int res = bytes.ToString(startP + offset, out str);
                if (res == -1)
                {
                    throw new Exception("Не смог считать данные о PlayerIDs");
                }
                offset += res;
                PlayerIDs.Add(str);
            }

            /*int lenArray;
            string[] outStrs = null;
            int res = bytes.ToStringArray(startP + offset, ref outStrs, out lenArray);
            if(res == -1)
            {
                throw new Exception("Не смог считать данные о PlayerIDs");
            }
            offset += res;*/

            reader.AddOffset(offset);
            //Hits = hits;
            //PlayerIDs = outStrs;
            return true;
        }
        catch (Exception e) { Debug.LogError("FromReader is bad=" + e); }
        reader.Position = startPos;
        Clear();
        return false;
    }

    #endregion
}

public class NetworkHitsController : MonoSingleton<NetworkHitsController>
{

    private void Start()
    {
        HitsController.CreateHit += OnHit;
        m_CurrentData = NetworkHits.Get();
    }
    protected override void Destroy()
    {
        HitsController.CreateHit -= OnHit;
    }

    //private void Update()
    //{
    //    if (m_CurrentData != null)
    //    {
    //        Accept(m_CurrentData);
    //        NetworkHits.Return(m_CurrentData);
    //    }
    //}

    List<NetworkHits> m_ActualData = new List<NetworkHits>(MAX_DATA);
    const int MAX_DATA = 64;

    NetworkHits m_CurrentData;

    void OnHit(HitInfo hitInfo)
    {
        //Debug.LogError("OnHit Target=" + hitInfo.Target + " SubTarget=" + hitInfo.PartTarget);
        var playerObj = GetPlayer(hitInfo.Target);
        if (playerObj == null) return;
        var playerSub = GetPlayer(hitInfo.PartTarget);
        if (playerSub == null) return;
        var ids = m_CurrentData.PlayerIDs;
        var idObj = playerObj.Player.ID;
        var idSub = playerSub.Player.ID;
        int indexObj = ids.IndexOf(idObj);
        if (indexObj == -1)
        {
            ids.Add(idObj);
            indexObj = ids.Count - 1;
        }
        int indexSub = ids.IndexOf(idSub);
        if (indexSub == -1)
        {
            ids.Add(idSub);
            indexSub = ids.Count - 1;
        }
        //Debug.LogError("OnHit2 Obj=" + playerObj + " Sub=" + playerSub);

        var hit = new NetworkHitData();
        hit.Dir = hitInfo.Direction;
        hit.Pos = hitInfo.Position;
        hit.IndexObjectPlayer = (byte)indexObj;
        hit.IndexSubjectPlayer = (byte)indexSub;

        m_CurrentData.Hits.Add(hit);
    }

    public bool GetData(NetDataWriter writer)
    {
        if (m_CurrentData.Hits.Count <= 0) return false;
        bool res = m_CurrentData.PutInWriter(writer);
        if (m_ActualData.Count >= MAX_DATA) m_ActualData.RemoveAt(0);
        m_ActualData.Add(m_CurrentData);
        m_CurrentData = NetworkHits.Get();
        return res;
    }

    Dictionary<string, PlayerMainControl> m_PlayerInfo = new Dictionary<string, PlayerMainControl>(GameConstants.MAX_PLAYERS_IN_GAME);

    PlayerMainControl GetPlayer(string ID)
    {
        return GameController.I.GetPlayerControl(ID);
    }

    PlayerMainControl GetPlayer(GameObject go)
    {
        return GameController.I.GetPlayerControl(go);
    }

    public void AddData(NetDataReader reader)
    {
        var data = NetworkHits.Get();
        bool res = data.FromReader(reader);
        if (!res)
        {
            NetworkHits.Return(data);
            return;
        }
        var hits = data.Hits;
        if (hits == null || hits.Count <= 0)
        {
            NetworkHits.Return(data);
            return;
        }
        m_CurrentData = data;
        Accept(data);
    }

    void Accept(NetworkHits data)
    {
        var hits = data.Hits;
        var ids = data.PlayerIDs;
        m_PlayerInfo.Clear();
        //берем игровое представление наших игроков
        for (int i = 0; i < ids.Count; i++) m_PlayerInfo.Add(ids[i], GetPlayer(ids[i]));

        //перебираем все попадания
        for (int i = 0; i < hits.Count; i++)
        {
            var hitInfo = hits[i];
            var idObj = ids[hitInfo.IndexObjectPlayer];
            var idSub = ids[hitInfo.IndexSubjectPlayer];
            var playerObj = m_PlayerInfo[idObj];
            var playerSub = m_PlayerInfo[idSub];
            //Debug.LogError("Receive Hit Obj=" + playerObj + " Sub=" + playerSub);
            float impulse = 1f;
            playerSub.Unit.Receiver.SetRangeHitAbsorbLogicImpulse(hitInfo.Pos, hitInfo.Dir, ref impulse);
        }

        m_PlayerInfo.Clear();
    }
}