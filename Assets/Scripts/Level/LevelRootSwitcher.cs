using UnityEngine;

/// <summary>
/// 关卡切换：同一场景内通过启用/禁用不同的关卡根节点来实现。
/// 每个关卡根节点下包含该关卡的背景、可交互物体、谜题等内容。
/// </summary>
public sealed class LevelRootSwitcher : MonoBehaviour, ILevelSwitcher
{
    [Tooltip("按顺序填入各关卡的根节点（GameObject）。")]
    [SerializeField] private GameObject[] _levelRoots;

    [Tooltip("游戏开始时启用的关卡下标。")]
    [SerializeField] private int _startLevelIndex;

    public int CurrentLevelIndex { get; private set; }

    public int LevelCount => _levelRoots != null ? _levelRoots.Length : 0;

    private void Awake()
    {
        CurrentLevelIndex = Mathf.Clamp(_startLevelIndex, 0, Mathf.Max(0, LevelCount - 1));
        ApplyActiveState(CurrentLevelIndex);
    }

    public bool TryGoToNextLevel(out string error)
    {
        error = null;
        if (LevelCount <= 0)
        {
            error = "LevelRootSwitcher: 未配置关卡根节点列表。";
            return false;
        }

        if (!LevelIndexSequence.TryGetNextIndex(CurrentLevelIndex, LevelCount, out var next, out var seqError))
        {
            error = $"LevelRootSwitcher: {seqError}";
            return false;
        }

        CurrentLevelIndex = next;
        ApplyActiveState(CurrentLevelIndex);
        return true;
    }

    private void ApplyActiveState(int activeIndex)
    {
        if (_levelRoots == null)
        {
            return;
        }

        for (var i = 0; i < _levelRoots.Length; i++)
        {
            var root = _levelRoots[i];
            if (root == null)
            {
                continue;
            }

            root.SetActive(i == activeIndex);
        }
    }
}
