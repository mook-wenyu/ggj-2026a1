/// <summary>
/// 开场流程状态机（纯逻辑，可单测）。
///
/// 状态流转：
/// ShowingInfo -> PlayingGift -> WaitingConfirm -> Completed
/// </summary>
public sealed class GamePrologueStateModel
{
    public enum State
    {
        NotStarted = 0,
        ShowingInfo = 1,
        PlayingGift = 2,
        WaitingConfirm = 3,
        Completed = 4,
    }

    public State Current { get; private set; } = State.NotStarted;

    public bool IsCompleted => Current == State.Completed;

    public bool TryBegin()
    {
        if (Current != State.NotStarted)
        {
            return false;
        }

        Current = State.ShowingInfo;
        return true;
    }

    public bool TryConfirmInfo()
    {
        if (Current != State.ShowingInfo)
        {
            return false;
        }

        Current = State.PlayingGift;
        return true;
    }

    public bool TryFinishGift()
    {
        if (Current != State.PlayingGift)
        {
            return false;
        }

        Current = State.WaitingConfirm;
        return true;
    }

    public bool TryConfirmAcquireMask()
    {
        if (Current != State.WaitingConfirm)
        {
            return false;
        }

        Current = State.Completed;
        return true;
    }
}
