namespace Trailblazor;

/// <summary>The rows a <see cref="TbDataGrid{TItem}.DataProvider"/> found for a <see cref="GridRequest"/>.</summary>
/// <param name="Items">The rows asked for, in order.</param>
/// <param name="TotalCount">How many rows there are in all, so the grid can page through them, or null when the
/// provider doesn't count them; then <see cref="HasMore"/> says whether a next page follows. A virtualized grid needs
/// the count, and throws when a page comes back without one.</param>
public sealed record GridResult<TItem>(IReadOnlyList<TItem> Items, int? TotalCount)
{
    /// <summary>With no <see cref="TotalCount"/>, whether rows follow these, so the grid offers a next page. On the
    /// last page, leave it false, and the grid counts the rows as those on the pages before plus these.</summary>
    public bool HasMore { get; init; }

    /// <summary>The totals the request's <see cref="GridRequest.Aggregates"/> asked for, by column key, over every row
    /// that meets the filters. A column the provider leaves out shows no total. Give them in every result, including
    /// each window of rows a virtualized grid loads.</summary>
    public IReadOnlyDictionary<string, object?> Totals { get; init; } = new Dictionary<string, object?>();
}
