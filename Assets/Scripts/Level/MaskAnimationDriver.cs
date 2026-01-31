using System.Collections;

using UnityEngine;

/// <summary>
/// 面具切换表现（动画 + 渐隐）的驱动器。
///
/// 重要：该脚本只负责“表现”，不负责改变玩法状态（切换世界由 MaskWorldController 负责）。
/// </summary>
public sealed class MaskAnimationDriver : MonoBehaviour
{
    private const string DefaultMaskObjectName = "mask";
    private const string MaskBoolParamName = "mask";
    private const string WearStateName = "wear_mask";
    private const string RemoveStateName = "removal_mask";
    private const float DefaultClipDurationSeconds = 0.75f;
    private const float HoldLastFrameNormalizedTime = 0.999f;

    [Header("引用（推荐显式绑定）")]
    [SerializeField] private Animator _maskAnimator;
    [SerializeField] private GameObject _maskRoot;
    [SerializeField] private SpriteRenderer _maskSprite;
    [SerializeField] private AnimationClip _wearClip;

    [Header("时序")]
    [Tooltip("戴上面具动画结束后，额外停留时间（让玩家看清‘定格戴上’）。")]
    [SerializeField] private float _holdAfterWearSeconds = 0.05f;
    [Tooltip("切世界后，面具的快速渐隐时长。")]
    [SerializeField] private float _fadeOutSeconds = 0.15f;
    [Tooltip("是否使用不受 Time.timeScale 影响的时间（用于等待/渐隐）。")]
    [SerializeField] private bool _useUnscaledTime = false;

    private bool _loggedMissingAnimator;

    private void Awake()
    {
        EnsureReferencesBound();
        HideImmediate();
    }

    public void ShowWornPose()
    {
        EnsureReferencesBound();
        if (_maskAnimator == null || _maskRoot == null)
        {
            return;
        }

        _maskRoot.SetActive(true);
        SetAlpha(1f);

        // 让 AnimatorController 处于“戴上”条件，避免 AnyState 逻辑把状态拉回摘面具。
        _maskAnimator.SetBool(MaskBoolParamName, true);

        // 直接定位到最后一帧，作为“已戴上”的定格画面。
        _maskAnimator.speed = 1f;
        // 注意：normalizedTime 的整数部分代表“循环次数”。用 1f 可能会落到下一次循环的 0% 导致显示第一帧。
        _maskAnimator.Play(WearStateName, 0, HoldLastFrameNormalizedTime);
        _maskAnimator.Update(0f);
    }

    public IEnumerator PlayWearAndHold()
    {
        EnsureReferencesBound();
        if (_maskAnimator == null || _maskRoot == null)
        {
            yield break;
        }

        _maskRoot.SetActive(true);
        SetAlpha(1f);

        _maskAnimator.speed = 1f;
        _maskAnimator.SetBool(MaskBoolParamName, true);
        _maskAnimator.Play(WearStateName, 0, 0f);
        _maskAnimator.Update(0f);

        yield return WaitSeconds(GetClipDurationSeconds());

        // 定格到最后一帧（避免小数误差导致不是最终帧）。
        _maskAnimator.Play(WearStateName, 0, HoldLastFrameNormalizedTime);
        _maskAnimator.Update(0f);

        if (_holdAfterWearSeconds > 0f)
        {
            yield return WaitSeconds(_holdAfterWearSeconds);
        }
    }

    public IEnumerator FadeOutAndHide()
    {
        EnsureReferencesBound();
        if (_maskRoot == null)
        {
            yield break;
        }

        // 没有 SpriteRenderer 也能正常隐藏，只是没有渐隐。
        if (_maskSprite == null)
        {
            _maskRoot.SetActive(false);
            yield break;
        }

        var duration = Mathf.Max(0f, _fadeOutSeconds);
        if (duration <= 0f)
        {
            SetAlpha(0f);
            _maskRoot.SetActive(false);
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            var t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(1f, 0f, t));
            elapsed += GetDeltaTime();
            yield return null;
        }

        SetAlpha(0f);
        _maskRoot.SetActive(false);
    }

    public IEnumerator PlayRemovalAndHide()
    {
        EnsureReferencesBound();
        if (_maskAnimator == null || _maskRoot == null)
        {
            yield break;
        }

        _maskRoot.SetActive(true);
        SetAlpha(1f);

        // 说明：我们不依赖“负 speed 直接倒放”（在部分情况下会出现冻结/采样异常）。
        // 这里通过“逐帧采样 normalizedTime 从 1 -> 0”来稳定实现倒放。
        var duration = GetClipDurationSeconds();
        if (duration <= 0f)
        {
            _maskRoot.SetActive(false);
            yield break;
        }

        // 暂停 Animator 自驱动时间推进，由我们手动采样。
        _maskAnimator.speed = 0f;

        // 保持在 wear 状态即可（clip 相同），避免依赖 removal 状态的负 speed。
        _maskAnimator.SetBool(MaskBoolParamName, true);

        var elapsed = 0f;
        while (elapsed < duration)
        {
            var t = Mathf.Clamp01(elapsed / duration);
            var normalized = Mathf.Lerp(HoldLastFrameNormalizedTime, 0f, t);
            _maskAnimator.Play(WearStateName, 0, normalized);
            _maskAnimator.Update(0f);

            elapsed += GetDeltaTime();
            yield return null;
        }

        // 结束：确保落在第一帧（未戴上）。
        _maskAnimator.Play(WearStateName, 0, 0f);
        _maskAnimator.Update(0f);

        _maskRoot.SetActive(false);

        // 还原 Animator，避免影响下一次戴面具。
        _maskAnimator.speed = 1f;
        _maskAnimator.SetBool(MaskBoolParamName, false);
    }

    public void HideImmediate()
    {
        EnsureReferencesBound();
        if (_maskRoot == null)
        {
            return;
        }

        SetAlpha(1f);
        _maskRoot.SetActive(false);
    }

    private void EnsureReferencesBound()
    {
        if (_maskAnimator != null)
        {
            if (_maskRoot == null)
            {
                _maskRoot = _maskAnimator.gameObject;
            }

            if (_maskSprite == null && _maskRoot != null)
            {
                _maskSprite = _maskRoot.GetComponent<SpriteRenderer>();
            }

            CacheWearClip();
            return;
        }

        // 兜底：从场景中按名称查找（包含 inactive）。
        var animators = Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var a in animators)
        {
            if (a != null && a.gameObject.name == DefaultMaskObjectName)
            {
                _maskAnimator = a;
                _maskRoot = a.gameObject;
                _maskSprite = _maskRoot != null ? _maskRoot.GetComponent<SpriteRenderer>() : null;
                CacheWearClip();
                return;
            }
        }

        if (!_loggedMissingAnimator)
        {
            Debug.LogError(
                $"MaskAnimationDriver: 场景中找不到名为 {DefaultMaskObjectName} 的 Animator（用于面具过场表现）。请在 Inspector 绑定 _maskAnimator。",
                this);
            _loggedMissingAnimator = true;
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

    private float GetClipDurationSeconds()
    {
        return _wearClip != null ? _wearClip.length : DefaultClipDurationSeconds;
    }

    private IEnumerator WaitSeconds(float seconds)
    {
        var duration = Mathf.Max(0f, seconds);
        if (duration <= 0f)
        {
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void SetAlpha(float alpha)
    {
        if (_maskSprite == null)
        {
            return;
        }

        var c = _maskSprite.color;
        c.a = Mathf.Clamp01(alpha);
        _maskSprite.color = c;
    }
}
