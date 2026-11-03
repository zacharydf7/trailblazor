namespace Trailblazor;

/// <summary>Where a <see cref="TbDataGrid{TItem}"/> puts its columns' filters.</summary>
public enum GridFilterMode
{
    /// <summary>A row of filters under the headers, each applied as it changes.</summary>
    Row,

    /// <summary>A filter button on each header, opening a menu that applies nothing until its Apply button is
    /// pressed: a condition and a value, a date range, or checkboxes of values. A column's filter template applies
    /// itself.</summary>
    Menu,
}
