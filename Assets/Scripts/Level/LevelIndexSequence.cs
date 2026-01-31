/// <summary>
/// 纯逻辑：计算下一关下标。
/// 提供可单测的关卡序列规则，避免把边界条件散落在 MonoBehaviour 里。
/// </summary>
public static class LevelIndexSequence
{
    public static bool TryGetNextIndex(int currentIndex, int levelCount, out int nextIndex, out string error)
    {
        nextIndex = -1;
        error = null;

        if (levelCount <= 0)
        {
            error = "未配置关卡数量。";
            return false;
        }

        if (currentIndex < 0 || currentIndex >= levelCount)
        {
            error = $"当前关卡下标越界：{currentIndex}（关卡数={levelCount}）。";
            return false;
        }

        var candidate = currentIndex + 1;
        if (candidate >= levelCount)
        {
            error = "已到最后一关。";
            return false;
        }

        nextIndex = candidate;
        return true;
    }
}
