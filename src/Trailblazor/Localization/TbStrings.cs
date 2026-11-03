using System.Collections.Frozen;

namespace Trailblazor;

/// <summary>The keys of Trailblazor's built-in strings, for <see cref="ITbLocalizer"/> and
/// <see cref="TrailblazorOptions.Strings"/>. Each summary gives the English text, with what its placeholders hold.
/// </summary>
public static class TbStrings
{
    /// <summary>"Close": names an alert's close button.</summary>
    public const string AlertClose = "Alert.Close";

    /// <summary>"Clear": names an autocomplete's clear button.</summary>
    public const string AutocompleteClear = "Autocomplete.Clear";

    /// <summary>"Searching…": shown while an autocomplete waits for suggestions.</summary>
    public const string AutocompleteSearching = "Autocomplete.Searching";

    /// <summary>"No matches": shown when an autocomplete finds nothing.</summary>
    public const string AutocompleteNoResults = "Autocomplete.NoResults";

    /// <summary>"Couldn't load suggestions.": shown and read out when an autocomplete's search fails.</summary>
    public const string AutocompleteSearchFailed = "Autocomplete.SearchFailed";

    /// <summary>"Type to search": shown before the user has typed enough for suggestions.</summary>
    public const string AutocompleteTypeToSearch = "Autocomplete.TypeToSearch";

    /// <summary>"Suggestions: {0}": announced when suggestions arrive; {0} is how many.</summary>
    public const string AutocompleteResults = "Autocomplete.Results";

    /// <summary>"Remove": names the remove button of a chip with no text.</summary>
    public const string ChipRemove = "Chip.Remove";

    /// <summary>"Remove {0}": names a chip's remove button; {0} is the chip's text.</summary>
    public const string ChipRemoveNamed = "Chip.RemoveNamed";

    /// <summary>"Choose a color": names the button that opens a color picker's panel.</summary>
    public const string ColorPickerOpen = "ColorPicker.Open";

    /// <summary>"Pick a color from the screen": names the eyedropper button.</summary>
    public const string ColorPickerEyedropper = "ColorPicker.Eyedropper";

    /// <summary>"Clear": names a color picker's clear button.</summary>
    public const string ColorPickerClear = "ColorPicker.Clear";

    /// <summary>"Swatches": names the list of preset colors.</summary>
    public const string ColorPickerSwatches = "ColorPicker.Swatches";

    /// <summary>"Hue": names the hue slider.</summary>
    public const string ColorPickerHue = "ColorPicker.Hue";

    /// <summary>"Saturation": names the saturation axis of the color area.</summary>
    public const string ColorPickerSaturation = "ColorPicker.Saturation";

    /// <summary>"Brightness": names the brightness axis of the color area.</summary>
    public const string ColorPickerBrightness = "ColorPicker.Brightness";

    /// <summary>"Opacity": names the alpha slider.</summary>
    public const string ColorPickerAlpha = "ColorPicker.Alpha";

    /// <summary>"Red": names the red channel field.</summary>
    public const string ColorPickerRed = "ColorPicker.Red";

    /// <summary>"Green": names the green channel field.</summary>
    public const string ColorPickerGreen = "ColorPicker.Green";

    /// <summary>"Blue": names the blue channel field.</summary>
    public const string ColorPickerBlue = "ColorPicker.Blue";

    /// <summary>"Enter a color as #RRGGBB.": shown when the typed color can't be read.</summary>
    public const string ColorPickerBadColor = "ColorPicker.BadColor";

    /// <summary>"Start date": names a date range's start field, and is added to the start day's name in the calendar.
    /// </summary>
    public const string DateRangePickerStart = "DateRangePicker.Start";

    /// <summary>"End date": names a date range's end field, and is added to the end day's name in the calendar.
    /// </summary>
    public const string DateRangePickerEnd = "DateRangePicker.End";

    /// <summary>"Choose dates": names the button that opens the calendar.</summary>
    public const string DateRangePickerOpen = "DateRangePicker.Open";

    /// <summary>"Previous month": names the calendar's previous-month button.</summary>
    public const string DateRangePickerPreviousMonth = "DateRangePicker.PreviousMonth";

    /// <summary>"Next month": names the calendar's next-month button.</summary>
    public const string DateRangePickerNextMonth = "DateRangePicker.NextMonth";

    /// <summary>"Presets": names the list of preset ranges.</summary>
    public const string DateRangePickerPresets = "DateRangePicker.Presets";

    /// <summary>"Clear": names a date range's clear button.</summary>
    public const string DateRangePickerClear = "DateRangePicker.Clear";

    /// <summary>"Now choose the end date.": announced once the start date is chosen.</summary>
    public const string DateRangePickerChooseEnd = "DateRangePicker.ChooseEnd";

