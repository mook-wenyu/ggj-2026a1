using UnityEngine;

/// <summary>
/// 交互动作：在所有条件满足后执行（例如：加入背包、结束关卡）。
/// </summary>
public abstract class InteractionAction : MonoBehaviour
{
    /// <summary>
    /// 执行动作。
    /// - 返回 true：表示“动作已成功触发/执行”。
    /// - 返回 false：表示失败（例如依赖缺失），交互不会把目标物体隐藏。
    /// </summary>
    public abstract bool TryExecute(in InteractionContext context);
}
