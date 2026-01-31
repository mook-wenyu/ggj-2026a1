using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡黑屏过场（全局唯一）。
/// 职责：提供淡入/淡出黑屏能力；具体“切关卡”流程由更上层的 LevelFlowController 负责。
/// </summary>
public sealed class BlackPanel : MonoSingleton<BlackPanel>
{
    [Header("引用（可不填，会自动查找/补齐）")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _image;

    [Header("行为")]
    [SerializeField] private bool _startHidden = true;

    [Tooltip("黑屏淡入/淡出时长（秒）。")]
    [SerializeField] private float _fadeDuration = 0.35f;

    private Coroutine _fadeRoutine;
    private bool _initialized;

    public bool IsFading => _fadeRoutine != null;

    /// <summary>
    /// 控制黑屏是否拦截输入。
    /// 用于关卡切换流程中，在不同阶段控制输入（例如：展示下一关界面时不拦截；切关过场时拦截）。
    /// </summary>
    public void SetInputBlocked(bool blocked)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.interactable = blocked;
        _canvasGroup.blocksRaycasts = blocked;
    }

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

        SetAlphaImmediate(_startHidden ? 0f : 1f);
    }

    public override void OnSingletonInit()
    {
        EnsureInitialized();
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

        if (_image == null)
        {
            _image = GetComponent<Image>();
        }

        // 兜底：若未配置 Image，也尽量保证黑屏可见。
        if (_image == null)
        {
            _image = gameObject.AddComponent<Image>();
        }

        _image.color = Color.black;
    }

    public void FadeIn(Action onComplete = null)
    {
        FadeTo(1f, _fadeDuration, onComplete);
    }

    public void FadeOut(Action onComplete = null)
    {
        FadeTo(0f, _fadeDuration, onComplete);
    }

    public void FadeTo(float targetAlpha, float duration, Action onComplete = null)
    {
        EnsureInitialized();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration, onComplete));
    }

    public void SetAlphaImmediate(float alpha)
    {
        EnsureInitialized();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        SetAlphaInternal(alpha);
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
    {
        var startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
        var t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            var p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            var a = Mathf.Lerp(startAlpha, targetAlpha, p);
            SetAlphaInternal(a);
            yield return null;
        }

        SetAlphaInternal(targetAlpha);
        _fadeRoutine = null;
        onComplete?.Invoke();
    }

    private void SetAlphaInternal(float alpha)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        alpha = Mathf.Clamp01(alpha);
        _canvasGroup.alpha = alpha;

        // 默认：alpha=0 时不挡输入；alpha>0 时遮挡。
        // 需要特殊输入策略时，可在外部调用 SetInputBlocked 覆盖。
        var blocks = alpha > 0.001f;
        _canvasGroup.interactable = blocks;
        _canvasGroup.blocksRaycasts = blocks;
    }
}
