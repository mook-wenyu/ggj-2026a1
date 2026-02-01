using UnityEngine;

/// <summary>
/// 奖杯交互（通用版）：
/// - 点击：弹出物品图片（ItemsConfig.iconPath）；
/// - 关闭弹窗后：若满足条件则收集并可隐藏目标。
/// </summary>
public sealed class TrophyInspectAndCollectAction : InteractionAction, IInteractionEntryAction
{
    private const string DefaultPopupPrefabResourcePath = "Prefabs/ItemInfoPopup";

    [Header("物品")]
    [Tooltip("物品 ID（ItemsConfig.id）。")]
    [SerializeField] private string _itemId = "item_trophy";

    [Tooltip("满足该谜题后才允许收集（为空表示永不收集，只能查看）。")]
    [SerializeField] private string _collectRequiresPuzzleId = "level2_safe";

    [Range(0f, 1f)]
    [SerializeField] private float _overlayAlpha = 0.6f;

    [Tooltip("收集成功后是否隐藏奖杯物体。")]
    [SerializeField] private bool _hideTargetOnCollected = true;

    private ItemInfoPopupView _popupInstance;
    private Canvas _rootCanvas;
    private GameObject _pendingTarget;

    public override bool TryExecute(in InteractionContext context)
    {
        if (_pendingTarget != null)
        {
            return false;
        }

        EnsurePopup();
        if (_popupInstance == null)
        {
            Debug.LogError("TrophyInspectAndCollectAction: 找不到/创建 ItemInfoPopupView。", context.Target);
            return false;
        }

        var id = string.IsNullOrWhiteSpace(_itemId) ? null : _itemId.Trim();
        if (string.IsNullOrEmpty(id) || !InventoryItemCatalog.TryCreateItem(id, out var item) || item == null)
        {
            Debug.LogWarning($"TrophyInspectAndCollectAction: 找不到物品配置，已忽略（id={id}）。", context.Target);
            return false;
        }

        _pendingTarget = context.Target;

        _popupInstance.Show(item);
        _popupInstance.SetOverlayAlpha(_overlayAlpha);
        _popupInstance.SetCloseInteractable(true);
        _popupInstance.SetCloseRequestedHandler(HandleCloseRequested);

        // 该动作不直接让物体消失；是否收集由后续流程决定。
        return false;
    }

    private void OnDestroy()
    {
        if (_popupInstance != null)
        {
            Destroy(_popupInstance.gameObject);
            _popupInstance = null;
        }
    }

    private void HandleCloseRequested()
    {
        if (_popupInstance == null)
        {
            ResetState();
            return;
        }

        if (CanCollect())
        {
            var id = string.IsNullOrWhiteSpace(_itemId) ? null : _itemId.Trim();
            if (!string.IsNullOrEmpty(id))
            {
                InventoryService.TryCollect(id);
            }

            if (_hideTargetOnCollected && _pendingTarget != null)
            {
                _pendingTarget.SetActive(false);
            }
        }

        _popupInstance.ClearCloseRequestedHandler();
        _popupInstance.Hide();
        ResetState();
    }

    private bool CanCollect()
    {
        if (string.IsNullOrWhiteSpace(_collectRequiresPuzzleId))
        {
            return false;
        }

        return PuzzleProgress.IsSolved(_collectRequiresPuzzleId.Trim());
    }

    private void ResetState()
    {
        _pendingTarget = null;
    }

    private void EnsurePopup()
    {
        if (_popupInstance != null)
        {
            return;
        }

        if (_rootCanvas == null)
        {
            _rootCanvas = Object.FindObjectOfType<Canvas>();
        }

        if (_rootCanvas == null)
        {
            return;
        }

        var prefabGo = Resources.Load<GameObject>(DefaultPopupPrefabResourcePath);
        var prefab = prefabGo != null ? prefabGo.GetComponent<ItemInfoPopupView>() : null;
        if (prefab == null)
        {
            return;
        }

        _popupInstance = Instantiate(prefab, _rootCanvas.transform);
        _popupInstance.Hide();
    }
}
