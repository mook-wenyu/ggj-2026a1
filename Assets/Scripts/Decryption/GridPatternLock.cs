using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 九宫格密码：玩家通过点击/拖拽在点之间连线，每个点只能连一次。
/// 说明：该类仅负责 UI 表现与输入采集，解密是否成功由可替换的校验逻辑决定。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class GridPatternLock : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("九宫格点集合（含 9 个子物体），不填则自动查找名为「九宫格点」的子物体")]
    [SerializeField] private RectTransform _gridPointsContainer;

    [Tooltip("连线容器，用于放置线条，不填则自动创建")]
    [SerializeField] private RectTransform _lineContainer;

    [Tooltip("提示文字，错误时显示「答案错误」")]
    [SerializeField] private TMP_Text _hintText;

    [Header("显示")]
    [SerializeField] private Color _lineColor = Color.black;
    [SerializeField] private float _lineWidth = 10f;

    [Tooltip("被连线经过的九宫格点颜色")]
    [SerializeField] private Color _linkedPointColor = new(0.5f, 0.5f, 0.5f, 1f);

    [Tooltip("答案错误时的字体大小")]
    [SerializeField] private float _errorFontSize = 48f;

    [Header("按钮")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _closeButton;

    [Header("校验（默认提供两组可接受答案）")]
    [SerializeField] private int[] _acceptedPattern1 = { 3, 0, 7, 2, 5 };
    [SerializeField] private int[] _acceptedPattern2 = { 5, 2, 7, 0, 3 };

    /// <summary>
    /// 解密成功事件（参数为玩家输入的图案序列拷贝）。
    /// </summary>
    public event Action<int[]> Solved;

    /// <summary>
    /// 玩家主动关闭事件（例如点击关闭按钮）。
    /// </summary>
    public event Action Cancelled;

    private readonly List<int> _pattern = new();
    private readonly List<Image> _lineImages = new();

    private Image _currentLineImage; // 跟随鼠标的线
    private bool _isDragging;
    private GridPointItem[] _points;
    private Color[] _originalPointColors;
    private RectTransform _containerRect;
    private static Texture2D _whiteTex;

    private string _originalHintText;
    private Color _originalHintColor;
    private float _originalHintFontSize;
    private Coroutine _errorCoroutine;

    private bool _initialized;
    private bool _buttonBound;

    private int[] _overrideAcceptedPattern1;
    private int[] _overrideAcceptedPattern2;

    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// 运行时覆盖可接受答案（传 null 表示使用 Inspector 默认配置）。
    /// </summary>
    public void OverrideAcceptedPatterns(int[] pattern1, int[] pattern2)
    {
        _overrideAcceptedPattern1 = pattern1;
        _overrideAcceptedPattern2 = pattern2;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        EnsureInitialized();
        RestoreHintText();
        ClearPattern();
    }

    public void Hide()
    {
        StopErrorRoutineIfNeeded();
        ClearPattern();
        gameObject.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (_gridPointsContainer == null)
        {
            var t = transform.Find("九宫格点");
            if (t != null)
            {
                _gridPointsContainer = t as RectTransform;
            }

            if (_gridPointsContainer == null)
            {
                _gridPointsContainer = GetComponentInChildren<GridLayoutGroup>()?.GetComponent<RectTransform>();
            }

            if (_gridPointsContainer == null && GetComponent<GridLayoutGroup>() != null)
            {
                _gridPointsContainer = transform as RectTransform;
            }
        }

        if (_gridPointsContainer == null)
        {
            Debug.LogError("GridPatternLock: 未找到九宫格点集合", this);
            return;
        }

        _containerRect = _gridPointsContainer;
        SetupPoints();
        SetupLineContainer();
        _currentLineImage = CreateLineImage("CurrentLine");
        SetupHintText();
        SetupButtons();
    }

    private void SetupHintText()
    {
        if (_hintText == null)
        {
            _hintText = transform.Find("提示文字")?.GetComponent<TMP_Text>();
        }

        if (_hintText != null)
        {
            _originalHintText = _hintText.text;
            _originalHintColor = _hintText.color;
            _originalHintFontSize = _hintText.fontSize;
        }
    }

    private void RestoreHintText()
    {
        if (_hintText == null)
        {
            return;
        }

        _hintText.text = _originalHintText;
        _hintText.color = _originalHintColor;
        _hintText.fontSize = _originalHintFontSize;
    }

    private void SetupButtons()
    {
        if (_buttonBound)
        {
            return;
        }

        _buttonBound = true;

        if (_confirmButton == null) _confirmButton = FindButton("确认");
        if (_resetButton == null) _resetButton = FindButton("重置");
        if (_closeButton == null) _closeButton = FindButton("关闭");

        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveAllListeners();
            _confirmButton.onClick.AddListener(OnConfirm);
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveAllListeners();
            _resetButton.onClick.AddListener(OnReset);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(OnClose);
        }
    }

    private Button FindButton(string name)
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

    private void OnConfirm()
    {
        EnsureInitialized();

        if (CheckPassword())
        {
            // 关键：先隐藏再回调，避免回调中再次打开导致 UI 状态交织。
            var solvedPattern = CopyPattern();
            Hide();
            Solved?.Invoke(solvedPattern);
            return;
        }

        ShowErrorAndReset();
    }

    private void OnReset()
    {
        ClearPattern();
        RestoreHintText();
    }

    private void OnClose()
    {
        Hide();
        Cancelled?.Invoke();
    }

    private bool CheckPassword()
    {
        var p1 = _overrideAcceptedPattern1 ?? _acceptedPattern1;
        var p2 = _overrideAcceptedPattern2 ?? _acceptedPattern2;

        if (PatternLockValidator.IsMatch(_pattern, p1))
        {
            return true;
        }

        if (PatternLockValidator.IsMatch(_pattern, p2))
        {
            return true;
        }

        return false;
    }

    private int[] CopyPattern()
    {
        var arr = new int[_pattern.Count];
        for (var i = 0; i < _pattern.Count; i++)
        {
            arr[i] = _pattern[i];
        }

        return arr;
    }

    private void ShowErrorAndReset()
    {
        StopErrorRoutineIfNeeded();
        _errorCoroutine = StartCoroutine(ErrorFeedbackCoroutine());
    }

    private void StopErrorRoutineIfNeeded()
    {
        if (_errorCoroutine == null)
        {
            return;
        }

        StopCoroutine(_errorCoroutine);
        _errorCoroutine = null;
    }

    private IEnumerator ErrorFeedbackCoroutine()
    {
        if (_hintText == null)
        {
            ClearPattern();
            yield break;
        }

        _hintText.text = "答案错误";
        _hintText.color = Color.red;
        _hintText.fontSize = _errorFontSize;
        yield return new WaitForSeconds(3f);
        RestoreHintText();
        ClearPattern();
        _errorCoroutine = null;
    }

    private void SetupPoints()
    {
        var points = new List<GridPointItem>();
        var colors = new List<Color>();

        for (var i = 0; i < _gridPointsContainer.childCount; i++)
        {
            var child = _gridPointsContainer.GetChild(i);
            if (child == null)
            {
                continue;
            }

            var item = child.GetComponent<GridPointItem>();
            if (item == null)
            {
                item = child.gameObject.AddComponent<GridPointItem>();
            }

            item.index = i;
            item.onPointerDown = OnPointDown;
            item.onPointerEnter = OnPointEnter;
            points.Add(item);

            var graphic = child.GetComponent<Graphic>();
            colors.Add(graphic != null ? graphic.color : Color.white);
        }

        _points = points.ToArray();
        _originalPointColors = colors.ToArray();

        if (_points.Length != 9)
        {
            Debug.LogWarning($"GridPatternLock: 期望 9 个点，当前有 {_points.Length} 个", this);
        }
    }

    private void SetupLineContainer()
    {
        if (_lineContainer == null)
        {
            var go = new GameObject("Lines");
            go.transform.SetParent(_containerRect.parent, false);
            _lineContainer = go.AddComponent<RectTransform>();
            var r = _containerRect;
            _lineContainer.anchorMin = r.anchorMin;
            _lineContainer.anchorMax = r.anchorMax;
            _lineContainer.anchoredPosition = r.anchoredPosition;
            _lineContainer.sizeDelta = r.sizeDelta;
            _lineContainer.SetAsFirstSibling();
        }

        _lineContainer.SetSiblingIndex(_containerRect.GetSiblingIndex());
    }

    private void OnPointDown(int index)
    {
        if (!_isDragging)
        {
            ClearPattern();
        }

        if (_pattern.Contains(index))
        {
            return;
        }

        _pattern.Add(index);
        _isDragging = true;
        RefreshLines();
    }

    private void OnPointEnter(int index)
    {
        if (!_isDragging || _pattern.Contains(index))
        {
            return;
        }

        _pattern.Add(index);
        RefreshLines();
    }

    private void Update()
    {
        if (_lineContainer == null)
        {
            return;
        }

        if (_isDragging && _pattern.Count > 0)
        {
            var canvas = _lineContainer.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _lineContainer, Input.mousePosition, cam, out var local))
            {
                var lastPos = GetPointLocalPosition(_pattern[_pattern.Count - 1]);
                _currentLineImage.gameObject.SetActive(true);
                SetLineBetween(_currentLineImage, lastPos, local);
            }
        }
        else if (_currentLineImage != null)
        {
            _currentLineImage.gameObject.SetActive(false);
        }

        if (_isDragging && !Input.GetMouseButton(0))
        {
            _isDragging = false;
        }
    }

    private Vector2 GetPointLocalPosition(int index)
    {
        if (index < 0 || index >= _points.Length)
        {
            return Vector2.zero;
        }

        var rt = _points[index].GetComponent<RectTransform>();
        var canvas = _lineContainer.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _lineContainer,
            RectTransformUtility.WorldToScreenPoint(cam, rt.position),
            cam,
            out var local
        );

        return local;
    }

    private void RefreshLines()
    {
        while (_lineImages.Count < _pattern.Count)
        {
            _lineImages.Add(CreateLineImage($"Line_{_lineImages.Count}"));
        }

        for (var i = 0; i < _lineImages.Count; i++)
        {
            _lineImages[i].gameObject.SetActive(i < _pattern.Count - 1);
        }

        for (var i = 0; i < _pattern.Count - 1; i++)
        {
            var from = GetPointLocalPosition(_pattern[i]);
            var to = GetPointLocalPosition(_pattern[i + 1]);
            SetLineBetween(_lineImages[i], from, to);
        }

        UpdatePointColors();
    }

    private void UpdatePointColors()
    {
        if (_points == null || _originalPointColors == null)
        {
            return;
        }

        for (var i = 0; i < _points.Length && i < _originalPointColors.Length; i++)
        {
            var graphic = _points[i].GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = _pattern.Contains(i) ? _linkedPointColor : _originalPointColors[i];
            }
        }
    }

    private Image CreateLineImage(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_lineContainer, false);

        var img = go.AddComponent<Image>();
        img.color = _lineColor;
        img.raycastTarget = false;
        img.sprite = GetWhiteSprite();

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(100, _lineWidth);
        return img;
    }

    private void SetLineBetween(Image img, Vector2 from, Vector2 to)
    {
        if (img == null)
        {
            return;
        }

        var rt = img.rectTransform;
        var dist = Vector2.Distance(from, to);
        if (dist < 0.1f)
        {
            rt.sizeDelta = new Vector2(0.1f, _lineWidth);
            return;
        }

        rt.sizeDelta = new Vector2(dist, _lineWidth);
        rt.anchoredPosition = (from + to) * 0.5f;
        var angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        rt.localEulerAngles = new Vector3(0, 0, angle);
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        return Sprite.Create(_whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    /// <summary>获取当前绘制的图案（0-8 的索引序列）</summary>
    public IReadOnlyList<int> GetPattern() => _pattern;

    /// <summary>清空图案</summary>
    public void ClearPattern()
    {
        _pattern.Clear();
        _isDragging = false;

        foreach (var img in _lineImages)
        {
            if (img != null)
            {
                img.gameObject.SetActive(false);
            }
        }

        if (_currentLineImage != null)
        {
            _currentLineImage.gameObject.SetActive(false);
        }

        UpdatePointColors();
    }
}
