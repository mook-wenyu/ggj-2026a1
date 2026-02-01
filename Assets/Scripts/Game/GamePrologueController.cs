using System.Collections;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开场叙事：在第一关开始前展示“快递-面具”文案，并授予面具能力。
/// 设计目标：
/// - 低耦合：不侵入关卡/解密/交互系统。
/// - 可复用：通过 Inspector 配置文案与是否跳过。
/// </summary>
public sealed class GamePrologueController : MonoBehaviour
{
    private const string PrologueCompletedKeyPrefix = "Game.Prologue.Completed";
    private const string DefaultPopupPrefabResourcePath = "Prefabs/ProloguePopup";
    private const string DefaultGiftObjectName = "gift";
    private const int DefaultPrologueVersion = 2;

    [Header("引用（可不填，会自动查找/加载）")]
    [SerializeField] private MaskWorldController _maskController;
    [SerializeField] private Canvas _rootCanvas;
    [SerializeField] private ProloguePopupView _popupPrefab;

    [Tooltip("开场纯黑背景（可选，必须是 UI：Image）。仅用于文案阶段保持纯黑背景，避免遮挡开场文本。")]
    [SerializeField] private GameObject _prologueBackdrop;

    [Tooltip("礼物动画的 Animator。不填则按名称 gift 自动查找。")]
    [SerializeField] private Animator _giftAnimator;

    [Header("叙事文案")]
    [SerializeField] private string _title = "快递";

    [TextArea(6, 20)]
    [SerializeField]
    private string _content =
        "你在上班路上收到一份快递。\n\n" +
        "寄件人一栏写着：\"童年的我\"。\n\n" +
        "你拆开纸箱，里面是一张面具。\n\n" +
        "你决定先把它带在身边……";

    [Header("行为")]
    [Tooltip("开场流程版本号（用于持久化 Key 版本化；改动流程后请 +1，避免旧存档直接跳过新流程）。")]
    [SerializeField] private int _prologueVersion = DefaultPrologueVersion;

    [Tooltip("为 true 时：若已完成开场（RuntimePrefs），将不再展示文案。")]
    [SerializeField] private bool _skipIfCompleted = true;

    [Tooltip("为 true 时：关闭开场弹窗后写入 RuntimePrefs 标记。")]
    [SerializeField] private bool _markCompleted = true;

    [Tooltip("当开场已完成且跳过时，是否自动授予面具（避免玩家因跳过而无法戴面具）。")]
    [SerializeField] private bool _autoAcquireMaskOnSkip = true;

    [Header("表现")]
    [Tooltip("阅读文案时遮罩透明度（0=不遮罩，1=纯黑）。")]
    [Range(0f, 1f)]
    [SerializeField] private float _overlayAlphaWhenReading = 1f;

    [Tooltip("播放礼物动画时遮罩透明度（建议 0：保证能看清动画）。")]
    [Range(0f, 1f)]
    [SerializeField] private float _overlayAlphaWhenPlayingGift = 0f;

    private ProloguePopupView _popupInstance;
    private bool _started;

    private string _completedKey;

    private GamePrologueStateModel _stateModel;
    private GiftAnimationPlayer _giftPlayer;
    private Coroutine _giftRoutine;

    private void Awake()
    {
        _completedKey = BuildCompletedKey();
        _stateModel = new GamePrologueStateModel();
        EnsureGiftPlayer();
        _giftPlayer?.HideImmediate();
        SetPrologueBackdropActive(false);

    }

