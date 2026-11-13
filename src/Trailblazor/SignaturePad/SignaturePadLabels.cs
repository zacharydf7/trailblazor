namespace Trailblazor;

/// <summary>The words a <see cref="TbSignaturePad"/> shows and reads out, each in place of the app's own text when set.
/// </summary>
/// <param name="Undo">The button that takes back the last stroke.</param>
/// <param name="Clear">The button that clears the signature.</param>
/// <param name="TypeName">The toggle that switches to typing a name instead of drawing.</param>
/// <param name="TypedName">The label of the field the name is typed in.</param>
/// <param name="Blank">What screen readers hear for the pad before anything is drawn; {0} is the pad's label.</param>
/// <param name="Drawn">What screen readers hear for the pad once something is drawn; {0} is the pad's label.</param>
public sealed record SignaturePadLabels(string? Undo = null, string? Clear = null, string? TypeName = null,
    string? TypedName = null, string? Blank = null, string? Drawn = null);
