namespace Trailblazor;

/// <summary>Extra CSS classes for the parts inside a <see cref="TbMenu"/>, its submenus and its items.</summary>
public sealed record MenuClasses
{
    /// <summary>Added to the button that opens the menu (<c>data-part="trigger"</c>), unless a <c>Trigger</c>
    /// template renders it.</summary>
    public string? Trigger { get; init; }

    /// <summary>Added to the menu's list and each submenu's list (<c>data-part="menu"</c>).</summary>
    public string? Menu { get; init; }

    /// <summary>Added to every item, including checkbox and radio items, along with the item's own <c>Class</c>. Also
    /// added to the item that opens each submenu, whose own <c>Class</c> goes on the submenu as a whole.</summary>
    public string? Item { get; init; }

    /// <summary>Added to every item's icon (<c>data-part="icon"</c>).</summary>
    public string? Icon { get; init; }

    /// <summary>Added to every item's text (<c>data-part="label"</c>).</summary>
    public string? Label { get; init; }

    /// <summary>Added to the arrow on every item that opens a submenu (<c>data-part="chevron"</c>).</summary>
    public string? Chevron { get; init; }

    /// <summary>Added to the check or dot of every checkbox and radio item (<c>data-part="indicator"</c>).</summary>
    public string? Indicator { get; init; }

    /// <summary>Added to every group's label (<c>data-part="group-label"</c>).</summary>
    public string? GroupLabel { get; init; }
}
