using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioMgr : MonoSingleton<AudioMgr>
{
    [Header("音频设置")]
    public AudioSource musicSource; // 背景音乐播放器
    public AudioSource soundSource; // 音效播放器
    public AudioScriptable audioConfig; // 音频配置
    private BgmAudioType nowBgm;
    public enum BgmAudioType
    {
        Main,
        Memory,
        End
    }

    [Header("音量设置")]
    [Range(0, 1)]
    public float musicVolume = 1f; // 音乐音量
    [Range(0, 1)]
    public float soundVolume = 1f; // 音效音量

    public Dictionary<string, AudioClip> sounds = new();

    public void Init()
    {
        // 初始化音频
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();
        if (soundSource == null)
            soundSource = gameObject.AddComponent<AudioSource>();

        // 设置音频源属性
        musicSource.loop = true;
        soundSource.loop = false;
        audioConfig = Resources.Load<AudioScriptable>("Audios/AudioScriptable");
        UpdateVolume();
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="music"></param>
    public void PlayMusic(AudioClip music)
    {
        if (music == null || musicSource == null) return;
        Debug.Log("PlayMusic:" + music.name);
        musicSource.clip = music;
        musicSource.Play();
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="name"></param>
    // public void PlayMusic(string name)
    // {
    //     if (sounds.ContainsKey(name))
    //     {
    //         PlayMusic(sounds[name]);
    //     }
    //     else
    //     {
    //         sounds[name] = Resources.Load<AudioClip>("Audios/" + name);
    //         PlayMusic(sounds[name]);
    //     }
    // }
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="type"></param>
    public void PlayMusic(BgmAudioType type)
    {
        Debug.Log("PlayMusic:" + type);
        switch (type)
        {
            case BgmAudioType.Main:
                //if (nowBgm == BgmAudioType.Main) return;
                nowBgm = BgmAudioType.Main;
                PlayMusic(audioConfig.GetMainBgm("MainBgm"));
                break;
            case BgmAudioType.Memory:
                //if (nowBgm == BgmAudioType.Memory) return;
                nowBgm = BgmAudioType.Memory;
                PlayMusic(audioConfig.GetMainBgm("MemoryBgm"));
                break;
            case BgmAudioType.End:
                //if (nowBgm == BgmAudioType.End) return;
                nowBgm = BgmAudioType.End;
                PlayMusic(audioConfig.GetMainBgm("ResultBgm"));
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="sound"></param>
    public void PlaySound(AudioClip sound)
    {
        if (sound == null || soundSource == null) return;
        Debug.Log("PlaySound:" + sound.name);
        soundSource.PlayOneShot(sound, soundVolume);
    }

    /// <summary>
    /// 播放音效, 第二个参数为true时，表示是传入id号
    /// </summary>
    /// <param name="name"></param>
    public void PlaySound(string name, bool isId = false)
    {
        //根据id播放特殊音效
        if (isId == true)
        {
            AudioClip clip = audioConfig.GetSpecialItemSound(name);
            Debug.Log("PlaySound by id:" + clip.name); 
            if (clip != null)
                PlaySound(clip);
            return;
        }
        if (sounds.ContainsKey(name))
            {
                PlaySound(sounds[name]);
            }
            else
            {
                sounds[name] = Resources.Load<AudioClip>("Audios/" + name);
                PlaySound(sounds[name]);
            }
    }

    /// <summary>
    /// 更新音量
    /// </summary>
    public void UpdateVolume()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (soundSource != null)
            soundSource.volume = soundVolume;
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
}
