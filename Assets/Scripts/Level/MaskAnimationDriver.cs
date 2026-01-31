using System.Collections;

using UnityEngine;

/// <summary>
/// 面具动画驱动器：监听 MaskWorldController 的状态变化，播放“戴面具/摘面具”动画。
/// 设计目标：
/// - 玩法状态（MaskWorldController）与表现（Animator/SpriteRenderer）解耦；
/// - 允许在 Inspector 显式绑定引用；未绑定时提供可控兜底查找；
/// - 只在状态发生变化时播放动画（避免 AcquireMask() 触发的重复 false 导致误播放）。
/// </summary>
public sealed class MaskAnimationDriver : MonoBehaviour
{
    private const string DefaultMaskObjectName = "mask";
    private const string MaskBoolParamName = "mask";
    private const string WearStateName = "wear_mask";
    private const string RemoveStateName = "removal_mask";
    private const float DefaultClipDurationSeconds = 0.75f;

    [Header("引用（推荐显式绑定）")]
    [SerializeField] private MaskWorldController _controller;
    [SerializeField] private Animator _maskAnimator;
    [SerializeField] private GameObject _maskRoot;
    [SerializeField] private AnimationClip _wearClip;

    [Header("行为")]
    [Tooltip("摘面具动画播完后，是否自动隐藏 mask 根物体。")]
    [SerializeField] private bool _hideWhenMaskOff = true;

    private bool _subscribed;
    private bool _hasKnownState;
    private bool _isMaskOn;
    private Coroutine _hideRoutine;

    private void Awake()
    {
        if (_controller == null)
        {
            _controller = GetComponent<MaskWorldController>();
            if (_controller == null)
            {
                _controller = Object.FindObjectOfType<MaskWorldController>();
            }
        }

        EnsureAnimatorBound();
        CacheWearClip();

        // 初始：只同步显示状态，不播放动画。
        _isMaskOn = _controller != null && _controller.IsMaskOn;
        _hasKnownState = true;
        ApplyStaticVisibility(_isMaskOn);

        Subscribe();
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

        if (_controller != null)
        {
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

        if (_controller != null)
        {
            _controller.MaskStateChanged -= HandleMaskStateChanged;
        }

        _subscribed = false;
    }

    private void EnsureAnimatorBound()
    {
        if (_maskAnimator != null)
        {
            if (_maskRoot == null)
            {
                _maskRoot = _maskAnimator.gameObject;
            }
            return;
        }

        // 兜底：从场景中查找（包含 inactive）。
        var animators = Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var a in animators)
        {
            if (a != null && a.gameObject.name == DefaultMaskObjectName)
            {
                _maskAnimator = a;
                _maskRoot = a.gameObject;
                break;
            }
        }

        if (_maskAnimator == null)
        {
            Debug.LogError(
                $"MaskAnimationDriver: 场景中找不到名为 {DefaultMaskObjectName} 的 Animator（用于面具动画）。请在 Inspector 绑定 _maskAnimator。",
                this);
        }
    }

    private void CacheWearClip()
    {
        if (_wearClip != null || _maskAnimator == null)
        {
            return;
        }

        var controller = _maskAnimator.runtimeAnimatorController;
        if (controller == null)
        {
            return;
        }

        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name == WearStateName)
            {
                _wearClip = clip;
                return;
            }
        }
    }

    private void HandleMaskStateChanged(bool isMaskOn)
    {
        // 关键：只在“状态发生变化”时播放动画，避免 AcquireMask() 触发的重复 false 误播放摘面具动画。
        if (_hasKnownState && isMaskOn == _isMaskOn)
        {
            return;
        }

        _isMaskOn = isMaskOn;
        _hasKnownState = true;

        if (_maskAnimator == null || _maskRoot == null)
        {
            return;
        }

        StopHideRoutineIfNeeded();
        _maskRoot.SetActive(true);

        // 只改参数，让 AnimatorController 负责状态切换。
        _maskAnimator.SetBool(MaskBoolParamName, isMaskOn);

        if (!isMaskOn && _hideWhenMaskOff)
        {
            _hideRoutine = StartCoroutine(HideAfterSeconds(GetClipDurationSeconds()));
        }
    }

    private void ApplyStaticVisibility(bool isMaskOn)
    {
        if (_maskRoot == null)
        {
            return;
        }

        // 未戴面具：隐藏整个对象，避免屏幕上出现第一帧。
        if (!isMaskOn)
        {
            _maskRoot.SetActive(false);
            return;
        }

        _maskRoot.SetActive(true);
        if (_maskAnimator != null)
        {
            // 初始为“已戴上”时：直接定位到最后一帧，避免开局播一次动画。
            _maskAnimator.SetBool(MaskBoolParamName, true);
            _maskAnimator.Play(WearStateName, 0, 1f);
            _maskAnimator.Update(0f);
        }
    }

    private float GetClipDurationSeconds()
    {
        return _wearClip != null ? _wearClip.length : DefaultClipDurationSeconds;
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        if (seconds > 0f)
        {
            yield return new WaitForSeconds(seconds);
        }

        // 若期间又戴上了面具，则不隐藏。
        if (!_isMaskOn && _maskRoot != null)
        {
            _maskRoot.SetActive(false);
        }

        _hideRoutine = null;
    }

    private void StopHideRoutineIfNeeded()
    {
        if (_hideRoutine == null)
        {
            return;
        }

        StopCoroutine(_hideRoutine);
        _hideRoutine = null;
    }
}
