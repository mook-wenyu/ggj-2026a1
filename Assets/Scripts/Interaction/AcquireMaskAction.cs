using UnityEngine;

/// <summary>
/// 动作：获得面具。
/// 规则：面具不会进入物品栏；获得后由 UI 生成一个常驻按钮用于切换双世界。
/// </summary>
public sealed class AcquireMaskAction : InteractionAction
{
    [Tooltip("不填则自动 FindObjectOfType；推荐在 Inspector 绑定。")]
    [SerializeField] private MaskWorldController _controller;

    public override bool TryExecute(in InteractionContext context)
    {
        var controller = _controller != null ? _controller : Object.FindObjectOfType<MaskWorldController>();
        if (controller == null)
        {
            Debug.LogError("AcquireMaskAction: 场景中找不到 MaskWorldController。", context.Target);
            return false;
        }

        controller.AcquireMask();
        return true;
    }
}
