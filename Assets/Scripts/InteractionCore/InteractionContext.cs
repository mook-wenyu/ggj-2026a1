using UnityEngine;

/// <summary>
/// 交互上下文：把“谁触发、触发的是什么对象”等信息打包，便于动作/条件复用。
/// </summary>
public readonly struct InteractionContext
{
    public InteractionContext(GameObject target, MonoBehaviour trigger)
    {
        Target = target;
        Trigger = trigger;
    }

    /// <summary>
    /// 被交互的目标对象（通常就是挂了 InteractiveItem 的 GameObject）。
    /// </summary>
    public GameObject Target { get; }

    /// <summary>
    /// 触发交互的脚本（通常是 InteractiveItem）。
    /// </summary>
    public MonoBehaviour Trigger { get; }
}
