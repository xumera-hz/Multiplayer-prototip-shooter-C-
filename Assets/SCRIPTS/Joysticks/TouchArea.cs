using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class TouchArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    public bool Contain(Vector2 pos)
    {
        bool contain = RectTransformUtility.RectangleContainsScreenPoint(TargetRect, pos, null);
        if (!IgnoreUpImage) contain = contain && m_IsClicked;
        return contain;
    }


    [SerializeField] int ID;
    [Tooltip("Игнорирование выше лежащих UI элементов")]
    [SerializeField] bool IgnoreUpImage;
    [SerializeField] Image m_TargetImage;
    RectTransform m_Target;

    public bool ActiveImage
    {
        get { return TargetImage.gameObject.activeSelf; }
        set { TargetImage.gameObject.SetActive(value); }
    }

    public RectTransform TargetRect
    {
        get
        {
            if (m_Target == null)
            {
                var targ = TargetImage;
                m_Target = targ == null ? GetComponent<RectTransform>() : targ.rectTransform;
            }
            return m_Target;
        }
    }


    bool m_IsClicked;

    public void Active(bool state)
    {
        var img = TargetImage;
        if (img) img.raycastTarget = state;
        if (gameObject.activeSelf == state) return;
        gameObject.SetActive(state);
        m_IsClicked = false;
    }

    void OnDisable()
    {
        m_IsClicked = false;
    }

    public Image TargetImage
    {
        get
        {
            if (m_TargetImage == null) m_TargetImage = GetComponentInChildren<Image>(true);
            return m_TargetImage;
        }
    }

    public event Action<int, PointerEventData> PointerDown, PointerUp;

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        m_IsClicked = false;
        if (PointerUp != null) PointerUp(ID, eventData);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        m_IsClicked = true;
        if (PointerDown != null) PointerDown(ID, eventData);
    }

    public Vector3 position
    {
        get { return TargetRect.position; }
        set { TargetRect.position = value; }
    }

    public Vector3 localPosition
    {
        get { return TargetRect.localPosition; }
        set { TargetRect.localPosition = value; }
    }

    public Rect rect
    {
        get { return TargetRect.rect; }
    }
}
