using NUnit.Framework;

public sealed class PuzzleProgressTests
{
    [SetUp]
    public void SetUp()
    {
        PuzzleProgress.ResetAll();
    }

    [TearDown]
    public void TearDown()
    {
        PuzzleProgress.ResetAll();
    }

    [Test]
    public void IsSolved_WhenIdIsWhitespace_ReturnsFalse()
    {
        Assert.IsFalse(PuzzleProgress.IsSolved("   "));
    }

    [Test]
    public void IsSolved_WhenMarkedSolved_ReturnsTrue()
    {
        PuzzleProgress.MarkSolved("p1");
        Assert.IsTrue(PuzzleProgress.IsSolved("p1"));
    }

    [Test]
    public void SetSolved_TrimsId_BeforeStoring()
    {
        PuzzleProgress.SetSolved("  p2  ", true);
        Assert.IsTrue(PuzzleProgress.IsSolved("p2"));
    }
}
