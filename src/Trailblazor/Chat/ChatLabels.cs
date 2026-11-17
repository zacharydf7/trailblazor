namespace Trailblazor;

/// <summary>The words a chat shows and says to screen readers.</summary>
public sealed record ChatLabels
{
    /// <summary>Names the box the user types in. Leave it null for the app's text for
    /// <see cref="TbStrings.ChatMessage"/>.</summary>
    public string? Message { get; init; }

    /// <summary>The send button's text. Leave it null for the app's text for <see cref="TbStrings.ChatSend"/>.
    /// </summary>
    public string? Send { get; init; }

    /// <summary>The attach button's text. Leave it null for the app's text for <see cref="TbStrings.ChatAttach"/>.
    /// </summary>
    public string? Attach { get; init; }

    /// <summary>Says that one person is typing, with <c>{0}</c> for their name. Leave it null for the app's text for
    /// <see cref="TbStrings.ChatTyping"/>.</summary>
    public string? Typing { get; init; }

    /// <summary>Says that several people are typing, with <c>{0}</c> for all but the last name and <c>{1}</c> for the
    /// last. Leave it null for the app's text for <see cref="TbStrings.ChatTypingMany"/>.</summary>
    public string? TypingMany { get; init; }

    /// <summary>The button that goes back to the newest message after scrolling up. Leave it null for the app's text
    /// for <see cref="TbStrings.ChatNewest"/>.</summary>
    public string? Newest { get; init; }
}
