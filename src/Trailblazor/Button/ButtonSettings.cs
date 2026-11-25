namespace Trailblazor;

/// <summary>App-wide button settings, set in <c>AddTrailblazor(options => ...)</c>. A button's own
/// <c>BusyWhileRunning</c> overrides <see cref="BusyWhileRunning"/>.</summary>
public sealed class ButtonSettings
{
    /// <summary>Whether a button counts as busy while the task its <c>OnClick</c> returned is still running: it
    /// ignores clicks, and after <see cref="BusySettings.ShowDelay"/> shows its spinner. Toggle buttons and links never
    /// do. Defaults to true.</summary>
    public bool BusyWhileRunning { get; set; } = true;
}
