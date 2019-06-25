using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateLevel : MonoBehaviour {

    [SerializeField] Transform m_Root;

    const string LEVELS_ROOT = "Levels/";
    const string LEVEL_NAME = "Level";
    public const string GAME_LEVEL_NAME = "_GAME_LEVEL_";
    const int DEFAULT_GROUND_ID = GROUND_ID;
    const int GROUND_ID = 0;
    const int GRASS_ID = 1;
    const int WALL_ID = 2;
    const int WATER_ID = 3;

    static readonly Dictionary<int, Cell> m_CellsInfo;

    static CreateLevel()
    {
        m_CellsInfo = new Dictionary<int, Cell>()
        {
            { GROUND_ID, new Cell(GROUND_ID,"Ground") { IsGround = true } },
            { GRASS_ID, new Cell(GRASS_ID,"Grass") { IsBlock = true } },
            { WALL_ID, new Cell(WALL_ID,"Wall") { IsBlock = true } },
            { WATER_ID, new Cell(WATER_ID,"Water") { IsGround = true } },
        };
    }

    class Cell
    {
        const string LEVEL_CELLS_ROOT = "LevelCells/";
        public int ID { get; private set; }
        public string Name { get; private set; }

        GameObject m_Prefab;

        public bool IsBlock { get; set; }
        public bool IsGround { get; set; }

        public GameObject Prefab
        {
            get
            {
                if (m_Prefab == null) m_Prefab = GetPrefab<GameObject>(LEVEL_CELLS_ROOT + Name);
                return m_Prefab;
            }
        }

        T GetPrefab<T>(string path) where T : class
        {
            return Resources.Load(path) as T;
        }

        public Cell(int id, string name)
        {
            ID = id;
            Name = name;
        }
    }

    static T GetPrefab<T>(string path) where T: class
    {
        return Resources.Load(path) as T;
    }

    //[ground]=0
    //[grass]=1
    //[wall]=2
    //[water]=3

    [ContextMenu("TestCreate")]
    void TestCreate()
    {
        Create(0);
    }

    private void Start()
    {
        Create(0);
    }

    const float MULTIPLIER_POS_OFFSET = 2f;

    public void Create(int id)
    {
        var str = GetLevelString(id.ToString());
        var arr = ParseLevelString(str);
        Build(arr, new GameObject(GAME_LEVEL_NAME).transform);
    }

    void Build(int[,] array, Transform par)
    {
        par.SetParent(m_Root);
        int rowsLen = array.GetLength(0);
        int columnsLen = array.GetLength(1);
        Vector3 pos = Vector3.zero;
        var rotZero = Quaternion.identity;
        for (int i = 0; i < rowsLen; i++)
        {
            pos.x = i * MULTIPLIER_POS_OFFSET;
            for (int j = 0; j < columnsLen; j++)
            {
                pos.z = j * MULTIPLIER_POS_OFFSET;
                int ind = array[i, j];
                if (ind == -1) continue;
                var node = m_CellsInfo[ind];
                if (node.IsBlock)
                {
                    var go2 = Instantiate(m_CellsInfo[DEFAULT_GROUND_ID].Prefab, pos, rotZero);
                    go2.transform.SetParent(par);
                }
                var go = Instantiate(node.Prefab, pos, rotZero);
                go.transform.SetParent(par);
            }
        }
    }

    string GetLevelString(string id)
    {
        string path = LEVELS_ROOT + LEVEL_NAME + id;
        var asset = GetPrefab<TextAsset>(path);
        if (asset == null) return string.Empty;
        return asset.text;
    }

    int[,] ParseLevelString(string inStr)
    {
        const char SPACE = ' ';
        inStr = inStr.Trim('\n');
        string[] lines= inStr.Split(',');
        string[] strs = lines[0].Split(SPACE);
        int[,] outArray = new int[lines.Length, strs.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            strs = lines[i].Split(SPACE);
            for (int j = 0; j < strs.Length; j++)
            {
                int res;
                if (!int.TryParse(strs[j], out res))
                {
                    Debug.LogError(GetType() + " error: bad parse=" + strs[j]);
                    res = -1;
                }
                outArray[i, j] = res;
            }
        }
        return outArray;
    }

}
