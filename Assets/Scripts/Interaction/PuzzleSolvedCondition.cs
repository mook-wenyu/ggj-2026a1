using UnityEngine;

/// <summary>
/// 条件：要求某个谜题已被解开。
/// </summary>
public sealed class PuzzleSolvedCondition : PickupCondition
{
    [Tooltip("谜题 ID（建议全局唯一，例如：level1_keypad）。")]
    [SerializeField] private string _puzzleId;

    [Tooltip("当谜题未解开时的提示文本（可选）。")]
    [SerializeField] private string _blockedReasonOverride;

    public override bool CanPickup(out string blockedReason)
    {
        blockedReason = null;

        if (PuzzleProgress.IsSolved(_puzzleId))
        {
            return true;
        }

        blockedReason = string.IsNullOrWhiteSpace(_blockedReasonOverride)
            ? $"需要先解开谜题：{_puzzleId}"
            : _blockedReasonOverride.Trim();

        return false;
    }
}
