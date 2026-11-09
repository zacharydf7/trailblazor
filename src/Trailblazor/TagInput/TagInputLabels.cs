namespace Trailblazor;

/// <summary>The words a tag input shows and says to screen readers.</summary>
public sealed record TagInputLabels
{
    /// <summary>Tells screen readers, as the input's description, how to add a tag and reach the tags. Leave it null
    /// for the app's text for <see cref="TbStrings.TagInputHint"/>.</summary>
    public string? Hint { get; init; }

    /// <summary>Names a tag's remove button, with <c>{0}</c> for the tag. Leave it null for the app's text for
    /// <see cref="TbStrings.TagInputRemove"/>.</summary>
    public string? Remove { get; init; }

    /// <summary>Read out when a tag is added, with <c>{0}</c> for the tag. Leave it null for the app's text for
    /// <see cref="TbStrings.TagInputAdded"/>.</summary>
    public string? Added { get; init; }

    /// <summary>Read out when a tag is removed, with <c>{0}</c> for the tag. Leave it null for the app's text for
    /// <see cref="TbStrings.TagInputRemoved"/>.</summary>
    public string? Removed { get; init; }

    /// <summary>Shown when the typed tag is already there, with <c>{0}</c> for the tag. Leave it null for the app's
    /// text for <see cref="TbStrings.TagInputDuplicate"/>.</summary>
    public string? Duplicate { get; init; }

    /// <summary>Shown when a tag is typed once there are <c>MaxTags</c>, with <c>{0}</c> for that number. Leave it null
    /// for the app's text for <see cref="TbStrings.TagInputTooMany"/>.</summary>
    public string? TooMany { get; init; }
}
