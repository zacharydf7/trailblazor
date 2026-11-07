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
- [x] With cell navigation, Enter on a cell with nothing to go into clicks the row, which is how an app opens it, as in a mail list or a file list. `DataGridTests.Row_ShouldBeClicked_WhenEnterIsPressedOnACellWithNothingToGoInto`, `DataGridTests.Row_ShouldBeClicked_WhenEnterIsPressedWithTextSelected`, `DataGridTests.Row_ShouldNotBeClicked_WhenEnterStartsAnEdit`, `DataGridTests.Row_ShouldNotBeClicked_WhenF2IsPressedOnACellWithNothingToGoInto`, `DataGridTests.HeaderRow_ShouldGetNoClick_WhenEnterIsPressedOnAHeaderCell`
- [x] With cell navigation, holding Enter on a cell with nothing to go into, or on a control the cell hands focus to, acts once. `DataGridTests.Row_ShouldBeClickedOnce_WhenEnterIsHeld`, `DataGridTests.Row_ShouldLoseOnlyOneRow_WhenEnterIsHeldOnADeleteButton`
- [x] Shift+Up and Shift+Down extend the selected rows in a multiple-selection grid, and moving back shrinks the range. `GridSelectTests.Grid_ShouldGrowAndShrinkTheSelection_WhenShiftAndTheArrowsMove`, `GridSelectTests.Grid_ShouldGrowUpward_WhenShiftAndUpMoveFromARow`
- [x] Shift+Up and Shift+Down extend from the row last chosen, even after a plain move, and carry on across a group's heading and onto a row a virtualized grid hasn't drawn yet. `GridSelectTests.Grid_ShouldGoOnFromTheRowLastChosen_WhenAPlainMoveCameBeforeShiftAndAnArrow`, `GridSelectTests.Grid_ShouldExtendAcrossAGroupHeading_WhenShiftAndTheArrowsReachOne`, `GridSelectTests.Grid_ShouldExtendOntoARowNotDrawnYet_WhenTheGridIsVirtualized`
- [x] With no chosen row among the rows, they start from the row they left, even if the first move lands on a group's heading. `GridSelectTests.Grid_ShouldKeepTheRowItStartedFrom_WhenTheFirstMoveLandsOnAGroupHeading`, `TbDataGridTests.Extend_ShouldStartFromTheRowItLeft_WhenTheChosenRowHasBeenFilteredAway`
- [x] Ctrl+A selects every row in a multiple-selection grid, the same rows as the header's checkbox, and no page text. A second press keeps them. `GridSelectTests.Grid_ShouldSelectEveryRowAndNoText_WhenCtrlAIsPressedInACell`, `TbDataGridTests.SelectAll_ShouldKeepEveryRowSelected_WhenRaisedTwice`
- [x] Ctrl+A stays with the browser in a grid whose rows aren't all loaded. `GridSelectTests.Grid_ShouldLeaveCtrlAToTheBrowser_WhenItsRowsArentAllLoaded`
- [x] Typing on an editable cell starts its edit, with what's typed replacing the value. `GridEditKeysTests.Typing_ShouldStartTheEditWithWhatWasTyped_WhenPressedOnATextCell`, `GridEditKeysTests.Typing_ShouldStartTheEditWithWhatWasTyped_WhenPressedOnANumberCell`, `GridEditKeysTests.Typing_ShouldKeepAMinusSign_WhenTheNumberComesAfterTheFieldIsDrawn`
- [x] A select or a checkbox just opens, Space still selects the row, and a key with Ctrl, Cmd or Alt doesn't start an edit. `GridEditKeysTests.Typing_ShouldJustOpenTheEdit_WhenTheCellsFieldTakesNoText`, `GridEditKeysTests.Typing_ShouldSelectTheRowAndNotEdit_WhenSpaceIsPressed`, `GridEditKeysTests.Typing_ShouldLeaveTheCellAlone_WhenAModifierIsHeld`
- [x] Tab in a cell's edit saves it and opens the next editable cell, and Shift+Tab the one before, across rows. `GridEditKeysTests.Tab_ShouldSaveAndOpenTheNextCell_WhenPressedInACellsEdit`, `GridEditKeysTests.Tab_ShouldSaveAndOpenThePreviousCell_WhenPressedWithShift`, `GridEditKeysTests.Tab_ShouldGoOnToTheNextRow_WhenPressedInARowsLastEditableCell`, `GridEditKeysTests.Tab_ShouldGoOnFromAChoice_WhenItsListIsClosed`, `GridEditKeysTests.Tab_ShouldSaveWhatWasTyped_WhenTheEditorsFieldCommitsOnChange`
- [x] A value that fails its check keeps the edit open. Tab in the last editable cell leaves the grid, and Shift+Tab in the first goes back to its cell. `GridEditKeysTests.Tab_ShouldKeepTheEditOpenWithWhy_WhenTheValueFailsItsCheck`, `GridEditKeysTests.Tab_ShouldSaveAndLeaveTheGrid_WhenPressedInTheLastEditableCell`, `GridEditKeysTests.Tab_ShouldSaveAndGoBackToItsCell_WhenShiftIsHeldInTheFirstEditableCell`
- [x] Tab opens no edit on a row its save filtered or sorted out of view. `TbDataGridEditTests.EditStart_ShouldOpenNoEdit_WhenTheSaveTookItsRowAway`

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
- [x] A drag held still near the top or bottom of the scroll box keeps scrolling. `DragScrollTests.DataGrid_ShouldKeepScrolling_WhenARowDragIsHeldStillNearTheBottom`
- [x] Double-clicking a row that isn't being edited raises one row click, and doesn't select and unselect it. `DataGridTests.Row_ShouldBeClickedOnce_WhenDoubleClicked`
- [x] Shift+clicking a row selects from the last one chosen through the clicked row and moves focus to it, without selecting text. Shift+clicking a closer row shrinks the range. `GridSelectTests.Grid_ShouldSelectARangeFocusTheRowAndSelectNoText_WhenARowIsShiftClicked`
- [x] With nothing chosen, or once the parent or the header's checkbox has changed the selection, Shift+clicking a row adds just that row and never unselects one. `GridSelectTests.Grid_ShouldAddJustTheRow_WhenItIsShiftClickedWithNothingChosenBefore`, `TbDataGridTests.ShiftClick_ShouldAddTheRowAndKeepItSelected_WhenTheParentSetTheSelectionSinceTheLastChoice`, `TbDataGridTests.ShiftClick_ShouldNotBringBackRows_WhenTheHeadersBoxClearedThem`, `TbDataGridTests.ShiftClick_ShouldKeepEveryRowSelected_WhenTheyWereAllSelectedTogether`
- [x] A parent that stores a copy of the selection doesn't end the range. `TbDataGridTests.ShiftClick_ShouldSelectTheRange_WhenTheParentStoresACopyOfTheSelection`
- [x] Shift+clicking the header's checkbox still selects or clears every row. `GridSelectTests.Grid_ShouldToggleEveryRow_WhenTheHeadersBoxIsShiftClicked`
- [x] Shift+clicking in a field in a row extends the field's own text selection, Ctrl+A there still selects its text, and with `RowClickSelects="false"` a row's text selects as usual. `GridSelectTests.Grid_ShouldLeaveAFieldsOwnTextSelection_WhenShiftClickedInARowsField`, `GridSelectTests.Grid_ShouldLeaveCtrlAToAField_WhenAShiftClickCameBeforeIt`, `GridSelectTests.Grid_ShouldLeaveShiftClickToTheBrowser_WhenAClickOnARowDoesntSelectIt`
- [x] Shift+clicking a row's checkbox selects the range too, and every checkbox shows what's selected. `GridSelectTests.Grid_ShouldSelectARangeAndKeepItsBoxesRight_WhenABoxIsShiftClicked`
- [x] Dragging across a row's text to copy it leaves the row unselected and raises no row click. `DataGridTests.Row_ShouldNotBeClicked_WhenADragSelectsItsText`
- [x] A plain click on text already selected in a row still clicks the row. `DataGridTests.Row_ShouldBeClicked_WhenItsSelectedTextIsClicked`, `DataGridTests.Row_ShouldBeClicked_WhenThePointerSlipsWhileClickingItsSelectedText`
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
- [x] With cell navigation, when the focused row goes away, focus moves to the row that takes its place, or to the new last row when it was the last. A virtualized grid leaves focus on the page when its last row goes or the focused row scrolls away. `DataGridTests.Focus_ShouldMoveToTheRowThatTakesItsPlace_WhenTheFocusedRowIsRemoved`, `DataGridTests.Focus_ShouldMoveToTheRowThatTakesItsPlace_WhenTheFocusedRowIsRemovedFromAVirtualizedGrid`, `DataGridTests.Focus_ShouldMoveToTheLastRow_WhenTheFocusedLastRowIsRemoved`, `DataGridTests.Focus_ShouldStayOnThePage_WhenARowIsRemovedAfterFocusLeftTheGrid`, `DataGridTests.Focus_ShouldStayOnThePage_WhenAVirtualizedGridDropsTheFocusedRowAsItScrolls`

## Not applicable

- Arrows selecting as they move: that would be a new option, as `SelectionFollowsFocus` is on the tree, not a fix.
- Ctrl+Space selecting a column, and Shift with the arrows selecting cells: the grid selects rows, and cell ranges are #176.
- A second F2 leaving a cell: Enter saves and Escape cancels.
- Left and Right closing and opening a group: its heading is a button that Enter or Space toggles, not a row of a tree grid.
- A plain click replacing the selection in a multiple-selection grid: rows toggle like the checkboxes beside them, as in a mail list.
