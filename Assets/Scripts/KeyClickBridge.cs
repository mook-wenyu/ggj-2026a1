using UnityEngine;

/// <summary>
/// 挂载在带 Collider 的 GameObject 上，点击时触发 OnClicked 事件。
/// 用于 FifthSceneImageSequence 的钥匙等可点击 3D/2D 对象。
/// </summary>
public class KeyClickBridge : MonoBehaviour
{
    public event System.Action OnClicked;

    private void OnMouseUpAsButton()
    {
        OnClicked?.Invoke();
    }
}
