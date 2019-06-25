using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class test_console2 : MonoBehaviour {

	public bool show_output = true;
	public bool show_stack = false;
	public static test_console2 I;
	void Awake()
	{
		I = this;
		DontDestroyOnLoad (gameObject);
		strb.AppendLine("CONSOLE:");
	}

    private void Start()
    {
        show = false;
    }

    public void Update()
	{
		if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Escape))
		{
			show = !show;
			//Debug.Log("~");
		}
	}
	
	int error_count = 0;

    System.Text.StringBuilder strb = new System.Text.StringBuilder(MAX_LEN * 2);
    string m_FinalText = string.Empty;
    Dictionary<int, int> m_StringsCount = new Dictionary<int, int>(1000);
    List<int> m_OrderHash = new List<int>(1000);
    List<string> m_Strings = new List<string>(1000);
    int m_CommonLength;
    bool m_DirtyChangeFinalText;
    const int MAX_LEN = 15000;

    GUIStyle m_Style;

    void OnEnable()
	{
		Application.logMessageReceivedThreaded += HandleLog;
		//Application.RegisterLogCallback(HandleLog);
	}
	void OnDisable()
	{
		Application.logMessageReceivedThreaded -= HandleLog;
		//Application.RegisterLogCallback(null);
	}

    int GetCountsOfDigits(int number)
    {
        int count = (number == 0) ? 1 : 0;
        while (number != 0)
        {
            count++;
            number /= 10;
        }
        return count;
    }

    void AddString(string str)
    {
        if (string.IsNullOrEmpty(str)) return;
        int hash = str.GetHashCode();
        int countReply = 0;
        if (!m_OrderHash.Contains(hash))
        {
            m_OrderHash.Add(hash);
            m_Strings.Add(str);
            m_StringsCount.Add(hash, 1);
            countReply = 1;
            //m_CommonLength += (str.Length + 1);
        }
        else
        {
            countReply = m_StringsCount[hash];
            countReply++;
            m_StringsCount[hash] = countReply;
        }
        m_DirtyChangeFinalText = true;
    }

    void ApplyFinalText()
    {
        if (!m_DirtyChangeFinalText) return;
        strb.Remove(0, strb.Length);
        for (int i = 0; i < m_OrderHash.Count; i++)
        {
            strb.Append(m_Strings[i]).Append(" (").Append(m_StringsCount[m_OrderHash[i]]).Append(")\n");
            int curLen = strb.Length;
            if (curLen > MAX_LEN)
            {
                int deltaLen = curLen - MAX_LEN;
                strb.Remove(0, deltaLen);
            }
        }

        m_FinalText = strb.ToString();
        m_DirtyChangeFinalText = false;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception/* || type == LogType.Error*/)
        {
            error_count++;
        }

        if (!show_output && !show_stack) return;

        if (show_output)
        {
            string str = type == LogType.Exception ? (logString + stackTrace) : logString;
            AddString(str);
        }
        //вписываем стек всегда если есть ошибка
        if (show_stack)
        {
            AddString(stackTrace);
        }
    }
	
	//Rect pos_rect = new Rect(50, 75+50, 400, 400);
	//public Rect view_rect = new Rect(0, 0, 400, 60000);
	Vector2 scroll_pos;
	public bool show = false;
	public void OnGUI()
	{
		if (show)
		{
            Rect view_rect = new Rect(0, 0, Screen.width, MAX_LEN * 2);
            var pos_rect = new Rect(50, 75 + 50, Screen.width - 100, Screen.height - (75 + 50));
            //var pos_rect = new Rect(50, 75 + 50, 100, 100);
            ApplyFinalText();
            GUI.color = Color.white;
            GUI.Label(new Rect(pos_rect.x, pos_rect.y - 20, 200, 50), "[errors " + error_count + "] length: " + strb.Length);//, "box");
			
			scroll_pos = GUI.BeginScrollView(pos_rect, scroll_pos, view_rect);
            var color = Color.black;
            color.a = 0.9f;
            GUI.color = color;
            var rect = new Rect(0, 0, view_rect.width - 50, view_rect.height);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(rect, m_FinalText);
			GUI.EndScrollView();
		}
	}
}
