/// <summary>
/// 全局背包服务：让玩法代码不依赖 UI。
/// </summary>
public static class InventoryService
{
    public static InventoryState State { get; } = new();

    /// <summary>
    /// 重置背包会话态（清空已收集物品）。
    /// </summary>
    public static void Reset()
    {
        State.Clear();
    }

    /// <summary>
    /// 收集物品（不消耗）：只需要传入物品 ID。
    /// 物品显示信息由 ExcelKit 生成的 ItemsConfig 提供。
    /// </summary>
    public static bool TryCollect(string id)
    {
        if (!InventoryItemCatalog.TryCreateItem(id, out var item))
        {
            return false;
        }

        return State.TryAdd(item);
    }
}
