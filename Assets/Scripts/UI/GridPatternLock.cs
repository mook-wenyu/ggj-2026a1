using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 九宫格密码：玩家通过点击/拖拽在点之间连线，每个点只能连一次。
/// 挂载到「九宫格点集合」的父物体（或星座解密UI上），并确保九宫格点集合下有 9 个子物体。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class GridPatternLock : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("九宫格点集合（含 9 个子物体），不填则自动查找名为「九宫格点」的子物体")]
    public RectTransform gridPointsContainer;
    [Tooltip("连线容器，用于放置线条，不填则自动创建")]
    public RectTransform lineContainer;
    [Tooltip("提示文字，错误时显示「答案错误」")]
    public TMP_Text hintText;

    [Header("显示")]
    public Color lineColor = Color.black;
    public float lineWidth = 10f;
    [Tooltip("被连线经过的九宫格点颜色")]
    public Color linkedPointColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [Tooltip("答案错误时的字体大小")]
    public float errorFontSize = 48f;

    [Header("按钮")]
    public Button confirmButton;
    public Button resetButton;
    public Button closeButton;

    private static readonly int[] Password1 = { 3, 0, 7, 2, 5 };
    private static readonly int[] Password2 = { 5, 2, 7, 0, 3 };

    private readonly List<int> _pattern = new List<int>();
    private readonly List<Image> _lineImages = new List<Image>();
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

    private void Awake()
    {
        if (gridPointsContainer == null)
        {
            var t = transform.Find("九宫格点");
            if (t != null) gridPointsContainer = t as RectTransform;
            if (gridPointsContainer == null)
                gridPointsContainer = GetComponentInChildren<GridLayoutGroup>()?.GetComponent<RectTransform>();
            if (gridPointsContainer == null && GetComponent<GridLayoutGroup>() != null)
                gridPointsContainer = transform as RectTransform;
        }
        if (gridPointsContainer == null)
        {
            Debug.LogError("GridPatternLock: 未找到九宫格点集合");
            return;
        }

        _containerRect = gridPointsContainer;
        SetupPoints();
        SetupLineContainer();
        _currentLineImage = CreateLineImage("CurrentLine");
        SetupHintText();
        SetupButtons();
    }

    private void SetupHintText()
    {
        if (hintText == null)
        {
            var t = transform.Find("提示文字");
            if (t != null) hintText = t.GetComponent<TMP_Text>();
        }
        if (hintText != null)
        {
            _originalHintText = hintText.text;
            _originalHintColor = hintText.color;
            _originalHintFontSize = hintText.fontSize;
        }
    }

    private void SetupButtons()
    {
        if (confirmButton == null) confirmButton = FindButton("确认");
        if (resetButton == null) resetButton = FindButton("重置");
        if (closeButton == null) closeButton = FindButton("关闭");
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
        if (closeButton != null) closeButton.onClick.AddListener(OnClose);
    }

    private Button FindButton(string name)
    {
        var btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns)
            if (b.gameObject.name == name) return b;
        return null;
    }

    private void OnConfirm()
    {
        if (CheckPassword())
        {
            CloseWindow();
            // TODO: 解锁相应条件
        }
        else
        {
            ShowErrorAndReset();
        }
    }

    private void OnReset()
    {
        ClearPattern();
    }

    private void OnClose()
    {
        CloseWindow();
    }

    private bool CheckPassword()
    {
        if (_pattern.Count != 5) return false;
        if (SequenceEquals(_pattern, Password1)) return true;
        if (SequenceEquals(_pattern, Password2)) return true;
        return false;
    }

    private static bool SequenceEquals(IList<int> a, IList<int> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private void CloseWindow()
    {
        gameObject.SetActive(false);
    }

    private void ShowErrorAndReset()
    {
        if (_errorCoroutine != null) StopCoroutine(_errorCoroutine);
        _errorCoroutine = StartCoroutine(ErrorFeedbackCoroutine());
    }

    private IEnumerator ErrorFeedbackCoroutine()
    {
        if (hintText == null) { ClearPattern(); yield break; }
        hintText.text = "答案错误";
        hintText.color = Color.red;
        hintText.fontSize = errorFontSize;
        yield return new WaitForSeconds(3f);
        hintText.text = _originalHintText;
        hintText.color = _originalHintColor;
        hintText.fontSize = _originalHintFontSize;
        ClearPattern();
        _errorCoroutine = null;
    }

    private void SetupPoints()
    {
        var points = new List<GridPointItem>();
        var colors = new List<Color>();
        for (int i = 0; i < gridPointsContainer.childCount; i++)
        {
            var child = gridPointsContainer.GetChild(i);
            var item = child.GetComponent<GridPointItem>();
            if (item == null) item = child.gameObject.AddComponent<GridPointItem>();
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
            Debug.LogWarning($"GridPatternLock: 期望 9 个点，当前有 {_points.Length} 个");
    }

    private void SetupLineContainer()
    {
        if (lineContainer == null)
        {
            var go = new GameObject("Lines");
            go.transform.SetParent(_containerRect.parent, false);
            lineContainer = go.AddComponent<RectTransform>();
            var r = _containerRect;
            lineContainer.anchorMin = r.anchorMin;
            lineContainer.anchorMax = r.anchorMax;
            lineContainer.anchoredPosition = r.anchoredPosition;
            lineContainer.sizeDelta = r.sizeDelta;
            lineContainer.SetAsFirstSibling();
        }
        lineContainer.SetSiblingIndex(_containerRect.GetSiblingIndex());
    }

    private void OnPointDown(int index)
    {
        if (!_isDragging) ClearPattern();
        if (_pattern.Contains(index)) return;
        _pattern.Add(index);
        _isDragging = true;
        RefreshLines();
    }

    private void OnPointEnter(int index)
    {
        if (!_isDragging || _pattern.Contains(index)) return;
        _pattern.Add(index);
        RefreshLines();
    }

    private void Update()
    {
        if (lineContainer == null) return;
        if (_isDragging && _pattern.Count > 0)
        {
            var canvas = lineContainer.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                lineContainer, Input.mousePosition, cam, out var local))
            {
                var lastPos = GetPointLocalPosition(_pattern[_pattern.Count - 1]);
                _currentLineImage.gameObject.SetActive(true);
                SetLineBetween(_currentLineImage, lastPos, local);
            }
        }
        else
        {
            _currentLineImage.gameObject.SetActive(false);
        }
        if (_isDragging && !Input.GetMouseButton(0))
            _isDragging = false;
    }

    private Vector2 GetPointLocalPosition(int index)
    {
        if (index < 0 || index >= _points.Length) return Vector2.zero;
        var rt = _points[index].GetComponent<RectTransform>();
        var canvas = lineContainer.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineContainer, RectTransformUtility.WorldToScreenPoint(cam, rt.position),
            cam, out var local);
        return local;
    }

    private void RefreshLines()
    {
        while (_lineImages.Count < _pattern.Count)
            _lineImages.Add(CreateLineImage($"Line_{_lineImages.Count}"));
        for (int i = 0; i < _lineImages.Count; i++)
            _lineImages[i].gameObject.SetActive(i < _pattern.Count - 1);
        for (int i = 0; i < _pattern.Count - 1; i++)
        {
            var from = GetPointLocalPosition(_pattern[i]);
            var to = GetPointLocalPosition(_pattern[i + 1]);
            SetLineBetween(_lineImages[i], from, to);
        }
        UpdatePointColors();
    }

    private void UpdatePointColors()
    {
        if (_points == null || _originalPointColors == null) return;
        for (int i = 0; i < _points.Length && i < _originalPointColors.Length; i++)
        {
            var graphic = _points[i].GetComponent<Graphic>();
            if (graphic != null)
                graphic.color = _pattern.Contains(i) ? linkedPointColor : _originalPointColors[i];
        }
    }

    private Image CreateLineImage(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(lineContainer, false);
        var img = go.AddComponent<Image>();
        img.color = lineColor;
        img.raycastTarget = false;
        img.sprite = GetWhiteSprite();
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(100, lineWidth);
        return img;
    }

    private void SetLineBetween(Image img, Vector2 from, Vector2 to)
    {
        var rt = img.rectTransform;
        float dist = Vector2.Distance(from, to);
        if (dist < 0.1f) { rt.sizeDelta = new Vector2(0.1f, lineWidth); return; }
        rt.sizeDelta = new Vector2(dist, lineWidth);
        rt.anchoredPosition = (from + to) * 0.5f;
        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
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
            img.gameObject.SetActive(false);
        _currentLineImage.gameObject.SetActive(false);
        UpdatePointColors();
    }
}
