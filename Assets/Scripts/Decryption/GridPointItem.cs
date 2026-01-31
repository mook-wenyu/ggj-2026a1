using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 九宫格上的单个点，用于报告点击和进入事件。
/// 挂载到 九宫格点 预制体上。
/// 同时提供“圆形命中区域”过滤，让透明区域不响应点击/拖拽。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasRenderer))]
[DisallowMultipleComponent]
public sealed class GridPointItem : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, ICanvasRaycastFilter
{
    [Header("异形按钮（圆形）")]
    [Tooltip("圆形命中半径缩放（1=内切圆）。用于让透明区域不可用。")]
    [SerializeField, Range(0.1f, 1f)] private float _circleRadiusScale = 1f;

    [HideInInspector] public int index;
    public Action<int> onPointerDown;
    public Action<int> onPointerEnter;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        onPointerDown?.Invoke(index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnter?.Invoke(index);
    }

    /// <summary>
    /// 关键：把点击命中从“矩形”改为“圆形”。
    /// Unity UI 的默认射线检测是 RectTransform 的矩形；这会导致圆形图片的透明角也能被点击。
    /// </summary>
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        var rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return true;
        }

        var rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            // 避免在布局未就绪时误判导致完全不可点。
            return true;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var localPoint))
        {
            return false;
        }

        var radius = 0.5f * Mathf.Min(rect.width, rect.height) * Mathf.Clamp01(_circleRadiusScale);
        var delta = localPoint - rect.center;
        return delta.sqrMagnitude <= radius * radius;
    }
}
