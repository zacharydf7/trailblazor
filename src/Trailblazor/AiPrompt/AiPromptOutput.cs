namespace Trailblazor;

/// <summary>One response in a <see cref="TbAiPrompt"/>: the prompt it answers and the text generated so far.</summary>
/// <param name="Prompt">The prompt, as the user sent it.</param>
/// <param name="Text">The text generated so far: all of it once <paramref name="Status"/> is
/// <see cref="AiPromptStatus.Done"/>, or what arrived before it stopped or failed.</param>
/// <param name="Status">Whether it's still generating, done, stopped by the user, or failed.</param>
public sealed record AiPromptOutput(string Prompt, string Text, AiPromptStatus Status)
{
    /// <summary>What went wrong when <see cref="Status"/> is <see cref="AiPromptStatus.Failed"/>, for an
    /// <see cref="TbAiPrompt.OutputTemplate"/> that shows more than the usual message.</summary>
    public Exception? Error { get; init; }
}
