namespace Trailblazor;

/// <summary>App-wide timing for a busy state the library starts on its own, such as a button's spinner while its click
/// runs. <c>Loading</c> and a submitting form show at once. Set it in <c>AddTrailblazor(options => ...)</c>.</summary>
public sealed class BusySettings
{
    /// <summary>How long something waits before it shows as busy, so quick work doesn't flash a spinner. Defaults to
    /// 200 milliseconds.</summary>
    public TimeSpan ShowDelay
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            field = value;
        }
    } = TimeSpan.FromMilliseconds(200);

    /// <summary>How long a busy state stays once it shows, so it doesn't flicker off just after it appears. A button
    /// keeps ignoring clicks while it shows. Defaults to 400 milliseconds.</summary>
    public TimeSpan Minimum
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            field = value;
        }
    } = TimeSpan.FromMilliseconds(400);
}
