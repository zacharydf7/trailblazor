namespace Trailblazor;

/// <summary>Extra CSS classes for the parts inside a <see cref="TbAlert"/>.</summary>
public sealed record AlertClasses
{
    /// <summary>Added to the icon slot (<c>data-part="icon"</c>).</summary>
    public string? Icon { get; init; }

    /// <summary>Added to the message (<c>data-part="message"</c>).</summary>
    public string? Message { get; init; }

    /// <summary>Added to the close button (<c>data-part="close"</c>).</summary>
    public string? Close { get; init; }
}
