namespace Trailblazor;

/// <summary>App-wide dialog settings, set in <c>AddTrailblazor(options => ...)</c>. A dialog's own
/// <see cref="DialogOptions"/> override them.</summary>
public sealed class DialogSettings
{
    /// <summary>Whether Escape cancels a dialog whose options don't say. Defaults to true.</summary>
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Whether a click outside a modal dialog cancels it when its options don't say. Defaults to true.
    /// </summary>
    public bool DismissOnBackdrop { get; set; } = true;
}
