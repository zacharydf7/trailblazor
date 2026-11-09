namespace Trailblazor;

/// <summary>Extra CSS classes for the parts inside a checkbox, switch, radio group, rating or slider.</summary>
public sealed record ChoiceClasses
{
    /// <summary>Added to the label (<c>data-part="label"</c>), which is the legend of a radio group or rating.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>Added to the input element (<c>data-part="input"</c>), which is each radio of a radio group or
    /// rating and each thumb of a range slider (<c>start-thumb</c> and <c>end-thumb</c>).</summary>
    public string? Input { get; init; }

    /// <summary>Added to the helper text (<c>data-part="helper"</c>).</summary>
    public string? Helper { get; init; }

    /// <summary>Added to the validation messages (<c>data-part="error"</c>).</summary>
    public string? Error { get; init; }
}
