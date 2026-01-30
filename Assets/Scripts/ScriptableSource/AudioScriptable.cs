using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioEntry
{
    public string key;
    public AudioClip clip;
}

[CreateAssetMenu(fileName = "AudioScriptable", menuName = "ScriptableObjects/AudioScriptable", order = 1)]
public class AudioScriptable : ScriptableObject
{
    // 在 Inspector 中可编辑的键值对列表（代替 Dictionary）
    [Header("背景音乐名称 和 音乐")]
    [SerializeField] private List<AudioEntry> mainBgms = new();

    [Header("特殊物品id 和 音效")]
    [SerializeField] private List<AudioEntry> specialItemSounds = new();

    // 运行时构建的快速查找表
    private Dictionary<string, AudioClip> mainBgmDict;
    private Dictionary<string, AudioClip> specialItemDict;

    private void OnEnable()
    {
        BuildDictionaries();
    }

    [ContextMenu("Rebuild Dictionaries")]
    public void BuildDictionaries()
    {
        mainBgmDict = new Dictionary<string, AudioClip>();
        specialItemDict = new Dictionary<string, AudioClip>();

        if (mainBgms != null)
        {
            foreach (var e in mainBgms)
            {
                if (string.IsNullOrEmpty(e.key) || e.clip == null) continue;
                mainBgmDict[e.key] = e.clip;
            }
        }

        if (specialItemSounds != null)
        {
            foreach (var e in specialItemSounds)
            {
                if (string.IsNullOrEmpty(e.key) || e.clip == null) continue;
                specialItemDict[e.key] = e.clip;
            }
        }
    }

    public AudioClip GetMainBgm(string name)
    {
        if (mainBgmDict == null) BuildDictionaries();
        return mainBgmDict != null && mainBgmDict.TryGetValue(name, out var clip) ? clip : null;
    }

    public AudioClip GetSpecialItemSound(string id)
    {
        if (specialItemDict == null) BuildDictionaries();
        AudioClip c2 = specialItemDict != null && specialItemDict.TryGetValue(id, out var clip) ? clip : null;
        if (c2 == null)
        {
            c2 = specialItemDict.TryGetValue("else", out var clip1) ? clip1 : null;
            Debug.Log("GetSpecialItemSound: use else"+c2);
            return c2;
        }
        Debug.Log("GetSpecialItemSound: use else"+c2);
        return c2;
    }
}