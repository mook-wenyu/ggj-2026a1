using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 判断窗口：包含「是」和「否」两个按钮，点击任意一个会关闭窗口并执行对应逻辑。
/// </summary>
public class JudgmentWindow : MonoBehaviour
{
    [Tooltip("「是」按钮，不填则自动查找名为「是」的子物体")]
    public Button yesButton;
    [Tooltip("「否」按钮，不填则自动查找名为「否」的子物体")]
    public Button noButton;

    private void Awake()
    {
        if (yesButton == null)
            yesButton = FindButtonInChildren("是");
        if (noButton == null)
            noButton = FindButtonInChildren("否");

        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesClicked);
        else
            Debug.LogWarning("JudgmentWindow: 未找到「是」按钮");

        if (noButton != null)
            noButton.onClick.AddListener(OnNoClicked);
        else
            Debug.LogWarning("JudgmentWindow: 未找到「否」按钮");
    }

    private Button FindButtonInChildren(string name)
    {
        var btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns)
        {
            if (b.gameObject.name == name)
                return b;
        }
        return null;
    }

    private void OnYesClicked()
    {
        CloseWindow();
        Debug.Log("执行了【是】的逻辑");
        // TODO: 在此处添加具体逻辑
    }

    private void OnNoClicked()
    {
        CloseWindow();
        Debug.Log("执行了【否】的逻辑");
        // TODO: 在此处添加具体逻辑
    }

    private void CloseWindow()
    {
        gameObject.SetActive(false);
    }
}
