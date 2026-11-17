namespace Trailblazor;

/// <summary>The words a mention input says to screen readers. Each one left null uses the app's text.</summary>
public sealed record MentionInputLabels
{
    /// <summary>Read out as suggestions come in, with <c>{0}</c> for how many. Leave it null for the app's text for
    /// <see cref="TbStrings.MentionInputSuggestions"/>.</summary>
    public string? Suggestions { get; init; }

    /// <summary>Read out when the search fails. Leave it null for the app's text for
    /// <see cref="TbStrings.MentionInputSearchFailed"/>.</summary>
    public string? SearchFailed { get; init; }
}
