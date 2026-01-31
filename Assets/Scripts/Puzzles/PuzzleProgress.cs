using System;
using System.Collections.Generic;

/// <summary>
/// 谜题进度（是否已解开）。
/// 当前实现为“仅运行期内存态”，避免引入持久化复杂度。
/// 如需跨存档/跨会话持久化，可在此处替换为 PlayerPrefs/存档系统。
/// </summary>
public static class PuzzleProgress
{
    private static readonly Dictionary<string, bool> SolvedById = new(StringComparer.Ordinal);

    public static bool IsSolved(string puzzleId)
    {
        if (!TryNormalizeId(puzzleId, out var id))
        {
            return false;
        }

        return SolvedById.TryGetValue(id, out var solved) && solved;
    }

    public static void MarkSolved(string puzzleId)
    {
        SetSolved(puzzleId, true);
    }

    public static void SetSolved(string puzzleId, bool solved)
    {
        if (!TryNormalizeId(puzzleId, out var id))
        {
            return;
        }

        SolvedById[id] = solved;
    }

    /// <summary>
    /// 仅用于测试/调试：清空所有进度。
    /// </summary>
    public static void ResetAll()
    {
        SolvedById.Clear();
    }

    private static bool TryNormalizeId(string puzzleId, out string normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(puzzleId))
        {
            return false;
        }

        normalized = puzzleId.Trim();
        return normalized.Length > 0;
    }
}
