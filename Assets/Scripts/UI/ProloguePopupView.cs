using System;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开场叙事弹窗：全屏纯黑背景 + 白色大字居中显示。
/// 
/// 说明：该视图不复用物品详情面板，避免样式耦合与误用。
/// </summary>
public sealed class ProloguePopupView : MonoBehaviour
{
    private const int TitleFontSize = 60;
    private const int BodyFontSize = 34;

    private static Sprite s_solidSprite;

    [Header("引用（可选，未绑定会按层级名称自动查找）")]
    [SerializeField] private Button _overlayButton;
    [SerializeField] private Image _overlayImage;
    [SerializeField] private GameObject _contentRoot;
    [SerializeField] private TMP_Text _contentText;

    private Action _onClosed;
    private Action _onCloseRequested;

    private void Awake()
    {
        AutoBindIfNeeded();
        ApplyLayout();
        ApplyStyle();
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

        if (_overlayImage == null)
        {
            _overlayImage = GetComponent<Image>();
        }

        if (_contentRoot == null)
        {
            _contentRoot = transform.Find("Content")?.gameObject;
            if (_contentRoot == null)
            {
                // 兜底：简化版预制体可能没有 Content 节点。
                _contentRoot = transform.Find("Text")?.gameObject;
            }
        }

        if (_contentText == null)
        {
            _contentText = transform.Find("Content/Text")?.GetComponent<TMP_Text>();
            if (_contentText == null)
            {
                _contentText = transform.Find("Text")?.GetComponent<TMP_Text>();
            }

            if (_contentText == null)
            {
                _contentText = GetComponentInChildren<TMP_Text>(includeInactive: true);
            }
        }

        if (_contentRoot == null && _contentText != null)
        {
            _contentRoot = _contentText.gameObject;
        }
    }

    private void ApplyLayout()
    {
        if (TryGetComponent<RectTransform>(out var rootRect))
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.localScale = Vector3.one;
        }

        RectTransform contentRect = null;
        if (_contentRoot != null)
        {
            contentRect = _contentRoot.GetComponent<RectTransform>();
        }

        RectTransform textRect = null;
        if (_contentText != null)
        {
            textRect = _contentText.rectTransform;
        }

        // 内容区域：尽量使用较大的边距，保证大字在各分辨率下不会贴边。
        if (contentRect != null && contentRect != rootRect)
        {
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(120f, 120f);
            contentRect.offsetMax = new Vector2(-120f, -120f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.localScale = Vector3.one;
        }

        if (textRect == null)
        {
            return;
        }

        // 若内容根节点就是 Text，自身使用边距；否则 Text 填满内容根节点。
        if (_contentRoot == _contentText.gameObject)
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(120f, 120f);
            textRect.offsetMax = new Vector2(-120f, -120f);
        }
        else
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        textRect.anchoredPosition = Vector2.zero;
        textRect.localScale = Vector3.one;
    }

    private void ApplyStyle()
    {
        if (_overlayImage != null)
        {
            if (_overlayImage.sprite == null)
            {
                _overlayImage.sprite = GetSolidSprite();
            }

            var c = _overlayImage.color;
            c.r = 0f;
            c.g = 0f;
            c.b = 0f;
            _overlayImage.color = c;
            _overlayImage.raycastTarget = true;
        }

        if (_contentText != null)
        {
            _contentText.color = Color.white;
            _contentText.enableWordWrapping = true;
            _contentText.richText = true;
            _contentText.alignment = TextAlignmentOptions.Center;
            _contentText.raycastTarget = false;
        }
    }

    private static Sprite GetSolidSprite()
    {
        if (s_solidSprite != null)
        {
            return s_solidSprite;
        }

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;

        s_solidSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit: 1f
        );
        return s_solidSprite;
    }

    private void BindButtons()
    {
        if (_overlayButton == null)
        {
            return;
        }

        _overlayButton.onClick.RemoveAllListeners();
        _overlayButton.onClick.AddListener(HandleOverlayClicked);

        // 兜底：保证 Button 的 TargetGraphic 指向黑底 Image。
        if (_overlayImage != null)
        {
            _overlayButton.targetGraphic = _overlayImage;
        }
    }

    public void ShowText(string title, string description, Sprite icon = null, Action onClosed = null)
    {
        // icon 参数保留用于签名兼容（开场弹窗不显示 icon）。
        gameObject.SetActive(true);

        _onClosed = onClosed;
        _onCloseRequested = null;

        SetPanelVisible(true);

        if (_contentText != null)
        {
            var safeTitle = title ?? string.Empty;
            var safeBody = description ?? string.Empty;

            _contentText.text = string.IsNullOrWhiteSpace(safeTitle)
                ? safeBody
                : $"<size={TitleFontSize}>{safeTitle}</size>\n\n<size={BodyFontSize}>{safeBody}</size>";
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

    public void SetPanelVisible(bool visible)
    {
        if (_contentRoot != null)
        {
            _contentRoot.SetActive(visible);
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
