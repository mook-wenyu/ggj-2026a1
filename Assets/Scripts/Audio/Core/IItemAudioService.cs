using UnityEngine;

/// <summary>
/// 物品音频服务：
/// - UI 只依赖该接口即可判断是否有音频，并触发播放/重放。
/// - 具体实现（例如 AudioMgr / 其他音频系统）可替换。
/// </summary>
public interface IItemAudioService
{
    /// <summary>
    /// 根据 Resources 路径加载音频（不含扩展名）。
    /// 返回 false 表示路径为空或资源不存在。
    /// </summary>
    bool TryGetItemAudioClip(string audioPath, out AudioClip clip);

    void PlayItemAudio(AudioClip clip);
}