    private void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        StartCoroutine(BootstrapRoutine());
    }

    private IEnumerator BootstrapRoutine()
    {
        EnsureReferences();

        // 若关键引用尚未就绪，则等一帧再兜底（避免场景初始化时序导致的空引用）。
        if (_rootCanvas == null || _maskController == null)
        {
            yield return null;
            EnsureReferences();
        }

        if (_maskController == null)
        {
            Debug.LogError("GamePrologueController: 场景中找不到 MaskWorldController，无法授予面具。", this);
            SetPrologueBackdropActive(false);
            yield break;
        }

        if (_maskController.HasMask)
        {
            MarkCompletedIfNeeded();
            _giftPlayer?.HideImmediate();
            SetPrologueBackdropActive(false);
            yield break;
        }

        if (_skipIfCompleted && RuntimePrefs.GetInt(_completedKey, 0) == 1)
        {
            if (_autoAcquireMaskOnSkip)
            {
                _maskController.AcquireMask();
            }

            _giftPlayer?.HideImmediate();
            SetPrologueBackdropActive(false);
            yield break;
        }

        // 开场正式开始：打开纯黑背景。
        SetPrologueBackdropActive(true);

        EnsurePopup();
        if (_popupInstance == null)
        {
            SetPrologueBackdropActive(false);
            yield break;
        }

        // 目标：黑底盖住物品栏，但文字在黑底之上。
        EnsureReadingLayering();

        if (!_stateModel.TryBegin())
        {
            SetPrologueBackdropActive(false);
            yield break;
        }

        _popupInstance.ShowText(_title, _content, icon: null, onClosed: null);
        _popupInstance.SetPanelVisible(true);
        _popupInstance.SetOverlayAlpha(_overlayAlphaWhenReading);
        _popupInstance.SetCloseInteractable(true);
        _popupInstance.SetCloseRequestedHandler(HandleInfoCloseRequested);
    }

    private void HandleInfoCloseRequested()
    {
        if (_popupInstance == null)
        {
            return;
        }

        if (!_stateModel.TryConfirmInfo())
        {
            return;
        }

        _popupInstance.SetCloseInteractable(false);
        _popupInstance.SetPanelVisible(false);
        _popupInstance.SetOverlayAlpha(_overlayAlphaWhenPlayingGift);

        // 礼物动画阶段不需要黑底；同时由弹窗遮罩（可为 0 透明）拦截场景交互。
        SetPrologueBackdropActive(false);

        EnsureGiftPlayer();
        _giftRoutine = StartCoroutine(PlayGiftThenWaitConfirmRoutine());
    }

    private IEnumerator PlayGiftThenWaitConfirmRoutine()
    {
        if (_giftPlayer != null)
        {
            yield return _giftPlayer.PlayAndHoldLastFrame();
        }

        _giftRoutine = null;

        if (!_stateModel.TryFinishGift())
        {
            yield break;
        }

        // 动画定格后：等待玩家再点一下（遮罩仍在，用来屏蔽场景交互）。
        if (_popupInstance != null)
        {
            _popupInstance.SetOverlayAlpha(_overlayAlphaWhenPlayingGift);
            _popupInstance.SetCloseRequestedHandler(HandleFinalConfirmRequested);
            _popupInstance.SetCloseInteractable(true);
        }
    }

    private void HandleFinalConfirmRequested()
    {
        if (_popupInstance == null)
        {
            return;
        }

        if (!_stateModel.TryConfirmAcquireMask())
        {
            return;
        }

        EnsureReferences();

        _popupInstance.SetCloseInteractable(false);
        _popupInstance.ClearCloseRequestedHandler();

        _maskController?.AcquireMask();
        MarkCompletedIfNeeded();
        _giftPlayer?.HideImmediate();

        SetPrologueBackdropActive(false);

        _popupInstance.Hide();

        if (_popupInstance != null)
        {
            Destroy(_popupInstance.gameObject);
            _popupInstance = null;
        }
    }

    private void EnsureReferences()
    {
        if (_maskController == null || !IsInLoadedScene(_maskController))
        {
            _maskController = Object.FindObjectOfType<MaskWorldController>();
        }

        if (_rootCanvas == null || !IsInLoadedScene(_rootCanvas))
        {
            _rootCanvas = Object.FindObjectOfType<Canvas>();
        }

        if (_giftAnimator != null && !IsInLoadedScene(_giftAnimator))
        {
            _giftAnimator = null;
        }
    }

    private void EnsureReadingLayering()
    {
        if (_rootCanvas == null)
        {
            return;
        }

        // 目标：背景盖住物品栏，但文字在背景之上。
        // 做法：把背景移动到画布顶层，再把文字弹窗移动到顶层（文字自然在背景之上）。
        if (_prologueBackdrop != null && _prologueBackdrop.transform.parent == _rootCanvas.transform)
        {
            _prologueBackdrop.transform.SetAsLastSibling();
        }

        if (_popupInstance != null && _popupInstance.transform.parent == _rootCanvas.transform)
        {
            _popupInstance.transform.SetAsLastSibling();
        }
    }

    private void SetPrologueBackdropActive(bool active)
    {
        if (_prologueBackdrop != null && !IsInLoadedScene(_prologueBackdrop))
        {
            _prologueBackdrop = null;
        }

        if (active)
        {
            EnsureReferences();

            if (_rootCanvas == null)
            {
                Debug.LogError("GamePrologueController: 场景中找不到 Canvas，无法创建 PrologueBackdrop。", this);
            }
            else
            {
                // 约束：黑底必须是 UI（RectTransform + Image），否则容易盖住弹窗文本。
                if (_prologueBackdrop != null
                    && (_prologueBackdrop.GetComponent<RectTransform>() == null
                        || _prologueBackdrop.GetComponent<Image>() == null))
                {
                    Debug.LogWarning(
                        "GamePrologueController: PrologueBackdrop 必须是 UI（RectTransform/Image），已忽略该引用并改为运行时创建。",
                        this
                    );
                    _prologueBackdrop = null;
                }

                if (_prologueBackdrop != null && _prologueBackdrop.transform.parent != _rootCanvas.transform)
                {
                    // 兜底：保证黑底与弹窗处于同一个 Canvas 下，避免层级错乱。
                    _prologueBackdrop.transform.SetParent(_rootCanvas.transform, worldPositionStays: false);
                }

                if (_prologueBackdrop == null)
                {
                    _prologueBackdrop = CreateRuntimeBackdrop(_rootCanvas.transform);
                }

                if (_prologueBackdrop != null && _prologueBackdrop.transform.parent == _rootCanvas.transform)
                {
                    // 先放到顶层（盖住物品栏），再由 EnsureReadingLayering() 把文字弹窗放到其上方。
                    _prologueBackdrop.transform.SetAsLastSibling();
                }
            }
        }

        if (_prologueBackdrop != null)
        {
            _prologueBackdrop.SetActive(active);
        }
    }

    private static bool IsInLoadedScene(Component component)
    {
        if (component == null)
        {
            return false;
        }

        var scene = component.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    private static bool IsInLoadedScene(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        var scene = gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    private static GameObject CreateRuntimeBackdrop(Transform canvasTransform)
    {
        // 兜底：若场景未显式配置开场黑底，则运行时创建一个全屏 UI Image。
        var go = new GameObject("PrologueBackdrop", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasTransform, worldPositionStays: false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        go.SetActive(false);
        return go;
    }

    private void EnsureGiftPlayer()
    {
        if (_giftAnimator == null)
        {
            // 兜底：从场景中按名称查找（包含 inactive）。
            var animators = Object.FindObjectsByType<Animator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var a in animators)
            {
                if (a != null && a.gameObject.name == DefaultGiftObjectName)
                {
                    _giftAnimator = a;
                    break;
                }
            }
        }

        _giftPlayer = _giftAnimator != null ? new GiftAnimationPlayer(_giftAnimator, stateName: "gift") : null;
    }

    private void EnsurePopup()
    {
        if (_popupInstance != null)
        {
            return;
        }

        if (_rootCanvas == null)
        {
            Debug.LogError("GamePrologueController: 场景中找不到 Canvas，无法展示开场文案。", this);
            return;
        }

        var prefab = _popupPrefab;
        if (prefab == null)
        {
            var prefabGo = Resources.Load<GameObject>(DefaultPopupPrefabResourcePath);
            prefab = prefabGo != null ? prefabGo.GetComponent<ProloguePopupView>() : null;
        }

        if (prefab == null)
        {
            Debug.LogError(
                "GamePrologueController: 找不到开场弹窗预制体。请在 Inspector 绑定 _popupPrefab，" +
                $"或确保 Resources/{DefaultPopupPrefabResourcePath}.prefab 存在。",
                this
            );
            return;
        }

        _popupInstance = Instantiate(prefab, _rootCanvas.transform);
    }

    private void MarkCompletedIfNeeded()
    {
        if (!_markCompleted)
        {
            return;
        }

        RuntimePrefs.SetInt(_completedKey, 1);
    }

    private string BuildCompletedKey()
    {
        var v = _prologueVersion <= 0 ? DefaultPrologueVersion : _prologueVersion;
        return $"{PrologueCompletedKeyPrefix}.v{v}";
    }
}
