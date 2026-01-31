using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractiveItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("物品ID（ItemsConfig.id）。\n必填：点击会收集到物品栏并弹出统一的物品详情弹窗；同时该物体在场景中消失。")]
    public string itemId;

    public float hoverScale = 1.1f;
    public float hoverDuration = 0.05f;

    [Tooltip("中心小白点提示预制体，不填则自动加载 Prefabs/point")]
    public GameObject pointPrefab;

    [Tooltip("小白点缩放，设为 0 关闭提示")]
    public float clickHintSize = 0.15f;

    private Vector3 _originalScale;
    private GameObject _clickHint;

    private void Start()
    {
        _originalScale = transform.localScale;

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
        TryCollectAndOpen();
    }

    // 支持 UI Image 的点击（需场景中有 EventSystem）
    public void OnPointerClick(PointerEventData eventData)
    {
        TryCollectAndOpen();
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

    private void TryCollectAndOpen()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogError($"InteractiveItem: {gameObject.name} 未配置 itemId，无法打开物品详情弹窗。", this);
            return;
        }

        var id = itemId.Trim();
        var inventoryPanel = Object.FindObjectOfType<InventoryPanel>();
        if (inventoryPanel == null)
        {
            Debug.LogError($"InteractiveItem: 场景中找不到 InventoryPanel，无法打开物品详情弹窗（id={id}）。", this);
            return;
        }

        if (!inventoryPanel.TryCollectAndOpenFromScene(id))
        {
            Debug.LogError($"InteractiveItem: 打开物品详情失败（id={id}）。", this);
            return;
        }

        gameObject.SetActive(false);
    }
}
