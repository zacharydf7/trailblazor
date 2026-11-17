namespace Trailblazor;

/// <summary>The words an in-place editor's buttons say to screen readers. Each one left null uses the app's text.
/// </summary>
public sealed record InPlaceEditorLabels
{
    /// <summary>The word that starts the name of the button that shows the value and starts editing it, before the
    /// label and the value. Leave it null for the app's text for <see cref="TbStrings.InPlaceEditorEdit"/>.</summary>
    public string? Edit { get; init; }

    /// <summary>The button that saves the edit. Leave it null for the app's text for
    /// <see cref="TbStrings.InPlaceEditorSave"/>.</summary>
    public string? Save { get; init; }

    /// <summary>The button that throws the edit away. Leave it null for the app's text for
    /// <see cref="TbStrings.InPlaceEditorCancel"/>.</summary>
    public string? Cancel { get; init; }
}
