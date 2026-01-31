using System;
using System.Collections.Generic;

/// <summary>
/// 纯数据背包：收集向、不消耗；以 ID 去重。
/// 说明：不在这里做 Debug.Log，避免逻辑层产生日志噪音；由调用方决定如何提示。
/// </summary>
public sealed class InventoryState
{
    private readonly List<InventoryItem> _items = new();
    private readonly Dictionary<string, InventoryItem> _itemsById = new(StringComparer.Ordinal);

    /// <summary>
    /// 新物品进入背包时触发。
    /// </summary>
    public event Action<InventoryItem> ItemAdded;

    /// <summary>
    /// 重复 ID 的物品被更新时触发（例如补全描述/图标）。
    /// </summary>
    public event Action<InventoryItem> ItemUpdated;

    /// <summary>
    /// 按获得顺序存储：越后越新。
    /// UI 如需“从下到上排序”，可在展示层决定插入/排列策略。
    /// </summary>
    public IReadOnlyList<InventoryItem> Items => _items;

    /// <summary>
    /// 清空背包内容。
    /// 说明：当前背包为“收集向、不消耗”，因此清空通常只发生在“新开一局/重新开始”。
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _itemsById.Clear();
    }

    public bool Contains(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        return _itemsById.ContainsKey(id);
    }

    public bool TryGet(string id, out InventoryItem item)
    {
        if (string.IsNullOrEmpty(id))
        {
            item = null;
            return false;
        }

        return _itemsById.TryGetValue(id, out item);
    }

    /// <summary>
    /// 尝试添加物品。
    /// - 返回 true：新增。
    /// - 返回 false：参数非法或重复（重复时会做“更新”）。
    /// </summary>
    public bool TryAdd(InventoryItem item)
    {
        if (!IsValid(item))
        {
            return false;
        }

        if (_itemsById.TryGetValue(item.Id, out var existing))
        {
            // 物品不消耗：重复添加视为“更新信息”，避免刷屏。
            _itemsById[item.Id] = item;
            var index = _items.IndexOf(existing);
            if (index >= 0)
            {
                _items[index] = item;
            }

            ItemUpdated?.Invoke(item);
            return false;
        }

        _items.Add(item);
        _itemsById.Add(item.Id, item);
        ItemAdded?.Invoke(item);
        return true;
    }

    private static bool IsValid(InventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(item.Id);
    }
}
