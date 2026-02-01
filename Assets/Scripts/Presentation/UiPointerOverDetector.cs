using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 指针命中检测：用于阻止“点 UI 但触发场景物体”的点击穿透。
///
/// 说明：
/// - 不能只依赖 EventSystem.current.IsPointerOverGameObject()，因为它依赖输入模块的内部状态，
///   在部分回调/时序下可能尚未更新。
/// - 这里通过 RaycastAll 基于当前屏幕坐标即时计算，结果更稳定。
/// </summary>
public static class UiPointerOverDetector
{
    private static readonly List<RaycastResult> Results = new(16);
    private static PointerEventData s_eventData;
    private static EventSystem s_cachedEventSystem;

    public static bool IsPointerOverUi()
    {
        // 兼容：优先取触摸坐标；否则使用鼠标。
        if (Input.touchCount > 0)
        {
            return IsPointerOverUi(Input.GetTouch(0).position);
        }

        return IsPointerOverUi(Input.mousePosition);
    }

    public static bool IsPointerOverUi(Vector2 screenPosition)
    {
        return IsPointerOverUi(EventSystem.current, screenPosition);
    }

    public static bool IsPointerOverUi(EventSystem eventSystem, Vector2 screenPosition)
    {
        if (eventSystem == null)
        {
            return false;
        }

        // 快速路径：若内部状态已更新，直接返回 true。
        // 注意：返回 false 时不能直接认为“没有 UI”，仍需走 RaycastAll。
        if (eventSystem.IsPointerOverGameObject())
        {
            return true;
        }

        if (s_eventData == null || s_cachedEventSystem != eventSystem)
        {
            s_cachedEventSystem = eventSystem;
            s_eventData = new PointerEventData(eventSystem);
        }

        s_eventData.position = screenPosition;
        Results.Clear();
        eventSystem.RaycastAll(s_eventData, Results);

        for (var i = 0; i < Results.Count; i++)
        {
            var go = Results[i].gameObject;
            if (go == null)
            {
                continue;
            }

            // UI 对象必然有 RectTransform（防止 PhysicsRaycaster 命中世界物体时误判）。
            if (go.GetComponent<RectTransform>() != null)
            {
                return true;
            }
        }

        return false;
    }
}
