/// <summary>
/// 游戏重置入口：集中管理“会话态重置/进度重置”，避免在各处散落 PlayerPrefs/静态状态清理。
/// </summary>
public static class GameResetService
{
    private const int MaxPrologueVersionToClear = 10;
    private const string PrologueCompletedKeyPrefix = "Game.Prologue.Completed";
    private const string LanguageIndexKey = "CURRENT_LANGUAGE_INDEX";

    /// <summary>
    /// 重置本局会话态（不影响跨会话设置/进度）。
    /// </summary>
    public static void ResetSession()
    {
        InventoryService.Reset();
        PuzzleProgress.ResetAll();
    }

    /// <summary>
    /// 重置“新开一局”所需的持久化进度。
    /// 默认保留语言设置（用户偏好）。
    /// </summary>
    public static void ResetProgressForNewGame(bool keepLanguage = true)
    {
        // 清理开场跳过标记：为了兼容旧版本，按范围删除。
        for (var v = 1; v <= MaxPrologueVersionToClear; v++)
        {
            RuntimePrefs.DeleteKey($"{PrologueCompletedKeyPrefix}.v{v}");
        }

        if (!keepLanguage)
        {
            RuntimePrefs.DeleteKey(LanguageIndexKey);
        }

        RuntimePrefs.Save();
    }
}
