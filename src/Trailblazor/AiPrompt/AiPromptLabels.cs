namespace Trailblazor;

/// <summary>The words an AI prompt shows and says to screen readers. Leave any of them null for the app's text for the
/// matching <see cref="TbStrings"/> key.</summary>
public sealed record AiPromptLabels
{
    /// <summary>The generate button's text, for <see cref="TbStrings.AiPromptGenerate"/>.</summary>
    public string? Generate { get; init; }

    /// <summary>The button that stops a response while it generates, for <see cref="TbStrings.AiPromptStop"/>.
    /// </summary>
    public string? Stop { get; init; }

    /// <summary>Names the group of suggestions, for <see cref="TbStrings.AiPromptSuggestions"/>.</summary>
    public string? Suggestions { get; init; }

    /// <summary>Names the list of responses, for <see cref="TbStrings.AiPromptResponses"/>.</summary>
    public string? Responses { get; init; }

    /// <summary>A response's copy button, for <see cref="TbStrings.AiPromptCopy"/>.</summary>
    public string? Copy { get; init; }

    /// <summary>A response's retry button, for <see cref="TbStrings.AiPromptRetry"/>.</summary>
    public string? Retry { get; init; }

    /// <summary>A response's insert button, for <see cref="TbStrings.AiPromptInsert"/>.</summary>
    public string? Insert { get; init; }

    /// <summary>Read out when a response starts, for <see cref="TbStrings.AiPromptGenerating"/>.</summary>
    public string? Generating { get; init; }

    /// <summary>Read out when a response is done, for <see cref="TbStrings.AiPromptDone"/>.</summary>
    public string? Done { get; init; }

    /// <summary>Read out when the user stops a response, for <see cref="TbStrings.AiPromptStopped"/>.</summary>
    public string? Stopped { get; init; }

    /// <summary>Shown on a failed response and read out, for <see cref="TbStrings.AiPromptFailed"/>.</summary>
    public string? Failed { get; init; }

    /// <summary>Read out once a response is copied, for <see cref="TbStrings.AiPromptCopied"/>.</summary>
    public string? Copied { get; init; }
}
