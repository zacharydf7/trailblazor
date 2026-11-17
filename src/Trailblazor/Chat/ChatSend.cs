using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor;

/// <summary>What the user sends from a <see cref="TbChat"/>'s composer.</summary>
/// <param name="Text">The text typed, with whitespace trimmed from both ends. It can be empty when files are sent.
/// </param>
/// <param name="Files">The files from the last choice made with the attach button, less any the user removed.
/// </param>
public sealed record ChatSend(string Text, IReadOnlyList<IBrowserFile> Files);
