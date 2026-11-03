namespace Trailblazor;

/// <summary>Where a <see cref="TbColumn{TItem}"/> stays in view while a <see cref="TbDataGrid{TItem}"/>'s rows scroll
/// sideways.</summary>
public enum ColumnPin
{
    /// <summary>Nowhere; the column scrolls with the rest.</summary>
    None,

    /// <summary>At the start of the row.</summary>
    Start,

    /// <summary>At the end of the row.</summary>
    End,
}
