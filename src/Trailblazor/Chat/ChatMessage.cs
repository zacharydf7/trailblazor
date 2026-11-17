namespace Trailblazor;

/// <summary>One message in a <see cref="TbChat"/>.</summary>
/// <param name="Id">Tells messages apart, so a message keeps its place and is read out once.</param>
/// <param name="Author">Who wrote it.</param>
/// <param name="Text">What it says. While <see cref="IsStreaming"/> is true, it can grow with each update.</param>
/// <param name="Sent">When it was sent, shown with it in the chat's <see cref="TbChat.TimeZone"/>.</param>
public sealed record ChatMessage(string Id, ChatAuthor Author, string Text, DateTimeOffset Sent)
{
    /// <summary>Files sent with the message, listed under its text.</summary>
    public IReadOnlyList<ChatAttachment> Attachments { get; init; } = [];

    /// <summary>Whether the text is still arriving, such as a reply streamed from a model. Screen readers hear the
    /// message once, when this turns false.</summary>
    public bool IsStreaming { get; init; }
}
