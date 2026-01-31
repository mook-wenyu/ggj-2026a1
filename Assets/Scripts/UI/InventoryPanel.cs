using System;
using System.Collections.Generic;
using Ggj.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : MonoSingleton<InventoryPanel>
{
    private const string DefaultSlotPrefabResourcePath = "Prefabs/ItemSlot";

    [Header("引用（可不填，会自动查找）")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _slotTemplate;
    [SerializeField] private Canvas _rootCanvas;

    [Header("行为")]
    [Tooltip("为 true 时：列表视觉上从下到上（越上越新）；实现方式是把新物品插入到 Content 顶部。")]
    [SerializeField] private bool _sortFromBottomToTop = true;

    [Tooltip("添加物品后，是否自动滚动到最新一端（排序从下到上时滚到顶部）。")]
    [SerializeField] private bool _autoScrollToNewest = true;

    private readonly Dictionary<string, SlotView> _viewsById = new(StringComparer.Ordinal);
    private string _selectedItemId;

    /// <summary>
    /// 当前选中的物品 ID（点击槽位后会更新）。
    /// 说明：点击解密类常用于“选中后对场景物体使用”。本项目当前只做高亮与信息弹窗。
    /// </summary>
    public string SelectedItemId => _selectedItemId;

    private ItemInfoPopup _popup;
    private TMP_FontAsset _popupFont;
    private bool _initialized;
    private bool _subscribed;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        SubscribeInventoryEvents();
        RebuildAll();
    }

    private void OnDisable()
    {
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
    /// 新需求：只需要传入物品 ID；显示信息从 ExcelKit 生成的 ItemsConfig 读取。
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
            var t = transform.Find("Viewport/Content");
            _content = t as RectTransform;
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
            var tmp = _slotTemplate.GetComponentInChildren<TMP_Text>(true);
            _popupFont = tmp != null ? tmp.font : null;

            // 若模板是场景中的占位物体，运行时隐藏它，仅作为克隆源。
            if (_slotTemplate.scene.IsValid() && _slotTemplate.activeSelf)
            {
                _slotTemplate.SetActive(false);
            }
        }

        if (_popup == null && _rootCanvas != null)
        {
            _popup = ItemInfoPopup.Create(_rootCanvas.transform, _popupFont);
            _popup.Hide();
        }
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
        _popup?.Show(view.Item);
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

        private readonly GameObject _root;
        private readonly Button _button;
        private readonly Image _background;
        private readonly Image _icon;
        private readonly TMP_Text _label;
        private readonly Action<SlotView> _onClick;
        private readonly Color _normalBackgroundColor;

        public InventoryItem Item { get; private set; }

        public SlotView(GameObject root, InventoryItem item, Action<SlotView> onClick)
        {
            _root = root;
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

    private sealed class ItemInfoPopup
    {
        private readonly GameObject _root;
        private readonly TMP_Text _title;
        private readonly TMP_Text _description;
        private readonly Image _icon;

        private ItemInfoPopup(GameObject root, TMP_Text title, TMP_Text description, Image icon)
        {
            _root = root;
            _title = title;
            _description = description;
            _icon = icon;
        }

        public static ItemInfoPopup Create(Transform parent, TMP_FontAsset font)
        {
            // 半透明遮罩：点击遮罩即可关闭。
            var overlay = new GameObject("ItemInfoPopup", typeof(RectTransform), typeof(Image), typeof(Button));
            overlay.transform.SetParent(parent, false);
            overlay.transform.SetAsLastSibling();

            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.6f);

            var overlayButton = overlay.GetComponent<Button>();
            overlayButton.transition = Selectable.Transition.None;

            // 内容面板
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 420f);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(1f, 1f, 1f, 0.96f);

            // 用一个“空按钮”吞掉面板内的点击，避免事件冒泡到遮罩导致误关闭。
            var panelButton = panel.AddComponent<Button>();
            panelButton.transition = Selectable.Transition.None;
            panelButton.targetGraphic = panelImage;

            // 图标
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(panel.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(24f, -24f);
            iconRect.sizeDelta = new Vector2(128f, 128f);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.enabled = false;

            // 标题
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(168f, -28f);
            titleRect.sizeDelta = new Vector2(-216f, 48f);

            var titleText = titleGo.GetComponent<TextMeshProUGUI>();
            ApplyTmpStyle(titleText, font, 32, FontStyles.Bold, TextAlignmentOptions.Left);
            titleText.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // 描述
            var descGo = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGo.transform.SetParent(panel.transform, false);
            var descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 1f);
            descRect.offsetMin = new Vector2(24f, 24f);
            descRect.offsetMax = new Vector2(-24f, -96f);

            var descText = descGo.GetComponent<TextMeshProUGUI>();
            ApplyTmpStyle(descText, font, 22, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            descText.enableWordWrapping = true;
            descText.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // 关闭按钮
            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panel.transform, false);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);
            closeRect.sizeDelta = new Vector2(48f, 48f);

            var closeImage = closeGo.GetComponent<Image>();
            closeImage.color = new Color(0f, 0f, 0f, 0.08f);

            var closeButton = closeGo.GetComponent<Button>();

            var closeTextGo = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            var closeText = closeTextGo.GetComponent<TextMeshProUGUI>();
            ApplyTmpStyle(closeText, font, 28, FontStyles.Bold, TextAlignmentOptions.Center);
            closeText.text = "X";
            closeText.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var popup = new ItemInfoPopup(overlay, titleText, descText, iconImage);

            overlayButton.onClick.AddListener(popup.Hide);
            closeButton.onClick.AddListener(popup.Hide);

            return popup;
        }

        public void Show(InventoryItem item)
        {
            if (item == null)
            {
                return;
            }

            _root.SetActive(true);

            if (_title != null)
            {
                _title.text = item.GetDisplayNameOrId();
            }

            if (_description != null)
            {
                _description.text = item.GetDescriptionOrDefault();
            }

            if (_icon != null)
            {
                _icon.sprite = item.Icon;
                _icon.enabled = item.Icon != null;
            }
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        private static void ApplyTmpStyle(
            TextMeshProUGUI text,
            TMP_FontAsset font,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment
        )
        {
            if (text == null)
            {
                return;
            }

            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
        }
    }
}
