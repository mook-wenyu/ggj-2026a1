using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 保险柜UI：第一重密码为4位数字(1027)，第二重密码为3个滚盘(▲●▲)。
/// </summary>
public class SafeUI : MonoBehaviour
{
    private const string FirstPassword = "1027";
    private static readonly char[] Symbols = { '■', '▲', '●' };

    [Header("第一重密码")]
    [SerializeField] private Button[] _numberButtons;

    [Header("第二重密码")]
    [SerializeField] private Button[] _rollerButtons;

    [Header("其他")]
    [SerializeField] private Button _closeButton;

    private readonly List<char> _firstInput = new List<char>(4);
    private readonly int[] _rollerStates = { 0, 0, 0 };

    private void Awake()
    {
        EnsureRefs();
        BindButtons();
    }

    private void EnsureRefs()
    {
        if (_numberButtons == null || _numberButtons.Length == 0)
            _numberButtons = FindNumberButtons();
        if (_rollerButtons == null || _rollerButtons.Length == 0)
            _rollerButtons = FindRollerButtons();
        if (_closeButton == null)
            _closeButton = FindButton("关闭");
    }

    private void BindButtons()
    {
        for (int i = 0; i <= 9; i++)
        {
            var btn = GetNumberButton(i);
            if (btn != null)
            {
                int n = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnNumberClicked(n));
            }
        }

        for (int i = 0; i < _rollerButtons.Length; i++)
        {
            int idx = i;
            var btn = _rollerButtons[i];
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnRollerClicked(idx));
            }
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(Close);
        }
    }

    private Button GetNumberButton(int digit)
    {
        foreach (var b in _numberButtons)
            if (b != null && b.gameObject.name == digit.ToString())
                return b;
        return null;
    }

    private void OnNumberClicked(int digit)
    {
        if (_firstInput.Count >= 4) return;
        _firstInput.Add((char)('0' + digit));
        if (_firstInput.Count == 4)
            CheckFirstPassword();
    }

    private void CheckFirstPassword()
    {
        var input = new string(_firstInput.ToArray());
        if (input != FirstPassword)
        {
            Close();
            return;
        }
        Debug.Log("保险柜第一重密码正确");
    }

    private void OnRollerClicked(int index)
    {
        if (index < 0 || index >= _rollerStates.Length || index >= _rollerButtons.Length) return;
        _rollerStates[index] = (_rollerStates[index] + 1) % Symbols.Length;
        var btn = _rollerButtons[index];
        var tmp = btn != null ? btn.GetComponentInChildren<TMP_Text>(true) : null;
        if (tmp != null)
            tmp.text = Symbols[_rollerStates[index]].ToString();
        CheckSecondPassword();
    }

    private void CheckSecondPassword()
    {
        if (_rollerStates[0] != 1 || _rollerStates[1] != 2 || _rollerStates[2] != 1)
            return;
        Debug.Log("保险柜第二重密码正确");
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _firstInput.Clear();
        for (int i = 0; i < _rollerStates.Length; i++)
            _rollerStates[i] = 0;
        RefreshRollers();
    }

    private void RefreshRollers()
    {
        for (int i = 0; i < _rollerStates.Length && i < _rollerButtons.Length; i++)
        {
            var tmp = _rollerButtons[i]?.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                tmp.text = Symbols[_rollerStates[i]].ToString();
        }
    }

    private Button FindButton(string name)
    {
        var btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns)
            if (b.gameObject.name == name) return b;
        return null;
    }

    private Button[] FindNumberButtons()
    {
        var list = new List<Button>();
        for (int i = 0; i <= 9; i++)
        {
            var b = FindButton(i.ToString());
            if (b != null) list.Add(b);
        }
        return list.ToArray();
    }

    private Button[] FindRollerButtons()
    {
        var list = new List<Button>();
        foreach (var name in new[] { "滚盘1", "滚盘2", "滚盘3" })
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name) continue;
                var b = t.GetComponent<Button>();
                if (b != null) list.Add(b);
                break;
            }
        }
        return list.ToArray();
    }

}
