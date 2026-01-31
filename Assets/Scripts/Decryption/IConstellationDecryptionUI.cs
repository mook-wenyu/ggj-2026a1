using System;

/// <summary>
/// 星座解密 UI 的抽象接口：用于让玩法/交互层以“接口”依赖 UI，提升可测试性与可替换性。
/// </summary>
public interface IConstellationDecryptionUI
{
    /// <summary>
    /// 打开星座九宫格解密。
    /// - 返回 true：表示成功打开（已开始等待玩家输入）。
    /// - 返回 false：表示打开失败（例如缺少必要引用）。
    /// </summary>
    bool TryShowConstellationPatternLock(
        Action onSolved,
        Action onCancelled = null,
        int[] acceptedPattern1Override = null,
        int[] acceptedPattern2Override = null);
}
