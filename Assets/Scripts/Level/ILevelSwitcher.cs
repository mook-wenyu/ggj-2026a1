/// <summary>
/// 关卡切换策略。
/// 说明：为了保持可测试与可替换，此处用接口抽象“下一关”的计算与切换。
/// </summary>
public interface ILevelSwitcher
{
    int CurrentLevelIndex { get; }
    int LevelCount { get; }

    /// <summary>
    /// 切到下一关。
    /// - 返回 true：切关成功。
    /// - 返回 false：已到最后一关或配置错误。
    /// </summary>
    bool TryGoToNextLevel(out string error);
}
