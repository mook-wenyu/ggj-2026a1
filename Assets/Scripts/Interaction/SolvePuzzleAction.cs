using UnityEngine;

/// <summary>
/// 动作：把某个谜题标记为“已解开”。
/// 用于测试/快速搭建：点一下即视为解谜成功。
/// </summary>
public sealed class SolvePuzzleAction : InteractionAction
{
    [Tooltip("谜题 ID（需与 PuzzleSolvedCondition 一致）。")]
    [SerializeField] private string _puzzleId;

    [Tooltip("标记为 true 表示解开；false 表示重置为未解开。")]
    [SerializeField] private bool _solved = true;

    public override bool TryExecute(in InteractionContext context)
    {
        if (string.IsNullOrWhiteSpace(_puzzleId))
        {
            Debug.LogError("SolvePuzzleAction: 未配置 puzzleId。", context.Target);
            return false;
        }

        PuzzleProgress.SetSolved(_puzzleId, _solved);
        return true;
    }
}
