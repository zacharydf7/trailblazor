namespace Trailblazor;

/// <summary>App-wide settings, passed to <c>AddTrailblazor(options => ...)</c>.</summary>
public sealed class TrailblazorOptions
{
    /// <summary>Where toasts appear, how many show at once, and how long they stay.</summary>
    public ToastSettings Toasts { get; } = new();

    /// <summary>What dialogs do when their own options leave it unset.</summary>
    public DialogSettings Dialogs { get; } = new();
}
