using System.Collections;

using UnityEngine;

/// <summary>
/// Gift 动画播放器：负责“播放一次并定格到最后一帧”。
///
/// 设计目标：
/// - 纯粹表现层封装：不关心剧情/授予面具/切关。
/// - 可通过构造注入 Animator，便于在控制器中替换/复用。
/// </summary>
public sealed class GiftAnimationPlayer
{
    private const float HoldLastFrameNormalizedTime = 0.999f;

    private readonly Animator _animator;
    private readonly GameObject _root;
    private readonly SpriteRenderer _sprite;
    private readonly int _layer;
    private readonly string _stateName;
    private readonly int _stateHash;

    public GiftAnimationPlayer(Animator animator, string stateName = "gift", int layer = 0)
    {
        _animator = animator;
        _root = animator != null ? animator.gameObject : null;
        _sprite = _root != null ? _root.GetComponent<SpriteRenderer>() : null;
        _layer = layer;
        _stateName = string.IsNullOrWhiteSpace(stateName) ? "gift" : stateName;
        _stateHash = Animator.StringToHash(_stateName);
    }

    public bool IsValid => _animator != null && _root != null;

    public void HideImmediate()
    {
        if (_root == null)
        {
            return;
        }

        SetAlpha(1f);
        _root.SetActive(false);
        if (_animator != null)
        {
            _animator.speed = 1f;
        }
    }

    public IEnumerator PlayAndHoldLastFrame()
    {
        if (!IsValid)
        {
            yield break;
        }

        _root.SetActive(true);
        SetAlpha(1f);

        // 若此前为了“定格”把 speed 置 0，这里先恢复。
        _animator.speed = 1f;
        _animator.Play(_stateName, _layer, 0f);
        _animator.Update(0f);

        // 等到接近结束（不用 1f：1 的整数部分代表“循环次数”，可能导致回到第一帧）。
        while (true)
        {
            var info = _animator.GetCurrentAnimatorStateInfo(_layer);
            if (info.shortNameHash == _stateHash && info.normalizedTime >= HoldLastFrameNormalizedTime)
            {
                break;
            }

            yield return null;
        }

        // 强制采样到最后一帧并冻结。
        _animator.Play(_stateName, _layer, HoldLastFrameNormalizedTime);
        _animator.Update(0f);
        _animator.speed = 0f;
    }

    private void SetAlpha(float alpha)
    {
        if (_sprite == null)
        {
            return;
        }

        var c = _sprite.color;
        c.a = Mathf.Clamp01(alpha);
        _sprite.color = c;
    }
}
