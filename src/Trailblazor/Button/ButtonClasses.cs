namespace Trailblazor;

/// <summary>Extra CSS classes for the parts inside a <see cref="TbButton"/>.</summary>
public sealed record ButtonClasses
{
    /// <summary>Added to the label (<c>data-part="label"</c>).</summary>
    public string? Label { get; init; }

    /// <summary>Added to the start slot (<c>data-part="start"</c>).</summary>
    public string? Start { get; init; }

    /// <summary>Added to the end slot (<c>data-part="end"</c>).</summary>
    public string? End { get; init; }

    /// <summary>Added to the busy spinner (<c>data-part="spinner"</c>).</summary>
    public string? Spinner { get; init; }
}
