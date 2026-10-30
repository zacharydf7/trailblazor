namespace Trailblazor;

/// <summary>The words a <see cref="TbPagination"/> says to screen readers and shows in its compact mode.</summary>
public sealed record PaginationLabels
{
    /// <summary>Names the navigation landmark, unless the pagination sets its own <c>AriaLabel</c>. Defaults to
    /// "Pagination".</summary>
    public string Navigation { get; init; } = "Pagination";

    /// <summary>Names a page button, with <c>{0}</c> for its number. Defaults to "Page {0}".</summary>
    public string Page { get; init; } = "Page {0}";

    /// <summary>Names the page box in compact mode. Defaults to "Page".</summary>
    public string PageBox { get; init; } = "Page";

    /// <summary>The text after the page box in compact mode, with <c>{0}</c> for the page count. Defaults to
    /// "of {0}".</summary>
    public string Of { get; init; } = "of {0}";

    /// <summary>Names the button that goes to the previous page. Defaults to "Previous page".</summary>
    public string Previous { get; init; } = "Previous page";

    /// <summary>Names the button that goes to the next page. Defaults to "Next page".</summary>
    public string Next { get; init; } = "Next page";

    /// <summary>Names the button that goes to the first page. Defaults to "First page".</summary>
    public string First { get; init; } = "First page";

    /// <summary>Names the button that goes to the last page. Defaults to "Last page".</summary>
    public string Last { get; init; } = "Last page";
}
