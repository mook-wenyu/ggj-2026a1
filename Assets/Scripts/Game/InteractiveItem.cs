                                                                                                                                            using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractiveItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum ItemType
    {
        Bed,
        Table,
        Chair,
        Desk,
        Shelf,
        Cabinet,
        Other
    }
    
    public ItemType itemType = ItemType.Other;
    public float hoverScale = 1.1f;
    public float hoverDuration = 0.05f;
    public GameObject uiPrefab;
    [Tooltip("中心小白点提示预制体，不填则自动加载 Prefabs/point")]
    public GameObject pointPrefab;
    [Tooltip("小白点缩放，设为 0 关闭提示")]
    public float clickHintSize = 0.15f;
    
    private Vector3 originalScale;
    private bool isHovering = false;
    private GameObject currentUI;
    private float uiOpenTime;  // 用于防止打开UI的同一帧被误关闭
    private GameObject clickHint;  // 中心小白点提示
    
    private void Start()
    {
        originalScale = transform.localScale;
        
        // 若为 UI 元素且场景无 EventSystem，自动创建一个（否则 IPointerClick 不会触发）
        if (GetComponent<RectTransform>() != null && Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem").AddComponent<EventSystem>();
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
        
        // 确保物体有碰撞器（2D/3D 世界物体）或 Graphic（UI 元素），否则点击不会触发
        var isUI = GetComponent<RectTransform>() != null;
        if (!isUI)
        {
            if (GetComponent<Collider2D>() == null && GetComponent<Collider>() == null)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                var spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                    collider.size = spriteRenderer.sprite.bounds.size;
            }
        }
        else
        {
            // UI 元素需确保可接收射线检测
            var graphic = GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = true;
        }
        
        // 如果没有指定UI预制体，尝试从Prefabs文件夹加载
        if (uiPrefab == null)
        {
            string prefabPath = "Assets/Prefabs/UI.prefab";
            #if UNITY_EDITOR
            uiPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            #endif
            
            // 如果Prefabs文件夹中没有，尝试从Resources加载
            if (uiPrefab == null)
            {
                uiPrefab = Resources.Load<GameObject>("UI");
            }
            
            // 输出加载结果
            if (uiPrefab != null)
            {
                Debug.Log("Successfully loaded UI prefab: " + uiPrefab.name);
            }
            else
            {
                Debug.LogError("Failed to load UI prefab! Please assign it manually in the Inspector.");
            }
        }
        
        if (pointPrefab == null)
        {
            #if UNITY_EDITOR
            pointPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/point.prefab");
            #endif
            if (pointPrefab == null)
                pointPrefab = Resources.Load<GameObject>("point");
        }
        
        CreateClickHint();
    }
    
    private void CreateClickHint()
    {
        if (clickHintSize <= 0 || pointPrefab == null) return;
        
        if (GetComponent<RectTransform>() != null)
        {
            CreateClickHintUI();
        }
        else
        {
            CreateClickHintWorld();
        }
    }
    
    private void CreateClickHintUI()
    {
        var sr = pointPrefab.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        clickHint = new GameObject("ClickHint");
        clickHint.transform.SetParent(transform, false);
        var rect = clickHint.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        var pixelSize = Mathf.Max(8, clickHintSize * 80f);
        rect.sizeDelta = new Vector2(pixelSize, pixelSize);
        var image = clickHint.AddComponent<Image>();
        image.sprite = sr.sprite;
        image.color = Color.white;
        image.raycastTarget = false;
    }
    
    private void CreateClickHintWorld()
    {
        clickHint = Instantiate(pointPrefab, transform, false);
        clickHint.name = "ClickHint";
        clickHint.transform.localPosition = Vector3.zero;
        clickHint.transform.localScale = Vector3.one * clickHintSize;
        clickHint.transform.localRotation = Quaternion.identity;
        var sr = clickHint.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var parentSr = GetComponent<SpriteRenderer>();
            sr.sortingLayerName = parentSr != null ? parentSr.sortingLayerName : "Default";
            sr.sortingOrder = (parentSr != null ? parentSr.sortingOrder : 0) + 10;
        }
    }
    
    private void OnMouseEnter()
    {
        isHovering = true;
        StartCoroutine(HoverEffect(true));
    }
    
    private void OnMouseExit()
    {
        isHovering = false;
        StartCoroutine(HoverEffect(false));
    }
    
    private System.Collections.IEnumerator HoverEffect(bool isEntering)
    {
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = isEntering ? originalScale * hoverScale : originalScale;
        
        while (elapsedTime < hoverDuration)
        {
            float t = elapsedTime / hoverDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    private void OnMouseDown()
    {
        HandleClick();
    }
    
    // 支持 UI Image 的点击（需场景中有 EventSystem）
    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GetComponent<RectTransform>() != null) OnMouseEnter();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (GetComponent<RectTransform>() != null) OnMouseExit();
    }
    
    private void HandleClick()
    {
        Debug.Log($"Clicked on {itemType} item: {gameObject.name}");
        ShowUI();
    }
    
    private void ShowUI()
    {
        if (uiPrefab == null)
        {
            Debug.LogError("UI prefab is not assigned! Creating a basic UI panel as fallback.");
            CreateBasicUI();
            return;
        }
        
        // 确保只有一个UI实例
        if (currentUI != null)
        {
            Destroy(currentUI);
        }
        
        try
        {
            // 实例化UI
            currentUI = Instantiate(uiPrefab);
            currentUI.transform.SetParent(null);
            currentUI.transform.localScale = Vector3.one;
            
            // 重要：预制体可能保存为未激活状态，必须显式激活才能显示
            currentUI.SetActive(true);
            uiOpenTime = Time.time;  // 记录打开时间，防止同一帧被关闭
            
            // 确保UI在最上层
            Canvas canvas = currentUI.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 9999;
                // Screen Space - Camera 模式下如果未指定相机，改用主相机或 Overlay 模式
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                {
                    var mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        canvas.worldCamera = mainCam;
                    }
                    else
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                }
                Debug.Log($"Set canvas sorting order to 9999 for UI: {currentUI.name}");
            }
            
            // 检查UI的Canvas组件
            if (canvas == null)
            {
                Debug.LogWarning($"UI prefab {uiPrefab.name} has no Canvas component! Adding Canvas...");
                canvas = currentUI.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;
                currentUI.AddComponent<CanvasScaler>();
                currentUI.AddComponent<GraphicRaycaster>();
            }
            
            Debug.Log($"Successfully instantiated UI for {itemType} item: {currentUI.name}");
            Debug.Log($"UI position: {currentUI.transform.position}, scale: {currentUI.transform.localScale}");
            Debug.Log($"UI active: {currentUI.activeSelf}, active in hierarchy: {currentUI.activeInHierarchy}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to instantiate UI: {e.Message}");
            CreateBasicUI();
        }
    }
    
    private void CreateBasicUI()
    {
        // 创建一个基本的UI面板作为备用
        currentUI = new GameObject("BasicUI");
        
        // 添加Canvas
        Canvas canvas = currentUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // 添加CanvasScaler
        CanvasScaler scaler = currentUI.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 添加GraphicRaycaster
        currentUI.AddComponent<GraphicRaycaster>();
        
        // 创建背景面板
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(currentUI.transform);
        
        // 添加RectTransform
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 300);
        panelRect.anchoredPosition = Vector2.zero;
        
        // 添加Image组件作为背景
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // 创建标题文本
        GameObject titleText = new GameObject("TitleText");
        titleText.transform.SetParent(panel.transform);
        
        RectTransform titleRect = titleText.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(360, 50);
        titleRect.anchoredPosition = new Vector2(0, 100);
        
        UnityEngine.UI.Text title = titleText.AddComponent<UnityEngine.UI.Text>();
        title.text = itemType.ToString();
        title.fontSize = 24;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        
        // 创建正文文本
        GameObject contentText = new GameObject("ContentText");
        contentText.transform.SetParent(panel.transform);
        
        RectTransform contentRect = contentText.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(360, 120);
        contentRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Text content = contentText.AddComponent<UnityEngine.UI.Text>();
        content.text = "Click any key to close this panel.\n\nText content will be loaded from Excel later.";
        content.fontSize = 16;
        content.alignment = TextAnchor.MiddleCenter;
        content.color = Color.white;
        content.supportRichText = true;
        
        // 创建关闭按钮
        GameObject closeButton = new GameObject("CloseButton");
        closeButton.transform.SetParent(panel.transform);
        
        RectTransform closeRect = closeButton.AddComponent<RectTransform>();
        closeRect.sizeDelta = new Vector2(120, 40);
        closeRect.anchoredPosition = new Vector2(0, -100);
        
        UnityEngine.UI.Button button = closeButton.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Image buttonImage = closeButton.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        GameObject buttonText = new GameObject("ButtonText");
        buttonText.transform.SetParent(closeButton.transform);
        
        RectTransform buttonTextRect = buttonText.AddComponent<RectTransform>();
        buttonTextRect.sizeDelta = new Vector2(100, 30);
        buttonTextRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Text buttonTextComponent = buttonText.AddComponent<UnityEngine.UI.Text>();
        buttonTextComponent.text = "Close";
        buttonTextComponent.fontSize = 16;
        buttonTextComponent.alignment = TextAnchor.MiddleCenter;
        buttonTextComponent.color = Color.white;
        
        // 添加按钮点击事件
        button.onClick.AddListener(() => {
            if (currentUI != null)
            {
                Destroy(currentUI);
                currentUI = null;
            }
        });
        
        currentUI.SetActive(true);
        uiOpenTime = Time.time;
        Debug.Log($"Created basic UI for {itemType} item");
    }
    
    private void Update()
    {
        // 按 Escape 或任意键关闭UI
        // 重要：Input.anyKeyDown 包含鼠标点击！打开UI的点击会立刻触发关闭，所以需要冷却时间
        if (currentUI == null) return;
        if (Time.time - uiOpenTime < 0.15f) return;  // 打开后 0.15 秒内忽略输入，防止同一次点击立即关闭
        
        if (Input.GetKeyDown(KeyCode.Escape) || Input.anyKeyDown)
        {
            Destroy(currentUI);
            currentUI = null;
        }
    }
}