using NUnit.Framework;

public sealed class MaskToggleSequencePlannerTests
{
    [Test]
    public void Build_WhenCurrentlyOff_ReturnsWearThenSwitchThenFadeOut()
    {
        var steps = MaskToggleSequencePlanner.Build(currentIsMaskOn: false);
        CollectionAssert.AreEqual(
            new[]
            {
                MaskToggleSequencePlanner.Step.PlayWearAndHold,
                MaskToggleSequencePlanner.Step.SwitchWorld,
                MaskToggleSequencePlanner.Step.FadeOutAndHide,
            },
            steps);
    }

    [Test]
    public void Build_WhenCurrentlyOn_ReturnsShowThenSwitchThenRemoval()
    {
        var steps = MaskToggleSequencePlanner.Build(currentIsMaskOn: true);
        CollectionAssert.AreEqual(
            new[]
            {
                MaskToggleSequencePlanner.Step.ShowWornPose,
                MaskToggleSequencePlanner.Step.SwitchWorld,
                MaskToggleSequencePlanner.Step.PlayRemovalAndHide,
            },
            steps);
    }
}