    /// <summary>"{0} to {1}": announced once the end date is chosen; {0} and {1} are the two dates.</summary>
    public const string DateRangePickerChosen = "DateRangePicker.Chosen";

    /// <summary>"Enter the start date as {0}.": shown when the start can't be read; {0} is the date format.</summary>
    public const string DateRangePickerBadStart = "DateRangePicker.BadStart";

    /// <summary>"Enter the end date as {0}.": shown when the end can't be read; {0} is the date format.</summary>
    public const string DateRangePickerBadEnd = "DateRangePicker.BadEnd";

    /// <summary>"{0} can't be chosen.": shown for a date the rules leave out; {0} is the date.</summary>
    public const string DateRangePickerUnavailable = "DateRangePicker.Unavailable";

    /// <summary>"The end date is before the start date.": shown for a range that runs backwards.</summary>
    public const string DateRangePickerEndBeforeStart = "DateRangePicker.EndBeforeStart";

    /// <summary>"Close": names a dialog's close button.</summary>
    public const string DialogClose = "Dialog.Close";

    /// <summary>"Minimize": names a dialog's minimize button.</summary>
    public const string DialogMinimize = "Dialog.Minimize";

    /// <summary>"Maximize": names a dialog's maximize button.</summary>
    public const string DialogMaximize = "Dialog.Maximize";

    /// <summary>"Restore": names the minimize or maximize button of a dialog that is minimized or maximized.</summary>
    public const string DialogRestore = "Dialog.Restore";

    /// <summary>"Move": names a dialog's Move button and menu.</summary>
    public const string DialogMove = "Dialog.Move";

    /// <summary>"Arrow keys move the dialog.": describes the Move button.</summary>
    public const string DialogMoveHint = "Dialog.MoveHint";

    /// <summary>"Resize": names a dialog's Resize button and menu.</summary>
    public const string DialogResize = "Dialog.Resize";

    /// <summary>"Arrow keys resize the dialog.": describes the Resize button.</summary>
    public const string DialogResizeHint = "Dialog.ResizeHint";

    /// <summary>"Center": the Move menu's item for <see cref="DialogPosition.Center"/>.</summary>
    public const string DialogPositionCenter = "Dialog.Position.Center";

    /// <summary>"Top": the Move menu's item for <see cref="DialogPosition.Top"/>.</summary>
    public const string DialogPositionTop = "Dialog.Position.Top";

    /// <summary>"Bottom": the Move menu's item for <see cref="DialogPosition.Bottom"/>.</summary>
    public const string DialogPositionBottom = "Dialog.Position.Bottom";

    /// <summary>"Start": the Move menu's item for <see cref="DialogPosition.Start"/>.</summary>
    public const string DialogPositionStart = "Dialog.Position.Start";

    /// <summary>"End": the Move menu's item for <see cref="DialogPosition.End"/>.</summary>
    public const string DialogPositionEnd = "Dialog.Position.End";

    /// <summary>"Top start": the Move menu's item for <see cref="DialogPosition.TopStart"/>.</summary>
    public const string DialogPositionTopStart = "Dialog.Position.TopStart";

    /// <summary>"Top end": the Move menu's item for <see cref="DialogPosition.TopEnd"/>.</summary>
    public const string DialogPositionTopEnd = "Dialog.Position.TopEnd";

    /// <summary>"Bottom start": the Move menu's item for <see cref="DialogPosition.BottomStart"/>.</summary>
    public const string DialogPositionBottomStart = "Dialog.Position.BottomStart";

    /// <summary>"Bottom end": the Move menu's item for <see cref="DialogPosition.BottomEnd"/>.</summary>
    public const string DialogPositionBottomEnd = "Dialog.Position.BottomEnd";

    /// <summary>"Small": the Resize menu's item for <see cref="DialogSize.Small"/>.</summary>
    public const string DialogSizeSmall = "Dialog.Size.Small";

    /// <summary>"Medium": the Resize menu's item for <see cref="DialogSize.Medium"/>.</summary>
    public const string DialogSizeMedium = "Dialog.Size.Medium";

    /// <summary>"Large": the Resize menu's item for <see cref="DialogSize.Large"/>.</summary>
    public const string DialogSizeLarge = "Dialog.Size.Large";

    /// <summary>"Extra large": the Resize menu's item for <see cref="DialogSize.ExtraLarge"/>.</summary>
    public const string DialogSizeExtraLarge = "Dialog.Size.ExtraLarge";

    /// <summary>"OK": the confirm button of <see cref="IDialogService.ConfirmAsync"/>.</summary>
    public const string DialogConfirm = "Dialog.Confirm";

