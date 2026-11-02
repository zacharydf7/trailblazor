namespace Trailblazor;

/// <summary>App-wide tooltip settings, set in <c>AddTrailblazor(options => ...)</c>. A tooltip's own delays override
/// them.</summary>
public sealed class TooltipSettings
{
    // The browser's timers fire at once for anything longer, about 24.8 days.
    internal static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(int.MaxValue);

    /// <summary>How long the pointer rests on a trigger before its tooltip appears, up to 24 days. Keyboard focus shows
    /// it at once. Defaults to 300 milliseconds.</summary>
    public TimeSpan ShowDelay
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxDelay);
            field = value;
        }
    } = TimeSpan.FromMilliseconds(300);

    /// <summary>How long a tooltip stays after the pointer leaves its trigger, so the pointer can move onto it, up to
    /// 24 days. Defaults to 300 milliseconds.</summary>
    public TimeSpan HideDelay
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxDelay);
            field = value;
        }
    } = TimeSpan.FromMilliseconds(300);
}
