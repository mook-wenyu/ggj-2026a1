using UnityEngine;

/// <summary>
/// 面具按钮生成器：当玩家获得面具后，在指定 UI 容器下生成常驻按钮。
/// </summary>
public sealed class MaskButtonSpawner : MonoBehaviour
{
    private const string DefaultPrefabResourcePath = "Prefabs/MaskToggleButton";

    [Header("引用（可不填，会自动查找/加载）")]
    [SerializeField] private MaskWorldController _controller;
    [SerializeField] private RectTransform _parent;
    [SerializeField] private MaskToggleButtonView _buttonPrefab;

    private MaskToggleButtonView _instance;

    private void Awake()
    {
        if (_parent == null)
        {
            _parent = transform as RectTransform;
        }

        if (_controller == null)
        {
            _controller = Object.FindObjectOfType<MaskWorldController>();
        }

        if (_buttonPrefab == null)
        {
            var prefabGo = Resources.Load<GameObject>(DefaultPrefabResourcePath);
            _buttonPrefab = prefabGo != null ? prefabGo.GetComponent<MaskToggleButtonView>() : null;
        }

        if (_controller != null)
        {
            _controller.MaskAcquired += HandleMaskAcquired;
        }

        TryEnsureButton();
    }

    private void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.MaskAcquired -= HandleMaskAcquired;
        }
    }

    private void HandleMaskAcquired()
    {
        TryEnsureButton();
    }

    private void TryEnsureButton()
    {
        if (_controller == null || !_controller.HasMask)
        {
            return;
        }

        if (_instance != null)
        {
            return;
        }

        if (_buttonPrefab == null || _parent == null)
        {
            Debug.LogError("MaskButtonSpawner: 缺少按钮预制体或父节点，无法生成面具按钮。", this);
            return;
        }

        _instance = Instantiate(_buttonPrefab, _parent);
        _instance.name = "MaskToggleButton";
        _instance.Bind(_controller);
    }
}
