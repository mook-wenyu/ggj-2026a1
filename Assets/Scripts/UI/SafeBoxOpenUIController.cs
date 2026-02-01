using PrimeTween;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 保险柜开柜 UI 控制器：初次展示底图+奖杯，点击后奖杯丝滑左移、底图切换、显示故事与返回按钮。
/// </summary>
public class SafeBoxOpenUIController : MonoBehaviour
{
    [Header("引用（可留空，将按名称查找）")]
    [SerializeField] private RectTransform _baseImageRect;
    [SerializeField] private Image _baseImage;
    [SerializeField] private RectTransform _trophyRect;
    [SerializeField] private GameObject _storyObject;
    [SerializeField] private Button _returnButton;

    [Header("底图切换")]
    [Tooltip("点击后切换为此精灵（empty.png），留空则尝试从 Resources 加载")]
    [SerializeField] private Sprite _spriteEmpty;

    [Tooltip("初始精灵（Empty_light.png），留空则使用底图当前精灵")]
    [SerializeField] private Sprite _spriteEmptyLight;

    [Header("奖杯动画")]
    [SerializeField] private float _trophyAnimDuration = 0.6f;

    private bool _hasProceeded;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        _hasProceeded = false;
        ResetToInitialState();
        EnsureClickReceiver();
        EnsureReturnButton();
    }

    private void ResolveReferences()
    {
        if (_baseImageRect == null)
        {
            _baseImageRect = transform.Find("底图") as RectTransform;
        }

        if (_baseImage == null && _baseImageRect != null)
        {
            _baseImage = _baseImageRect.GetComponent<Image>();
        }

        if (_trophyRect == null)
        {
            _trophyRect = transform.Find("奖杯") as RectTransform;
        }

        if (_storyObject == null)
        {
            var story = transform.Find("故事");
            _storyObject = story != null ? story.gameObject : null;
        }

        if (_returnButton == null)
        {
            var ret = transform.Find("返回");
            _returnButton = ret != null ? ret.GetComponent<Button>() : null;
        }
    }

    private void ResetToInitialState()
    {
        if (_storyObject != null)
        {
            _storyObject.SetActive(false);
        }

        if (_returnButton != null)
        {
            _returnButton.gameObject.SetActive(false);
        }

        if (_baseImage != null)
        {
            if (_spriteEmptyLight != null)
            {
                _baseImage.sprite = _spriteEmptyLight;
            }
        }

        if (_trophyRect != null)
        {
            _trophyRect.anchoredPosition = new Vector2(-139f, -83f);
            _trophyRect.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            _trophyRect.localEulerAngles = Vector3.zero;
            _trophyRect.sizeDelta = new Vector2(280f, 418f);
        }
    }

    private void EnsureClickReceiver()
    {
        if (_baseImageRect != null && _baseImage != null)
        {
            SetupClickButton(_baseImageRect, _baseImage);
        }

        if (_trophyRect != null)
        {
            var img = _trophyRect.GetComponent<Image>();
            if (img != null)
            {
                SetupClickButton(_trophyRect, img);
            }
        }
    }

    private void SetupClickButton(RectTransform rect, Image graphic)
    {
        var btn = rect.GetComponent<Button>();
        if (btn == null)
        {
            btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = graphic;
            btn.transition = Selectable.Transition.None;
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnProceedClick);
    }

    private void EnsureReturnButton()
    {
        if (_returnButton == null)
        {
            return;
        }

        _returnButton.onClick.RemoveAllListeners();
        _returnButton.onClick.AddListener(OnReturnClick);
    }

    private void OnProceedClick()
    {
        if (_hasProceeded)
        {
            return;
        }

        _hasProceeded = true;
        DisableClickReceivers();

        if (_trophyRect != null)
        {
            AnimateTrophy();
        }
        else
        {
            ShowContentAfterTrophy();
        }
    }

    private void ShowContentAfterTrophy()
    {
        if (_baseImage != null && _spriteEmpty != null)
        {
            _baseImage.sprite = _spriteEmpty;
        }

        if (_storyObject != null)
        {
            _storyObject.SetActive(true);
        }

        if (_returnButton != null)
        {
            _returnButton.gameObject.SetActive(true);
        }
    }

    private void DisableClickReceivers()
    {
        if (_baseImageRect != null)
        {
            var btn = _baseImageRect.GetComponent<Button>();
            if (btn != null) btn.enabled = false;
        }

        if (_trophyRect != null)
        {
            var btn = _trophyRect.GetComponent<Button>();
            if (btn != null) btn.enabled = false;
        }
    }

    private void AnimateTrophy()
    {
        var duration = _trophyAnimDuration;

        Sequence.Create()
            .Group(Tween.UIAnchoredPosition(_trophyRect, new Vector2(-528f, -31f), duration, Ease.OutCubic))
            .Group(Tween.Scale(_trophyRect, new Vector3(1.8f, 1.8f, 1.8f), duration, Ease.OutCubic))
            .Group(Tween.LocalEulerAngles(_trophyRect, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, -9.14f), duration, Ease.OutCubic))
            .ChainCallback(ShowContentAfterTrophy);
    }

    private void OnReturnClick()
    {
        gameObject.SetActive(false);
    }
}
