using UnityEngine;

/// <summary>
/// 动作：切换“戴面具/摘面具”。
/// 典型用法：把该组件挂在可点击的面具/镜子/机关上。
/// </summary>
public sealed class ToggleMaskAction : InteractionAction
{
    [Tooltip("不填则自动 FindObjectOfType；推荐在 Inspector 绑定，减少运行时查找。")]
    [SerializeField] private MaskWorldController _controller;

    public override bool TryExecute(in InteractionContext context)
    {
        var controller = _controller != null ? _controller : Object.FindObjectOfType<MaskWorldController>();
        if (controller == null)
        {
            Debug.LogError("ToggleMaskAction: 场景中找不到 MaskWorldController。", context.Target);
            return false;
        }

        controller.ToggleMask();
        return true;
    }
}
