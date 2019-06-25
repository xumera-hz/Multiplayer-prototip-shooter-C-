using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System;

public struct ConnectInfo
{
    public string IP { get; private set; }
    public int Port { get; private set; }
    public bool IsServer { get; private set; }
    public bool Validation { get; private set; }

    public ConnectInfo(string ip, int port, bool isServer, bool validation)
    {
        IP = ip; Port = port; IsServer = isServer; Validation = validation;
    }
    public ConnectInfo(int port) : this(null, port, false, false) { }
    public ConnectInfo(string ip, int port) : this(ip, port, false, false) { }
}

public struct ClientConnectInfo
{
    public ConnectInfo ConnectInfo;
    public string PlayerName;
    public TypeUnit TypeUnit;

    public string IP { get { return ConnectInfo.IP; } }
    public int Port { get { return ConnectInfo.Port; } }
}

public class ConnectController : MonoBehaviour {

    [SerializeField] InputField m_IP = null, m_Port = null;
    [SerializeField] InputField m_PlayerName;

    public static string IP { get; private set; }
    public static int Port { get; private set; }
    public static bool IsServer { get; private set; }
    public static bool Validation { get; private set; }

    #region ValidateData

    char OnValidateInputIP(string text, int charIndex, char addedChar)
    {
#if UNITY_EDITOR
        //Debug.LogError(string.Format("OnValidateInputIP text={0} index={1} char={2}", text, charIndex, addedChar));
#endif

        const char Empty = '\0';
        const char Point = '.';
        int len = text.Length;
        //если превысили макс. кол-во символов, чао
        if (len >= 15) return Empty;
        bool isPoint = addedChar == Point;
        bool isNumber = char.IsNumber(addedChar);
        //если не точка и не число, чао
        if (!isPoint && !isNumber) return Empty;
        //если это первый символ, дратути
        if (len <= 0 && isNumber) return addedChar;
        //проверяем, точка единственная, и у нее нет соседей
        if (isPoint)
        {
            //Ну точкой первый символ такое себе
            if (len <= 0) return Empty;
            //помеха слева
            if (charIndex > 0 && len >= charIndex && text[charIndex - 1] == Point) return Empty;
            //помеха справа
            if (len > (charIndex + 1) && text[charIndex + 1] == Point) return Empty;

            //Точек подсчитаем, а вдруг их там больше 3
            int c = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == Point) c++;
            }

            //ну блин, так и знал
            if (c >= 3) return Empty;

