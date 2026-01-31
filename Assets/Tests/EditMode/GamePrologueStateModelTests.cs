using NUnit.Framework;

public sealed class GamePrologueStateModelTests
{
    [Test]
    public void TryBegin_WhenNotStarted_EntersShowingInfo()
    {
        var model = new GamePrologueStateModel();

        Assert.IsTrue(model.TryBegin());
        Assert.AreEqual(GamePrologueStateModel.State.ShowingInfo, model.Current);
    }

    [Test]
    public void TryConfirmInfo_WhenNotShowingInfo_ReturnsFalse()
    {
        var model = new GamePrologueStateModel();

        Assert.IsFalse(model.TryConfirmInfo());
        Assert.AreEqual(GamePrologueStateModel.State.NotStarted, model.Current);
    }

    [Test]
    public void Flow_WhenAllStepsConfirmed_EndsCompleted()
    {
        var model = new GamePrologueStateModel();

        Assert.IsTrue(model.TryBegin());
        Assert.IsTrue(model.TryConfirmInfo());
        Assert.IsTrue(model.TryFinishGift());
        Assert.IsTrue(model.TryConfirmAcquireMask());

        Assert.IsTrue(model.IsCompleted);
        Assert.AreEqual(GamePrologueStateModel.State.Completed, model.Current);
    }
}
