using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 面具切换按钮视图：点击切换双世界，并在“戴上/摘下”之间切换视觉状态。
/// </summary>
public sealed class MaskToggleButtonView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;

    [Header("裂痕（可选）")]
    [Tooltip("裂痕阶段图标：0=无裂痕；1=通关 1 关后；以此类推并自动封顶。\n若为空或未配置，将保持当前图标不变。")]
    [SerializeField] private Sprite[] _crackStageSprites;

    [Header("颜色")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _maskOnColor = new(0.85f, 0.95f, 1f, 1f);

    private MaskWorldController _controller;
    private LevelFlowController _levelFlow;
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
            _controller.MaskTransitioningChanged += HandleMaskTransitioningChanged;
        }

        if (_levelFlow == null)
        {
            _levelFlow = Object.FindObjectOfType<LevelFlowController>();
        }

        if (_levelFlow != null)
        {
            _levelFlow.LevelCompleted += HandleLevelCompleted;
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
            _controller.MaskTransitioningChanged -= HandleMaskTransitioningChanged;
        }

        if (_levelFlow != null)
        {
            _levelFlow.LevelCompleted -= HandleLevelCompleted;
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
        Refresh();
    }

    private void HandleMaskAcquired()
    {
        Refresh();
    }

    private void HandleMaskStateChanged(bool _)
    {
        Refresh();
    }

    private void HandleMaskTransitioningChanged(bool _)
    {
        Refresh();
    }

    private void HandleLevelCompleted(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_button != null)
        {
            _button.interactable = _controller != null && _controller.HasMask && !_controller.IsTransitioning;
        }

        if (_icon != null)
        {
            TryApplyCrackSprite();

            var isOn = _controller != null && _controller.IsMaskOn;
            _icon.color = isOn ? _maskOnColor : _normalColor;
        }
    }

    private void TryApplyCrackSprite()
    {
        if (_crackStageSprites == null || _crackStageSprites.Length == 0)
        {
            return;
        }

        var completed = _levelFlow != null ? _levelFlow.CompletedLevelCount : 0;
        var stageIndex = MaskCrackProgression.GetStageIndex(completed, _crackStageSprites.Length);
        if (stageIndex < 0 || stageIndex >= _crackStageSprites.Length)
        {
            return;
        }

        var stageSprite = _crackStageSprites[stageIndex];
        if (stageSprite == null)
        {
            return;
        }

        _icon.sprite = stageSprite;
    }
}
