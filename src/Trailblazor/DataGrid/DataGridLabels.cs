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

    /// <summary>Read out when an export fails. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridExportFailed"/>.</summary>
    public string? ExportFailed { get; init; }

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

    /// <summary>The pager's count of rows shown, with <c>{0}</c> and <c>{1}</c> for the first and last row on the
    /// page and <c>{2}</c> for the total, such as "Trails {0} to {1} of {2}". Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridRange"/>.</summary>
    public string? Range { get; init; }

    /// <summary>The pager's count of rows shown when the <see cref="TbDataGrid{TItem}.DataProvider"/> doesn't count
    /// them and more follow, with <c>{0}</c> and <c>{1}</c> for the first and last row on the page, such as "Trails
    /// {0} to {1} of many". Leave it null for the app's text for <see cref="TbStrings.DataGridRangeOfMany"/>.
    /// </summary>
    public string? RangeOfMany { get; init; }

    /// <summary>The words of the row handles when rows can be moved: each handle's name, with <c>{0}</c> for the
    /// row's label, and its hint, its menu's items and what's read out after a move.</summary>
    public SortableLabels RowHandles { get; init; } = new();

    /// <summary>The names of the comparisons a filter menu offers.</summary>
    public ComparisonLabels Comparisons { get; init; } = new();

    /// <summary>Labels the list of columns a data grid's rows can be grouped by: "Group by" in English. Leave it null
    /// for the app's text for <see cref="TbStrings.DataGridGroupBy"/>.</summary>
    public string? GroupBy { get; init; }

    /// <summary>Names the menu above a data grid that shows and hides its columns: "Columns" in English. Leave it null
    /// for the app's text for <see cref="TbStrings.DataGridColumns"/>.</summary>
    public string? Columns { get; init; }

    /// <summary>Names the data grid's menu that downloads its rows as a file: "Export" in English. Leave it null for
    /// the app's text for <see cref="TbStrings.DataGridExport"/>.</summary>
    public string? Export { get; init; }

    /// <summary>The Export menu's item that downloads the rows as a CSV file: "CSV" in English. Leave it null for the
    /// app's text for <see cref="TbStrings.DataGridExportCsv"/>.</summary>
    public string? ExportCsv { get; init; }

    /// <summary>The Export menu's item that downloads the rows as an Excel workbook: "Excel" in English. Leave it null
    /// for the app's text for <see cref="TbStrings.DataGridExportExcel"/>.</summary>
    public string? ExportExcel { get; init; }

    /// <summary>The Export menu's item that downloads the rows as a PDF: "PDF" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridExportPdf"/>.</summary>
    public string? ExportPdf { get; init; }

    /// <summary>Names a data grid's pages; <c>{0}</c> is the grid's name: "Pages of {0}" in English. Leave it null for
    /// the app's text for <see cref="TbStrings.DataGridPages"/>.</summary>
    public string? Pages { get; init; }

    /// <summary>Names the column of a data grid whose rows can be moved, which holds each row's move handle: "Order" in
    /// English. Leave it null for the app's text for <see cref="TbStrings.DataGridOrder"/>.</summary>
    public string? Order { get; init; }

    /// <summary>Names the column of a data grid that holds each row's details button: "Details" in English. Leave it
    /// null for the app's text for <see cref="TbStrings.DataGridDetails"/>.</summary>
    public string? Details { get; init; }

    /// <summary>Names a row's button that opens and closes its details; <c>{0}</c> is the row's label: "Details for
    /// {0}" in English. Leave it null for the app's text for <see cref="TbStrings.DataGridDetailsFor"/>.</summary>
    public string? DetailsFor { get; init; }

    /// <summary>Names the column of a data grid that holds each row's Delete button: "Delete" in English. Leave it null
    /// for the app's text for <see cref="TbStrings.DataGridDelete"/>.</summary>
    public string? Delete { get; init; }

    /// <summary>Names a row's Delete button; <c>{0}</c> is the row's label: "Delete {0}" in English. Leave it null for
    /// the app's text for <see cref="TbStrings.DataGridDeleteRow"/>.</summary>
    public string? DeleteRow { get; init; }

    /// <summary>Said once a row is deleted; <c>{0}</c> is the row's label: "{0} deleted." in English. Leave it null for
    /// the app's text for <see cref="TbStrings.DataGridRowDeleted"/>.</summary>
    public string? RowDeleted { get; init; }

    /// <summary>Said once a row of a grid that edits in batches is marked deleted; <c>{0}</c> is the row's label: "{0}
    /// marked for deletion." in English. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridRowMarkedDeleted"/>.</summary>
    public string? RowMarkedDeleted { get; init; }

    /// <summary>Said once a row marked deleted is restored, with its Delete button pressed again; <c>{0}</c> is the
    /// row's label: "{0} restored." in English. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridRowRestored"/>.</summary>
    public string? RowRestored { get; init; }

    /// <summary>Tells screen readers a cell's row is marked deleted in a grid that edits in batches: "Marked for
    /// deletion" in English. Leave it null for the app's text for <see cref="TbStrings.DataGridMarkedDeleted"/>.
    /// </summary>
    public string? MarkedDeleted { get; init; }

    /// <summary>The text of a data grid's button that adds a row: "Add row" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridAddRow"/>.</summary>
    public string? AddRow { get; init; }

    /// <summary>Names the row the user is adding within its Save and Cancel buttons' names: "new row" in English, as in
    /// "Save new row". Leave it null for the app's text for <see cref="TbStrings.DataGridNewRow"/>.</summary>
    public string? NewRow { get; init; }

    /// <summary>Said once a row the user added is saved: "Row added." in English. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridRowAdded"/>.</summary>
    public string? RowAdded { get; init; }

    /// <summary>Names the column of a data grid's selection buttons: "Select" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridSelect"/>.</summary>
    public string? Select { get; init; }

    /// <summary>Names a row's selection box; <c>{0}</c> is the row's label: "Select {0}" in English. Leave it null for
    /// the app's text for <see cref="TbStrings.DataGridSelectRow"/>.</summary>
    public string? SelectRow { get; init; }

    /// <summary>Names a tree grid row's button that shows and hides its child rows; <c>{0}</c> is the row's label:
    /// "Rows under {0}" in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridRowsUnder"/>.</summary>
    public string? RowsUnder { get; init; }

    /// <summary>Tells screen readers a batch grid's cell holds a change not yet saved: "Changed" in English. Leave it
    /// null for the app's text for <see cref="TbStrings.DataGridChanged"/>.</summary>
    public string? Changed { get; init; }

    /// <summary>Says how many changes a batch grid holds; <c>{0}</c> is the count: "Unsaved changes: {0}" in English.
    /// Leave it null for the app's text for <see cref="TbStrings.DataGridUnsavedChanges"/>.</summary>
    public string? UnsavedChanges { get; init; }

    /// <summary>The button that saves a batch grid's changes: "Save changes" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridSaveChanges"/>.</summary>
    public string? SaveChanges { get; init; }

    /// <summary>The button that drops a batch grid's changes: "Discard changes" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridDiscardChanges"/>.</summary>
    public string? DiscardChanges { get; init; }

    /// <summary>Read out once a batch grid's changes are saved: "Changes saved" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridChangesSaved"/>.</summary>
    public string? ChangesSaved { get; init; }

    /// <summary>The button that saves a data grid row's edit: "Save" in English. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridSave"/>.</summary>
    public string? Save { get; init; }

    /// <summary>Names the button that saves a data grid row's edit; <c>{0}</c> is the row's label: "Save {0}" in
    /// English. Leave it null for the app's text for <see cref="TbStrings.DataGridSaveRow"/>.</summary>
    public string? SaveRow { get; init; }

    /// <summary>The button that cancels a data grid row's edit: "Cancel" in English. Leave it null for the app's text
    /// for <see cref="TbStrings.DataGridCancel"/>.</summary>
    public string? Cancel { get; init; }

    /// <summary>Names the button that cancels a data grid row's edit; <c>{0}</c> is the row's label: "Cancel editing
    /// {0}" in English. Leave it null for the app's text for <see cref="TbStrings.DataGridCancelRow"/>.</summary>
    public string? CancelRow { get; init; }

    /// <summary>Shown when an edited data grid cell's value doesn't fit its field, such as a number too large;
    /// <c>{0}</c> is the column: "{0} can't hold that value." in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridEditInvalid"/>.</summary>
    public string? EditInvalid { get; init; }

    /// <summary>Said once cells pasted into a grid that edits in batches are kept as changes; <c>{0}</c> is how many:
    /// "Cells pasted: {0}" in English. Leave it null for the app's text for <see cref="TbStrings.DataGridPasted"/>.
    /// </summary>
    public string? Pasted { get; init; }

    /// <summary>Said when a pasted cell can't hold its value, so nothing is pasted; <c>{0}</c> says why: "Nothing was
    /// pasted: {0}" in English. Leave it null for the app's text for <see cref="TbStrings.DataGridPasteRejected"/>.
    /// </summary>
    public string? PasteRejected { get; init; }

    /// <summary>Shown when a cell edit leaves empty a column that needs a value; <c>{0}</c> is the column:
    /// "{0} is required." in English. Leave it null for the app's text for <see cref="TbStrings.FieldRequired"/>.
    /// </summary>
    public string? EditRequired { get; init; }

    /// <summary>Read out when a column sorts its rows smallest first; <c>{0}</c> is the column: "Sorted by {0},
    /// ascending." in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridSortedAscending"/>.</summary>
    public string? SortedAscending { get; init; }

    /// <summary>Read out when a column sorts its rows largest first; <c>{0}</c> is the column: "Sorted by {0},
    /// descending." in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridSortedDescending"/>.</summary>
    public string? SortedDescending { get; init; }

    /// <summary>Read out when a column stops sorting the rows; <c>{0}</c> is the column: "No longer sorted by {0}." in
    /// English. Leave it null for the app's text for <see cref="TbStrings.DataGridUnsorted"/>.</summary>
    public string? Unsorted { get; init; }

    /// <summary>Tells which of several sorts a column is; <c>{0}</c> is its place, <c>{1}</c> the count and <c>{2}</c>
    /// its direction: "sort {0} of {1}, {2}" in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridSortPosition"/>.</summary>
    public string? SortPosition { get; init; }

    /// <summary>A sort from smallest to largest: "ascending" in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridAscending"/>.</summary>
    public string? Ascending { get; init; }

    /// <summary>A sort from largest to smallest: "descending" in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridDescending"/>.</summary>
    public string? Descending { get; init; }

    /// <summary>Names a data grid column's menu; <c>{0}</c> is the column: "Options for {0}" in English. Leave it null
    /// for the app's text for <see cref="TbStrings.DataGridColumnMenu"/>.</summary>
    public string? ColumnMenu { get; init; }

    /// <summary>A column menu item that moves the column one place toward the start of the row; <c>{0}</c> is the
    /// column: "Move {0} earlier" in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridEarlier"/>.</summary>
    public string? Earlier { get; init; }

    /// <summary>A column menu item that moves the column one place toward the end of the row; <c>{0}</c> is the column:
    /// "Move {0} later" in English. Leave it null for the app's text for <see
    /// cref="TbStrings.DataGridLater"/>.</summary>
    public string? Later { get; init; }

    /// <summary>A column menu item that widens the column; <c>{0}</c> is the column: "Make {0} wider" in English. Leave
    /// it null for the app's text for <see cref="TbStrings.DataGridWider"/>.</summary>
    public string? Wider { get; init; }

    /// <summary>A column menu item that narrows the column; <c>{0}</c> is the column: "Make {0} narrower" in English.
    /// Leave it null for the app's text for <see cref="TbStrings.DataGridNarrower"/>.</summary>
    public string? Narrower { get; init; }

    /// <summary>A column menu item that gives every column back the width it started with: "Reset column widths" in
    /// English. Leave it null for the app's text for <see cref="TbStrings.DataGridResetWidths"/>.</summary>
    public string? ResetWidths { get; init; }

    /// <summary>A column menu item that hides the column; <c>{0}</c> is the column: "Hide {0}" in English. Leave it
    /// null for the app's text for <see cref="TbStrings.DataGridHide"/>.</summary>
    public string? Hide { get; init; }

    /// <summary>Read out after a column is hidden from its own menu; <c>{0}</c> is the column: "{0} hidden" in English.
    /// Leave it null for the app's text for <see cref="TbStrings.DataGridColumnHidden"/>.</summary>
    public string? ColumnHidden { get; init; }

    /// <summary>Read out after a column moves from its menu; <c>{0}</c> is the column, <c>{1}</c> its new place,
    /// counting from 1, and <c>{2}</c> how many columns there are: "{0} moved to column {1} of {2}" in English. Leave
    /// it null for the app's text for <see cref="TbStrings.DataGridColumnMoved"/>.</summary>
    public string? ColumnMoved { get; init; }

    /// <summary>Names a data grid's filter for a column; <c>{0}</c> is the column: "Filter {0}" in English. Leave it
    /// null for the app's text for <see cref="TbStrings.DataGridFilter"/>.</summary>
    public string? Filter { get; init; }

    /// <summary>Names a data grid's filter menu button while its column is filtered; <c>{0}</c> is the column: "Filter
    /// {0}, on" in English. Leave it null for the app's text for <see cref="TbStrings.DataGridFilterOn"/>.</summary>
    public string? FilterOn { get; init; }

    /// <summary>Names the list of conditions in a data grid's filter menu: "Condition" in English. Leave it null for
    /// the app's text for <see cref="TbStrings.DataGridFilterCondition"/>.</summary>
    public string? FilterCondition { get; init; }

    /// <summary>Names the value field in a data grid's filter menu: "Value" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridFilterValue"/>.</summary>
    public string? FilterValue { get; init; }

    /// <summary>Names the choice of how a data grid's filter menu joins its two conditions: "Match" in English. Leave
    /// it null for the app's text for <see cref="TbStrings.DataGridFilterMatch"/>.</summary>
    public string? FilterMatch { get; init; }

    /// <summary>The choice that a row must meet both conditions: "And" in English. Leave it null for the app's text
    /// for <see cref="TbStrings.DataGridFilterAnd"/>.</summary>
    public string? FilterAnd { get; init; }

    /// <summary>The choice that a row may meet either condition: "Or" in English. Leave it null for the app's text for
    /// <see cref="TbStrings.DataGridFilterOr"/>.</summary>
    public string? FilterOr { get; init; }

    /// <summary>Names the second list of conditions in a data grid's filter menu: "Second condition" in English. Leave
    /// it null for the app's text for <see cref="TbStrings.DataGridFilterSecondCondition"/>.</summary>
    public string? FilterSecondCondition { get; init; }

    /// <summary>Names the second value field in a data grid's filter menu: "Second value" in English. Leave it null
    /// for the app's text for <see cref="TbStrings.DataGridFilterSecondValue"/>.</summary>
    public string? FilterSecondValue { get; init; }

    /// <summary>The button that adds a second condition to a data grid's filter menu: "Add a condition" in English.
    /// Leave it null for the app's text for <see cref="TbStrings.DataGridFilterAddCondition"/>.</summary>
    public string? FilterAddCondition { get; init; }

    /// <summary>The button that takes the second condition out of a data grid's filter menu: "Remove the second
    /// condition" in English. Leave it null for the app's text for <see cref="TbStrings.DataGridFilterRemoveCondition"/>.</summary>
    public string? FilterRemoveCondition { get; init; }

    /// <summary>Shown in a data grid's choice filter when it filters nothing: "Any" in English. Leave it null for the
    /// app's text for <see cref="TbStrings.DataGridFilterAny"/>.</summary>
    public string? FilterAny { get; init; }

    /// <summary>How a data grid writes true in a yes-or-no column, in its cells and its filter: "Yes" in English. Leave
    /// it null for the app's text for <see cref="TbStrings.DataGridFilterYes"/>.</summary>
    public string? FilterYes { get; init; }

    /// <summary>How a data grid writes false in a yes-or-no column, in its cells and its filter: "No" in English. Leave
    /// it null for the app's text for <see cref="TbStrings.DataGridFilterNo"/>.</summary>
    public string? FilterNo { get; init; }

    /// <summary>The button that clears a data grid column's filter: "Clear" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridFilterClear"/>.</summary>
    public string? FilterClear { get; init; }

    /// <summary>The button that applies a data grid's filter menu: "Apply" in English. Leave it null for the app's text
    /// for <see cref="TbStrings.DataGridFilterApply"/>.</summary>
    public string? FilterApply { get; init; }

    /// <summary>Read out when a data grid's filters change; <c>{0}</c> is how many rows meet them: "Rows shown: {0}."
    /// in English. Leave it null for the app's text for <see cref="TbStrings.DataGridRowsShown"/>.</summary>
    public string? RowsShown { get; init; }

    /// <summary>What's read out when the filters change, the <see cref="TbDataGrid{TItem}.DataProvider"/> doesn't
    /// count the rows that meet them and more follow the page, with <c>{0}</c> for those shown so far: "Rows shown: at
    /// least {0}." in English.
    /// Leave it null for the app's text for <see cref="TbStrings.DataGridRowsShownAtLeast"/>.</summary>
    public string? RowsShownAtLeast { get; init; }

    /// <summary>Names a data grid's totals row for screen readers: "Totals" in English. Leave it null for the app's
    /// text for <see cref="TbStrings.DataGridTotals"/>.</summary>
    public string? Totals { get; init; }

    /// <summary>Names a group's totals row for screen readers; <c>{0}</c> is the group's heading: "Totals for {0}" in
    /// English. Leave it null for the app's text for <see cref="TbStrings.DataGridGroupTotals"/>.</summary>
    public string? GroupTotals { get; init; }

    /// <summary>A group's heading; <c>{0}</c> is the column and <c>{1}</c> the group's value: "{0}: {1}" in English.
    /// Leave it null for the app's text for <see cref="TbStrings.DataGridGroupHeading"/>.</summary>
    public string? GroupHeading { get; init; }

    /// <summary>The value of a group whose rows have no value in its column: "None" in English. Leave it null for the
    /// app's text for <see cref="TbStrings.DataGridGroupNone"/>.</summary>
    public string? GroupNone { get; init; }

    /// <summary>How many rows a group holds; <c>{0}</c> is the count: "Rows: {0}" in English. Leave it null for the
    /// app's text for <see cref="TbStrings.DataGridGroupCount"/>.</summary>
    public string? GroupCount { get; init; }

    /// <summary>Names a column's total in a data grid's totals row when it adds up the values: "Sum" in English. Leave
    /// it null for the app's text for <see cref="TbStrings.DataGridSum"/>.</summary>
    public string? Sum { get; init; }

    /// <summary>Names a column's total in a data grid's totals row when it is the mean of the values: "Average" in
    /// English. Leave it null for the app's text for <see cref="TbStrings.DataGridAverage"/>.</summary>
    public string? Average { get; init; }

    /// <summary>Names a column's total in a data grid's totals row when it counts the rows with a value: "Count" in
    /// English. Leave it null for the app's text for <see cref="TbStrings.DataGridCount"/>.</summary>
    public string? Count { get; init; }

    /// <summary>Names a column's total in a data grid's totals row when it is the smallest value: "Min" in English.
    /// Leave it null for the app's text for <see cref="TbStrings.DataGridMin"/>.</summary>
    public string? Min { get; init; }

    /// <summary>Names a column's total in a data grid's totals row when it is the largest value: "Max" in English.
    /// Leave it null for the app's text for <see cref="TbStrings.DataGridMax"/>.</summary>
    public string? Max { get; init; }
}
