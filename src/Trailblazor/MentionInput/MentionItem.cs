namespace Trailblazor;

/// <summary>A person or thing a <see cref="TbMentionInput"/> can mention.</summary>
/// <param name="Text">What the mention shows after its trigger, such as a name.</param>
public sealed record MentionItem(string Text)
{
    /// <summary>The app's own id for it, such as a user id, which each <see cref="Mention"/> of it carries.</summary>
    public string? Id { get; init; }

    /// <summary>A second line under the text in the list of suggestions, such as a role.</summary>
    public string? Description { get; init; }
}
