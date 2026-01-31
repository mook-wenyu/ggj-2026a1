using System;

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 九宫格上的单个点，用于报告点击和进入事件。
/// 挂载到 九宫格点 预制体上。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasRenderer))]
public class GridPointItem : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
{
    [HideInInspector] public int index;
    public Action<int> onPointerDown;
    public Action<int> onPointerEnter;

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown?.Invoke(index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnter?.Invoke(index);
    }
}
