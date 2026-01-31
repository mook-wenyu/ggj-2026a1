#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;

/// <summary>
/// 创建保险柜UI预制体的编辑器工具。
/// 菜单：Tools > Create Safe UI Prefab
/// </summary>
public static class SafeUICreator
{
    [MenuItem("Tools/Create Safe UI Prefab")]
    public static void Create()
    {
        var root = new GameObject("保险柜UI");
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<SafeUI>();

        var es = new GameObject("EventSystem");
        es.transform.SetParent(root.transform);
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        var panel = CreatePanel(root.transform);
        CreateFirstPassword(panel.transform);
        CreateSecondPassword(panel.transform);
        CreateCloseButton(panel.transform);

        var path = "Assets/Prefabs/保险柜UI.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Selection.activeObject = prefab;
        Debug.Log($"已创建保险柜UI预制体: {path}");
    }

    static GameObject CreatePanel(Transform parent)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600, 500);
        rect.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);
        return go;
    }

    static void CreateFirstPassword(Transform panel)
    {
        var area = new GameObject("第一重密码");
        area.transform.SetParent(panel, false);
        var rect = area.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.8f);
        rect.anchorMax = new Vector2(0.5f, 0.8f);
        rect.sizeDelta = new Vector2(400, 120);
        rect.anchoredPosition = Vector2.zero;

        var grid = new GameObject("数字键盘");
        grid.transform.SetParent(area.transform, false);
        var gr = grid.AddComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.3f);
        gr.anchorMax = new Vector2(0.5f, 0.7f);
        gr.sizeDelta = new Vector2(280, 80);
        gr.anchoredPosition = Vector2.zero;
        var layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(50, 50);
        layout.spacing = new Vector2(10, 10);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;

        foreach (var i in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 })
        {
            var btn = new GameObject(i.ToString());
            btn.transform.SetParent(grid.transform, false);
            var br = btn.AddComponent<RectTransform>();
            var bi = btn.AddComponent<Image>();
            bi.color = new Color(0.4f, 0.4f, 0.5f, 1f);
            btn.AddComponent<Button>();
            var bt = new GameObject("Text");
            bt.transform.SetParent(btn.transform, false);
            var btr = bt.AddComponent<RectTransform>();
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.offsetMin = btr.offsetMax = Vector2.zero;
            var btxt = bt.AddComponent<TextMeshProUGUI>();
            btxt.text = i.ToString();
            btxt.fontSize = 24;
            btxt.alignment = TextAlignmentOptions.Center;
        }
    }

    static void CreateSecondPassword(Transform panel)
    {
        var area = new GameObject("第二重密码");
        area.transform.SetParent(panel, false);
        var rect = area.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.45f);
        rect.anchorMax = new Vector2(0.5f, 0.45f);
        rect.sizeDelta = new Vector2(400, 80);
        rect.anchoredPosition = Vector2.zero;
        var layout = area.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        for (int i = 1; i <= 3; i++)
        {
            var roller = new GameObject("滚盘" + i);
            roller.transform.SetParent(area.transform, false);
            var rr = roller.AddComponent<RectTransform>();
            rr.sizeDelta = new Vector2(80, 80);
            var ri = roller.AddComponent<Image>();
            ri.color = new Color(0.35f, 0.35f, 0.4f, 1f);
            roller.AddComponent<Button>();
            var rt = new GameObject("Text (TMP)");
            rt.transform.SetParent(roller.transform, false);
            var rtr = rt.AddComponent<RectTransform>();
            rtr.anchorMin = Vector2.zero;
            rtr.anchorMax = Vector2.one;
            rtr.offsetMin = rtr.offsetMax = Vector2.zero;
            var rtxt = rt.AddComponent<TextMeshProUGUI>();
            rtxt.text = "■";
            rtxt.fontSize = 36;
            rtxt.alignment = TextAlignmentOptions.Center;
        }
    }

    static void CreateCloseButton(Transform panel)
    {
        var btn = new GameObject("关闭");
        btn.transform.SetParent(panel, false);
        var rect = btn.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.1f);
        rect.anchorMax = new Vector2(0.5f, 0.1f);
        rect.sizeDelta = new Vector2(120, 40);
        rect.anchoredPosition = Vector2.zero;
        var img = btn.AddComponent<Image>();
        img.color = new Color(0.5f, 0.3f, 0.3f, 1f);
        btn.AddComponent<Button>();
        var bt = new GameObject("Text");
        bt.transform.SetParent(btn.transform, false);
        var btr = bt.AddComponent<RectTransform>();
        btr.anchorMin = Vector2.zero;
        btr.anchorMax = Vector2.one;
        btr.offsetMin = btr.offsetMax = Vector2.zero;
        var btxt = bt.AddComponent<TextMeshProUGUI>();
        btxt.text = "关闭";
        btxt.fontSize = 20;
        btxt.alignment = TextAlignmentOptions.Center;
    }
}
#endif
