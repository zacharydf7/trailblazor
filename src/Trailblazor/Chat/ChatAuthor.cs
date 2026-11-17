namespace Trailblazor;

/// <summary>Who wrote a <see cref="ChatMessage"/>.</summary>
/// <param name="Id">Tells authors apart, such as a user's id. <see cref="TbChat.CurrentAuthorId"/> is matched against
/// it.</param>
/// <param name="Name">The name shown with the author's messages, and read out with them.</param>
public sealed record ChatAuthor(string Id, string Name);
