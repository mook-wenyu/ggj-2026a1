using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

public static class ConfigManager
{
    private static readonly Dictionary<string, Dictionary<string, BaseConfig>> _jsonDataDict = new();
    private static readonly HashSet<string> _loadedPaths = new();
    private static readonly HashSet<string> _normalizedIdLogCache = new(StringComparer.Ordinal);

    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public static void LoadAll(string jsonPath)
    {
        var jsonConfigs = Resources.LoadAll<TextAsset>(jsonPath);
        foreach (var jsonConfig in jsonConfigs)
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, BaseConfig>>(jsonConfig.text, _jsonSerializerSettings);
            var key = jsonConfig.name.Split('_');
            _jsonDataDict[key[0]] = config;
        }
        _loadedPaths.Add(jsonPath);
    }

    public static void EnsureLoaded(string jsonPath)
    {
        if (!_loadedPaths.Contains(jsonPath))
        {
            LoadAllFromResources(jsonPath);
        }
    }

    public static void LoadAllFromResources(string jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath))
        {
            Debug.LogWarning("ConfigManager.LoadAllFromResources 收到空路径，已忽略");
            return;
        }

        if (_loadedPaths.Contains(jsonPath))
        {
            return;
        }

        var jsonConfigs = Resources.LoadAll<TextAsset>(jsonPath);
        foreach (var jsonConfig in jsonConfigs)
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, BaseConfig>>(jsonConfig.text, _jsonSerializerSettings);
            // 使用类型名作为键（去除 Sheet 后缀）
            var key = jsonConfig.name.Split('_')[0];
            _jsonDataDict[key] = config;
        }

        _loadedPaths.Add(jsonPath);
    }

    /// <summary>
    /// 规范化外部传入的配置 ID，并在发现异常空白时记录一次告警。
    /// </summary>
    private static string NormalizeId(string id, string configName)
    {
        if (string.IsNullOrEmpty(id))
        {
            return id;
        }

        var trimmedId = id.Trim();
        if (!string.Equals(id, trimmedId, StringComparison.Ordinal))
        {
            var cacheKey = $"{configName}:{trimmedId}";
            if (_normalizedIdLogCache.Add(cacheKey))
            {
                Debug.LogWarning($"ConfigManager 自动裁剪 {configName} 的配置 ID，原值：'{id}'，裁剪后：'{trimmedId}'。请检查数据源是否包含多余空白。");
            }
        }

        return trimmedId;
    }

    /// <summary>
    /// 获取指定类型和ID的配置数据
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="id">配置ID</param>
    /// <returns>配置数据</returns>
    [Preserve]
    public static T Get<T>(string id) where T : BaseConfig
    {
        var configName = typeof(T).Name;
        var normalizedId = NormalizeId(id, configName);

        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogWarning($"ConfigManager.Get<{configName}> 收到空的配置 ID");
            return null;
        }

        if (_jsonDataDict.TryGetValue(configName, out var dict) && dict.TryGetValue(normalizedId, out var config))
        {
            return config as T;
        }

        Debug.LogWarning($"未找到类型 {configName} ID为 {normalizedId} 的配置数据");
        return null;
    }

    /// <summary>
    /// 获取指定类型和ID的配置数据（非泛型版本）
    /// </summary>
    /// <param name="type">配置类型</param>
    /// <param name="id">配置ID</param>
    /// <returns>配置数据</returns>
    [Preserve]
    public static BaseConfig Get(Type type, string id)
    {
        if (type == null)
        {
            Debug.LogError("类型参数不能为 null");
            return null;
        }

        if (!typeof(BaseConfig).IsAssignableFrom(type))
        {
            Debug.LogError($"类型 {type.Name} 不继承自 BaseConfig");
            return null;
        }

        var configName = type.Name;
        var normalizedId = NormalizeId(id, configName);

        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogError("配置ID不能为空");
            return null;
        }

        if (_jsonDataDict.TryGetValue(configName, out var dict) && dict.TryGetValue(normalizedId, out var config))
        {
            if (config != null && type.IsInstanceOfType(config))
            {
                return config;
            }
        }

        Debug.LogWarning($"未找到类型 {configName} ID为 {normalizedId} 的配置数据");
        return null;
    }

    /// <summary>
    /// 获取指定类型的所有配置数据
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <returns>配置数据列表</returns>
    [Preserve]
    public static IReadOnlyList<T> GetAll<T>() where T : BaseConfig
    {
        var configName = typeof(T).Name;

        if (_jsonDataDict.TryGetValue(configName, out var dict))
        {
            return dict.Values.Cast<T>().ToList();
        }

        Debug.LogWarning($"未找到类型 {configName} 的配置数据");
        return Array.Empty<T>();
    }

    /// <summary>
    /// 获取指定类型的所有配置数据（非泛型版本）
    /// </summary>
    /// <param name="type">配置类型</param>
    /// <returns>配置数据列表</returns>
    [Preserve]
    public static IReadOnlyList<BaseConfig> GetAll(Type type)
    {
        if (type == null)
        {
            Debug.LogError("类型参数不能为 null");
            return Array.Empty<BaseConfig>();
        }

        if (!typeof(BaseConfig).IsAssignableFrom(type))
        {
            Debug.LogError($"类型 {type.Name} 不继承自 BaseConfig");
            return Array.Empty<BaseConfig>();
        }

        var configName = type.Name;

        if (_jsonDataDict.TryGetValue(configName, out var dict))
        {
            // 使用 LINQ 将 BaseConfig 转换为具体类型，然后再转回 BaseConfig
            var result = new List<BaseConfig>();
            foreach (var config in dict.Values)
            {
                if (config != null && type.IsInstanceOfType(config))
                {
                    result.Add(config);
                }
            }
            return result;
        }

        Debug.LogWarning($"未找到类型 {configName} 的配置数据");
        return Array.Empty<BaseConfig>();
    }

    /// <summary>
    /// 检查指定类型和ID的配置数据是否存在
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <typeparam name="T">配置类型</typeparam>
    /// <returns></returns>
    public static bool Has<T>(string id) where T : BaseConfig
    {
        var configName = typeof(T).Name;
        var normalizedId = NormalizeId(id, configName);
        if (string.IsNullOrEmpty(normalizedId))
        {
            return false;
        }

        return _jsonDataDict.TryGetValue(configName, out var dict) && dict.ContainsKey(normalizedId);
    }

    /// <summary>
    /// 检查指定类型和ID的配置数据是否存在（非泛型版本）
    /// </summary>
    /// <param name="type">配置类型</param>
    /// <param name="id">配置ID</param>
    /// <returns>如果配置存在则返回 true，否则返回 false</returns>
    [Preserve]
    public static bool Has(Type type, string id)
    {
        if (type == null)
        {
            Debug.LogError("类型参数不能为 null");
            return false;
        }

        if (!typeof(BaseConfig).IsAssignableFrom(type))
        {
            Debug.LogError($"类型 {type.Name} 不继承自 BaseConfig");
            return false;
        }

        var configName = type.Name;
        var normalizedId = NormalizeId(id, configName);
        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogError("配置ID不能为空");
            return false;
        }

        return _jsonDataDict.TryGetValue(configName, out var dict) &&
               dict.TryGetValue(normalizedId, out var config) &&
               config != null &&
               type.IsInstanceOfType(config);
    }

    /// <summary>
    /// 移除指定类型和ID的配置数据，
    /// ID为空则移除指定类型所有配置数据
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <typeparam name="T">配置类型</typeparam>
    public static void Remove<T>(string id = null) where T : BaseConfig
    {
        var configName = typeof(T).Name;
        if (!_jsonDataDict.TryGetValue(configName, out var dict)) return;

        if (string.IsNullOrEmpty(id))
        {
            _jsonDataDict.Remove(configName);
            return;
        }

        var normalizedId = NormalizeId(id, configName);
        if (string.IsNullOrEmpty(normalizedId))
        {
            return;
        }

        dict.Remove(normalizedId);
    }

    /// <summary>
    /// 移除指定类型和ID的配置数据（非泛型版本），
    /// ID为空则移除指定类型所有配置数据
    /// </summary>
    /// <param name="type">配置类型</param>
    /// <param name="id">配置ID</param>
    [Preserve]
    public static void Remove(Type type, string id = null)
    {
        if (type == null)
        {
            Debug.LogError("类型参数不能为 null");
            return;
        }

        if (!typeof(BaseConfig).IsAssignableFrom(type))
        {
            Debug.LogError($"类型 {type.Name} 不继承自 BaseConfig");
            return;
        }

        var configName = type.Name;
        if (!_jsonDataDict.TryGetValue(configName, out var dict)) return;

        if (string.IsNullOrEmpty(id))
        {
            _jsonDataDict.Remove(configName);
            return;
        }

        var normalizedId = NormalizeId(id, configName);
        if (string.IsNullOrEmpty(normalizedId))
        {
            return;
        }

        dict.Remove(normalizedId);
    }
}

