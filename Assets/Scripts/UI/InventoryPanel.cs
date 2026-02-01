using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryPanel : MonoSingleton<InventoryPanel>
{
    private const string DefaultSlotPrefabResourcePath = "Prefabs/ItemSlot";
    private const string DefaultPopupPrefabResourcePath = "Prefabs/ItemInfoPopup";

    [Header("引用（可不填，会自动查找）")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _slotTemplate;
    [SerializeField] private Canvas _rootCanvas;

    [Header("预制体")]
    [Tooltip("物品详情弹窗预制体（推荐在 Inspector 绑定；未绑定时会尝试从 Resources 加载）。")]
    [SerializeField] private ItemInfoPopupView _popupPrefab;

    [Header("行为")]
    [Tooltip("为 true 时：优先展示最新获得的物品（固定插槽模式下为“越新越靠前”）；" +
             "动态列表模式下为“新物品插入到 Content 顶部”。")]
    [SerializeField] private bool _sortFromBottomToTop = true;

    [Tooltip("添加物品后，是否自动滚动到最新一端（排序从下到上时滚到顶部）。")]
    [SerializeField] private bool _autoScrollToNewest = true;

    [Header("音频")]
    [Tooltip("从场景交互打开物品详情时，若该物品配置了音频，是否自动播放一次。\n（物品栏点击默认不自动播放，需手动点重播。）")]
    [SerializeField] private bool _autoPlayItemAudioOnOpen = true;

    private readonly Dictionary<string, SlotView> _viewsById = new(StringComparer.Ordinal);
    private readonly List<SlotView> _fixedSlots = new();

    // 固定插槽模式：复用 Content 下已有的 ItemSlot 子节点（不运行时生成/销毁）。
    private bool _useFixedSlots;
    private string _selectedItemId;

    public string SelectedItemId => _selectedItemId;

    /// <summary>
    /// 允许外部覆盖物品音频服务（例如测试/替换实现）。
    /// </summary>
    public IItemAudioService ItemAudioServiceOverride { get; set; }

    private ItemInfoPopupView _popup;
    private bool _initialized;
    private bool _subscribed;

    /// <summary>
    /// 获取或创建物品栏面板：
    /// - 优先复用场景中已有实例（包含 inactive）；
    /// - 若不存在则从 Resources/Prefabs/InventoryPanel 生成，并确保场景有 Canvas/EventSystem。
    /// </summary>
    public static bool TryGetOrCreate(out InventoryPanel panel)
    {
        panel = null;

        if (!SingletonCreator.IsUnitTestMode && !Application.isPlaying)
        {
            return false;
        }

        var existing = FindExisting();
        if (existing != null)
        {
            panel = existing;
            return true;
        }

        var canvas = FindOrCreateCanvas();
        EnsureEventSystem();

        var prefabGo = Resources.Load<GameObject>("Prefabs/InventoryPanel");
        if (prefabGo == null)
        {
            Debug.LogError("InventoryPanel: 找不到 Resources/Prefabs/InventoryPanel.prefab，无法创建物品栏面板。");
            return false;
        }

        var instanceGo = Instantiate(prefabGo, canvas.transform);
        panel = instanceGo != null
            ? (instanceGo.GetComponent<InventoryPanel>() ?? instanceGo.GetComponentInChildren<InventoryPanel>(true))
            : null;
        if (panel == null)
        {
            Debug.LogError("InventoryPanel: 实例化 InventoryPanel 失败（预制体上缺少 InventoryPanel 组件）。");

            if (instanceGo != null)
            {
                if (SingletonCreator.IsUnitTestMode && !Application.isPlaying)
                {
                    DestroyImmediate(instanceGo);
                }
                else
                {
                    Destroy(instanceGo);
                }
            }

            return false;
        }

        return true;
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private static InventoryPanel FindExisting()
    {
        var existing = FindObjectsByType<InventoryPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existing == null || existing.Length == 0)
        {
            return null;
        }

        // 优先选 activeInHierarchy 的那一个。
        for (var i = 0; i < existing.Length; i++)
        {
            var p = existing[i];
            if (p != null && p.gameObject.activeInHierarchy)
            {
                return p;
            }
        }

        return existing[0];
    }

    private static Canvas FindOrCreateCanvas()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (canvases != null && canvases.Length > 0)
        {
            Canvas best = null;
            var bestScore = int.MinValue;

            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null)
                {
                    continue;
                }

                if (c.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                // 优先 active，其次 root，其次 sortingOrder 高。
                var score = 0;
                if (c.gameObject.activeInHierarchy)
                {
                    score += 1000;
                }

                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    score += 300;
                }
                else if (c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    score += 200;
                }

                if (c.isRootCanvas)
                {
                    score += 50;
                }

                score += c.sortingOrder;

                if (best == null || score > bestScore)
                {
                    best = c;
                    bestScore = score;
                }
            }

            if (best != null)
            {
                return best;
            }
        }

        var go = new GameObject("Canvas");
        go.layer = 5; // UI
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 最小配置：能渲染、能接收 UI 射线。
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    private void OnEnable()
    {
        // 避免在编辑器非运行态执行运行时逻辑（会污染场景、生成对象、Destroy 子节点等）。
        if (!Application.isPlaying && !SingletonCreator.IsUnitTestMode)
        {
            return;
        }

        EnsureInitialized();
        SubscribeInventoryEvents();
        RebuildAll();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying && !SingletonCreator.IsUnitTestMode)
        {
            return;
        }

        UnsubscribeInventoryEvents();
    }

    public override void OnSingletonInit()
    {
        EnsureInitialized();
        SubscribeInventoryEvents();
        RebuildAll();
    }

    /// <summary>
    /// 供玩法层调用：收集物品（不消耗）。
    /// 物品显示信息从 ExcelKit 生成的 ItemsConfig 读取。
    /// </summary>
    public bool TryCollect(string id)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("InventoryPanel.TryCollect 收到空 ID，已忽略");
            return false;
        }

        return InventoryService.TryCollect(id);
    }

    /// <summary>
    /// 兼容/调试入口：允许直接传入显示信息覆盖配置。
    /// </summary>
    public bool TryCollectCustom(string id, string displayName, string description, Sprite icon = null)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("InventoryPanel.TryCollectCustom 收到空 ID，已忽略");
            return false;
        }

        return InventoryService.State.TryAdd(new InventoryItem(id, displayName, description, icon));
    }

    /// <summary>
    /// 供玩法层调用：从“场景交互”打开物品详情。
    /// - 会将物品加入背包（若已存在则更新/忽略新增）。
    /// - 若该物品配置了音频，会自动播放一次（受 _autoPlayItemAudioOnOpen 控制）。
    /// </summary>
    public bool TryCollectAndOpenFromScene(string id)
    {
        EnsureInitialized();
        SubscribeInventoryEvents();
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("InventoryPanel.TryCollectAndOpenFromScene 收到空 ID，已忽略");
            return false;
        }

        if (!InventoryItemCatalog.TryCreateItem(id, out var item) || item == null)
        {
            Debug.LogWarning($"InventoryPanel.TryCollectAndOpenFromScene 找不到物品配置：{id}");
            return false;
        }

        InventoryService.State.TryAdd(item);
        Select(item.Id);
        ShowItemInfo(item, autoPlayAudio: true);
        return true;
    }

    /// <summary>
    /// 供玩法层调用：从“场景交互”打开物品详情（不收集）。
    /// 典型用途：只能查看、不可获得的场景物件（例如装饰/提示物）。
    /// </summary>
    public bool TryOpenItemInfoOnlyFromScene(string id)
    {
        EnsureInitialized();
        SubscribeInventoryEvents();
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("InventoryPanel.TryOpenItemInfoOnlyFromScene 收到空 ID，已忽略");
            return false;
        }

        var trimmedId = id.Trim();
        if (!InventoryItemCatalog.TryCreateItem(trimmedId, out var item) || item == null)
        {
            Debug.LogWarning($"InventoryPanel.TryOpenItemInfoOnlyFromScene 找不到物品配置：{trimmedId}");
            return false;
        }

        ShowItemInfo(item, autoPlayAudio: true);
        return true;
    }

    public void HideItemInfo()
    {
        _popup?.Hide();
    }

    public bool TryGetItem(string id, out InventoryItem item)
    {
        return InventoryService.State.TryGet(id, out item);
    }

    public bool TryGetSelectedItem(out InventoryItem item)
    {
        return InventoryService.State.TryGet(_selectedItemId, out item);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (_scrollRect == null)
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        if (_content == null && _scrollRect != null)
        {
            _content = _scrollRect.content;
        }

        if (_content == null)
        {
            _content = transform.Find("Viewport/Content") as RectTransform;
        }

        if (_rootCanvas == null)
        {
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        // 先尝试“固定插槽模式”：Content 下若已有多个 ItemSlot，则复用它们。
        TryInitializeFixedSlots(_content);

        if (!_useFixedSlots)
        {
            if (_slotTemplate == null)
            {
                _slotTemplate = FindTemplateFromContent(_content);
                if (_slotTemplate == null)
                {
                    _slotTemplate = Resources.Load<GameObject>(DefaultSlotPrefabResourcePath);
                }
            }

            if (_slotTemplate == null)
            {
                Debug.LogError(
                    "InventoryPanel: 找不到物品槽模板。请在 Inspector 绑定 _slotTemplate，" +
                    $"或确保 Resources/{DefaultSlotPrefabResourcePath}.prefab 存在。"
                );
            }
            else
            {
                // 若模板是场景中的占位物体，运行时隐藏它，仅作为克隆源。
                if (_slotTemplate.scene.IsValid() && _slotTemplate.activeSelf)
                {
                    _slotTemplate.SetActive(false);
                }
            }
        }

        EnsurePopup();
    }

    private void TryInitializeFixedSlots(RectTransform content)
    {
        if (_useFixedSlots || content == null)
        {
            return;
        }

        var slotRoots = FindSlotRootsFromContent(content);
        // 约定：至少 2 个 ItemSlot 才视为“固定插槽”。
        if (slotRoots.Count < 2)
        {
            return;
        }

        _useFixedSlots = true;
        _fixedSlots.Clear();

        foreach (var root in slotRoots)
        {
            if (root == null)
            {
                continue;
            }

            // 固定插槽应始终可见（空槽也显示）。
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            var view = new SlotView(root, item: null, onClick: OnSlotClicked);
            view.SetInteractable(false);
            view.SetSelected(false);
            _fixedSlots.Add(view);
        }
    }

    private void EnsurePopup()
    {
        if (_popup != null || _rootCanvas == null)
        {
            return;
        }

        var prefab = _popupPrefab;
        if (prefab == null)
        {
            var prefabGo = Resources.Load<GameObject>(DefaultPopupPrefabResourcePath);
            prefab = prefabGo != null ? prefabGo.GetComponent<ItemInfoPopupView>() : null;
        }

        if (prefab == null)
        {
            Debug.LogError(
                "InventoryPanel: 找不到物品详情弹窗预制体。请在 Inspector 绑定 _popupPrefab，" +
                $"或确保 Resources/{DefaultPopupPrefabResourcePath}.prefab 存在。"
            );
            return;
        }

        _popup = Instantiate(prefab, _rootCanvas.transform);
        _popup.Hide();
    }

    private void SubscribeInventoryEvents()
    {
        if (_subscribed)
        {
            return;
        }

        InventoryService.State.ItemAdded += HandleItemAdded;
        InventoryService.State.ItemUpdated += HandleItemUpdated;
        InventoryService.State.Cleared += HandleInventoryCleared;
        _subscribed = true;
    }

    private void UnsubscribeInventoryEvents()
    {
        if (!_subscribed)
        {
            return;
        }

        InventoryService.State.ItemAdded -= HandleItemAdded;
        InventoryService.State.ItemUpdated -= HandleItemUpdated;
        InventoryService.State.Cleared -= HandleInventoryCleared;
        _subscribed = false;
    }

    private void HandleItemAdded(InventoryItem item)
    {
        EnsureInitialized();
        if (_useFixedSlots)
        {
            RebuildAll();
            return;
        }

        CreateOrUpdateSlot(item);
        AutoScrollToNewestIfNeeded();
    }

    private void HandleItemUpdated(InventoryItem item)
    {
        EnsureInitialized();
        if (_useFixedSlots)
        {
            RebuildAll();
            return;
        }

        CreateOrUpdateSlot(item);
    }

    private void HandleInventoryCleared()
    {
        EnsureInitialized();
        RebuildAll();
        HideItemInfo();
    }

    private void RebuildAll()
    {
        if (_content == null)
        {
            return;
        }

        if (_useFixedSlots)
        {
            RebuildFixedSlots();
            return;
        }

        // 清理旧视图（保留模板）。
        for (var i = _content.childCount - 1; i >= 0; i--)
        {
            var child = _content.GetChild(i).gameObject;
            if (_slotTemplate != null && child == _slotTemplate)
            {
                continue;
            }

            Destroy(child);
        }

        _viewsById.Clear();

        foreach (var item in InventoryService.State.Items)
        {
            CreateOrUpdateSlot(item);
        }

        AutoScrollToNewestIfNeeded();
    }

    private void CreateOrUpdateSlot(InventoryItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Id) || _content == null)
        {
            return;
        }

        if (_viewsById.TryGetValue(item.Id, out var existing))
        {
            existing.SetItem(item);
            existing.Refresh();
            return;
        }

        if (_slotTemplate == null)
        {
            return;
        }

        var slotGo = Instantiate(_slotTemplate, _content);
        slotGo.name = $"ItemSlot_{item.Id}";
        slotGo.SetActive(true);

        var view = new SlotView(slotGo, item, OnSlotClicked);
        _viewsById[item.Id] = view;
        view.Refresh();

        if (_sortFromBottomToTop)
        {
            // 关键：新物品插入到顶部，视觉上形成“从下到上”的排序（越上越新）。
            slotGo.transform.SetAsFirstSibling();
        }
        else
        {
            slotGo.transform.SetAsLastSibling();
        }
    }

    private void OnSlotClicked(SlotView view)
    {
        if (view == null || view.Item == null)
        {
            return;
        }

        Select(view.Item.Id);
        // 物品栏点击：不自动播放音频。
        ShowItemInfo(view.Item, autoPlayAudio: false);
    }

    private void ShowItemInfo(InventoryItem item, bool autoPlayAudio)
    {
        if (item == null)
        {
            return;
        }

        var audioService = ResolveItemAudioService();
        AudioClip clip = null;
        var hasAudio = false;

        if (audioService != null && !string.IsNullOrWhiteSpace(item.AudioPath))
        {
            hasAudio = audioService.TryGetItemAudioClip(item.AudioPath, out clip);
        }

        Action onReplayClicked = null;
        if (hasAudio)
        {
            onReplayClicked = () => audioService.PlayItemAudio(clip);
        }

        _popup?.Show(
            item,
            hasAudio,
            onReplayClicked
        );

        if (hasAudio && autoPlayAudio && _autoPlayItemAudioOnOpen)
        {
            audioService.PlayItemAudio(clip);
        }
    }

    private IItemAudioService ResolveItemAudioService()
    {
        if (ItemAudioServiceOverride != null)
        {
            return ItemAudioServiceOverride;
        }

        // 避免在非运行态（例如编辑器查看场景）误创建单例。
        if (!Application.isPlaying && !SingletonCreator.IsUnitTestMode)
        {
            return null;
        }

        return AudioMgr.Instance;
    }

    private void Select(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        if (string.Equals(_selectedItemId, itemId, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.IsNullOrEmpty(_selectedItemId) && _viewsById.TryGetValue(_selectedItemId, out var oldView))
        {
            oldView.SetSelected(false);
        }

        _selectedItemId = itemId;
        if (_viewsById.TryGetValue(_selectedItemId, out var newView))
        {
            newView.SetSelected(true);
        }
    }

    private void AutoScrollToNewestIfNeeded()
    {
        if (!_autoScrollToNewest || _scrollRect == null)
        {
            return;
        }

        // 我们使用“新物品插入顶部”来实现从下到上的排序，所以最新一端在顶部。
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = _sortFromBottomToTop ? 1f : 0f;
    }

    private static GameObject FindTemplateFromContent(RectTransform content)
    {
        if (content == null)
        {
            return null;
        }

        for (var i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (!child.TryGetComponent<Button>(out _))
            {
                continue;
            }

            // 约定：场景里通常会放一个名为 ItemSlot 的占位，用作模板。
            if (child.name.IndexOf("ItemSlot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private void RebuildFixedSlots()
    {
        _viewsById.Clear();

        if (_fixedSlots.Count == 0)
        {
            // 兜底：固定模式但未找到插槽时，不做任何事。
            return;
        }

        var items = InventoryService.State.Items;
        var slotCount = _fixedSlots.Count;
        var itemCount = items != null ? items.Count : 0;

        // 固定插槽容量不足时，优先显示“最新”的 N 个物品。
        var startIndex = itemCount > slotCount ? itemCount - slotCount : 0;

        for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            var view = _fixedSlots[slotIndex];
            if (view == null)
            {
                continue;
            }

            InventoryItem item = null;
            if (_sortFromBottomToTop)
            {
                // 越新越靠前。
                var idx = itemCount - 1 - slotIndex;
                if (idx >= 0 && idx < itemCount)
                {
                    item = items[idx];
                }
            }
            else
            {
                // 按获得顺序填充（但在溢出时仍显示最新的一段）。
                var idx = startIndex + slotIndex;
                if (idx >= 0 && idx < itemCount)
                {
                    item = items[idx];
                }
            }

            view.SetItem(item);
            view.Refresh();
            view.SetInteractable(item != null);
            view.SetSelected(false);

            if (item != null && !string.IsNullOrWhiteSpace(item.Id))
            {
                _viewsById[item.Id] = view;
            }
        }

        // 复原选中态：若当前选中项不可见/不存在，则清空。
        if (!string.IsNullOrEmpty(_selectedItemId) && _viewsById.TryGetValue(_selectedItemId, out var selected))
        {
            selected.SetSelected(true);
        }
        else
        {
            _selectedItemId = null;
        }
    }

    private static List<GameObject> FindSlotRootsFromContent(RectTransform content)
    {
        var list = new List<GameObject>();
        if (content == null)
        {
            return list;
        }

        for (var i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (!child.TryGetComponent<Button>(out _))
            {
                continue;
            }

            // 约定：固定插槽命名包含 "ItemSlot"。
            if (child.name.IndexOf("ItemSlot", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            list.Add(child.gameObject);
        }

        return list;
    }

    private sealed class SlotView
    {
        private static readonly Color SelectedColor = new(0.95f, 0.92f, 0.78f, 1f);

        private readonly Button _button;
        private readonly Image _background;
        private readonly Image _icon;
        private readonly TMP_Text _label;
        private readonly Action<SlotView> _onClick;
        private readonly Color _normalBackgroundColor;

        public InventoryItem Item { get; private set; }

        public SlotView(GameObject root, InventoryItem item, Action<SlotView> onClick)
        {
            _onClick = onClick;
            Item = item;

            _button = root.GetComponent<Button>();
            _background = root.GetComponent<Image>();
            _icon = root.transform.Find("Image")?.GetComponent<Image>();
            _label = root.GetComponentInChildren<TMP_Text>(true);
            _normalBackgroundColor = _background != null ? _background.color : Color.white;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _onClick?.Invoke(this));
            }
        }

        public void SetItem(InventoryItem item)
        {
            Item = item;
        }

        public void Refresh()
        {
            if (_label != null)
            {
                _label.text = Item != null ? Item.GetDisplayNameOrId() : string.Empty;
            }

            if (_icon != null)
            {
                _icon.sprite = Item != null ? Item.Icon : null;
                _icon.enabled = Item != null && Item.Icon != null;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        public void SetSelected(bool selected)
        {
            if (_background == null)
            {
                return;
            }

            _background.color = selected ? SelectedColor : _normalBackgroundColor;
        }
    }
}
