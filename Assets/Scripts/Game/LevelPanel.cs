using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 下一关界面（全局唯一）。
/// 约定：默认“点击任意位置继续”，也可在 Inspector 绑定按钮。
/// </summary>
public sealed class LevelPanel : MonoSingleton<LevelPanel>, IPointerClickHandler
{
    public event Action ContinueRequested;

    [Header("引用（可不填，会自动查找/补齐）")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _continueButton;

    [Header("行为")]
    [SerializeField] private bool _startHidden = true;

    private bool _subscribed;
    private bool _initialized;

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
        SetVisible(!_startHidden);
    }

    public override void OnSingletonInit()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RequestContinue();
    }

    private void Subscribe()
    {
        if (_subscribed)
        {
            return;
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(RequestContinue);
        }

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(RequestContinue);
        }

        _subscribed = false;
    }

    private void RequestContinue()
    {
        ContinueRequested?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        // 场景里常把该面板设置为 inactive；这里确保 Show() 能真正显示。
        if (visible && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (_canvasGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
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

        if (_continueButton == null)
        {
            _continueButton = GetComponentInChildren<Button>(true);
        }
    }
}
