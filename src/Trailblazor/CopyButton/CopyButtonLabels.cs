namespace Trailblazor;

/// <summary>The words a <see cref="TbCopyButton"/> shows and says. Each one left null takes the app's text for its
/// <see cref="TbStrings"/> key.</summary>
public sealed record CopyButtonLabels
{
    /// <summary>The button's words, and its name when it shows only its icon, such as "Copy the command". Leave it
    /// null for the app's text for <see cref="TbStrings.CopyButtonCopy"/>.</summary>
    public string? Copy { get; init; }

    /// <summary>Shown and announced for a moment after it copies. Leave it null for the app's text for
    /// <see cref="TbStrings.CopyButtonCopied"/>.</summary>
    public string? Copied { get; init; }

    /// <summary>Shown and announced for a moment when the browser refuses the copy, as on a page that isn't secure.
    /// Leave it null for the app's text for <see cref="TbStrings.CopyButtonFailed"/>.</summary>
    public string? Failed { get; init; }
}
