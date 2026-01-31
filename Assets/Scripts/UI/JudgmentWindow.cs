using System;

using TMPro;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 判断窗口（单例）：用于在 UI 层展示“是/否”选择，并在用户点击后回调对应逻辑。
/// 注意：该类只负责 UI 呈现与按钮事件分发，不承载具体玩法逻辑。
/// </summary>
public sealed class JudgmentWindow : MonoSingleton<JudgmentWindow>
{
    [Header("引用（推荐在 Prefab/Scene 绑定）")]
    [FormerlySerializedAs("yesButton")]
    [SerializeField] private Button _yesButton;

    [FormerlySerializedAs("noButton")]
    [SerializeField] private Button _noButton;

    [Tooltip("询问内容文本（可选，未绑定则尝试按层级名查找「询问信息」")]
    [SerializeField] private TMP_Text _messageText;

    private bool _initialized;
    private Action _onYes;
    private Action _onNo;

    private void Awake()
    {
        if (mInstance != null && mInstance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            return;
        }

        mInstance = this;
        EnsureInitialized();
    }

    public override void OnSingletonInit()
    {
        EnsureInitialized();
    }

    public void Show(string message, Action onYes, Action onNo)
    {
        EnsureInitialized();

        _onYes = onYes;
        _onNo = onNo;

        if (_messageText != null)
        {
            _messageText.text = message ?? string.Empty;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _onYes = null;
        _onNo = null;
        gameObject.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (_yesButton == null)
        {
            _yesButton = FindButtonInChildren("是");
        }

        if (_noButton == null)
        {
            _noButton = FindButtonInChildren("否");
        }

        if (_messageText == null)
        {
            _messageText = transform.Find("询问信息")?.GetComponent<TMP_Text>();
        }

        BindButtons();
    }

    private void BindButtons()
    {
        if (_yesButton != null)
        {
            _yesButton.onClick.RemoveAllListeners();
            _yesButton.onClick.AddListener(HandleYesClicked);
        }
        else
        {
            Debug.LogWarning("JudgmentWindow: 未找到「是」按钮", this);
        }

        if (_noButton != null)
        {
            _noButton.onClick.RemoveAllListeners();
            _noButton.onClick.AddListener(HandleNoClicked);
        }
        else
        {
            Debug.LogWarning("JudgmentWindow: 未找到「否」按钮", this);
        }
    }

    private Button FindButtonInChildren(string name)
    {
        var btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns)
        {
            if (b != null && b.gameObject.name == name)
            {
                return b;
            }
        }

        return null;
    }

    private void HandleYesClicked()
    {
        var cb = _onYes;
        Hide();
        cb?.Invoke();
    }

    private void HandleNoClicked()
    {
        var cb = _onNo;
        Hide();
        cb?.Invoke();
    }

}
