namespace Trailblazor;

/// <summary>The words a list box shows and says to screen readers.</summary>
public sealed record ListBoxLabels
{
    /// <summary>Shown and read out in the list when it has no options, such as "You haven't saved a trip yet". Leave it
    /// null for the app's text for <see cref="TbStrings.ListBoxEmpty"/>.</summary>
    public string? Empty { get; init; }
}
