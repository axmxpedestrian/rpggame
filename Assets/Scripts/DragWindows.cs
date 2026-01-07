using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragWindows : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [SerializeField] private RectTransform panelRectTrans;

    public void OnDrag(PointerEventData eventData)
    {
        panelRectTrans.anchoredPosition += eventData.delta;
    }
    //显示在最前方
    public void OnBeginDrag(PointerEventData eventData)
    {
        panelRectTrans.SetAsLastSibling();
    }

}
