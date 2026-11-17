namespace Trailblazor;

/// <summary>Where a <see cref="TbAiPrompt"/>'s response is: shown on it as <c>data-status</c>, in lower case.</summary>
public enum AiPromptStatus
{
    /// <summary>Text is still arriving.</summary>
    Generating,

    /// <summary>All the text has arrived.</summary>
    Done,

    /// <summary>The user stopped it before it was done.</summary>
    Stopped,

    /// <summary><see cref="TbAiPrompt.Generate"/> threw before it was done.</summary>
    Failed,
}
