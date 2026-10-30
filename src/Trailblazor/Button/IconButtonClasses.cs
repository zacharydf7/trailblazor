namespace Trailblazor;

/// <summary>Extra CSS classes for the parts inside a <see cref="TbIconButton"/>.</summary>
public sealed record IconButtonClasses
{
    /// <summary>Added to the icon (<c>data-part="icon"</c>).</summary>
    public string? Icon { get; init; }

    /// <summary>Added to the busy spinner (<c>data-part="spinner"</c>).</summary>
    public string? Spinner { get; init; }
}
