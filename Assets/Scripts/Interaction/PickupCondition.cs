using UnityEngine;

/// <summary>
/// 拾取/交互前置条件。
/// 设计为 MonoBehaviour 组件，便于通过组合（Add Component）配置在单个道具上。
/// </summary>
public abstract class PickupCondition : MonoBehaviour
{
    /// <summary>
    /// 是否允许继续执行交互动作。
    /// </summary>
    public abstract bool CanPickup(out string blockedReason);

    /// <summary>
    /// 当条件未满足时可选回调：用于提示玩家或触发引导。
    /// 注意：这里不做“自动继续拾取”，避免引入异步状态机；需要自动流程可在后续扩展。
    /// </summary>
    public virtual void OnPickupBlocked(string blockedReason)
    {
    }
}
