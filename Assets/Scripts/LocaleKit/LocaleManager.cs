using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

public static class LocaleManager
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    private static readonly List<Language> supportedLanguages = new()
    {
            new Language
            {
                langKey = "cn",
                language = SystemLanguage.ChineseSimplified
            },
            new Language
            {
                langKey = "en",
                language = SystemLanguage.English
            }
    };

    /// <summary>
    /// 语言改变事件
    /// </summary>
    public static event Action<SystemLanguage> OnLanguageChanged;

    private static readonly Dictionary<SystemLanguage, Dictionary<string, string>> _localeDataDict = new();

    private static bool isInitialized;
    private static SystemLanguage _currentLanguage = SystemLanguage.ChineseSimplified;

    /// <summary>
    /// 当前语言
    /// </summary>
    public static SystemLanguage CurrentLanguage
    {
        get
        {
            EnsureInitialized();
            return _currentLanguage;
        }
        set
        {
            EnsureInitialized();
            _currentLanguage = value;
            var languageIndex = supportedLanguages.FindIndex(l => l.language == value);
            RuntimePrefs.SetInt("CURRENT_LANGUAGE_INDEX", languageIndex);
            OnLanguageChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// 初始化语言数据
    /// </summary>
    public static void InitLanguage()
    {
        if (isInitialized)
        {
            return;
        }

        ConfigManager.EnsureLoaded("JsonConfigs");

        _localeDataDict.Clear();

        var languageAssets = Resources.LoadAll<TextAsset>("JsonConfigs");
        if (languageAssets == null || languageAssets.Length == 0)
        {
            Debug.LogError("LanguagesConfig assets are missing");
            return;
        }

        foreach (var asset in languageAssets)
        {
            if (asset == null || !asset.name.StartsWith("LanguagesConfig_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Dictionary<string, BaseConfig> configs;
            try
            {
                configs = JsonConvert.DeserializeObject<Dictionary<string, BaseConfig>>(asset.text, JsonSettings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse {asset.name}: {ex.Message}");
                continue;
            }

            if (configs == null || configs.Count == 0)
            {
                continue;
            }

            foreach (var baseEntry in configs.Values)
            {
                if (baseEntry is not LanguagesConfig entry)
                {
                    continue;
                }

                var langKey = entry.langKey;
                var text = entry.text;
                if (string.IsNullOrEmpty(langKey) || string.IsNullOrEmpty(text))
                {
                    Debug.LogWarning($"LanguagesConfig '{asset.name}' has null or empty langKey/text, skipping.");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.id))
                {
                    Debug.LogWarning($"LanguagesConfig '{asset.name}' has null or empty id for langKey '{langKey}', skipping.");
                    continue;
                }

                var supportedLanguage = supportedLanguages.Find(l => string.Equals(l.langKey, langKey, StringComparison.OrdinalIgnoreCase));
                if (supportedLanguage.langKey == null)
                {
                    Debug.LogWarning($"Unsupported langKey '{langKey}' in {asset.name}, skipping.");
                    continue;
                }

                if (!_localeDataDict.TryGetValue(supportedLanguage.language, out var dict))
                {
                    dict = new Dictionary<string, string>();
                    _localeDataDict[supportedLanguage.language] = dict;
                }

                dict[entry.id] = text;
            }
        }

        if (_localeDataDict.Count == 0)
        {
            Debug.LogError("LanguagesConfig is empty");
            return;
        }

        var savedIndex = RuntimePrefs.GetInt("CURRENT_LANGUAGE_INDEX", 0);
        if (savedIndex >= 0 && savedIndex < supportedLanguages.Count)
        {
            _currentLanguage = supportedLanguages[savedIndex].language;
        }
        else
        {
            _currentLanguage = supportedLanguages[0].language;
        }

        ConfigManager.Remove<LanguagesConfig>();
        isInitialized = true;
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    /// <param name="language">目标语言</param>
    public static void ChangeLanguage(SystemLanguage language)
    {
        //isInitialized = false;
        EnsureInitialized();
        CurrentLanguage = language;
    }

    /// <summary>
    /// 获取本地化文本
    /// </summary>
    public static string GetText(string key)
    {
        return GetText(CurrentLanguage, key);
    }

    /// <summary>
    /// 获取指定语言的本地化文本
    /// </summary>
    public static string GetText(SystemLanguage language, string key)
    {
        EnsureInitialized();

        if (_localeDataDict.TryGetValue(language, out var dict) && dict.TryGetValue(key, out var text))
        {
            return text;
        }

        return key;
    }

    private static void EnsureInitialized()
    {
        if (!isInitialized)
        {
            InitLanguage();
        }
    }
}
