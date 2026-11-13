# Tree grid

Pattern: [Treegrid](https://www.w3.org/WAI/ARIA/apg/patterns/treegrid/), with focus on cells rather than rows

Familiar apps: File Explorer's and Finder's list views with folders, Microsoft Project's task outline, a budget sheet with grouped lines

## Keyboard

- [x] Right on a closed row's first cell opens it and keeps focus on its button; on an open row it moves to the next cell. `TreeGridTests.Keys_ShouldOpenAndCloseRowsAndGoToTheirParent_WhenPressedOnTheFirstCell`
- [x] Left on an open row's first cell closes it; on a closed row or a row without children it goes to the row's parent. `TreeGridTests.Keys_ShouldOpenAndCloseRowsAndGoToTheirParent_WhenPressedOnTheFirstCell`
- [x] Left reaches the row's parent even when a virtualized grid has scrolled the parent out of view. `TreeGridTests.Left_ShouldGoToTheParentRow_WhenItIsScrolledOutOfAVirtualizedGrid`
- [x] Left and Right swap in a right-to-left grid. `TreeGridTests.Keys_ShouldSwapLeftAndRight_WhenTheGridIsRightToLeft`
- [x] Enter or Space on a row's button opens or closes the row, and in an editable first column F2 edits the cell instead. `TreeGridTests.Keys_ShouldOpenAndCloseRowsAndGoToTheirParent_WhenPressedOnTheFirstCell`, `TreeGridTests.Toggle_ShouldOpenTheRowWithoutEditing_WhenTheFirstColumnIsEditable`
- [x] Even on a slow link, a second Right pressed before the server answers the first moves on rather than closing the row again, and Up or Down after opening a row waits for its children. `TreeGridTests.Keys_ShouldOpenThenMoveOn_WhenRightIsPressedTwiceQuickly`, `TreeGridTests.Keys_ShouldSwapLeftAndRight_WhenTheGridIsRightToLeft`
- [x] Typing on a row's editable first cell starts its edit, even with focus on the row's button. `TreeGridTests.Toggle_ShouldOpenTheRowWithoutEditing_WhenTheFirstColumnIsEditable`
- [x] Every other key works as in a data grid with cell navigation, which a tree grid always has. `TbDataGridTreeTests.Render_ShouldShowTheTopRowsOfATreeGrid_WhenGivenAChildrenSelector`, `data-grid.js`

## Pointer

- [x] A click on a row's button opens it, showing its children set in under it, and another click closes it with every row under it. `TbDataGridTreeTests.ExpandAsync_ShouldShowTheRowsChildrenUnderIt_WhenItsButtonIsClicked`, `TbDataGridTreeTests.CollapseAsync_ShouldHideEveryRowUnderIt_WhenItsButtonIsClickedAgain`
- [x] A click on the button doesn't click or select the row. `Trailblazor.lib.module.js`
- [x] Sorting orders rows among their siblings. `TbDataGridTreeTests.Sort_ShouldOrderRowsAmongTheirSiblings_WhenAColumnIsSorted`
- [x] A filter shows the matches with the rows they're nested under opened; rows closed while it applies open again when the filters change. `TbDataGridTreeTests.Filter_ShouldShowMatchesWithTheRowsAboveThemOpened_WhenFiltered`, `TbDataGridTreeTests.Toggle_ShouldCloseARowOnlyUntilTheFiltersChange_WhenFiltersApply`

## Touch

- [x] A tap on a row's button opens or closes it, and the button is at least 24 pixels square. `trailblazor.css`

## Focus

- [x] Focus stays on a row's button as the rows under it show or hide. `TreeGridTests.Keys_ShouldOpenAndCloseRowsAndGoToTheirParent_WhenPressedOnTheFirstCell`
- [x] `ScrollToAsync` opens the rows a row is nested under before going to it. `TbDataGridTreeTests.ScrollToAsync_ShouldOpenTheRowsAboveTheRow_WhenTheyAreClosed`

## Not applicable

- Focus on whole rows, which the pattern allows: the data grid moves between cells, so a tree grid's keys work the same way on its first cell.
- Loading children when a row opens: the rows come from `Items`; a tree view's `LoadChildren` covers trees too large to hold.
- Moving rows between parents: a tree grid's rows can't move; a tree view's drag and drop moves nested items.
- Pages: a page could start with a row whose parent is on the page before, so a long tree uses `Virtualize` instead.
