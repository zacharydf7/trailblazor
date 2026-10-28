namespace Trailblazor;

/// <summary>Extra CSS classes for the parts inside a form field.</summary>
public sealed record FieldClasses
{
    /// <summary>Added to the label (<c>data-part="label"</c>).</summary>
    public string? Label { get; init; }

    /// <summary>Added to the box around the input and its slots (<c>data-part="control"</c>).</summary>
    public string? Control { get; init; }

    /// <summary>Added to the input element (<c>data-part="input"</c>).</summary>
    public string? Input { get; init; }

    /// <summary>Added to the start slot (<c>data-part="start"</c>).</summary>
    public string? Start { get; init; }

    /// <summary>Added to the end slot (<c>data-part="end"</c>).</summary>
    public string? End { get; init; }

    /// <summary>Added to the helper text (<c>data-part="helper"</c>).</summary>
    public string? Helper { get; init; }

    /// <summary>Added to the validation messages (<c>data-part="error"</c>).</summary>
    public string? Error { get; init; }
}
