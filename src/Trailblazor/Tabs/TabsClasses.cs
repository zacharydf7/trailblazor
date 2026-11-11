namespace Trailblazor;

/// <summary>Extra CSS classes for the parts inside a <see cref="TbTabs"/>.</summary>
public sealed record TabsClasses
{
    /// <summary>Added to the row of tabs (<c>data-part="list"</c>).</summary>
    public string? List { get; init; }

    /// <summary>Added to every tab (<c>data-part="tab"</c>), along with a tab's own
    /// <see cref="TbTab.HeaderClass"/>.</summary>
    public string? Tab { get; init; }

    /// <summary>Added to every tab's icon (<c>data-part="icon"</c>).</summary>
    public string? Icon { get; init; }

    /// <summary>Added to the close mark of every tab with an <c>OnClose</c> (<c>data-part="close"</c>).</summary>
    public string? Close { get; init; }

    /// <summary>Added to every tab's panel (<c>data-part="panel"</c>), along with a tab's own <c>Class</c>.</summary>
    public string? Panel { get; init; }

    /// <summary>Added to the add button (<c>data-part="add"</c>).</summary>
    public string? Add { get; init; }

    /// <summary>Added to both scroll buttons (<c>data-part="scroll-start"</c> and <c>"scroll-end"</c>).</summary>
    public string? ScrollButton { get; init; }
}