    /// <summary>"Cancel": the cancel button of <see cref="IDialogService.ConfirmAsync"/>.</summary>
    public const string DialogCancel = "Dialog.Cancel";

    /// <summary>"{0} isn't valid.": shown when a field's text can't be read as its type; {0} is the field's label.
    /// </summary>
    public const string FieldInvalid = "Field.Invalid";

    /// <summary>"{0} is required.": shown when a <c>Required</c> field is empty; {0} is the field's label.</summary>
    public const string FieldRequired = "Field.Required";

    /// <summary>"The field": stands in for the label in a field's messages when it has none.</summary>
    public const string FieldUnnamed = "Field.Unnamed";

    /// <summary>"Drop files here, or click to choose them.": the file upload's drop zone.</summary>
    public const string FileUploadZone = "FileUpload.Zone";

    /// <summary>"Choose files": names the file upload's choose button.</summary>
    public const string FileUploadChoose = "FileUpload.Choose";

    /// <summary>"Remove {0}": names a chosen file's remove button; {0} is the file's name.</summary>
    public const string FileUploadRemove = "FileUpload.Remove";

    /// <summary>"Clear": names the button that removes every chosen file.</summary>
    public const string FileUploadClear = "FileUpload.Clear";

    /// <summary>"{0} files chosen.": announced after choosing; {0} is how many.</summary>
    public const string FileUploadChosen = "FileUpload.Chosen";

    /// <summary>"1 file chosen.": announced after choosing one file.</summary>
    public const string FileUploadChosenOne = "FileUpload.ChosenOne";

    /// <summary>"{0} isn't one of {1}.": shown for a file of the wrong type; {0} is its name, {1} the allowed types.
    /// </summary>
    public const string FileUploadWrongType = "FileUpload.WrongType";

    /// <summary>"{0} is larger than {1}.": shown for a file over the size limit; {0} is its name, {1} the limit.
    /// </summary>
    public const string FileUploadTooLarge = "FileUpload.TooLarge";

    /// <summary>"{0} B": a file's size in bytes; {0} is the number.</summary>
    public const string FileUploadBytes = "FileUpload.Bytes";

    /// <summary>"{0} KB": a file's size in kilobytes; {0} is the number.</summary>
    public const string FileUploadKilobytes = "FileUpload.Kilobytes";

    /// <summary>"{0} MB": a file's size in megabytes; {0} is the number.</summary>
    public const string FileUploadMegabytes = "FileUpload.Megabytes";

    /// <summary>"{0} GB": a file's size in gigabytes; {0} is the number.</summary>
    public const string FileUploadGigabytes = "FileUpload.Gigabytes";

    /// <summary>"Choose at most {0} files.": shown when too many files are chosen; {0} is the limit.</summary>
    public const string FileUploadTooMany = "FileUpload.TooMany";

    /// <summary>"Choose only one file.": shown when several files are chosen for a single-file upload.</summary>
    public const string FileUploadTooManyOne = "FileUpload.TooManyOne";

    /// <summary>"Information about {0}": names a label's info button; {0} is the label's text.</summary>
    public const string LabelInfo = "Label.Info";

    /// <summary>"opens in a new tab": read after the text of a link or menu item that opens a new tab.</summary>
    public const string NewTabHint = "NewTabHint";

    /// <summary>"Decrease": names a number field's step-down button.</summary>
    public const string NumberFieldDecrease = "NumberField.Decrease";

    /// <summary>"Increase": names a number field's step-up button.</summary>
    public const string NumberFieldIncrease = "NumberField.Increase";

    /// <summary>"{0} must be a number.": shown for text that isn't a number; {0} is the field's label.</summary>
    public const string NumberFieldNotANumber = "NumberField.NotANumber";

    /// <summary>"{0} must be from {1} to {2}.": shown for a number out of range; {1} and {2} are the limits.</summary>
    public const string NumberFieldRange = "NumberField.Range";

    /// <summary>"{0} must be at least {1}.": shown for a number below the minimum, {1}.</summary>
    public const string NumberFieldAtLeast = "NumberField.AtLeast";

    /// <summary>"{0} must be at most {1}.": shown for a number above the maximum, {1}.</summary>
    public const string NumberFieldAtMost = "NumberField.AtMost";

    /// <summary>"{0} removed": announced when a multi-select's chip is removed; {0} is its option.</summary>
    public const string MultiSelectRemoved = "MultiSelect.Removed";

    /// <summary>"Select all": names the option that chooses every option of a multi-select.</summary>
    public const string MultiSelectSelectAll = "MultiSelect.SelectAll";

    /// <summary>"Pagination": names the pagination's navigation landmark.</summary>
    public const string PaginationNavigation = "Pagination.Navigation";

