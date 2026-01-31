using NUnit.Framework;

public sealed class PatternLockValidatorTests
{
    [Test]
    public void IsMatch_WhenNull_ReturnsFalse()
    {
        Assert.False(PatternLockValidator.IsMatch(null, null));
        Assert.False(PatternLockValidator.IsMatch(new[] { 1 }, null));
        Assert.False(PatternLockValidator.IsMatch(null, new[] { 1 }));
    }

    [Test]
    public void IsMatch_WhenDifferentLength_ReturnsFalse()
    {
        Assert.False(PatternLockValidator.IsMatch(new[] { 1, 2 }, new[] { 1 }));
    }

    [Test]
    public void IsMatch_WhenSameSequence_ReturnsTrue()
    {
        Assert.True(PatternLockValidator.IsMatch(new[] { 3, 0, 7, 2, 5 }, new[] { 3, 0, 7, 2, 5 }));
    }

    [Test]
    public void IsMatch_WhenDifferentSequence_ReturnsFalse()
    {
        Assert.False(PatternLockValidator.IsMatch(new[] { 3, 0, 7, 2, 5 }, new[] { 5, 2, 7, 0, 3 }));
    }
}
