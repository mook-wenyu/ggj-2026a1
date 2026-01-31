using UnityEngine;

/// <summary>
/// 动作：结束当前关卡（不进入物品栏）。
/// </summary>
public sealed class CompleteLevelAction : InteractionAction
{
    [Tooltip("不填则自动 FindObjectOfType；推荐在 Inspector 绑定。")]
    [SerializeField] private LevelFlowController _levelFlowController;

    public override bool TryExecute(in InteractionContext context)
    {
        var controller = _levelFlowController != null
            ? _levelFlowController
            : Object.FindObjectOfType<LevelFlowController>();

        if (controller == null)
        {
            Debug.LogError("CompleteLevelAction: 场景中找不到 LevelFlowController，无法结束关卡。", context.Target);
            return false;
        }

        controller.CompleteLevel();
        return true;
    }
}
