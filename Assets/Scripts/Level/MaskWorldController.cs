using System;
using System.Collections;

using UnityEngine;
using Object = UnityEngine.Object;

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

    [Header("表现（可选）")]
    [Tooltip("不填则自动查找。用于播放戴/摘面具动画与渐隐。")]
    [SerializeField] private MaskAnimationDriver _animationDriver;

    private MaskStateModel _model;
    private DualWorldLevel _currentLevel;
    private LevelPrefabSwitcher _prefabSwitcher;

    private Coroutine _toggleRoutine;

    public bool IsTransitioning { get; private set; }

    public event Action MaskAcquired;
    public event Action<bool> MaskStateChanged;
    public event Action<bool> MaskTransitioningChanged;

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

        if (_animationDriver == null)
        {
            _animationDriver = GetComponent<MaskAnimationDriver>();
            if (_animationDriver == null)
            {
                _animationDriver = Object.FindObjectOfType<MaskAnimationDriver>();
            }
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

        if (!_model.HasMask)
        {
            return;
        }

        // 关键：切换期间不允许重复触发（避免疯狂循环/重复播放）。
        if (IsTransitioning)
        {
            return;
        }

        var currentIsMaskOn = _model.IsMaskOn;
        var targetIsMaskOn = !currentIsMaskOn;
        var steps = MaskToggleSequencePlanner.Build(currentIsMaskOn);

        IsTransitioning = true;
        MaskTransitioningChanged?.Invoke(true);
        _toggleRoutine = StartCoroutine(RunToggleSequence(steps, targetIsMaskOn));
    }

    private IEnumerator RunToggleSequence(MaskToggleSequencePlanner.Step[] steps, bool targetIsMaskOn)
    {
        // 若没有表现驱动，则仅做玩法层切换，保证可玩。
        var driver = _animationDriver;

        foreach (var step in steps)
        {
            switch (step)
            {
                case MaskToggleSequencePlanner.Step.PlayWearAndHold:
                    if (driver != null)
                    {
                        yield return driver.PlayWearAndHold();
                    }
                    break;
                case MaskToggleSequencePlanner.Step.ShowWornPose:
                    driver?.ShowWornPose();
                    break;
                case MaskToggleSequencePlanner.Step.SwitchWorld:
                    SetMaskOn(targetIsMaskOn);
                    break;
                case MaskToggleSequencePlanner.Step.FadeOutAndHide:
                    if (driver != null)
                    {
                        yield return driver.FadeOutAndHide();
                    }
                    break;
                case MaskToggleSequencePlanner.Step.PlayRemovalAndHide:
                    if (driver != null)
                    {
                        yield return driver.PlayRemovalAndHide();
                    }
                    break;
                default:
                    break;
            }
        }

        _toggleRoutine = null;
        IsTransitioning = false;
        MaskTransitioningChanged?.Invoke(false);
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
