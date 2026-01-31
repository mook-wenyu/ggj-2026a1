using System;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 解密 UI 根节点：统一管理所有解密界面的显示、隐藏与输入阻挡。
/// 说明：该类不直接依赖 PuzzleProgress 等玩法逻辑，避免 UI 与玩法强耦合。
/// </summary>
public sealed class DecryptionUIRoot : MonoSingleton<DecryptionUIRoot>, IConstellationDecryptionUI
{
    [Header("引用")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _overlayCloseButton;
    [SerializeField] private GridPatternLock _constellationPatternLock;

    private bool _initialized;
    private bool _subscribed;
    private bool _isOpen;

    private Action _onSolved;
    private Action _onCancelled;

    public bool IsOpen => _isOpen;

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
        _isOpen = true;

        return true;
    }

    public void CloseCurrent(bool invokeCancelled)
    {
        EnsureInitialized();

        if (!_isOpen)
        {
            return;
        }

        if (invokeCancelled)
        {
            var cb = _onCancelled;
            HideAllInternal(clearCallbacks: true);
            cb?.Invoke();
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

        if (_overlayCloseButton == null)
        {
            _overlayCloseButton = GetComponent<Button>();
        }

        if (_overlayCloseButton != null)
        {
            _overlayCloseButton.onClick.RemoveAllListeners();
            _overlayCloseButton.onClick.AddListener(() => CloseCurrent(invokeCancelled: true));
        }

        SubscribeChildEvents();
        SetVisible(false);
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

    private void HideAllInternal(bool clearCallbacks)
    {
        if (_constellationPatternLock != null)
        {
            _constellationPatternLock.OverrideAcceptedPatterns(null, null);
            _constellationPatternLock.Hide();
        }

        SetVisible(false);
        _isOpen = false;

        if (clearCallbacks)
        {
            _onSolved = null;
            _onCancelled = null;
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
