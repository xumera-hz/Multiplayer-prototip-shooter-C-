using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

#region UnityEditor
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;

[CustomEditor(typeof(UIPointerDownUpElements))]
public class UIPointerDownUpElementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        UIPointerDownUpElements targ = (UIPointerDownUpElements)target;
        if (targ.Valid)
        {
            targ.MyValidate();
            targ.Valid = false;
        }
        if (GUILayout.Button("ManualChange")) targ.MyValidate(true);
        DrawDefaultInspector();
    }
}

#endif
#endregion

[DisallowMultipleComponent]
public class UIPointerDownUpElements : MonoBehaviour
{
    
    [Serializable]
    protected class UIButtonInfo : UIButtonInfoBase
    {
        public int ID = -1;
        [HideInInspector]
        public bool State;
        [HideInInspector]
        public EventTrigger Trigger;
        [HideInInspector]
        public bool isSprite, isImage;
    }
    [SerializeField]
    protected List<UIButtonInfo> Elements;
    public int Count { get { return Elements.Count; } }
    public int[] GetIDS
    {
        get
        {
            var ids = new int[Elements.Count];
            for (int i = 0; i < Elements.Count; i++) ids[i] = Elements[i].ID;
            return ids;
        }
    }

    UIButtonInfo elem;

    public virtual event Action<int, bool> Callback;
    public virtual event Action<int> CallbackOnDown;
    public virtual event Action<int> CallbackOnUp;

    public virtual void UpTrigger(int id)
    {
        if (SetTriggerState(id, false))
        {
            if (Callback != null) Callback(id, false);
            if (CallbackOnUp != null) CallbackOnUp(id);
        }
    }

    public virtual void DownTrigger(int id)
    {
        if (SetTriggerState(id, true))
        {
            if (Callback != null) Callback(id, true);
            if (CallbackOnDown != null) CallbackOnDown(id);
        }
    }

    bool SetTriggerState(int id, bool state)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                if (elem.isSprite) elem.Image.sprite = state ? elem.SpriteDown : elem.SpriteUp;
                elem.State = state;
                return true;
            }
        }
        return false;
    }

    void ReturnInDefault()
    {
        for (int i = Elements.Count - 1; i >= 0; i--)
        {
            elem = Elements[i];
            if (elem.isSprite) elem.Image.sprite = elem.SpriteUp;
            bool prev = elem.State;
            elem.State = false;
            if (prev)
            {
                if (Callback != null) Callback(elem.ID, false);
                if (CallbackOnDown != null) CallbackOnDown(elem.ID);
            }
        }
    }

    public void SetDefault()
    {
        ReturnInDefault();
    }

    public void ActiveSprite(int id)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                if (elem.isSprite) elem.Image.sprite = elem.State ? elem.SpriteDown : elem.SpriteUp;
                break;
            }
        }
    }
    //Включает активность кнопки(через raycastTarget)
    public void ActiveClickable(int id, bool state)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                elem.TriggerTarget.raycastTarget = state;
                break;
            }
        }
    }

    public void SetActive(bool state)
    {
        gameObject.SetActive(state);
    }

    public void ActiveAllClickable(bool state)
    {
        for (int i = 0; i < Elements.Count; i++) Elements[i].TriggerTarget.raycastTarget = state;
    }

    public void Click2(int id, bool state, PointerEventData data)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                if (state) elem.Trigger.OnPointerDown(data);
                else elem.Trigger.OnPointerUp(data);
            }
        }
    }

    public void Click(int id, bool state)
    {
        if (state) DownTrigger(id);
        else UpTrigger(id);
    }

    public bool CheckTriggerArea(int id, Vector2 pos)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                Image _img = elem.OtherCheckAreaForTrigger == null ? elem.TriggerTarget : elem.OtherCheckAreaForTrigger;
                //Debug.LogError("CheckTriggerArea");
                return RectTransformUtility.RectangleContainsScreenPoint(_img.rectTransform, pos, null);
            }
        }
        return false;
    }

    public bool IsShowVisual(int id)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                return elem.TriggerTarget.enabled;
            }
        }
        return false;
    }
    public void ShowButton(int id, bool state)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                elem.TriggerTarget.gameObject.SetActive(state);
                break;
            }
        }
    }
    public void ShowVisual(int id, bool state)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                if (elem.Image != null) elem.Image.enabled = state;
                elem.TriggerTarget.enabled = state;
                break;
            }
        }
    }