    /// <summary>"Page {0}": names a page button; {0} is its number.</summary>
    public const string PaginationPage = "Pagination.Page";

    /// <summary>"Page": names the page box in compact mode.</summary>
    public const string PaginationPageBox = "Pagination.PageBox";

    /// <summary>"of {0}": shown after the page box in compact mode; {0} is the page count.</summary>
    public const string PaginationOf = "Pagination.Of";

    /// <summary>"Previous page": names the previous-page button.</summary>
    public const string PaginationPrevious = "Pagination.Previous";

    /// <summary>"Next page": names the next-page button.</summary>
    public const string PaginationNext = "Pagination.Next";

    /// <summary>"First page": names the first-page button.</summary>
    public const string PaginationFirst = "Pagination.First";

    /// <summary>"Last page": names the last-page button.</summary>
    public const string PaginationLast = "Pagination.Last";

    /// <summary>"Minimum": names a range slider's lower thumb, after the field's label.</summary>
    public const string RangeSliderStart = "RangeSlider.Start";

    /// <summary>"Maximum": names a range slider's upper thumb, after the field's label.</summary>
    public const string RangeSliderEnd = "RangeSlider.End";

    /// <summary>"Filter": names a select's filter box.</summary>
    public const string SelectFilter = "Select.Filter";

    /// <summary>"No matches": shown when a select's filter finds nothing.</summary>
    public const string SelectNoResults = "Select.NoResults";

    /// <summary>"Clear": names a select's clear button.</summary>
    public const string SelectClear = "Select.Clear";

    /// <summary>"Resize {0}": names a splitter's separator; {0} is the pane before it.</summary>
    public const string SplitterResize = "Splitter.Resize";

    /// <summary>"pane {0}": stands in for a pane's label in a separator's name and its grip menu when the pane has
    /// none; {0} is its number.</summary>
    public const string SplitterPane = "Splitter.Pane";

    /// <summary>"Size of {0}": names the menu on a splitter's grip; {0} is the pane before it.</summary>
    public const string SplitterMenu = "Splitter.Menu";

    /// <summary>"Make {0} larger": a grip menu item that grows the pane before the grip by 10%.</summary>
    public const string SplitterLarger = "Splitter.Larger";

    /// <summary>"Make {0} smaller": a grip menu item that shrinks the pane before the grip by 10%.</summary>
    public const string SplitterSmaller = "Splitter.Smaller";

    /// <summary>"Collapse {0}": a grip menu item that collapses a collapsible pane.</summary>
    public const string SplitterCollapse = "Splitter.Collapse";

    /// <summary>"Restore {0}": a grip menu item that restores a collapsed pane.</summary>
    public const string SplitterRestore = "Splitter.Restore";

    /// <summary>"On this page": names a table of contents' navigation landmark.</summary>
    public const string TableOfContentsNavigation = "TableOfContents.Navigation";

    /// <summary>"Clear": names a text field's clear button.</summary>
    public const string TextFieldClear = "TextField.Clear";

    /// <summary>"{0} isn't complete.": shown when a masked text field has empty slots; {0} is the field's label.
    /// </summary>
    public const string TextFieldMaskIncomplete = "TextField.MaskIncomplete";

    /// <summary>"Theme": names a theme picker.</summary>
    public const string ThemePicker = "Theme.Picker";

    /// <summary>"Light": names the theme picker's light choice.</summary>
    public const string ThemeLight = "Theme.Light";

    /// <summary>"Dark": names the theme picker's dark choice.</summary>
    public const string ThemeDark = "Theme.Dark";

    /// <summary>"Match the device": names the theme picker's choice that follows the device's setting.</summary>
    public const string ThemeSystem = "Theme.System";

    /// <summary>"{0} available, press F8.": read out after a toast's message when it has an action; {0} is the
    /// action's text.</summary>
    public const string ToastActionAvailable = "Toast.ActionAvailable";

    /// <summary>"Dismiss": names a toast's close button.</summary>
    public const string ToastDismiss = "Toast.Dismiss";

    /// <summary>"Notifications": names the region the toasts appear in.</summary>
    public const string ToastRegion = "Toast.Region";

    /// <summary>"More": names a toolbar's overflow menu.</summary>
    public const string ToolbarMore = "Toolbar.More";

    /// <summary>"Loading…": shown while a data grid loads its rows.</summary>
    public const string DataGridLoading = "DataGrid.Loading";

    /// <summary>"Couldn't load the rows.": shown when a data grid's rows fail to load.</summary>
    public const string DataGridLoadFailed = "DataGrid.LoadFailed";

    /// <summary>"Retry": names the button that loads a data grid's rows again after a failure.</summary>
    public const string DataGridRetry = "DataGrid.Retry";

