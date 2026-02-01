using UnityEngine;

/// <summary>
/// 动作：打开“保险柜解密”界面。
/// 设计目标：
/// - UI（DecryptionUIRoot）不依赖玩法；
/// - 交互层通过接口依赖 UI，便于替换与测试；
/// - 解密成功后的具体行为用“另一个 InteractionAction”承接，避免强耦合。
/// </summary>
public sealed class OpenSafeDecryptionAction : InteractionAction, IInteractionEntryAction
{
    [Header("引用")]
    [Tooltip("解密 UI（可选）。不填则自动在场景中查找 DecryptionUIRoot。")]
    [SerializeField] private MonoBehaviour _decryptionUiBehaviour;

    [Header("解密成功后（可选）")]
    [Tooltip("解密成功后执行的动作。不填时：若同物体上只有一个其他 InteractionAction，将自动执行它；否则忽略并警告。")]
    [SerializeField] private InteractionAction _onSolvedAction;

    [Header("取消后（可选）")]
    [SerializeField] private InteractionAction _onCancelledAction;

    private InteractionContext _pendingContext;
    private bool _waitingCallback;

    public override bool TryExecute(in InteractionContext context)
    {
        var ui = ResolveUi();
        if (ui == null)
        {
            Debug.LogError(
                "OpenSafeDecryptionAction: 找不到解密 UI（ISafeDecryptionUI）。请确认场景中存在 DecryptionUIRoot。",
                context.Target);
            return false;
        }

        _pendingContext = context;
        var opened = ui.TryShowSafeUi(HandleSolved, HandleCancelled);
        _waitingCallback = opened;
        return opened;
    }

    private ISafeDecryptionUI ResolveUi()
    {
        if (_decryptionUiBehaviour != null)
        {
            var typed = _decryptionUiBehaviour as ISafeDecryptionUI;
            if (typed == null)
            {
                Debug.LogError(
                    "OpenSafeDecryptionAction: _decryptionUiBehaviour 未实现 ISafeDecryptionUI。",
                    this);
            }
            return typed;
        }

        // 1) 优先从当前物体上找（便于测试/复用）。
        var behaviours = GetComponents<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            if (b is ISafeDecryptionUI candidate)
            {
                return candidate;
            }
        }

        // 2) 兜底：全场景查找（只在交互触发时执行）。
        return Object.FindObjectOfType<DecryptionUIRoot>(true);
    }

    private void HandleSolved()
    {
        if (!_waitingCallback)
        {
            return;
        }

        _waitingCallback = false;
        ResolveOnSolvedAction()?.TryExecute(_pendingContext);
    }

    private void HandleCancelled()
    {
        if (!_waitingCallback)
        {
            return;
        }

        _waitingCallback = false;
        _onCancelledAction?.TryExecute(_pendingContext);
    }

    private InteractionAction ResolveOnSolvedAction()
    {
        if (_onSolvedAction != null)
        {
            return _onSolvedAction;
        }

        // 自动推断：若除了自己之外只有一个 InteractionAction，就执行它；避免多动作场景歧义。
        InteractionAction candidate = null;
        var actions = GetComponents<InteractionAction>();
        foreach (var a in actions)
        {
            if (a == null || a == this)
            {
                continue;
            }

            if (candidate != null)
            {
                Debug.LogWarning(
                    "OpenSafeDecryptionAction: 同一物体上存在多个可执行动作，请在 Inspector 显式绑定 _onSolvedAction 以避免歧义。",
                    this);
                return null;
            }

            candidate = a;
        }

        return candidate;
    }
}
