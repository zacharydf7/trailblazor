namespace Trailblazor.Testing;

/// <summary>An option of a select, list box or tree select, and its state.</summary>
/// <param name="Text">The option's name, as a screen reader hears it, or a tree select item's text.</param>
/// <param name="Disabled">Whether the option is turned off, so it can't be chosen.</param>
/// <param name="Chosen">Whether the option is chosen.</param>
public sealed record OptionInfo(string Text, bool Disabled, bool Chosen);