    /// <summary>"Nothing to show.": shown by a data grid with no rows.</summary>
    public const string DataGridEmpty = "DataGrid.Empty";

    /// <summary>"Select all rows": names the header checkbox that selects every row on a data grid's page.</summary>
    public const string DataGridSelectAll = "DataGrid.SelectAll";

    /// <summary>"Select": names the column of a data grid's selection buttons.</summary>
    public const string DataGridSelect = "DataGrid.Select";

    /// <summary>"Select {0}": names a row's selection box; {0} is the row's label.</summary>
    public const string DataGridSelectRow = "DataGrid.SelectRow";

    /// <summary>"Order": names the column of a data grid whose rows can be moved, which holds each row's move
    /// handle.</summary>
    public const string DataGridOrder = "DataGrid.Order";

    /// <summary>"{0}–{1} of {2}": the rows a data grid's page shows; {0} and {1} are the first and last, {2} the
    /// count of all rows.</summary>
    public const string DataGridRange = "DataGrid.Range";

    /// <summary>"Pages of {0}": names a data grid's pages; {0} is the grid's name.</summary>
    public const string DataGridPages = "DataGrid.Pages";

    /// <summary>"Rows per page": names a data grid's list of page sizes.</summary>
    public const string DataGridPageSize = "DataGrid.PageSize";

    /// <summary>"Filter {0}": names a data grid's filter for a column; {0} is the column.</summary>
    public const string DataGridFilter = "DataGrid.Filter";

    /// <summary>"Filter {0}, on": names a data grid's filter menu button while its column is filtered; {0} is the
    /// column.</summary>
    public const string DataGridFilterOn = "DataGrid.FilterOn";

    /// <summary>"Any": shown in a data grid's choice filter when it filters nothing.</summary>
    public const string DataGridFilterAny = "DataGrid.FilterAny";

    /// <summary>"Yes": the choice that shows a data grid's rows whose yes-or-no column is true.</summary>
    public const string DataGridFilterYes = "DataGrid.FilterYes";

    /// <summary>"No": the choice that shows a data grid's rows whose yes-or-no column is false.</summary>
    public const string DataGridFilterNo = "DataGrid.FilterNo";

    /// <summary>"Apply": the button that applies a data grid's filter menu.</summary>
    public const string DataGridFilterApply = "DataGrid.FilterApply";

    /// <summary>"Clear": the button that clears a data grid column's filter.</summary>
    public const string DataGridFilterClear = "DataGrid.FilterClear";

    /// <summary>"Condition": names the list of conditions in a data grid's filter menu.</summary>
    public const string DataGridFilterCondition = "DataGrid.FilterCondition";

    /// <summary>"Value": names the value field in a data grid's filter menu.</summary>
    public const string DataGridFilterValue = "DataGrid.FilterValue";

    /// <summary>"Contains": the condition that the column's text contains the filter's value.</summary>
    public const string DataGridFilterContains = "DataGrid.FilterContains";

    /// <summary>"Doesn't contain": the condition that the column's text doesn't contain the filter's value.</summary>
    public const string DataGridFilterDoesNotContain = "DataGrid.FilterDoesNotContain";

    /// <summary>"Starts with": the condition that the column's text starts with the filter's value.</summary>
    public const string DataGridFilterStartsWith = "DataGrid.FilterStartsWith";

    /// <summary>"Ends with": the condition that the column's text ends with the filter's value.</summary>
    public const string DataGridFilterEndsWith = "DataGrid.FilterEndsWith";

    /// <summary>"Is": the condition that the column's value equals the filter's value.</summary>
    public const string DataGridFilterEqualTo = "DataGrid.FilterEqualTo";

    /// <summary>"Isn't": the condition that the column's value differs from the filter's value.</summary>
    public const string DataGridFilterNotEqualTo = "DataGrid.FilterNotEqualTo";

    /// <summary>"Less than": the condition that the column's value is less than the filter's value.</summary>
    public const string DataGridFilterLessThan = "DataGrid.FilterLessThan";

    /// <summary>"At most": the condition that the column's value is at most the filter's value.</summary>
    public const string DataGridFilterAtMost = "DataGrid.FilterAtMost";

    /// <summary>"More than": the condition that the column's value is more than the filter's value.</summary>
    public const string DataGridFilterGreaterThan = "DataGrid.FilterGreaterThan";

    /// <summary>"At least": the condition that the column's value is at least the filter's value.</summary>
    public const string DataGridFilterAtLeast = "DataGrid.FilterAtLeast";

    /// <summary>"Is empty": the condition that the column has no value.</summary>
    public const string DataGridFilterIsEmpty = "DataGrid.FilterIsEmpty";

