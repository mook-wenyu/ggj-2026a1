using UnityEngine;

/// <summary>
/// 双重世界关卡：摘下面具/戴上面具对应两套世界根节点。
/// 约定：
/// - 未戴面具：_worldWithoutMask 激活
/// - 戴上面具：_worldWithMask 激活
/// </summary>
public sealed class DualWorldLevel : MonoBehaviour
{
    [Header("世界根节点")]
    [SerializeField] private GameObject _worldWithoutMask;
    [SerializeField] private GameObject _worldWithMask;

    public bool IsMaskOn { get; private set; }

    private void Awake()
    {
        TryAutoWire();
        Apply();
    }

    public void SetMaskOn(bool isMaskOn)
    {
        if (IsMaskOn == isMaskOn)
        {
            return;
        }

        IsMaskOn = isMaskOn;
        Apply();
    }

    private void TryAutoWire()
    {
        if (_worldWithoutMask == null)
        {
            _worldWithoutMask = transform.Find("World_NoMask")?.gameObject;
        }

        if (_worldWithMask == null)
        {
            _worldWithMask = transform.Find("World_Mask")?.gameObject;
        }
    }

    private void Apply()
    {
        if (_worldWithoutMask != null)
        {
            _worldWithoutMask.SetActive(!IsMaskOn);
        }

        if (_worldWithMask != null)
        {
            _worldWithMask.SetActive(IsMaskOn);
        }
    }
}
