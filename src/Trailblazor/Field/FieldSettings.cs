namespace Trailblazor;

/// <summary>App-wide field settings, set in <c>AddTrailblazor(options => ...)</c>. A field's own parameters override
/// them.</summary>
public sealed class FieldSettings
{
    /// <summary>Whether fields show their validation status: a spinner and "Checking…" while a
    /// <c>ValidationAsync</c> check runs, a check mark once it passes, and an icon beside each error. Defaults to
    /// false.</summary>
    public bool ShowValidationStatus { get; set; }

    /// <summary>The icons for the validation status in place of the built-in ones.</summary>
    public FieldStatusIcons StatusIcons { get; set; } = new();
}
