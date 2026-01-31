using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品详情弹窗视图：尽量通过预制体搭建 UI，仅在运行时填充内容与绑定回调。
/// </summary>
public sealed class ItemInfoPopupView : MonoBehaviour
{
    private static readonly Vector2 DescOffsetMinNoReplay = new(24f, 24f);
    private static readonly Vector2 DescOffsetMinWithReplay = new(24f, 88f);

    [Header("引用（可选，未绑定会按层级名称自动查找）")]
    [SerializeField] private Button _overlayButton;
    [SerializeField] private Image _overlayImage;
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _replayButton;
    [SerializeField] private GameObject _replayRoot;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private RectTransform _descriptionRect;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _panelBackground;

    private Sprite _defaultPanelBackground;

    private Action _onReplay;
    private Action _onClosed;
    private Action _onCloseRequested;

    private void Awake()
    {
        AutoBindIfNeeded();
        BindButtons();

        _defaultPanelBackground = _panelBackground != null ? _panelBackground.sprite : null;

        // 统一约定：弹窗预制体在首次实例化后默认隐藏。
        Hide();
    }

    private void AutoBindIfNeeded()
    {
        if (_overlayButton == null)
        {
            _overlayButton = GetComponent<Button>();
        }

        if (_overlayImage == null)
        {
            _overlayImage = GetComponent<Image>();
        }

        if (_panelRoot == null)
        {
            _panelRoot = transform.Find("Panel")?.gameObject;
        }

        if (_closeButton == null)
        {
            _closeButton = transform.Find("Panel/Close")?.GetComponent<Button>();
        }

        if (_replayButton == null)
        {
            _replayButton = transform.Find("Panel/Replay")?.GetComponent<Button>();
        }

        if (_replayRoot == null)
        {
            _replayRoot = _replayButton != null
                ? _replayButton.gameObject
                : transform.Find("Panel/Replay")?.gameObject;
        }

        if (_icon == null)
        {
            _icon = transform.Find("Panel/Icon")?.GetComponent<Image>();
        }

        if (_title == null)
        {
            _title = transform.Find("Panel/Title")?.GetComponent<TMP_Text>();
        }

        if (_description == null)
        {
            _description = transform.Find("Panel/Description")?.GetComponent<TMP_Text>();
        }

        if (_descriptionRect == null)
        {
            _descriptionRect = _description != null
                ? _description.rectTransform
                : transform.Find("Panel/Description")?.GetComponent<RectTransform>();
        }

        if (_panelBackground == null)
        {
            _panelBackground = transform.Find("Panel")?.GetComponent<Image>();
        }
    }

    private void BindButtons()
    {
        if (_overlayButton != null)
        {
            _overlayButton.onClick.RemoveAllListeners();
            _overlayButton.onClick.AddListener(HandleCloseClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (_replayButton != null)
        {
            _replayButton.onClick.RemoveAllListeners();
            _replayButton.onClick.AddListener(HandleReplayClicked);
        }
    }

    public void Show(InventoryItem item, bool showReplay, Action onReplayClicked)
    {
        if (item == null)
        {
            return;
        }

        gameObject.SetActive(true);

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

        if (_panelBackground != null)
        {
            _panelBackground.sprite = item.DetailBackground != null
                ? item.DetailBackground
                : _defaultPanelBackground;
        }

        _onReplay = onReplayClicked;
        _onClosed = null;
        _onCloseRequested = null;

        if (_replayRoot != null)
        {
            _replayRoot.SetActive(showReplay);
        }

        if (_replayButton != null)
        {
            _replayButton.interactable = showReplay && _onReplay != null;
        }

        if (_descriptionRect != null)
        {
            _descriptionRect.offsetMin = showReplay ? DescOffsetMinWithReplay : DescOffsetMinNoReplay;
        }
    }

    /// <summary>
    /// 通用文本弹窗（不依赖 InventoryItem）。
    /// 主要用于剧情/提示等一次性叙事场景。
    /// </summary>
    public void ShowText(string title, string description, Sprite icon = null, Action onClosed = null)
    {
        gameObject.SetActive(true);

        if (_title != null)
        {
            _title.text = title ?? string.Empty;
        }

        if (_description != null)
        {
            _description.text = description ?? string.Empty;
        }

        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        if (_panelBackground != null)
        {
            _panelBackground.sprite = _defaultPanelBackground;
        }

        _onReplay = null;
        _onClosed = onClosed;
        _onCloseRequested = null;

        if (_replayRoot != null)
        {
            _replayRoot.SetActive(false);
        }

        if (_replayButton != null)
        {
            _replayButton.interactable = false;
        }

        if (_descriptionRect != null)
        {
            _descriptionRect.offsetMin = DescOffsetMinNoReplay;
        }
    }

    public void Hide()
    {
        _onReplay = null;
        var onClosed = _onClosed;
        _onClosed = null;
        _onCloseRequested = null;
        gameObject.SetActive(false);

        // 回调放在 SetActive(false) 之后：避免外部在回调里立即 Show 造成闪烁/层级竞争。
        onClosed?.Invoke();
    }

    /// <summary>
    /// 拦截“关闭”行为（遮罩点击/关闭按钮）。
    /// 若设置了该回调，则点击不会自动 Hide()。
    /// </summary>
    public void SetCloseRequestedHandler(Action onCloseRequested)
    {
        _onCloseRequested = onCloseRequested;
    }

    public void ClearCloseRequestedHandler()
    {
        _onCloseRequested = null;
    }

    public void SetCloseInteractable(bool interactable)
    {
        if (_overlayButton != null)
        {
            _overlayButton.interactable = interactable;
        }

        if (_closeButton != null)
        {
            _closeButton.interactable = interactable;
        }
    }

    public void SetPanelVisible(bool visible)
    {
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(visible);
        }
    }

    public void SetOverlayAlpha(float alpha)
    {
        if (_overlayImage == null)
        {
            return;
        }

        var c = _overlayImage.color;
        c.a = Mathf.Clamp01(alpha);
        _overlayImage.color = c;
    }

    private void HandleCloseClicked()
    {
        if (_onCloseRequested != null)
        {
            _onCloseRequested.Invoke();
            return;
        }

        Hide();
    }

    private void HandleReplayClicked()
    {
        _onReplay?.Invoke();
    }
}
