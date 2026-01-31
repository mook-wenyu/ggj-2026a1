using NUnit.Framework;

public sealed class LevelIndexSequenceTests
{
    [Test]
    public void TryGetNextIndex_WhenAtMiddle_ReturnsNextIndex()
    {
        Assert.IsTrue(LevelIndexSequence.TryGetNextIndex(0, 3, out var next, out _));
        Assert.AreEqual(1, next);
    }

    [Test]
    public void TryGetNextIndex_WhenAtLast_ReturnsFalse()
    {
        Assert.IsFalse(LevelIndexSequence.TryGetNextIndex(2, 3, out _, out var error));
        Assert.IsNotEmpty(error);
    }
}
