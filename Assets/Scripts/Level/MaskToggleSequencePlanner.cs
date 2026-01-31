/// <summary>
/// 面具切换序列规划（纯逻辑，可单测）。
///
/// 需求约束：
/// - 从“未戴”到“戴上”：先播放戴面具动画并定格 -> 再切换世界 -> 面具快速渐隐消失。
/// - 从“戴上”到“未戴”：先显示已戴上的面具 -> 切换世界 -> 再播放摘面具动画 -> 面具消失。
/// </summary>
public static class MaskToggleSequencePlanner
{
    public enum Step
    {
        /// <summary>
        /// 播放“戴面具”并在最后一帧定格。
        /// </summary>
        PlayWearAndHold,

        /// <summary>
        /// 立即显示“已戴上面具”的画面（用于摘面具流程开头）。
        /// </summary>
        ShowWornPose,

        /// <summary>
        /// 切换双世界（MaskWorldController.SetMaskOn）。
        /// </summary>
        SwitchWorld,

        /// <summary>
        /// 快速渐隐并隐藏面具。
        /// </summary>
        FadeOutAndHide,

        /// <summary>
        /// 播放“摘面具”（反向播放戴面具动画）并隐藏。
        /// </summary>
        PlayRemovalAndHide,
    }

    public static Step[] Build(bool currentIsMaskOn)
    {
        // 戴上：先戴上并定格 -> 切世界 -> 渐隐
        if (!currentIsMaskOn)
        {
            return new[]
            {
                Step.PlayWearAndHold,
                Step.SwitchWorld,
                Step.FadeOutAndHide,
            };
        }

        // 摘下：先展示已戴上 -> 切世界 -> 再摘下并消失
        return new[]
        {
            Step.ShowWornPose,
            Step.SwitchWorld,
            Step.PlayRemovalAndHide,
        };
    }
}