#if UNITY_EDITOR
    void Awake()
    {
        CheckDublicate();
        CheckError();
    }
    static System.Collections.Generic.List<EventTrigger> m_CheckDublicateList;
    public void CheckDublicate()
    {
        UIButtonInfo _elem;
        if (m_CheckDublicateList == null) m_CheckDublicateList = new System.Collections.Generic.List<EventTrigger>(10);
        else m_CheckDublicateList.Clear();
        for (int i = 0; i < Elements.Count; i++)
        {
            _elem = Elements[i];
            if (_elem.TriggerTarget == null)
            {
                Debug.LogWarning(GetType() + " error: " + gameObject + " TriggerTarget(self image on this) is null in element");
                continue;
            }
            _elem.TriggerTarget.GetComponents(m_CheckDublicateList);
            if (m_CheckDublicateList.Count > 1) Debug.LogError(GetType() + " error: " + _elem.TriggerTarget.gameObject + " dublicate trigger");
        }
    }
#endif

    void OnDisable()
    {
        ReturnInDefault();
    }

    void OnApplicationFocus(bool focusStatus)
    {
        if (!focusStatus) ReturnInDefault();
    }

    public void SetTriggerSprite(int id, Sprite spriteTrigger)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                elem.TriggerTarget.sprite = spriteTrigger;
                return;
            }
        }
        //if (id >= Elements.Length && spriteTrigger != null) return;
        //Elements[id].TriggerTarget.sprite = spriteTrigger;
    }
    public void SetAllTriggerSprite(Sprite spriteTrigger)
    {
        for (int i = 0; i < Elements.Count; i++) Elements[i].TriggerTarget.sprite = spriteTrigger;
    }

    public void SetAllColorTriggerSprite(Color color)
    {
        for (int i = 0; i < Elements.Count; i++) Elements[i].TriggerTarget.color = color;
    }

    public void SetColorTriggerSprite(int id, Color color)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                if (elem.Image == null)
                {
                    elem.TriggerTarget.color = color;
                }
                else
                {
                    elem.Image.color = color;
                }
                break;
            }
        }
    }
    public void SetVisualImageSprite(int id, Sprite spriteImage)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                if (elem.isImage) elem.Image.sprite = spriteImage;
                break;
            }
        }
    }
    public void SetSprites(int id, Sprite spriteUp, Sprite spriteDown)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            elem = Elements[i];
            if (elem.ID == id)
            {
                bool isImage = elem.Image != null;
                elem.isSprite = isImage && spriteUp != null && spriteDown != null;
                if (elem.isSprite)
                {
                    elem.SpriteUp = spriteUp;
                    elem.SpriteDown = spriteDown;
                    if (elem.Image.sprite == null)
                    {
                        Color clr = elem.Image.color;
                        clr.a = 1f;
                        elem.Image.color = clr;
                    }
                    elem.Image.sprite = elem.State ? spriteDown : spriteUp;
                }
                else
                {
                    elem.SpriteUp = null;
                    elem.SpriteDown = null;
                    if (isImage)
                    {
                        elem.Image.sprite = null;
                        Color clr = elem.Image.color;
                        clr.a = 0f;
                        elem.Image.color = clr;
                    }
                }
                break;
            }
        }
    }

    public UIButtonInfoBase GetButtonInfo(int id)
    {
        for (int i = Elements.Count - 1; i >= 0; i--)
        {
            if (Elements[i].ID == id) return Elements[i];
        }
        return null;
    }

    public int AddButton(UIButtonInfoBase param)
    {
        if (param.TriggerTarget == null)
        {
#if UNITY_EDITOR
            Debug.LogError("У ЭЛЕМЕНТА НЕ НАЗНАЧЕН TriggerTarget");
#endif
            return -1;
        }

        //hmmm
        //Array.Resize(ref Elements, Elements.Count + 1);
        var info = new UIButtonInfo()
        {
            TriggerTarget = param.TriggerTarget,
            Image = param.Image,
            SpriteDown = param.SpriteDown,
            SpriteUp = param.SpriteUp,
            OtherCheckAreaForTrigger = param.OtherCheckAreaForTrigger
        };
        //Elements[Elements.Count - 1] = info;
        Elements.Add(info);
        info.ID = Elements.Count - 1;
        info.Trigger = info.TriggerTarget.gameObject.AddComponent<EventTrigger>();
        info.Trigger.triggers.Clear();
        var onUp = new EventTrigger.Entry();
        onUp.eventID = EventTriggerType.PointerUp;
        int id = info.ID;
        onUp.callback.AddListener((a) => { UpTrigger(Elements[id].ID); });
        var onDown = new EventTrigger.Entry();
        onDown.eventID = EventTriggerType.PointerDown;
        onDown.callback.AddListener((a) => { DownTrigger(Elements[id].ID); });
        info.Trigger.triggers.Add(onUp);
        info.Trigger.triggers.Add(onDown);
        info.isImage = info.Image != null;
        info.isSprite = info.isImage && info.SpriteDown != null && info.SpriteUp != null;
        if (info.isSprite) info.Image.sprite = info.SpriteUp;
        return id;
    }

    #region AutoValidate
