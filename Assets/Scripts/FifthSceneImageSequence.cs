using PrimeTween;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FifthScenes 图片切换序列：点击钥匙后依次展示图片二、旋转对象、图片三、背景变黑、文本。
/// </summary>
public class FifthSceneImageSequence : MonoBehaviour
{
    [Header("钥匙")]
    [Tooltip("点击后消失的 GameObject（需有 Button 或 Collider 以响应点击）")]
    [SerializeField] private GameObject _keyObject;

    [Header("背景")]
    [Tooltip("挂载了 SpriteRenderer 的对象，显示三张图片，默认图片一，点击钥匙后切换")]
    [SerializeField] private SpriteRenderer _backgroundSprite;

    [Header("三张精灵图片")]
    [SerializeField] private Sprite _sprite1;
    [SerializeField] private Sprite _sprite2;
    [SerializeField] private Sprite _sprite3;

    [Header("旋转对象")]
    [Tooltip("点击钥匙后出现，短暂静止后匀速旋转一圈再消失")]
    [SerializeField] private Transform _rotatingObject;

    [Header("最终文本")]
    [Tooltip("背景变黑后显示")]
    [SerializeField] private GameObject _textObject;

    [Header("时间配置")]
    [SerializeField] private float _pauseBeforeRotation = 0.8f;
    [SerializeField] private float _rotationDuration = 1.5f;
    [SerializeField] private float _delayBeforeBlack = 0.5f;
    [SerializeField] private float _blackFadeDuration = 0.8f;

    private bool _hasProceeded;
    private KeyClickBridge _keyBridge;

    private void Awake()
    {
        ResetToInitialState();
    }

    private void OnEnable()
    {
        _hasProceeded = false;
        ResetToInitialState();
        EnsureKeyClickReceiver();
    }

    private void OnDisable()
    {
        if (_keyBridge != null)
        {
            _keyBridge.OnClicked -= OnKeyClicked;
        }
    }

    private void ResetToInitialState()
    {
        if (_keyObject != null)
        {
            _keyObject.SetActive(true);
        }

        SetBackgroundSprite(_sprite1);
        SetBackgroundColor(Color.white);

        if (_rotatingObject != null)
        {
            _rotatingObject.gameObject.SetActive(false);
            _rotatingObject.localEulerAngles = Vector3.zero;
        }

        if (_textObject != null)
        {
            _textObject.SetActive(false);
        }
    }

    private void SetBackgroundSprite(Sprite sprite)
    {
        if (sprite == null || _backgroundSprite == null) return;
        _backgroundSprite.sprite = sprite;
    }

    private void SetBackgroundColor(Color color)
    {
        if (_backgroundSprite == null) return;
        _backgroundSprite.color = color;
    }

    private void EnsureKeyClickReceiver()
    {
        if (_keyObject == null) return;

        var btn = _keyObject.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnKeyClicked);
            return;
        }

        if (_keyObject.GetComponent<Collider>() != null || _keyObject.GetComponent<Collider2D>() != null)
        {
            _keyBridge = _keyObject.GetComponent<KeyClickBridge>();
            if (_keyBridge == null)
            {
                _keyBridge = _keyObject.AddComponent<KeyClickBridge>();
            }
            _keyBridge.OnClicked -= OnKeyClicked;
            _keyBridge.OnClicked += OnKeyClicked;
        }
    }

    private void OnKeyClicked()
    {
        if (_hasProceeded) return;
        _hasProceeded = true;

        if (_keyObject != null)
        {
            _keyObject.SetActive(false);
        }

        SetBackgroundSprite(_sprite2);
        SetBackgroundColor(Color.white);

        if (_rotatingObject != null)
        {
            _rotatingObject.gameObject.SetActive(true);
            _rotatingObject.localEulerAngles = Vector3.zero;
        }

        RunSequence();
    }

    private void RunSequence()
    {
        var seq = Sequence.Create();

        if (_rotatingObject != null)
        {
            seq.ChainDelay(_pauseBeforeRotation);
            seq.Chain(Tween.LocalEulerAngles(
                _rotatingObject,
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, -360f),
                _rotationDuration,
                Ease.Linear));
            seq.ChainCallback(() => _rotatingObject.gameObject.SetActive(false));
        }
        else
        {
            seq.ChainDelay(_pauseBeforeRotation + _rotationDuration * 0.5f);
        }

        seq.ChainCallback(() => SetBackgroundSprite(_sprite3));
        seq.ChainDelay(_delayBeforeBlack);

        seq.Chain(Tween.Custom(0f, 1f, _blackFadeDuration, (t) =>
        {
            var c = Color.Lerp(Color.white, Color.black, t);
            SetBackgroundColor(c);
        }));

        seq.ChainCallback(ShowText);
    }

    private void ShowText()
    {
        if (_textObject != null)
        {
            _textObject.SetActive(true);
        }
    }
}
