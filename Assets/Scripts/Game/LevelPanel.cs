using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 下一关界面。
/// 约定：默认“点击任意位置继续”，也可在 Inspector 绑定按钮。
/// </summary>
public sealed class LevelPanel : MonoBehaviour, IPointerClickHandler
{
    public event Action ContinueRequested;

    [Header("引用（可不填，会自动查找/补齐）")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _continueButton;

    [Header("行为")]
    [SerializeField] private bool _startHidden = true;

    private bool _subscribed;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_continueButton == null)
        {
            _continueButton = GetComponentInChildren<Button>(true);
        }

        if (_startHidden)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RequestContinue();
    }

    private void Subscribe()
    {
        if (_subscribed)
        {
            return;
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(RequestContinue);
        }

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(RequestContinue);
        }

        _subscribed = false;
    }

    private void RequestContinue()
    {
        ContinueRequested?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
