namespace Trailblazor;

/// <summary>The words a <see cref="TbPagination"/> says to screen readers and shows in its compact mode.</summary>
public sealed record PaginationLabels
{
    /// <summary>Names the navigation landmark, unless the pagination sets its own <c>AriaLabel</c>. Leave it null for
    /// the app's text for <see cref="TbStrings.PaginationNavigation"/>.</summary>
    public string? Navigation { get; init; }

    /// <summary>Names a page button, with <c>{0}</c> for its number. Leave it null for the app's text for <see
    /// cref="TbStrings.PaginationPage"/>.</summary>
    public string? Page { get; init; }

    /// <summary>Names the page box in compact mode. Leave it null for the app's text for <see
    /// cref="TbStrings.PaginationPageBox"/>.</summary>
    public string? PageBox { get; init; }

    /// <summary>The text after the page box in compact mode, with <c>{0}</c> for the page count. Leave it null for the
    /// app's text for <see cref="TbStrings.PaginationOf"/>.</summary>
    public string? Of { get; init; }

    /// <summary>Names the button that goes to the previous page. Leave it null for the app's text for <see
    /// cref="TbStrings.PaginationPrevious"/>.</summary>
    public string? Previous { get; init; }

    /// <summary>Names the button that goes to the next page. Leave it null for the app's text for <see
    /// cref="TbStrings.PaginationNext"/>.</summary>
    public string? Next { get; init; }

    /// <summary>Names the button that goes to the first page. Leave it null for the app's text for <see
    /// cref="TbStrings.PaginationFirst"/>.</summary>
    public string? First { get; init; }

    /// <summary>Names the button that goes to the last page. Leave it null for the app's text for <see
    /// cref="TbStrings.PaginationLast"/>.</summary>
    public string? Last { get; init; }
}
