using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时首选项工具，支持在内存模式与 PlayerPrefs 模式之间切换，方便兼容不允许持久化的环境。
/// </summary>
public static class RuntimePrefs
{
    /// <summary>
    /// 存储模式枚举。
    /// </summary>
    public enum StorageMode
    {
        /// <summary>
        /// 仅使用内存字典存储键值。
        /// </summary>
        MemoryOnly,
        /// <summary>
        /// 通过 UnityEngine.PlayerPrefs 持久化键值。
        /// </summary>
        PlayerPrefs
    }

    private static readonly Dictionary<string, object> store = new(StringComparer.Ordinal);
    private static StorageMode currentMode = DetermineDefaultMode();
    private static bool allowFallbackToMemory = true;
    private static bool hasLoggedFallback;

    /// <summary>
    /// 当前启用的存储模式。
    /// </summary>
    public static StorageMode Mode
    {
        get => currentMode;
        set
        {
            currentMode = value;
            if (currentMode == StorageMode.MemoryOnly)
            {
                hasLoggedFallback = false;
            }
        }
    }

    /// <summary>
    /// 配置存储模式与回退策略。
    /// </summary>
    /// <param name="mode">目标模式。</param>
    /// <param name="enableFallback">是否允许 PlayerPrefs 失败后自动回退到内存。</param>
    public static void Configure(StorageMode mode, bool enableFallback = true)
    {
        Mode = mode;
        allowFallbackToMemory = enableFallback;
    }

    /// <summary>
    /// 设置整型值。
    /// </summary>
    public static void SetInt(string key, int value)
    {
        store[key] = value;
        TryWrite(() => PlayerPrefs.SetInt(key, value), "SetInt", key);
    }

    /// <summary>
    /// 读取整型值。
    /// </summary>
    public static int GetInt(string key, int defaultValue = 0)
    {
        if (TryConvertInt(store, key, out var cached))
        {
            return cached;
        }

        if (TryRead(() => PlayerPrefs.GetInt(key, defaultValue), "GetInt", key, out int result))
        {
            store[key] = result;
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    /// 设置浮点值。
    /// </summary>
    public static void SetFloat(string key, float value)
    {
        store[key] = value;
        TryWrite(() => PlayerPrefs.SetFloat(key, value), "SetFloat", key);
    }

    /// <summary>
    /// 读取浮点值。
    /// </summary>
    public static float GetFloat(string key, float defaultValue = 0f)
    {
        if (TryConvertFloat(store, key, out var cached))
        {
            return cached;
        }

        if (TryRead(() => PlayerPrefs.GetFloat(key, defaultValue), "GetFloat", key, out float result))
        {
            store[key] = result;
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    /// 设置字符串值。
    /// </summary>
    public static void SetString(string key, string value)
    {
        var normalized = value ?? string.Empty;
        store[key] = normalized;
        TryWrite(() => PlayerPrefs.SetString(key, normalized), "SetString", key);
    }

    /// <summary>
    /// 读取字符串值。
    /// </summary>
    public static string GetString(string key, string defaultValue = "")
    {
        if (store.TryGetValue(key, out var cached) && cached != null)
        {
            return cached.ToString();
        }

        if (TryRead(() => PlayerPrefs.GetString(key, defaultValue), "GetString", key, out string result))
        {
            store[key] = result;
            return result ?? defaultValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// 判断键是否存在。
    /// </summary>
    public static bool HasKey(string key)
    {
        if (store.ContainsKey(key))
        {
            return true;
        }

        if (Mode != StorageMode.PlayerPrefs)
        {
            return false;
        }

        bool exists = false;
        TryRead(() => PlayerPrefs.HasKey(key) ? 1 : 0, "HasKey", key, out int flag);
        exists = flag == 1;

        return exists;
    }

    /// <summary>
    /// 删除指定键。
    /// </summary>
    public static void DeleteKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        store.Remove(key);
        TryWrite(() => PlayerPrefs.DeleteKey(key), "DeleteKey", key);
    }

    /// <summary>
    /// 清空所有键值。
    /// </summary>
    public static void DeleteAll()
    {
        store.Clear();
        TryWrite(PlayerPrefs.DeleteAll, "DeleteAll");
    }

    /// <summary>
    /// 保存当前数据。
    /// </summary>
    public static void Save()
    {
        TryWrite(PlayerPrefs.Save, "Save");
    }

    private static StorageMode DetermineDefaultMode()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return StorageMode.MemoryOnly;
#else
        return StorageMode.PlayerPrefs;
#endif
    }

    private static bool IsPlayerPrefsEnabled => Mode == StorageMode.PlayerPrefs;

    private static bool TryWrite(Action action, string operation, string key = null)
    {
        if (!IsPlayerPrefsEnabled || action == null)
        {
            return false;
        }

        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            HandlePlayerPrefsFailure(ex, operation, key);
            return false;
        }
    }

    private static bool TryRead<T>(Func<T> action, string operation, string key, out T value)
    {
        value = default;
        if (!IsPlayerPrefsEnabled || action == null)
        {
            return false;
        }

        try
        {
            value = action();
            return true;
        }
        catch (Exception ex)
        {
            HandlePlayerPrefsFailure(ex, operation, key);
            value = default;
            return false;
        }
    }

    private static void HandlePlayerPrefsFailure(Exception ex, string operation, string key)
    {
        if (!allowFallbackToMemory)
        {
            Debug.LogWarning($"RuntimePrefs: PlayerPrefs 操作失败但未允许回退。操作={operation} 键={key} 错误={ex.Message}");
            return;
        }

        if (!hasLoggedFallback)
        {
            Debug.LogWarning($"RuntimePrefs: PlayerPrefs 操作失败，已经回退到内存模式。操作={operation} 键={key} 错误={ex.Message}");
            hasLoggedFallback = true;
        }

        Mode = StorageMode.MemoryOnly;
    }

    private static bool TryConvertInt(Dictionary<string, object> source, string key, out int value)
    {
        value = 0;
        if (!source.TryGetValue(key, out var cached))
        {
            return false;
        }

        switch (cached)
        {
            case int iv:
                value = iv;
                return true;
            case float fv:
                value = (int)fv;
                return true;
            case string sv when int.TryParse(sv, out var parsed):
                value = parsed;
                return true;
            default:
                return false;
        }
    }

    private static bool TryConvertFloat(Dictionary<string, object> source, string key, out float value)
    {
        value = 0f;
        if (!source.TryGetValue(key, out var cached))
        {
            return false;
        }

        switch (cached)
        {
            case float fv:
                value = fv;
                return true;
            case int iv:
                value = iv;
                return true;
            case string sv when float.TryParse(sv, out var parsed):
                value = parsed;
                return true;
            default:
                return false;
        }
    }
}
