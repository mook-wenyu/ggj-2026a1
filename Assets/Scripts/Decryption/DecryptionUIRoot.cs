using System;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 解密 UI 根节点：统一管理所有解密界面的显示、隐藏与输入阻挡。
/// 说明：该类不直接依赖 PuzzleProgress 等玩法逻辑，避免 UI 与玩法强耦合。
/// </summary>
public sealed class DecryptionUIRoot : MonoSingleton<DecryptionUIRoot>, IConstellationDecryptionUI, ISafeDecryptionUI
{
    private enum OpenModal
    {
        None = 0,
        Constellation = 1,
        Safe = 2,
    }

    [Header("引用")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _overlayCloseButton;
    [SerializeField] private GridPatternLock _constellationPatternLock;
    [SerializeField] private SafeUI _safeUi;

    private bool _initialized;
    private bool _subscribed;
    private OpenModal _openModal;

    private Action _onSolved;
    private Action _onCancelled;

    private Action _onSafeSolved;
    private Action _onSafeCancelled;

    public bool IsOpen => _openModal != OpenModal.None;

    private void Awake()
    {
        // 允许场景里存在一个实例；若有重复，运行时自动销毁重复项。
        if (mInstance != null && mInstance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            return;
        }

        mInstance = this;
        EnsureInitialized();
        HideAllInternal(clearCallbacks: true);
    }

    public override void OnSingletonInit()
    {
        EnsureInitialized();
    }

    public bool TryShowConstellationPatternLock(
        Action onSolved,
        Action onCancelled = null,
        int[] acceptedPattern1Override = null,
        int[] acceptedPattern2Override = null)
    {
        EnsureInitialized();

        if (_constellationPatternLock == null)
        {
            Debug.LogError("DecryptionUIRoot: 未绑定星座解密（GridPatternLock）", this);
            return false;
        }

        // 若正在展示其他解密 UI，先关闭（不触发取消回调，避免逻辑重入）。
        HideAllInternal(clearCallbacks: true);

        _onSolved = onSolved;
        _onCancelled = onCancelled;

        _constellationPatternLock.OverrideAcceptedPatterns(acceptedPattern1Override, acceptedPattern2Override);
        SetVisible(true);
        _constellationPatternLock.Open();
        _openModal = OpenModal.Constellation;

        return true;
    }

    public bool TryShowSafeUi(Action onSolved, Action onCancelled = null)
    {
        EnsureInitialized();

        if (_safeUi == null)
        {
            Debug.LogError("DecryptionUIRoot: 未绑定保险柜 UI（SafeUI）", this);
            return false;
        }

        // 若正在展示其他解密 UI，先关闭（不触发取消回调，避免逻辑重入）。
        HideAllInternal(clearCallbacks: true);

        _onSafeSolved = onSolved;
        _onSafeCancelled = onCancelled;

        SetVisible(true);
        _safeUi.gameObject.SetActive(true);
        _openModal = OpenModal.Safe;
        return true;
    }

    public void CloseCurrent(bool invokeCancelled)
    {
        EnsureInitialized();

        if (_openModal == OpenModal.None)
        {
            return;
        }

        var closing = _openModal;

        if (invokeCancelled)
        {
            var constellationCb = _onCancelled;
            var safeCb = _onSafeCancelled;
            HideAllInternal(clearCallbacks: true);

            if (closing == OpenModal.Constellation)
            {
                constellationCb?.Invoke();
            }
            else if (closing == OpenModal.Safe)
            {
                safeCb?.Invoke();
            }
        }
        else
        {
            HideAllInternal(clearCallbacks: true);
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_constellationPatternLock == null)
        {
            _constellationPatternLock = GetComponentInChildren<GridPatternLock>(true);
        }

        if (_safeUi == null)
        {
            _safeUi = GetComponentInChildren<SafeUI>(true);
        }

        EnsureOverlayCloseButton();

        if (_overlayCloseButton != null)
        {
            _overlayCloseButton.onClick.RemoveAllListeners();
            _overlayCloseButton.onClick.AddListener(() => CloseCurrent(invokeCancelled: true));
        }

        SubscribeChildEvents();
        SetVisible(false);
    }

    private void EnsureOverlayCloseButton()
    {
        if (_overlayCloseButton != null)
        {
            return;
        }

        _overlayCloseButton = GetComponent<Button>();
        if (_overlayCloseButton != null)
        {
            return;
        }

        // 兜底：为根节点创建一个全屏透明遮罩（用于阻挡点击穿透，并允许点击背景关闭）。
        var overlayGo = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        overlayGo.transform.SetParent(transform, worldPositionStays: false);
        overlayGo.transform.SetAsFirstSibling();

        var rect = overlayGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = overlayGo.GetComponent<Image>();
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        image.color = new Color(0f, 0f, 0f, 0.6f);
        image.raycastTarget = true;

        var btn = overlayGo.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        _overlayCloseButton = btn;
    }

    private void SubscribeChildEvents()
    {
        if (_subscribed)
        {
            return;
        }

        _subscribed = true;

        if (_constellationPatternLock != null)
        {
            _constellationPatternLock.Solved += HandleConstellationSolved;
            _constellationPatternLock.Cancelled += HandleConstellationCancelled;
        }

        if (_safeUi != null)
        {
            _safeUi.Solved += HandleSafeSolved;
            _safeUi.Cancelled += HandleSafeCancelled;
        }
    }

    private void HandleConstellationSolved(int[] solvedPattern)
    {
        var cb = _onSolved;
        HideAllInternal(clearCallbacks: true);
        cb?.Invoke();
    }

    private void HandleConstellationCancelled()
    {
        var cb = _onCancelled;
        HideAllInternal(clearCallbacks: true);
        cb?.Invoke();
    }

    private void HandleSafeSolved()
    {
        var cb = _onSafeSolved;
        HideAllInternal(clearCallbacks: true);
        cb?.Invoke();
    }

    private void HandleSafeCancelled()
    {
        var cb = _onSafeCancelled;
        HideAllInternal(clearCallbacks: true);
        cb?.Invoke();
    }

    private void HideAllInternal(bool clearCallbacks)
    {
        if (_constellationPatternLock != null)
        {
            _constellationPatternLock.OverrideAcceptedPatterns(null, null);
            _constellationPatternLock.Hide();
        }

        if (_safeUi != null)
        {
            _safeUi.gameObject.SetActive(false);
        }

        SetVisible(false);
        _openModal = OpenModal.None;

        if (clearCallbacks)
        {
            _onSolved = null;
            _onCancelled = null;
            _onSafeSolved = null;
            _onSafeCancelled = null;
        }
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

}
