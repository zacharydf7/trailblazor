namespace Trailblazor;

/// <summary>Which rows and columns a <see cref="TbDataGrid{TItem}"/> exports.</summary>
public enum GridExportRows
{
    /// <summary>The rows the filters leave, from every page, in the order they show, with the columns shown, in their
    /// order.</summary>
    Shown,

    /// <summary>Every row, whatever the filters, in the grid's sort, with every column, hidden ones too.</summary>
    All,
}
