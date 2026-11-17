namespace Trailblazor;

/// <summary>A file sent with a <see cref="ChatMessage"/>, shown as a link that opens it.</summary>
/// <param name="Name">The file's name, shown as the link's text.</param>
/// <param name="Url">Where the file is, such as a download address.</param>
public sealed record ChatAttachment(string Name, string Url);
