namespace Trailblazor;

/// <summary>A tick mark under a slider's track, with an optional label.</summary>
public sealed record SliderMark
{
    /// <summary>Where the mark sits, between the slider's minimum and maximum.</summary>
    public required decimal Value { get; init; }

    /// <summary>Text shown under the mark, such as "Steep".</summary>
    public string? Label { get; init; }
}