    /// <summary>"Isn't empty": the condition that the column has a value.</summary>
    public const string DataGridFilterIsNotEmpty = "DataGrid.FilterIsNotEmpty";

    /// <summary>"Rows shown: {0}.": read out when a data grid's filters change; {0} is how many rows meet them.
    /// </summary>
    public const string DataGridRowsShown = "DataGrid.RowsShown";

    /// <summary>"Sorted by {0}, ascending.": read out when a column sorts its rows smallest first; {0} is the column.
    /// </summary>
    public const string DataGridSortedAscending = "DataGrid.SortedAscending";

    /// <summary>"Sorted by {0}, descending.": read out when a column sorts its rows largest first; {0} is the column.
    /// </summary>
    public const string DataGridSortedDescending = "DataGrid.SortedDescending";

    /// <summary>"No longer sorted by {0}.": read out when a column stops sorting the rows; {0} is the column.
    /// </summary>
    public const string DataGridUnsorted = "DataGrid.Unsorted";

    /// <summary>"sort {0} of {1}, {2}": tells which of several sorts a column is; {0} is its place, {1} the count and
    /// {2} its direction.</summary>
    public const string DataGridSortPosition = "DataGrid.SortPosition";

    /// <summary>"ascending": a sort from smallest to largest.</summary>
    public const string DataGridAscending = "DataGrid.Ascending";

    /// <summary>"descending": a sort from largest to smallest.</summary>
    public const string DataGridDescending = "DataGrid.Descending";

    /// <summary>"Loading…": shown while a tree row loads its children.</summary>
    public const string TreeViewLoading = "TreeView.Loading";

    /// <summary>"Couldn't load these items.": shown when a tree row's children fail to load.</summary>
    public const string TreeViewLoadFailed = "TreeView.LoadFailed";

    /// <summary>"Retry": names the button that loads a failed tree row's children again.</summary>
    public const string TreeViewRetry = "TreeView.Retry";

    /// <summary>"Nothing to show.": shown by a tree with no rows.</summary>
    public const string TreeViewEmpty = "TreeView.Empty";

    /// <summary>"Alt+Shift and the arrow keys move this item.": describes a tree row that can be moved.</summary>
    public const string TreeViewMoveHint = "TreeView.MoveHint";

    /// <summary>"{0} moved before {1}.": said after a tree move, with what moved and its new neighbor.</summary>
    public const string TreeViewMovedBefore = "TreeView.MovedBefore";

    /// <summary>"{0} moved after {1}.": said after a tree move, with what moved and its new neighbor.</summary>
    public const string TreeViewMovedAfter = "TreeView.MovedAfter";

    /// <summary>"{0} moved into {1}.": said after a tree move, with what moved and its new parent.</summary>
    public const string TreeViewMovedInside = "TreeView.MovedInside";

    /// <summary>"{0} items": names several tree rows moved together; {0} is how many.</summary>
    public const string TreeViewItemCount = "TreeView.ItemCount";

    /// <summary>"Move {0}": the name of the handle that drags or moves an item of a sortable list or a grid row, with
    /// the item's text.</summary>
    public const string SortableHandle = "Sortable.Handle";

    /// <summary>"Arrow keys move it, and Home and End move it to the start or end. Enter opens a menu of moves.":
    /// describes a move handle.</summary>
    public const string SortableHint = "Sortable.Hint";

    /// <summary>"Move up": a move handle's menu item that moves its item one place toward the start.</summary>
    public const string SortableMoveUp = "Sortable.MoveUp";

    /// <summary>"Move down": a move handle's menu item that moves its item one place toward the end.</summary>
    public const string SortableMoveDown = "Sortable.MoveDown";

    /// <summary>"Move to the start": a move handle's menu item that moves its item first.</summary>
    public const string SortableMoveToStart = "Sortable.MoveToStart";

    /// <summary>"Move to the end": a move handle's menu item that moves its item last.</summary>
    public const string SortableMoveToEnd = "Sortable.MoveToEnd";

    /// <summary>"{0} moved to position {1} of {2}.": said after an item of a sortable list or a grid row moves, with
    /// its text, new position and the number of items.</summary>
    public const string SortableMoved = "Sortable.Moved";

    /// <summary>"An item can't move into itself.": why a tree move into an item's own branch is refused.</summary>
    public const string TreeViewCantMoveIntoItself = "TreeView.CantMoveIntoItself";

