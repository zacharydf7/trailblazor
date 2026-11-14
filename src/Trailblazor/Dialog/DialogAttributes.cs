namespace Trailblazor;

/// <summary>Extra attributes, such as an <c>id</c> or a <c>data-testid</c>, for a dialog's header buttons, set through
/// <see cref="DialogOptions.Attributes"/>. CSS classes go through <see cref="DialogOptions.Classes"/> instead.</summary>
public sealed record DialogAttributes
{
    /// <summary>Added to the minimize button (<c>data-part="minimize"</c>).</summary>
    public IReadOnlyDictionary<string, object>? Minimize { get; init; }

    /// <summary>Added to the maximize button (<c>data-part="maximize"</c>).</summary>
    public IReadOnlyDictionary<string, object>? Maximize { get; init; }

    /// <summary>Added to the close button (<c>data-part="close"</c>).</summary>
    public IReadOnlyDictionary<string, object>? Close { get; init; }
}
