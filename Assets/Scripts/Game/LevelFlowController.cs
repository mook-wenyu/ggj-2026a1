using System.Collections;

using UnityEngine;

/// <summary>
/// 关卡流程控制：负责“通关 -> 下一关界面 -> 黑屏过场 -> 切关 -> 淡出”。
/// </summary>
public sealed class LevelFlowController : MonoBehaviour
{
    [Header("引用（可不填，会自动查找）")]
    [SerializeField] private BlackPanel _blackPanel;
    [SerializeField] private LevelPanel _levelPanel;

    [Tooltip("关卡切换器（推荐挂在 WorldMgr 上）。")]
    [SerializeField] private MonoBehaviour _levelSwitcherBehaviour;

    private ILevelSwitcher _levelSwitcher;
    private Coroutine _routine;
    private bool _continueRequested;

    private void Awake()
    {
        if (_blackPanel == null)
        {
            _blackPanel = BlackPanel.Instance;
        }

        if (_levelPanel == null)
        {
            _levelPanel = LevelPanel.Instance;
        }

        _levelSwitcher = ResolveLevelSwitcher();

        _levelPanel?.Hide();
    }

    public void CompleteLevel()
    {
        if (_routine != null)
        {
            return;
        }

        _routine = StartCoroutine(CompleteLevelRoutine());
    }

    private IEnumerator CompleteLevelRoutine()
    {
        if (_blackPanel == null)
        {
            Debug.LogError("LevelFlowController: 场景中缺少 BlackPanel，无法播放过场。", this);
            _routine = null;
            yield break;
        }

        // 1) 先显示“下一关界面”，等待玩家确认；此时黑屏应隐藏且不拦截输入。
        _blackPanel.SetAlphaImmediate(0f);
        _blackPanel.SetInputBlocked(false);

        if (_levelPanel != null)
        {
            _continueRequested = false;
            _levelPanel.ContinueRequested += HandleContinueRequested;
            _levelPanel.Show();
            yield return new WaitUntil(() => _continueRequested);
            _levelPanel.ContinueRequested -= HandleContinueRequested;
        }
        else
        {
            Debug.LogWarning("LevelFlowController: 场景中缺少 LevelPanel，将直接切换到下一关。", this);
        }

        // 2) 点击“下一关”后再黑屏过场并切关。
        _blackPanel.SetInputBlocked(true);
        yield return FadeIn();
        _levelPanel?.Hide();

        // 3) 切关
        if (_levelSwitcher != null)
        {
            if (!_levelSwitcher.TryGoToNextLevel(out var error))
            {
                Debug.LogWarning(error);
            }
        }
        else
        {
            Debug.LogWarning("LevelFlowController: 未配置 ILevelSwitcher，无法真正切换到下一关。", this);
        }

        // 4) 黑屏淡出
        yield return FadeOut();

        _routine = null;
    }

    private ILevelSwitcher ResolveLevelSwitcher()
    {
        if (_levelSwitcherBehaviour != null)
        {
            if (_levelSwitcherBehaviour is ILevelSwitcher typed)
            {
                return typed;
            }

            Debug.LogError("LevelFlowController: _levelSwitcherBehaviour 未实现 ILevelSwitcher。", this);
        }

        // 优先取 WorldMgr 上挂的切关器（符合“关卡挂在 WorldMgr 下”的约定）。
        var worldMgr = GameObject.Find("WorldMgr");
        if (worldMgr != null)
        {
            var behaviours = worldMgr.GetComponents<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is ILevelSwitcher candidate)
                {
                    return candidate;
                }
            }
        }

        // 兜底：全场景扫描（只在 Awake 执行一次）。
        var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var behaviour in allBehaviours)
        {
            if (behaviour is ILevelSwitcher candidate)
            {
                return candidate;
            }
        }

        return null;
    }

    private void HandleContinueRequested()
    {
        _continueRequested = true;
    }

    private IEnumerator FadeIn()
    {
        var done = false;
        _blackPanel.FadeIn(() => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator FadeOut()
    {
        var done = false;
        _blackPanel.FadeOut(() => done = true);
        yield return new WaitUntil(() => done);
    }
}
