/// <summary>
/// 纯逻辑：根据通关进度计算面具裂痕阶段下标。
/// 约定：阶段 0 表示“无裂痕”；通关 1 关后进入阶段 1，以此类推并自动封顶。
/// </summary>
public static class MaskCrackProgression
{
    public static int GetStageIndex(int completedLevelCount, int stageCount)
    {
        if (stageCount <= 0)
        {
            return 0;
        }

        if (completedLevelCount <= 0)
        {
            return 0;
        }

        if (completedLevelCount >= stageCount)
        {
            return stageCount - 1;
        }

        return completedLevelCount;
    }
}
