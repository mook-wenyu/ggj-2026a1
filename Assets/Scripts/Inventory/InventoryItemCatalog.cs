using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ggj.Inventory
{
    /// <summary>
    /// 物品定义目录：从 ExcelKit 生成的 ItemsConfig 里读取物品静态信息。
    /// </summary>
    public static class InventoryItemCatalog
    {
        private const string ConfigResourcesPath = "JsonConfigs";

        private static readonly Dictionary<string, Sprite> SpriteCache = new(StringComparer.Ordinal);
        private static bool _isLoaded;

        public static bool TryCreateItem(string id, out InventoryItem item)
        {
            item = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            EnsureLoaded();

            if (!ConfigManager.Has<ItemsConfig>(id))
            {
                // 配置缺失时仍允许收集：避免因为表漏配导致软锁。
                item = new InventoryItem(id, id, null, null);
                return true;
            }

            var cfg = ConfigManager.Get<ItemsConfig>(id);
            if (cfg == null)
            {
                item = new InventoryItem(id, id, null, null);
                return true;
            }

            var displayName = cfg.name;
            var description = cfg.desc;
            var icon = LoadSpriteOrNull(cfg.iconPath);

            item = new InventoryItem(id, displayName, description, icon);
            return true;
        }

        private static void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;
            ConfigManager.EnsureLoaded(ConfigResourcesPath);
        }

        private static Sprite LoadSpriteOrNull(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                return null;
            }

            if (SpriteCache.TryGetValue(resourcesPath, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(resourcesPath);
            SpriteCache[resourcesPath] = sprite;
            return sprite;
        }
    }
}
