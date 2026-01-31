/// <summary>
/// 面具状态的纯逻辑模型（可单测）。
/// </summary>
public sealed class MaskStateModel
{
    public bool HasMask { get; private set; }
    public bool IsMaskOn { get; private set; }

    public MaskStateModel(bool startHasMask, bool startMaskOn)
    {
        HasMask = startHasMask;
        IsMaskOn = startHasMask && startMaskOn;
    }

    public bool TryAcquireMask()
    {
        if (HasMask)
        {
            return false;
        }

        HasMask = true;
        // 默认拿到面具后不自动戴上，避免切世界造成困惑。
        IsMaskOn = false;
        return true;
    }

    public bool TrySetMaskOn(bool isMaskOn)
    {
        if (!HasMask)
        {
            return false;
        }

        if (IsMaskOn == isMaskOn)
        {
            return false;
        }

        IsMaskOn = isMaskOn;
        return true;
    }

    public bool TryToggleMask()
    {
        if (!HasMask)
        {
            return false;
        }

        IsMaskOn = !IsMaskOn;
        return true;
    }
}
