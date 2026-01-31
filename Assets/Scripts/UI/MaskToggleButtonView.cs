using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 面具切换按钮视图：点击切换双世界，并在“戴上/摘下”之间切换视觉状态。
/// </summary>
public sealed class MaskToggleButtonView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;

    [Header("颜色")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _maskOnColor = new(0.85f, 0.95f, 1f, 1f);

    private MaskWorldController _controller;
    private bool _subscribed;

    public void Bind(MaskWorldController controller)
    {
        Unsubscribe();
        _controller = controller;
        Subscribe();
        Refresh();
    }

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_icon == null)
        {
            _icon = GetComponent<Image>();
        }

        Subscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_subscribed)
        {
            return;
        }

        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }

        if (_controller != null)
        {
            _controller.MaskAcquired += HandleMaskAcquired;
            _controller.MaskStateChanged += HandleMaskStateChanged;
        }

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        if (_controller != null)
        {
            _controller.MaskAcquired -= HandleMaskAcquired;
            _controller.MaskStateChanged -= HandleMaskStateChanged;
        }

        _subscribed = false;
    }

    private void HandleClick()
    {
        if (_controller == null)
        {
            _controller = Object.FindObjectOfType<MaskWorldController>();
            if (_controller != null)
            {
                Bind(_controller);
            }
        }

        _controller?.ToggleMask();
    }

    private void HandleMaskAcquired()
    {
        Refresh();
    }

    private void HandleMaskStateChanged(bool _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_button != null)
        {
            _button.interactable = _controller != null && _controller.HasMask;
        }

        if (_icon != null)
        {
            var isOn = _controller != null && _controller.IsMaskOn;
            _icon.color = isOn ? _maskOnColor : _normalColor;
        }
    }
}
