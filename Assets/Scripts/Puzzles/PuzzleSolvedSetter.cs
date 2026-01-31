using UnityEngine;

/// <summary>
/// 供场景/按钮事件调用：把某个谜题标记为已解开。
/// 用法示例：在解谜成功按钮的 OnClick 里绑定 PuzzleSolvedSetter.MarkSolved。
/// </summary>
public sealed class PuzzleSolvedSetter : MonoBehaviour
{
    [Tooltip("谜题 ID（需与 PuzzleSolvedCondition 一致）。")]
    [SerializeField] private string _puzzleId;

    public void MarkSolved()
    {
        PuzzleProgress.MarkSolved(_puzzleId);
    }

    public void SetSolved(bool solved)
    {
        PuzzleProgress.SetSolved(_puzzleId, solved);
    }
}
