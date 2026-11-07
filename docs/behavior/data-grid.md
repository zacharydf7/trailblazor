# Data grid

Pattern: [Grid](https://www.w3.org/WAI/ARIA/apg/patterns/grid/) with cell navigation or editing, [Table](https://www.w3.org/WAI/ARIA/apg/patterns/table/) without

Familiar apps: Excel, Google Sheets, Outlook's message list, File Explorer's details view

## Keyboard

- [x] Enter or Space on a header sorts by it, and with Shift adds it to the sorts. `DataGridTests.Grid_ShouldAddToTheSorts_WhenShiftEnterIsPressedOnAHeader`
- [x] With cell navigation, the arrows move between cells and stop at the grid's edges. `DataGridTests.CellNavigation_ShouldMoveBetweenCellsIntoAFieldAndBack_WhenDrivenByKeys`
- [x] Left and Right swap in a right-to-left grid. `data-grid.js`
- [x] Home and End go to the row's ends, Ctrl+Home and Ctrl+End to the grid's first and last cells, and Page Up and Page Down move by a screenful, even to rows not drawn yet. `DataGridTests.CellNavigation_ShouldScrollToRowsNotYetDrawn_WhenControlEndAndHomeArePressed`, `data-grid.js`
- [x] An up or down arrow keeps its column through a group's heading. `DataGridTests.CellNavigation_ShouldKeepItsColumnThroughAGroupsHeading_WhenMovingDown`
- [x] Arrows pass over a cell with a menu without opening it, and over a row's handle without moving the row. `DataGridTests.CellNavigation_ShouldMoveOverAMenusCellWithoutOpeningIt_WhenArrowsArePressed`
- [x] An arrow on a row's radio button moves on without choosing the row. `DataGridTests.CellNavigation_ShouldMoveFromARowsRadioWithoutChoosingIt_WhenAnArrowIsPressed`
- [x] Enter or F2 moves into a cell's controls, Tab goes around them and Escape comes back out. `DataGridTests.CellNavigation_ShouldGoRoundACellsButtonsAndBackOut_WhenEnteredWithEnter`
- [x] Space selects the focused cell's row; on a header it does nothing and doesn't scroll the page. `DataGridTests.CellNavigation_ShouldLeaveTheRowsUnselected_WhenSpaceIsPressedOnAHeader`
- [x] Enter or F2 starts an edit in an editable cell. `TbDataGridEditTests.Enter_ShouldRaiseTheEditWithAValueOfTheFieldsType_WhenTheGridHandlesEdits`, `TbDataGrid.cs`
- [x] Enter saves an edit, and when a value fails its check, the edit stays open and says why. `TbDataGridEditTests.Enter_ShouldKeepTheEditOpenWithWhy_WhenACheckFails`, `DataGridTests.CellEdit_ShouldSaveAndFocusTheCellAgain_WhenEnterStartsAndEndsIt`
- [x] Escape cancels an edit and doesn't also close a dialog around the grid. `DataGridTests.CellEdit_ShouldLeaveTheValue_WhenEscapeCancelsAChange`
- [x] In a row's edit, Tab stays among its fields and its Save and Cancel buttons. `DataGridTests.RowEdit_ShouldKeepTabInTheRowAndSaveItWhole_WhenEnterIsPressed`
- [x] Enter in a filter or an edit doesn't submit a form around the grid. `DataGridTests.Filters_ShouldApplyWithoutSubmittingThePagesForm_WhenEnterIsPressedInThem`, `data-grid.js`
- [x] Enter in a filter menu's value applies it, and Escape closes the menu without applying. `DataGridTests.FilterMenu_ShouldApplyTheValue_WhenEnterIsPressedInIt`
- [x] Arrows, Home and End on a row's handle move the row. `SortableTests.GridRows_ShouldReorderByKeysAndByDragging_WhenTheGridLetsRowsMove`
- [x] Enter on a group's heading opens or closes the group. `DataGridTests.GroupToggle_ShouldCloseItsGroupAndKeepFocus_WhenEnterIsPressedOnIt`
- [ ] Enter on a row opens it, as it does in a mail list or a file list. #317
- [ ] Shift+Up and Shift+Down extend the selected rows in a multiple-selection grid. #318
- [ ] Ctrl+A selects every row in a multiple-selection grid. #318
- [ ] Typing a letter on an editable cell starts its edit with that letter. #319
- [ ] Tab in a cell's edit saves it and moves to the next editable cell. #319

## Pointer

- [x] Clicking a header sorts ascending, then descending, then not at all. `TbDataGridTests.Sort_ShouldOrderTheRowsTurnThemAroundAndStop_WhenAHeaderIsClickedThreeTimes`
- [x] Shift+clicking a header adds it to the sorts and shows its place. `TbDataGridTests.Sort_ShouldBreakTiesWithASecondColumnAndSayItsPlace_WhenShiftClicked`
- [x] Clicking a row selects it, but a click on a button, link or field in the row is that control's own. `DataGridTests.Grid_ShouldSelectByBoxRowAndHeaderButNotByACellsButton_WhenEachIsClicked`
- [x] The header's checkbox selects the page's rows, or unselects them when all are selected, and shows when only some are. `TbDataGridTests.Select_ShouldSelectRowsAndTheWholePage_WhenTheirCheckboxesChange`
- [x] Double-clicking an editable cell opens its edit and selects the row only once. `DataGridTests.CellEdit_ShouldSelectTheRowOnce_WhenADoubleClickOpensAnEdit`
- [x] Clicking away from a cell's edit, or a row's edit, saves it. `DataGridTests.CellEdit_ShouldSave_WhenFocusLeavesItsField`
- [x] A row's Cancel button throws the edit away instead of saving it as focus leaves. `DataGridTests.RowEdit_ShouldSaveNothing_WhenCancelIsClicked`
- [x] Dragging a row's handle moves the row. `SortableTests.GridRows_ShouldReorderByKeysAndByDragging_WhenTheGridLetsRowsMove`
- [x] A refused move leaves the order alone and says why. `TbDataGridTests.RowMove_ShouldAskFirstAndLeaveTheOrder_WhenTheMoveIsRefused`
- [x] Escape during a drag cancels it, and its release doesn't open the handle's menu. `sortable.js`
- [x] Handles go away while the rows are sorted, filtered or grouped, since a move would mean nothing. `TbDataGridTests.RowMove_ShouldOfferNoHandles_WhenTheGridIsSorted`
- [ ] A drag held still near the top or bottom of the scroll box keeps scrolling. #302
- [ ] Double-clicking a row that isn't being edited opens it, and doesn't select and unselect it. #317
- [ ] Shift+clicking a row or its checkbox selects the range from the last one clicked. #318
- [ ] Dragging across a row's text to copy it leaves the row unselected and raises no row click. #317
- [ ] Columns can be resized, moved and hidden. #164

## Touch

- [x] A tap on a row selects it: the grid listens for the click a tap sends. `Trailblazor.lib.module.js`
- [x] A finger held on a handle for 300 ms starts a drag, through pointer events, and a swipe elsewhere scrolls. `pointer-drag.js`
- [x] A tap on a handle opens its menu, which moves the row with no drag. `MoveHandle.cs`
- [ ] A double tap opens a cell's edit, which touch has no other way to do. UNCONFIRMED

## Focus

- [x] Cell navigation makes the grid one tab stop, and Tab leaves it. `DataGridTests.CellNavigation_ShouldScrollToRowsNotYetDrawn_WhenControlEndAndHomeArePressed`
- [x] A cell holding just a button, link, checkbox or radio button hands focus to it, and a focused cell shows a ring. `DataGridTests.CellNavigation_ShouldMoveBetweenCellsIntoAFieldAndBack_WhenDrivenByKeys`
- [x] A grid always keeps one tab stop, even when the button holding it turns itself off. `DataGridTests.CellNavigation_ShouldKeepAStopForTab_WhenTheButtonHoldingItTurnsItselfOff`
- [x] Focus goes back to the cell when an edit ends. `DataGridTests.CellEdit_ShouldSaveAndFocusTheCellAgain_WhenEnterStartsAndEndsIt`
- [x] Focus stays in the grid when a save sorts the row elsewhere or filters it out. `DataGridTests.CellEdit_ShouldKeepFocusInTheGrid_WhenTheSavedRowSortsElsewhere`
- [x] Focus stays on a moved row's handle, even when the row moves out of view. `DataGridTests.VirtualRows_ShouldKeepFocusOnAMovedRowsHandle_WhenItMovesOutOfView`
- [x] Focus scrolls a control clear of the pinned columns and of a header that stays in view. `DataGridTests.Grid_ShouldScrollAFocusedControlOutFromUnderItsHeader_WhenTheRowsScrollInAHeight`
- [x] The keys reach the Retry button when rows fail to load. `DataGridTests.CellNavigation_ShouldReachRetry_WhenAVirtualizedGridsRowsFailToLoad`
- [x] Screen readers hear how the rows are sorted, how many a filter leaves and when a load fails. `TbDataGridTests.Sort_ShouldSayHowTheRowsAreSorted_WhenAHeaderIsClicked`, `TbDataGrid.cs`
- [x] When rows overflow a grid's height, with no cell navigation, the keys can scroll its named box. `TbDataGridTests.ScrollBox_ShouldStartAsANamedTabStop_WhenTheGridHasAHeight`, `DataGridTests.ScrollBox_ShouldStopBeingATabStop_WhenItsRowsNoLongerOverflow`
- [x] A wide grid with no height has a scroll box the keys can reach. `DataGridTests.ScrollBox_ShouldBeANamedTabStop_WhenAGridWithNoHeightIsTooWide`
- [ ] When the focused row goes away, focus moves to the row that takes its place. #317

## Not applicable

- Arrows selecting as they move: that would be a new option, as `SelectionFollowsFocus` is on the tree, not a fix.
- Ctrl+Space selecting a column, and Shift with the arrows selecting cells: the grid selects rows, and cell ranges are #176.
- A second F2 leaving a cell: Enter saves and Escape cancels.
- Left and Right closing and opening a group: its heading is a button that Enter or Space toggles, not a row of a tree grid.
- A plain click replacing the selection in a multiple-selection grid: rows toggle like the checkboxes beside them, as in a mail list.
