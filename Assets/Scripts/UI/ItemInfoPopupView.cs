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
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _replayButton;
    [SerializeField] private GameObject _replayRoot;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private RectTransform _descriptionRect;
    [SerializeField] private Image _icon;

    private Action _onReplay;

    private void Awake()
    {
        AutoBindIfNeeded();
        BindButtons();

        // 统一约定：弹窗预制体在首次实例化后默认隐藏。
        Hide();
    }

    private void AutoBindIfNeeded()
    {
        if (_overlayButton == null)
        {
            _overlayButton = GetComponent<Button>();
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
    }

    private void BindButtons()
    {
        if (_overlayButton != null)
        {
            _overlayButton.onClick.RemoveAllListeners();
            _overlayButton.onClick.AddListener(Hide);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(Hide);
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

        _onReplay = onReplayClicked;

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

    public void Hide()
    {
        _onReplay = null;
        gameObject.SetActive(false);
    }

    private void HandleReplayClicked()
    {
        _onReplay?.Invoke();
    }
}
