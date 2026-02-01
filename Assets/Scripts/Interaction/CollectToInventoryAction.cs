using UnityEngine;

/// <summary>
/// 动作：把道具加入背包，并弹出物品详情弹窗。
/// </summary>
public sealed class CollectToInventoryAction : InteractionAction
{
    [Tooltip("物品 ID（ItemsConfig.id）。")]
    [SerializeField] private string _itemId;

    [Tooltip("不填则自动 FindObjectOfType；推荐在 Inspector 绑定，减少运行时查找。")]
    [SerializeField] private InventoryPanel _inventoryPanel;

    public override bool TryExecute(in InteractionContext context)
    {
        if (string.IsNullOrWhiteSpace(_itemId))
        {
            Debug.LogError("CollectToInventoryAction: 未配置 itemId。", context.Target);
            return false;
        }

        var id = _itemId.Trim();
        var panel = _inventoryPanel;
        if (panel == null)
        {
            InventoryPanel.TryGetOrCreate(out panel);
        }

        if (panel == null)
        {
            Debug.LogError($"CollectToInventoryAction: 无法获取/创建 InventoryPanel，无法打开物品详情（id={id}）。", context.Target);
            return false;
        }

        if (!panel.TryCollectAndOpenFromScene(id))
        {
            Debug.LogError($"CollectToInventoryAction: 收集/打开物品详情失败（id={id}）。", context.Target);
            return false;
        }

        return true;
    }
}
