namespace Trailblazor.Spikes.Client.Grid;

public enum RowStatus
{
    Open,
    Active,
    Paused,
    Closed,
}

public sealed class GridRowData
{
    private static readonly string[] Owners = ["Avery", "Blake", "Casey", "Drew", "Emery", "Finley", "Gray"];

    public required int Index { get; init; }

    public required string Name { get; init; }

    public required string Owner { get; init; }

    public required int Quantity { get; init; }

    public required decimal Price { get; init; }

    public required double Score { get; init; }

    public required DateOnly Due { get; init; }

    public required DateTime Updated { get; init; }

    public required RowStatus Status { get; init; }

    // Bumped when a row's data changes; rows re-render only when it does.
    public int Version { get; set; }

    public static List<GridRowData> CreateRows(int count) => Enumerable.Range(0, count).Select(i => new GridRowData
    {
        Index = i,
        Name = $"Item {i}",
        Owner = Owners[i % Owners.Length],
        Quantity = i * 7 % 1000,
        Price = i * 13 % 10000 / 100m,
        Score = i * 31 % 1000 / 10.0,
        Due = new DateOnly(2026, 1, 1).AddDays(i % 365),
        Updated = new DateTime(2026, 1, 1, 8, 0, 0).AddMinutes(i * 17 % 100000),
        Status = (RowStatus)(i % 4),
    }).ToList();
}
