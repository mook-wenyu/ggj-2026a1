using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocaleText : MonoBehaviour
{
    private TextMeshProUGUI _textMeshProUGUI;
    private TextMeshPro _textMeshPro;

    [TextArea(0, int.MaxValue)] public string langKey;

    private void OnEnable()
    {
        LocaleManager.InitLanguage();
        LocaleManager.OnLanguageChanged += HandleLanguageChanged;
        UpdateText(langKey);
    }

    private void OnDisable()
    {
        LocaleManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(SystemLanguage language)
    {
        UpdateText(langKey);
    }

    /// <summary>
    /// 根据当前语言的语言键更新文本
    /// </summary>
    /// <param name="key">语言键</param>
    public void UpdateText(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        SetText(LocaleManager.GetText(key));
    }

    private void SetText(string text)
    {
        if (!_textMeshProUGUI && !_textMeshPro)
        {
            _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            _textMeshPro = GetComponent<TextMeshPro>();
        }
        if (text == "") // 空文本不更新
        {
            return;
        }
        if (_textMeshProUGUI)
            {
                _textMeshProUGUI.text = text;
            }

        if (_textMeshPro)
        {
            _textMeshPro.text = text;
        }
    }
}

