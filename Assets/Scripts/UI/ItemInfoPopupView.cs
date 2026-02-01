using System;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品详情弹窗：只展示一张图片（ItemsConfig.iconPath），点击任意位置关闭。
/// </summary>
public sealed class ItemInfoPopupView : MonoBehaviour
{
    [Header("引用（可选，未绑定会按层级名称自动查找）")]
    [SerializeField] private Button _overlayButton;
    [SerializeField] private Image _overlayImage;
    [SerializeField] private Image _icon;

    [Header("表现")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultOverlayAlpha = 0.6f;

    private Action _onClosed;
    private Action _onCloseRequested;

    private void Awake()
    {
        AutoBindIfNeeded();
        BindButtons();
        ApplyDefaultOverlayAlpha();

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

        if (_icon == null)
        {
            _icon = transform.Find("Icon")?.GetComponent<Image>();
        }
    }

    private void BindButtons()
    {
        if (_overlayButton == null)
        {
            return;
        }

        _overlayButton.onClick.RemoveAllListeners();
        _overlayButton.onClick.AddListener(HandleOverlayClicked);

        // 兜底：保证 Button 的 TargetGraphic 指向遮罩 Image。
        if (_overlayImage != null)
        {
            _overlayButton.targetGraphic = _overlayImage;
        }
    }

    private void ApplyDefaultOverlayAlpha()
    {
        if (_overlayImage == null)
        {
            return;
        }

        var c = _overlayImage.color;
        c.a = Mathf.Clamp01(_defaultOverlayAlpha);
        _overlayImage.color = c;
    }

    /// <summary>
    /// 展示物品详情：只使用 item.Icon。
    /// </summary>
    public void Show(InventoryItem item)
    {
        ShowIcon(item != null ? item.Icon : null, onClosed: null);
    }

    /// <summary>
    /// 兼容旧签名：忽略 showReplay/onReplayClicked。
    /// </summary>
    public void Show(InventoryItem item, bool showReplay, Action onReplayClicked)
    {
        ShowIcon(item != null ? item.Icon : null, onClosed: null);
    }

    public void ShowIcon(Sprite icon, Action onClosed = null)
    {
        AutoBindIfNeeded();
        gameObject.SetActive(true);

        _onClosed = onClosed;
        _onCloseRequested = null;

        ApplyDefaultOverlayAlpha();

        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }
    }

    public void Hide()
    {
        var onClosed = _onClosed;
        _onClosed = null;
        _onCloseRequested = null;
        gameObject.SetActive(false);

        // 回调放在 SetActive(false) 之后：避免外部在回调里立即 Show 造成闪烁/层级竞争。
        onClosed?.Invoke();
    }

    /// <summary>
    /// 拦截“关闭”行为（点击遮罩）。
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

    private void HandleOverlayClicked()
    {
        if (_onCloseRequested != null)
        {
            _onCloseRequested.Invoke();
            return;
        }

        Hide();
    }
}
