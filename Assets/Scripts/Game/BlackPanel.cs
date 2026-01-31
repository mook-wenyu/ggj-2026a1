using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡黑屏过场。
/// 职责：提供淡入/淡出黑屏能力；具体“切关卡”流程由更上层的 LevelFlowController 负责。
/// </summary>
public sealed class BlackPanel : MonoBehaviour
{
    [Header("引用（可不填，会自动查找/补齐）")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _image;

    [Header("行为")]
    [SerializeField] private bool _startHidden = true;

    [Tooltip("黑屏淡入/淡出时长（秒）。")]
    [SerializeField] private float _fadeDuration = 0.35f;

    private Coroutine _fadeRoutine;

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

        if (_image != null)
        {
            _image.color = Color.black;
        }

        if (_startHidden)
        {
            SetAlphaImmediate(0f);
        }
        else
        {
            SetAlphaImmediate(1f);
        }
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
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration, onComplete));
    }

    public void SetAlphaImmediate(float alpha)
    {
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
