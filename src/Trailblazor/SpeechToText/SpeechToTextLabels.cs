namespace Trailblazor;

/// <summary>The words a <see cref="TbSpeechToText"/> says to screen readers as it starts and stops listening. Each one
/// left null uses the app's text.</summary>
public sealed record SpeechToTextLabels
{
    /// <summary>Said as it starts listening. Leave it null for the app's text for
    /// <see cref="TbStrings.SpeechToTextListening"/>.</summary>
    public string? Listening { get; init; }

    /// <summary>Said as it stops listening. Leave it null for the app's text for
    /// <see cref="TbStrings.SpeechToTextStopped"/>.</summary>
    public string? Stopped { get; init; }
}
