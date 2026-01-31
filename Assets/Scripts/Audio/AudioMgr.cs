using UnityEngine;

/// <summary>
/// 简易音频管理器：
/// - 背景音乐（循环）
/// - 通用音效（可叠加）
/// - 物品音频（默认不叠加，重放前会 Stop）
///
/// 重要变更：不再使用 AudioScriptable；物品音频改为由 ItemsConfig（Excel）提供音频路径。
/// </summary>
public sealed class AudioMgr : MonoSingleton<AudioMgr>, IItemAudioService
{
    private const string DefaultAudioResourcesFolder = "Audios/";

    [Header("音频设置")]
    [SerializeField] private AudioSource musicSource; // 背景音乐播放器
    [SerializeField] private AudioSource soundSource; // 通用音效播放器
    [SerializeField] private AudioSource itemSoundSource; // 物品音频播放器（语音/提示音）

    public enum BgmAudioType
    {
        Main,
        Memory,
        End
    }

    [Header("音量设置")]
    [Range(0, 1)]
    [SerializeField] private float musicVolume = 1f;
    [Range(0, 1)]
    [SerializeField] private float soundVolume = 1f;
    [Range(0, 1)]
    [SerializeField] private float itemVolume = 1f;

    private bool _initialized;
    private AudioClipCache _clipCache;

    public void Init()
    {
        EnsureInitialized();
    }

    public override void OnSingletonInit()
    {
        EnsureInitialized();
    }

    private void Awake()
    {
        // 既支持“场景里挂一个 AudioMgr”，也支持“首次访问 Instance 时自动创建”。
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (soundSource == null)
        {
            soundSource = gameObject.AddComponent<AudioSource>();
        }

        if (itemSoundSource == null)
        {
            itemSoundSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        soundSource.loop = false;
        itemSoundSource.loop = false;

        _clipCache ??= new AudioClipCache(path => Resources.Load<AudioClip>(path));

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }

        if (soundSource != null)
        {
            soundSource.volume = soundVolume;
        }

        if (itemSoundSource != null)
        {
            itemSoundSource.volume = itemVolume;
        }
    }

    /// <summary>
    /// 播放背景音乐（会覆盖当前 BGM）。
    /// </summary>
    public void PlayMusic(AudioClip music)
    {
        EnsureInitialized();
        if (music == null || musicSource == null)
        {
            return;
        }

        musicSource.clip = music;
        musicSource.Play();
    }

    /// <summary>
    /// 播放背景音乐（约定 Resources/Audios 下存在对应的 clip）。
    /// </summary>
    public void PlayMusic(BgmAudioType type)
    {
        EnsureInitialized();

        var clipName = type switch
        {
            BgmAudioType.Main => "MainBgm",
            BgmAudioType.Memory => "MemoryBgm",
            BgmAudioType.End => "ResultBgm",
            _ => null
        };

        if (string.IsNullOrEmpty(clipName))
        {
            return;
        }

        if (_clipCache.TryGet(DefaultAudioResourcesFolder + clipName, out var clip))
        {
            PlayMusic(clip);
        }
    }

    /// <summary>
    /// 播放通用音效（允许叠加）。
    /// </summary>
    public void PlaySound(AudioClip sound)
    {
        EnsureInitialized();
        if (sound == null || soundSource == null)
        {
            return;
        }

        soundSource.PlayOneShot(sound, soundVolume);
    }

    /// <summary>
    /// 从 Resources/Audios/ 读取并播放通用音效。
    /// </summary>
    public void PlaySound(string name)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var clipPath = DefaultAudioResourcesFolder + name.Trim();
        if (_clipCache.TryGet(clipPath, out var clip))
        {
            PlaySound(clip);
        }
    }

    public bool TryGetItemAudioClip(string audioPath, out AudioClip clip)
    {
        EnsureInitialized();
        return _clipCache.TryGet(audioPath, out clip);
    }

    public void PlayItemAudio(AudioClip clip)
    {
        EnsureInitialized();
        if (clip == null || itemSoundSource == null)
        {
            return;
        }

        // 物品语音/提示音：默认不叠加，重放时先停止。
        itemSoundSource.Stop();
        itemSoundSource.PlayOneShot(clip, itemVolume);
    }

    public void UpdateVolume()
    {
        if (!_initialized)
        {
            EnsureInitialized();
            return;
        }

        ApplyVolumes();
    }

    public void StopMusic()
    {
        EnsureInitialized();
        musicSource?.Stop();
    }
}
