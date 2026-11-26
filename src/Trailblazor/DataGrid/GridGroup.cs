namespace Trailblazor;

/// <summary>One group a <see cref="TbDataGrid{TItem}.DataProvider"/> found for a <see cref="GridRequest"/> that asked
/// for groups: the rows that share a value in the column it groups by.</summary>
/// <param name="Value">The rows' value in the column, as the column's field gives it, or null for rows with none. The
/// grid shows it with the column's format, and gives it back in <see cref="GridRequest.Group"/> to open the group.
/// </param>
/// <param name="Count">How many rows that meet the filters are in the group.</param>
public sealed record GridGroup(object? Value, int Count)
{
    /// <summary>The group's totals, by column key, for the request's <see cref="GridRequest.Aggregates"/>; a grid with
    /// <see cref="TbDataGrid{TItem}.GroupTotals"/> shows them under the group's rows.</summary>
    public IReadOnlyDictionary<string, object?> Totals { get; init; } = new Dictionary<string, object?>();
}
