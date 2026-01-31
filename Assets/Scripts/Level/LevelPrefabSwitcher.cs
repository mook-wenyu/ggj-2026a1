using UnityEngine;

/// <summary>
/// 关卡切换：运行时实例化关卡预制体到指定父节点（推荐挂在 WorldMgr 上）。
/// 设计目标：关卡内容尽量独立为 Prefab，避免 MainScene 过度膨胀。
/// </summary>
public sealed class LevelPrefabSwitcher : MonoBehaviour, ILevelSwitcher
{
    [Tooltip("关卡预制体列表（按顺序）。")]
    [SerializeField] private GameObject[] _levelPrefabs;

    [Tooltip("当未直接绑定关卡预制体时，从 Resources 加载的路径列表（按顺序）。")]
    [SerializeField] private string[] _levelPrefabResourcePaths;

    [Tooltip("关卡实例挂载的父节点。不填则使用当前物体的 Transform。")]
    [SerializeField] private Transform _levelParent;

    [Tooltip("游戏开始时加载的关卡下标。")]
    [SerializeField] private int _startLevelIndex;

    [Header("可选行为")]
    [Tooltip("切换关卡时是否重置谜题进度。建议为 true，避免不同关卡互相污染。")]
    [SerializeField] private bool _resetPuzzleProgressOnLoad = true;

    public event System.Action<GameObject> LevelLoaded;

    private GameObject _currentInstance;

    public int CurrentLevelIndex { get; private set; }

    public int LevelCount => _levelPrefabs != null ? _levelPrefabs.Length : 0;

    private void Awake()
    {
        EnsureLevelPrefabsLoaded();
        CurrentLevelIndex = Mathf.Clamp(_startLevelIndex, 0, Mathf.Max(0, LevelCount - 1));
        EnsureParent();
        LoadLevel(CurrentLevelIndex);
    }

    public bool TryGoToNextLevel(out string error)
    {
        error = null;
        if (LevelCount <= 0)
        {
            error = "LevelPrefabSwitcher: 未配置关卡预制体列表。";
            return false;
        }

        if (!LevelIndexSequence.TryGetNextIndex(CurrentLevelIndex, LevelCount, out var next, out var seqError))
        {
            error = $"LevelPrefabSwitcher: {seqError}";
            return false;
        }

        CurrentLevelIndex = next;
        EnsureParent();
        LoadLevel(CurrentLevelIndex);
        return true;
    }

    private void EnsureParent()
    {
        if (_levelParent == null)
        {
            _levelParent = transform;
        }
    }

    private void EnsureLevelPrefabsLoaded()
    {
        if (_levelPrefabs != null && _levelPrefabs.Length > 0)
        {
            return;
        }

        if (_levelPrefabResourcePaths == null || _levelPrefabResourcePaths.Length == 0)
        {
            return;
        }

        _levelPrefabs = new GameObject[_levelPrefabResourcePaths.Length];
        for (var i = 0; i < _levelPrefabResourcePaths.Length; i++)
        {
            var path = _levelPrefabResourcePaths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            _levelPrefabs[i] = Resources.Load<GameObject>(path.Trim());
        }
    }

    private void LoadLevel(int index)
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }

        if (_resetPuzzleProgressOnLoad)
        {
            PuzzleProgress.ResetAll();
        }

        if (_levelPrefabs == null || index < 0 || index >= _levelPrefabs.Length)
        {
            return;
        }

        var prefab = _levelPrefabs[index];
        if (prefab == null)
        {
            return;
        }

        _currentInstance = Instantiate(prefab, _levelParent);
        _currentInstance.name = prefab.name;
        _currentInstance.transform.localPosition = Vector3.zero;
        _currentInstance.transform.localRotation = Quaternion.identity;
        _currentInstance.transform.localScale = Vector3.one;

        LevelLoaded?.Invoke(_currentInstance);
    }
}
