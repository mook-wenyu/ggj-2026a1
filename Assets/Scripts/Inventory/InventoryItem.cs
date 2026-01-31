using UnityEngine;

/// <summary>
/// 背包物品的最小数据模型。
/// 点击解密类偏收集：默认以唯一 ID 收集，不会消耗。
/// </summary>
public sealed class InventoryItem
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public Sprite Icon { get; }
    public string AudioPath { get; }

    public InventoryItem(string id, string displayName, string description, Sprite icon = null, string audioPath = null)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Icon = icon;
        AudioPath = audioPath;
    }

    public string GetDisplayNameOrId()
    {
        return string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }

    public string GetDescriptionOrDefault(string defaultText = "暂无描述。")
    {
        return string.IsNullOrEmpty(Description) ? defaultText : Description;
    }
}
