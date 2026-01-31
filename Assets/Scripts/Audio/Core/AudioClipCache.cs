using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioClip 的 Resources 加载缓存。
/// 设计目标：把“Trim/缓存/加载函数”从 MonoBehaviour 中抽离，便于单测与复用。
/// </summary>
public sealed class AudioClipCache
{
    private readonly Func<string, AudioClip> _loader;
    private readonly Dictionary<string, AudioClip> _cache = new(StringComparer.Ordinal);

    public AudioClipCache(Func<string, AudioClip> loader)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    public bool TryGet(string resourcesPath, out AudioClip clip)
    {
        clip = null;
        if (string.IsNullOrWhiteSpace(resourcesPath))
        {
            return false;
        }

        var key = resourcesPath.Trim();
        if (_cache.TryGetValue(key, out clip))
        {
            return clip != null;
        }

        clip = _loader(key);
        _cache[key] = clip; // 缓存 null，避免反复 Resources.Load
        return clip != null;
    }
}
