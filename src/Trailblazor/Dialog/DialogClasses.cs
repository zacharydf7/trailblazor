namespace Trailblazor;

/// <summary>Extra CSS classes for a dialog and the parts inside it, set through
/// <see cref="DialogOptions.Classes"/>.</summary>
public sealed record DialogClasses
{
    /// <summary>Added to the dialog itself (<c>data-part="dialog"</c>). It's the only way to add a class to a dialog
    /// opened from code.</summary>
    public string? Dialog { get; init; }

    /// <summary>Added to the header (<c>data-part="header"</c>).</summary>
    public string? Header { get; init; }

    /// <summary>Added to the title (<c>data-part="title"</c>).</summary>
    public string? Title { get; init; }

    /// <summary>Added to the minimize and maximize buttons (<c>data-part="minimize"</c> and
    /// <c>"maximize"</c>).</summary>
    public string? WindowButton { get; init; }

    /// <summary>Added to the close button (<c>data-part="close"</c>).</summary>
    public string? Close { get; init; }

    /// <summary>Added to the body (<c>data-part="body"</c>), which holds the dialog's content.</summary>
    public string? Body { get; init; }
}
