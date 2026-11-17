namespace Trailblazor;

/// <summary>A mention in a <see cref="TbMentionInput"/>'s text: the trigger it was typed with and the item chosen.
/// </summary>
/// <param name="Trigger">The trigger, such as <c>"@"</c>.</param>
/// <param name="Item">The item chosen from the suggestions.</param>
public sealed record Mention(string Trigger, MentionItem Item);