    private static readonly FrozenDictionary<string, string> EnglishText = new Dictionary<string, string>
    {
        [AlertClose] = "Close",
        [AutocompleteClear] = "Clear",
        [AutocompleteSearching] = "Searching…",
        [AutocompleteNoResults] = "No matches",
        [AutocompleteSearchFailed] = "Couldn't load suggestions.",
        [AutocompleteTypeToSearch] = "Type to search",
        [AutocompleteResults] = "Suggestions: {0}",
        [ChipRemove] = "Remove",
        [ChipRemoveNamed] = "Remove {0}",
        [ColorPickerOpen] = "Choose a color",
        [ColorPickerEyedropper] = "Pick a color from the screen",
        [ColorPickerClear] = "Clear",
        [ColorPickerSwatches] = "Swatches",
        [ColorPickerHue] = "Hue",
        [ColorPickerSaturation] = "Saturation",
        [ColorPickerBrightness] = "Brightness",
        [ColorPickerAlpha] = "Opacity",
        [ColorPickerRed] = "Red",
        [ColorPickerGreen] = "Green",
        [ColorPickerBlue] = "Blue",
        [ColorPickerBadColor] = "Enter a color as #RRGGBB.",
        [DateRangePickerStart] = "Start date",
        [DateRangePickerEnd] = "End date",
        [DateRangePickerOpen] = "Choose dates",
        [DateRangePickerPreviousMonth] = "Previous month",
        [DateRangePickerNextMonth] = "Next month",
        [DateRangePickerPresets] = "Presets",
        [DateRangePickerClear] = "Clear",
        [DateRangePickerChooseEnd] = "Now choose the end date.",
        [DateRangePickerChosen] = "{0} to {1}",
        [DateRangePickerBadStart] = "Enter the start date as {0}.",
        [DateRangePickerBadEnd] = "Enter the end date as {0}.",
        [DateRangePickerUnavailable] = "{0} can't be chosen.",
        [DateRangePickerEndBeforeStart] = "The end date is before the start date.",
        [DialogClose] = "Close",
        [DialogMinimize] = "Minimize",
        [DialogMaximize] = "Maximize",
        [DialogRestore] = "Restore",
        [DialogMove] = "Move",
        [DialogMoveHint] = "Arrow keys move the dialog.",
        [DialogResize] = "Resize",
        [DialogResizeHint] = "Arrow keys resize the dialog.",
        [DialogPositionCenter] = "Center",
        [DialogPositionTop] = "Top",
        [DialogPositionBottom] = "Bottom",
        [DialogPositionStart] = "Start",
        [DialogPositionEnd] = "End",
        [DialogPositionTopStart] = "Top start",
        [DialogPositionTopEnd] = "Top end",
        [DialogPositionBottomStart] = "Bottom start",
        [DialogPositionBottomEnd] = "Bottom end",
        [DialogSizeSmall] = "Small",
        [DialogSizeMedium] = "Medium",
        [DialogSizeLarge] = "Large",
        [DialogSizeExtraLarge] = "Extra large",
        [DialogConfirm] = "OK",
        [DialogCancel] = "Cancel",
        [FieldInvalid] = "{0} isn't valid.",
        [FieldRequired] = "{0} is required.",
        [FieldUnnamed] = "The field",
        [FileUploadZone] = "Drop files here, or click to choose them.",
        [FileUploadChoose] = "Choose files",
        [FileUploadRemove] = "Remove {0}",
        [FileUploadClear] = "Clear",
        [FileUploadChosen] = "{0} files chosen.",
        [FileUploadChosenOne] = "1 file chosen.",
        [FileUploadWrongType] = "{0} isn't one of {1}.",
        [FileUploadTooLarge] = "{0} is larger than {1}.",
        [FileUploadBytes] = "{0} B",
        [FileUploadKilobytes] = "{0} KB",
        [FileUploadMegabytes] = "{0} MB",
        [FileUploadGigabytes] = "{0} GB",
        [FileUploadTooMany] = "Choose at most {0} files.",
        [FileUploadTooManyOne] = "Choose only one file.",
        [LabelInfo] = "Information about {0}",
        [NewTabHint] = "opens in a new tab",
        [NumberFieldDecrease] = "Decrease",
        [NumberFieldIncrease] = "Increase",
        [NumberFieldNotANumber] = "{0} must be a number.",
        [NumberFieldRange] = "{0} must be from {1} to {2}.",
        [NumberFieldAtLeast] = "{0} must be at least {1}.",
        [NumberFieldAtMost] = "{0} must be at most {1}.",
        [MultiSelectRemoved] = "{0} removed",
        [MultiSelectSelectAll] = "Select all",
        [PaginationNavigation] = "Pagination",
        [PaginationPage] = "Page {0}",
        [PaginationPageBox] = "Page",
        [PaginationOf] = "of {0}",
        [PaginationPrevious] = "Previous page",
        [PaginationNext] = "Next page",
        [PaginationFirst] = "First page",
        [PaginationLast] = "Last page",
        [RangeSliderStart] = "Minimum",
        [RangeSliderEnd] = "Maximum",
        [SelectFilter] = "Filter",
        [SelectNoResults] = "No matches",
        [SelectClear] = "Clear",
        [SplitterResize] = "Resize {0}",
        [SplitterPane] = "pane {0}",
        [SplitterMenu] = "Size of {0}",
        [SplitterLarger] = "Make {0} larger",
        [SplitterSmaller] = "Make {0} smaller",
        [SplitterCollapse] = "Collapse {0}",
        [SplitterRestore] = "Restore {0}",
        [TableOfContentsNavigation] = "On this page",
        [TextFieldClear] = "Clear",
        [TextFieldMaskIncomplete] = "{0} isn't complete.",
        [ThemePicker] = "Theme",
        [ThemeLight] = "Light",
        [ThemeDark] = "Dark",
        [ThemeSystem] = "Match the device",
        [ToastActionAvailable] = "{0} available, press F8.",
        [ToastDismiss] = "Dismiss",
        [ToastRegion] = "Notifications",
        [ToolbarMore] = "More",
        [DataGridLoading] = "Loading…",
        [DataGridLoadFailed] = "Couldn't load the rows.",
        [DataGridRetry] = "Retry",
        [DataGridEmpty] = "Nothing to show.",
        [DataGridSelectAll] = "Select all rows",
        [DataGridSelect] = "Select",
        [DataGridSelectRow] = "Select {0}",
        [DataGridOrder] = "Order",
        [DataGridRange] = "{0}–{1} of {2}",
        [DataGridPages] = "Pages of {0}",
        [DataGridPageSize] = "Rows per page",
        [DataGridFilter] = "Filter {0}",
        [DataGridFilterOn] = "Filter {0}, on",
        [DataGridFilterAny] = "Any",
        [DataGridFilterYes] = "Yes",
        [DataGridFilterNo] = "No",
        [DataGridRowsShown] = "Rows shown: {0}.",
        [DataGridFilterApply] = "Apply",
        [DataGridFilterClear] = "Clear",
        [DataGridFilterCondition] = "Condition",
        [DataGridFilterValue] = "Value",
        [DataGridFilterContains] = "Contains",
        [DataGridFilterDoesNotContain] = "Doesn't contain",
        [DataGridFilterStartsWith] = "Starts with",
        [DataGridFilterEndsWith] = "Ends with",
        [DataGridFilterEqualTo] = "Is",
        [DataGridFilterNotEqualTo] = "Isn't",
        [DataGridFilterLessThan] = "Less than",
        [DataGridFilterAtMost] = "At most",
        [DataGridFilterGreaterThan] = "More than",
        [DataGridFilterAtLeast] = "At least",
        [DataGridFilterIsEmpty] = "Is empty",
        [DataGridFilterIsNotEmpty] = "Isn't empty",
        [DataGridSortedAscending] = "Sorted by {0}, ascending.",
        [DataGridSortedDescending] = "Sorted by {0}, descending.",
        [DataGridUnsorted] = "No longer sorted by {0}.",
        [DataGridSortPosition] = "sort {0} of {1}, {2}",
        [DataGridAscending] = "ascending",
        [DataGridDescending] = "descending",
        [TreeViewLoading] = "Loading…",
        [TreeViewLoadFailed] = "Couldn't load these items.",
        [TreeViewRetry] = "Retry",
        [TreeViewEmpty] = "Nothing to show.",
        [TreeViewMoveHint] = "Alt+Shift and the arrow keys move this item.",
        [TreeViewMovedBefore] = "{0} moved before {1}.",
        [TreeViewMovedAfter] = "{0} moved after {1}.",
        [TreeViewMovedInside] = "{0} moved into {1}.",
        [TreeViewItemCount] = "{0} items",
        [TreeViewCantMoveIntoItself] = "An item can't move into itself.",
        [SortableHandle] = "Move {0}",
        [SortableHint] =
            "Arrow keys move it, and Home and End move it to the start or end. Enter opens a menu of moves.",
        [SortableMoveUp] = "Move up",
        [SortableMoveDown] = "Move down",
        [SortableMoveToStart] = "Move to the start",
        [SortableMoveToEnd] = "Move to the end",
        [SortableMoved] = "{0} moved to position {1} of {2}.",
    }.ToFrozenDictionary();

    /// <summary>The English text for <paramref name="key"/>, for a localizer of your own to fall back on.</summary>
    /// <exception cref="KeyNotFoundException">The key isn't one of these.</exception>
    public static string English(string key) => EnglishText.TryGetValue(key, out var text) ? text
        : throw new KeyNotFoundException($"{key} isn't a Trailblazor string; the keys are in {nameof(TbStrings)}.");
}
