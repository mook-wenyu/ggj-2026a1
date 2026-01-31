using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [Tooltip("为 true 时：列表视觉上从下到上（越上越新）；实现方式是把新物品插入到 Content 顶部。")]
    [SerializeField] private bool _sortFromBottomToTop = true;

    [Tooltip("添加物品后，是否自动滚动到最新一端（排序从下到上时滚到顶部）。")]
    [SerializeField] private bool _autoScrollToNewest = true;

    [Header("音频")]
    [Tooltip("打开物品详情时，若该物品配置了音频，是否自动播放一次。")]
    [SerializeField] private bool _autoPlayItemAudioOnOpen = true;

    private readonly Dictionary<string, SlotView> _viewsById = new(StringComparer.Ordinal);
    private string _selectedItemId;

    public string SelectedItemId => _selectedItemId;

    /// <summary>
    /// 允许外部覆盖物品音频服务（例如测试/替换实现）。
    /// </summary>
    public IItemAudioService ItemAudioServiceOverride { get; set; }

    private ItemInfoPopupView _popup;
    private bool _initialized;
    private bool _subscribed;

    private void Awake()
    {
        EnsureInitialized();
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

        EnsurePopup();
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
        _subscribed = false;
    }

    private void HandleItemAdded(InventoryItem item)
    {
        EnsureInitialized();
        CreateOrUpdateSlot(item);
        AutoScrollToNewestIfNeeded();
    }

    private void HandleItemUpdated(InventoryItem item)
    {
        EnsureInitialized();
        CreateOrUpdateSlot(item);
    }

    private void RebuildAll()
    {
        if (_content == null)
        {
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
        ShowItemInfo(view.Item);
    }

    private void ShowItemInfo(InventoryItem item)
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

        if (hasAudio && _autoPlayItemAudioOnOpen)
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
            if (Item == null)
            {
                return;
            }

            if (_label != null)
            {
                _label.text = Item.GetDisplayNameOrId();
            }

            if (_icon != null)
            {
                _icon.sprite = Item.Icon;
                _icon.enabled = Item.Icon != null;
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
