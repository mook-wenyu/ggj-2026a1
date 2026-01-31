using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InteractiveItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.1f;
    public float hoverDuration = 0.05f;

    [Tooltip("中心小白点提示预制体，不填则自动加载 Prefabs/point")]
    public GameObject pointPrefab;

    [Tooltip("小白点缩放，设为 0 关闭提示")]
    public float clickHintSize = 0.15f;

    [Header("交互")]
    [Tooltip("所有前置条件满足后执行的动作（例如：加入背包/结束关卡）。")]
    [SerializeField] private InteractionAction _action;

    [Tooltip("动作为 true 时是否隐藏该物体。多数道具应勾选；若只是触发机关可关闭。")]
    [SerializeField] private bool _disableOnSuccess = true;

    [Header("兼容（推荐改用 InteractionAction）")]
    [Tooltip("旧版字段：不配置交互动作时，会用该 ID 走“加入背包 + 弹出详情”。")]
    [FormerlySerializedAs("itemId")]
    [SerializeField] private string _legacyItemId;

    private Vector3 _originalScale;
    private GameObject _clickHint;
    private PickupCondition[] _pickupConditions;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _pickupConditions = GetComponents<PickupCondition>();
        ResolveActionIfNeeded();
    }

    private void ResolveActionIfNeeded()
    {
        if (_action != null)
        {
            return;
        }

        var actions = GetComponents<InteractionAction>();
        if (actions == null || actions.Length == 0)
        {
            return;
        }

        if (actions.Length == 1)
        {
            _action = actions[0];
            return;
        }

        // 当同一物体上存在多个动作时，优先选择“入口动作”。
        InteractionAction entry = null;
        foreach (var a in actions)
        {
            if (a is not IInteractionEntryAction)
            {
                continue;
            }

            if (entry != null)
            {
                Debug.LogError(
                    $"InteractiveItem: {gameObject.name} 上存在多个入口动作（IInteractionEntryAction），请在 Inspector 显式绑定交互动作（_action）。",
                    this);
                return;
            }

            entry = a;
        }

        if (entry != null)
        {
            _action = entry;
            return;
        }

        Debug.LogError(
            $"InteractiveItem: {gameObject.name} 上存在多个交互动作，请在 Inspector 显式绑定交互动作（_action），" +
            "或为其中一个动作实现 IInteractionEntryAction 以消除歧义。",
            this);
    }

    private void Start()
    {
        EnsureEventSystemForUi();
        EnsureRaycastable();
        EnsurePointPrefab();
        CreateClickHint();
    }

    private void EnsureEventSystemForUi()
    {
        if (GetComponent<RectTransform>() == null)
        {
            return;
        }

        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var es = new GameObject("EventSystem").AddComponent<EventSystem>();
        es.gameObject.AddComponent<StandaloneInputModule>();
    }

    private void EnsureRaycastable()
    {
        var isUi = GetComponent<RectTransform>() != null;
        if (!isUi)
        {
            EnsureCollider();
            return;
        }

        // UI 元素需确保可接收射线检测
        var graphic = GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = true;
        }
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider2D>() != null || GetComponent<Collider>() != null)
        {
            return;
        }

        var collider = gameObject.AddComponent<BoxCollider2D>();
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            collider.size = spriteRenderer.sprite.bounds.size;
        }
    }

    private void EnsurePointPrefab()
    {
        if (pointPrefab != null)
        {
            return;
        }

#if UNITY_EDITOR
        pointPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/point.prefab");
#endif

        if (pointPrefab == null)
        {
            pointPrefab = Resources.Load<GameObject>("point");
        }
    }

    private void CreateClickHint()
    {
        if (clickHintSize <= 0 || pointPrefab == null)
        {
            return;
        }

        if (GetComponent<RectTransform>() != null)
        {
            CreateClickHintUi();
        }
        else
        {
            CreateClickHintWorld();
        }
    }

    private void CreateClickHintUi()
    {
        var sr = pointPrefab.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            return;
        }

        _clickHint = new GameObject("ClickHint");
        _clickHint.transform.SetParent(transform, false);

        var rect = _clickHint.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        var pixelSize = Mathf.Max(8, clickHintSize * 80f);
        rect.sizeDelta = new Vector2(pixelSize, pixelSize);

        var image = _clickHint.AddComponent<Image>();
        image.sprite = sr.sprite;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void CreateClickHintWorld()
    {
        _clickHint = Instantiate(pointPrefab, transform, false);
        _clickHint.name = "ClickHint";
        _clickHint.transform.localPosition = Vector3.zero;
        _clickHint.transform.localScale = Vector3.one * clickHintSize;
        _clickHint.transform.localRotation = Quaternion.identity;

        var sr = _clickHint.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            return;
        }

        var parentSr = GetComponent<SpriteRenderer>();
        sr.sortingLayerName = parentSr != null ? parentSr.sortingLayerName : "Default";
        sr.sortingOrder = (parentSr != null ? parentSr.sortingOrder : 0) + 10;
    }

    private void OnMouseEnter()
    {
        StartCoroutine(HoverEffect(true));
    }

    private void OnMouseExit()
    {
        StartCoroutine(HoverEffect(false));
    }

    private IEnumerator HoverEffect(bool isEntering)
    {
        var elapsedTime = 0f;
        var startScale = transform.localScale;
        var targetScale = isEntering ? _originalScale * hoverScale : _originalScale;

        while (elapsedTime < hoverDuration)
        {
            var t = hoverDuration <= 0f ? 1f : elapsedTime / hoverDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void OnMouseDown()
    {
        TryInteract();
    }

    // 支持 UI Image 的点击（需场景中有 EventSystem）
    public void OnPointerClick(PointerEventData eventData)
    {
        TryInteract();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GetComponent<RectTransform>() != null)
        {
            OnMouseEnter();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GetComponent<RectTransform>() != null)
        {
            OnMouseExit();
        }
    }

    private void TryInteract()
    {
        if (!TryCheckConditions(out var blockedReason, out var blocker))
        {
            if (blocker != null)
            {
                blocker.OnPickupBlocked(blockedReason);
            }

            Debug.Log($"InteractiveItem: {gameObject.name} 暂不可交互：{blockedReason}", this);
            return;
        }

        if (_action == null)
        {
            // 兼容：允许旧 prefab/scene 沿用 itemId。
            if (TryExecuteLegacyInventoryPickup())
            {
                if (_disableOnSuccess)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            Debug.LogError($"InteractiveItem: {gameObject.name} 未绑定交互动作（InteractionAction）。", this);
            return;
        }

        if (!_action.TryExecute(new InteractionContext(gameObject, this)))
        {
            return;
        }

        if (_disableOnSuccess)
        {
            gameObject.SetActive(false);
        }
    }

    private bool TryExecuteLegacyInventoryPickup()
    {
        if (string.IsNullOrWhiteSpace(_legacyItemId))
        {
            return false;
        }

        var id = _legacyItemId.Trim();
        var inventoryPanel = Object.FindObjectOfType<InventoryPanel>();
        if (inventoryPanel == null)
        {
            Debug.LogError($"InteractiveItem: 场景中找不到 InventoryPanel，无法打开物品详情弹窗（id={id}）。", this);
            return false;
        }

        if (!inventoryPanel.TryCollectAndOpenFromScene(id))
        {
            Debug.LogError($"InteractiveItem: 打开物品详情失败（id={id}）。", this);
            return false;
        }

        return true;
    }

    private bool TryCheckConditions(out string blockedReason, out PickupCondition blocker)
    {
        blockedReason = null;
        blocker = null;

        if (_pickupConditions == null || _pickupConditions.Length == 0)
        {
            return true;
        }

        foreach (var condition in _pickupConditions)
        {
            if (condition == null)
            {
                continue;
            }

            if (condition.CanPickup(out blockedReason))
            {
                continue;
            }

            blocker = condition;
            blockedReason = string.IsNullOrWhiteSpace(blockedReason) ? condition.GetType().Name : blockedReason;
            return false;
        }

        blockedReason = null;
        blocker = null;
        return true;
    }
}