#if UNITY_EDITOR
    [HideInInspector]
    public bool Valid;
    void OnValidate()
    {
        if (Application.isPlaying) return;
        Valid = true;
        //if(Elements!=null) countElements = Elements.Length;
    }

    void Reset()
    {
        MyValidate();
    }
    public void MyValidate(bool autoSetTriggers = false)
    {
        if (Elements == null) return;
        if (Application.isPlaying) return;
        if (gameObject.IsPrefab())
        {
#if UNITY_EDITOR
            Debug.LogError("MyValidate None");
            //Debug.LogError(mono.GetType()+"on +"+ mono.gameObject+ " PrefabType=" + prefabType);
#endif
            return;
        }
        if (!autoSetTriggers)
        {
            CheckError();
            return;
        }
        //countElements = Elements.Length;
        EventTrigger.Entry onDown, onUp;
        UIButtonInfo _elem;
        for (int i = 0; i < Elements.Count; i++)
        {
            _elem = Elements[i];
            if (_elem.TriggerTarget == null)
            {
                Debug.LogWarning(GetType() + " error: " + gameObject + " TriggerTarget(self image on this) is null in element");
                continue;
            }
            EventTrigger[] triggers = _elem.TriggerTarget.GetComponents<EventTrigger>();
            for (int j = 1; j < triggers.Length; j++)
            {
                DestroyImmediate(triggers[j]);
            }
            if (_elem.Trigger == null) _elem.Trigger = _elem.TriggerTarget.gameObject.AddComponent<EventTrigger>();
            _elem.Trigger.triggers.Clear();
            onUp = new EventTrigger.Entry();
            onUp.eventID = EventTriggerType.PointerUp;
            onDown = new EventTrigger.Entry();
            onDown.eventID = EventTriggerType.PointerDown;
            _elem.ID = _elem.ID > -1 ? _elem.ID : i;
            UnityEventTools.AddIntPersistentListener(onUp.callback, UpTrigger, _elem.ID);
            UnityEventTools.AddIntPersistentListener(onDown.callback, DownTrigger, _elem.ID);
            _elem.Trigger.triggers.Add(onDown);
            _elem.Trigger.triggers.Add(onUp);
            _elem.isImage = _elem.Image != null;
            _elem.isSprite = _elem.isImage && _elem.SpriteDown != null && _elem.SpriteUp != null;
            if (_elem.isSprite) _elem.Image.sprite = _elem.SpriteUp;
            if (!_elem.TriggerTarget.raycastTarget)
            {
                Debug.LogError(GetType() + " error: " + gameObject + " image is not raycastTarget true in element");
            }
        }

        CheckError();
    }

    void CheckError()
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            if (Elements[i].TriggerTarget == null)
            {
                Debug.LogWarning(GetType() + " error: " + gameObject + " TriggerTarget(self image on this) is null in element");
            }
            for (int j = i + 1; j < Elements.Count; j++)
            {
                if (Elements[i].ID == Elements[j].ID) Debug.LogError("Совпадают ID на " + Elements[i].TriggerTarget + " и " + Elements[j].TriggerTarget);
            }
        }
    }
#endif
    #endregion
}


[Serializable]
public class UIButtonInfoBase
{
    public Image TriggerTarget;
    public Image Image;
    public Sprite SpriteUp, SpriteDown;
    public Image OtherCheckAreaForTrigger;
}