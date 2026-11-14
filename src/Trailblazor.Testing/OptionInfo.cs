namespace Trailblazor.Testing;

/// <summary>An option of a select or list box: its name, as a screen reader hears it, and its state.</summary>
/// <param name="Text">The option's name.</param>
/// <param name="Disabled">Whether the option is turned off, so it can't be chosen.</param>
/// <param name="Chosen">Whether the option is chosen.</param>
public sealed record OptionInfo(string Text, bool Disabled, bool Chosen);
