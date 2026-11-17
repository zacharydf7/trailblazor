namespace Trailblazor;

/// <summary>What a <see cref="TbMentionInput"/> searches for: the trigger typed and the text typed after it.</summary>
/// <param name="Trigger">The trigger, such as <c>"@"</c>.</param>
/// <param name="Text">The text typed after the trigger so far, which can be empty.</param>
public sealed record MentionQuery(string Trigger, string Text);
