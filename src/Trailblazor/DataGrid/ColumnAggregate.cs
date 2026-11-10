namespace Trailblazor;

/// <summary>The total a <see cref="TbColumn{TItem}"/> shows in its grid's totals row.</summary>
public enum ColumnAggregate
{
    /// <summary>No total.</summary>
    None,

    /// <summary>The values added up.</summary>
    Sum,

    /// <summary>The mean of the values.</summary>
    Average,

    /// <summary>How many rows have a value in the column.</summary>
    Count,

    /// <summary>The smallest value.</summary>
    Min,

    /// <summary>The largest value.</summary>
    Max,
}
