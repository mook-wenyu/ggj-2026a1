using System;

using UnityEngine;

/// <summary>
/// 面具控制器：维护当前“是否戴面具”的状态，并把状态应用到当前关卡（DualWorldLevel）。
/// 推荐挂在 WorldMgr 上。
/// </summary>
public sealed class MaskWorldController : MonoBehaviour
{
    [Header("输入")]
    [SerializeField] private bool _enableKeyboardToggle = true;
    [SerializeField] private KeyCode _toggleKey = KeyCode.Space;

    [Header("初始状态")]
    [SerializeField] private bool _startHasMask;
    [SerializeField] private bool _startWithMask;

    private MaskStateModel _model;
    private DualWorldLevel _currentLevel;
    private LevelPrefabSwitcher _prefabSwitcher;

    public event Action MaskAcquired;
    public event Action<bool> MaskStateChanged;

    public bool HasMask => _model != null && _model.HasMask;
    public bool IsMaskOn => _model != null && _model.IsMaskOn;

    private void Awake()
    {
        _model = new MaskStateModel(
            startHasMask: _startHasMask,
            startMaskOn: _startWithMask
        );

        _prefabSwitcher = GetComponent<LevelPrefabSwitcher>();
        if (_prefabSwitcher != null)
        {
            _prefabSwitcher.LevelLoaded += HandleLevelLoaded;
        }

        // 若场景一开始已经存在关卡实例，也尝试绑定一次。
        TryBindCurrentLevel();
        ApplyToCurrentLevel();
    }

    private void OnDestroy()
    {
        if (_prefabSwitcher != null)
        {
            _prefabSwitcher.LevelLoaded -= HandleLevelLoaded;
        }
    }

    private void Update()
    {
        if (!_enableKeyboardToggle)
        {
            return;
        }

        if (HasMask && Input.GetKeyDown(_toggleKey))
        {
            ToggleMask();
        }
    }

    public void AcquireMask()
    {
        if (_model == null)
        {
            return;
        }

        if (!_model.TryAcquireMask())
        {
            return;
        }

        MaskAcquired?.Invoke();
        MaskStateChanged?.Invoke(_model.IsMaskOn);
        ApplyToCurrentLevel();
    }

    public void ToggleMask()
    {
        if (_model == null)
        {
            return;
        }

        if (!_model.TryToggleMask())
        {
            return;
        }

        ApplyToCurrentLevel();
        MaskStateChanged?.Invoke(_model.IsMaskOn);
    }

    public void SetMaskOn(bool isMaskOn)
    {
        if (_model == null)
        {
            return;
        }

        if (!_model.TrySetMaskOn(isMaskOn))
        {
            return;
        }

        ApplyToCurrentLevel();
        MaskStateChanged?.Invoke(_model.IsMaskOn);
    }

    private void HandleLevelLoaded(GameObject levelInstance)
    {
        _currentLevel = null;
        if (levelInstance != null)
        {
            _currentLevel = levelInstance.GetComponent<DualWorldLevel>();
            if (_currentLevel == null)
            {
                _currentLevel = levelInstance.GetComponentInChildren<DualWorldLevel>(true);
            }
        }

        ApplyToCurrentLevel();
    }

    private void TryBindCurrentLevel()
    {
        _currentLevel = GetComponentInChildren<DualWorldLevel>(true);
    }

    private void ApplyToCurrentLevel()
    {
        if (_currentLevel == null)
        {
            return;
        }

        _currentLevel.SetMaskOn(IsMaskOn);
    }
}
