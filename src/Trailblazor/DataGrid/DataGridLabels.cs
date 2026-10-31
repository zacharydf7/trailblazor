namespace Trailblazor;

/// <summary>The words a <see cref="TbDataGrid{TItem}"/> shows and says to screen readers.</summary>
public sealed record DataGridLabels
{
    /// <summary>Shown while rows load. Leave it null for the app's text for <see cref="TbStrings.DataGridLoading"/>.
    /// </summary>
    public string? Loading { get; init; }

    /// <summary>Shown, and read out, when rows couldn't load. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridLoadFailed"/>.</summary>
    public string? LoadFailed { get; init; }

    /// <summary>The button that loads the rows again after a failure. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridRetry"/>.</summary>
    public string? Retry { get; init; }

    /// <summary>Shown when there are no rows, unless <c>EmptyContent</c> says otherwise, and read out when a
    /// <c>DataProvider</c> finds none. Leave it null for the app's text for <see cref="TbStrings.DataGridEmpty"/>.
    /// </summary>
    public string? Empty { get; init; }

    /// <summary>Names the header checkbox that selects every row on the page. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridSelectAll"/>.</summary>
    public string? SelectAll { get; init; }

    /// <summary>Names the list of page sizes. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridPageSize"/>.</summary>
    public string? PageSize { get; init; }
}
