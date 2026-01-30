using System;
using UnityEngine;

/// <summary>
/// 语言信息类
/// </summary>
[Serializable]
public struct Language
{
    public string langKey;
    public SystemLanguage language;

    public Language(string langKey, SystemLanguage language)
    {
        this.langKey = langKey;
        this.language = language;
    }

}