            //ну, дратути
            return addedChar;
        }

        string newText = text.Insert(charIndex, addedChar.ToString());
        var strs = newText.Split(Point);
        //если точек больше 3, чао
        if (strs.Length > 4) return Empty;

        //находим индекс строки(байта адреса(0-255)), в который мы добавляем новый символ
        int index = 0;
        int count = 0;
        for (int i = 0; i < strs.Length; i++)
        {
            count += (strs[i].Length + 1);
            if (count > charIndex)
            {
                index = i;
                break;
            }
        }

        var str = strs[index];

        //если символов больше 3, чао
        if (str.Length > 3) return Empty;

        //если первый символ оказался нулем, чао
        if (str.Length > 1 && str[0] == '0') return Empty;

        int value;
        //если не смогли получить число, чао
        if (!int.TryParse(str, out value)) return Empty;

        //если вышли за пределы байта, чао
        if (value < 0 || value > 255) return Empty;

        return addedChar;
    }

    bool m_BlockRecursive;

    void OnValueChangedIP(string str)
    {
#if UNITY_EDITOR
        //Debug.LogError(string.Format("OnValueChangedIP text={0}", str));
#endif
        //ща тут навыделяем памяти)
        if (m_BlockRecursive) return;
        m_BlockRecursive = true;
        const string PointStr = ".";
        var strs = str.Split('.');
        string newStr = string.Empty;
        int len = strs.Length;
        for (int i = 0; i < len; i++)
        {
            if (strs[i].Length <= 0) continue;
            int value;
            if (!int.TryParse(strs[i], out value)) value = 0;
            else if (value < 0 || value > 255) value = 255;
            newStr = newStr + value;
            if (i < len - 1) newStr += PointStr;
        }
        m_IP.text = newStr;
        m_BlockRecursive = false;

        //System.Net.IPAddress ip;
        //bool validate = System.Net.IPAddress.TryParse(newStr, out ip)
    }

    bool ValidateIPV4(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        int ipLen = ip.Length;
        if (ipLen < 7 || ipLen > 15) return false;
        
        var strs = ip.Split('.');
        int len = strs.Length;
        if (len != 4) return false;
        for (int i = 0; i < len; i++)
        {
            int len2 = strs[i].Length;
            if (len2 <= 0 || len2 > 3) return false;
            int value;
            if (!int.TryParse(strs[i], out value)) return false;
            if (value < 0 || value > 255) return false;
        }
        return true;
    }

    char OnValidateInputPort(string text, int charIndex, char addedChar)
    {
        const char Empty = '\0';
        int len = text.Length;
        if (len >= 5) return Empty;
        if (!char.IsNumber(addedChar)) return Empty;
        if (len <= 0) return addedChar;
        if (text[0] == '0') return Empty;
        if (charIndex == 0 && addedChar == '0') return Empty;

        string newText;

        if (charIndex == 0) newText = addedChar + text;
        else if (charIndex >= text.Length) newText = text + addedChar;
        else newText = text.Substring(0, charIndex) + addedChar + text.Substring(charIndex, text.Length - charIndex);

        int port;
        if (!int.TryParse(newText, out port)) return Empty;
        if (!IsRealPort(port)) return Empty;

        return addedChar;
    }

    bool IsRealPort(int value)
    {
        return value >= 0 || value <= System.UInt16.MaxValue;
    }

    void OnValueChangedPort(string str)
    {
        if (m_BlockRecursive) return;
        m_BlockRecursive = true;
        int port;
        if (!int.TryParse(str, out port)) port = 0;
        else if (!IsRealPort(port)) port = 0;
        else m_Port.text = port.ToString();
        m_BlockRecursive = false;
    }

    void ActiveButton(Button but, bool state)
    {
        but.interactable = state;
    }

    #endregion

    public static event Action<Args> Events;

    void CallEvent(Args args)
    {
        if (Events != null) Events(args);
    }

    public enum TypeEvent { None, ClientConnect, ServerConnect }
    public enum TypeError { None, BadPort, BadIP, BadName }

    public struct Args
    {
        public TypeEvent Event;
        public TypeError Error;

        public ConnectInfo ConnectInfo;

        public string IP { get { return ConnectInfo.IP; } }
        public int Port { get { return ConnectInfo.Port; } }
        public string PlayerName;
    }

    private void Awake()
    {
        m_Port.text = "27999";
        //m_IP.text = "127.0.0.1";
        m_Port.characterLimit = 5;
        m_IP.characterLimit = 15;
        m_PlayerName.characterLimit = GameConstants.MAX_LENGTH_PLAYER_NAME - 3;
        m_PlayerName.text = "Player";
        m_IP.onValidateInput = OnValidateInputIP;
        m_IP.onValueChanged.AddListener(OnValueChangedIP);
        m_Port.onValidateInput = OnValidateInputPort;
        m_Port.onValueChanged.AddListener(OnValueChangedPort);
    }

    private void OnDestroy()
    {
        Events = null;
    }

    public void Server()
    {
        Validation = false;
        IsServer = false;
        string name = m_PlayerName.text;
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("Bad Name=" + m_PlayerName.text);
            CallEvent(new Args { Event = TypeEvent.ServerConnect, Error = TypeError.BadName });
            return;
        }
        int port;
        if(!int.TryParse(m_Port.text, out port))
        {
            Debug.LogError("Bad Port=" + m_IP.text);
            CallEvent(new Args { Event = TypeEvent.ServerConnect, Error = TypeError.BadPort });
            return;
        }
        Port = port;
        IsServer = true;
        Validation = true;
        CallEvent(new Args { Event = TypeEvent.ServerConnect, ConnectInfo = new ConnectInfo(port), PlayerName = name });
    }

    public void Client()
    {
        Validation = false;
        IsServer = false;
        string name = m_PlayerName.text;
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("Bad Name=" + m_PlayerName.text);
            CallEvent(new Args { Event = TypeEvent.ClientConnect, Error = TypeError.BadName });
            return;
        }
        IPAddress ipAddress;
        string ip = m_IP.text;
        if(!IPAddress.TryParse(m_IP.text, out ipAddress) || !ValidateIPV4(ip))
        {
            Debug.LogError("Bad IP=" + m_IP.text);
            CallEvent(new Args { Event = TypeEvent.ClientConnect, Error = TypeError.BadIP });
            return;
        }
        ip = ipAddress.ToString();
        int port;
        if (!int.TryParse(m_Port.text, out port))
        {
            Debug.LogError("Bad Port=" + m_IP.text);
            CallEvent(new Args { Event = TypeEvent.ClientConnect, Error = TypeError.BadPort });
            return;
        }
        IP = ip;
        Port = port;
        Validation = true;
        CallEvent(new Args { Event = TypeEvent.ClientConnect, ConnectInfo = new ConnectInfo(ip, port), PlayerName = name });
    }
}
