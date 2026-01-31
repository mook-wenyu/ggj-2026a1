using NUnit.Framework;

public sealed class MaskCrackProgressionTests
{
    [Test]
    public void GetStageIndex_WhenStageCountIsZero_ReturnsZero()
    {
        Assert.AreEqual(0, MaskCrackProgression.GetStageIndex(completedLevelCount: 10, stageCount: 0));
    }

    [Test]
    public void GetStageIndex_WhenCompletedCountIsNegative_ReturnsZero()
    {
        Assert.AreEqual(0, MaskCrackProgression.GetStageIndex(completedLevelCount: -1, stageCount: 3));
    }

    [Test]
    public void GetStageIndex_WhenNoLevelCompleted_ReturnsZero()
    {
        Assert.AreEqual(0, MaskCrackProgression.GetStageIndex(completedLevelCount: 0, stageCount: 3));
    }

    [Test]
    public void GetStageIndex_WhenCompletedCountWithinRange_ReturnsSameValue()
    {
        Assert.AreEqual(1, MaskCrackProgression.GetStageIndex(completedLevelCount: 1, stageCount: 5));
        Assert.AreEqual(3, MaskCrackProgression.GetStageIndex(completedLevelCount: 3, stageCount: 5));
    }

    [Test]
    public void GetStageIndex_WhenCompletedCountExceedsRange_ClampsToLast()
    {
        Assert.AreEqual(2, MaskCrackProgression.GetStageIndex(completedLevelCount: 99, stageCount: 3));
    }
}
