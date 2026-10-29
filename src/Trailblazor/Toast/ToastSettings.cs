namespace Trailblazor;

/// <summary>App-wide toast settings, set in <c>AddTrailblazor(options => ...)</c>.</summary>
public sealed class ToastSettings
{
    /// <summary>Where toasts appear. Defaults to <see cref="ToastPosition.TopEnd"/>.</summary>
    public ToastPosition Position { get; set; } = ToastPosition.TopEnd;

    /// <summary>How many toasts show at once, at least 1. Later ones wait until one closes. Defaults to 3.</summary>
    public int MaxVisible
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            field = value;
        }
    } = 3;

    /// <summary>How long a toast stays when its options don't say. Errors stay until dismissed instead.
    /// Defaults to 5 seconds.</summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
}
