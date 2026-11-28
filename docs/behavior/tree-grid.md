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
- [x] Down pressed while a row's children load waits for them and goes to the first. `TreeGridTests.Keys_ShouldGoIntoTheLoadedChildren_WhenDownIsPressedWhileTheyLoad`
- [x] Down pressed while a row's children fail to load moves on as soon as the row closes. `TreeGridTests.Keys_ShouldMoveOnAtOnce_WhenDownWaitsForChildrenThatFailToLoad`
- [x] Typing on a row's editable first cell starts its edit, even with focus on the row's button. `TreeGridTests.Toggle_ShouldOpenTheRowWithoutEditing_WhenTheFirstColumnIsEditable`
- [x] Every other key works as in a data grid with cell navigation, which a tree grid always has. `TbDataGridTreeTests.Render_ShouldShowTheTopRowsOfATreeGrid_WhenGivenAChildrenSelector`, `data-grid.js`

## Pointer

- [x] A click on a row's button opens it, showing its children set in under it, and another click closes it with every row under it. `TbDataGridTreeTests.ExpandAsync_ShouldShowTheRowsChildrenUnderIt_WhenItsButtonIsClicked`, `TbDataGridTreeTests.CollapseAsync_ShouldHideEveryRowUnderIt_WhenItsButtonIsClickedAgain`
- [x] A click on the button doesn't click or select the row. `Trailblazor.lib.module.js`
- [x] Sorting orders rows among their siblings. `TbDataGridTreeTests.Sort_ShouldOrderRowsAmongTheirSiblings_WhenAColumnIsSorted`
- [x] A filter shows the matches with the rows they're nested under opened; rows closed while it applies open again when the filters change. `TbDataGridTreeTests.Filter_ShouldShowMatchesWithTheRowsAboveThemOpened_WhenFiltered`, `TbDataGridTreeTests.Toggle_ShouldCloseARowOnlyUntilTheFiltersChange_WhenFiltersApply`
- [x] With `LoadChildren`, a row's button shows on the rows `HasChildren` allows, the row's children load the first time it opens and stay loaded, and the button disappears once a row turns out to have none. `TbDataGridLazyTreeTests.Toggle_ShouldLoadAndShowTheChildren_WhenAnUnloadedRowOpens`, `TbDataGridLazyTreeTests.Render_ShouldShowAButtonOnlyOnRowsThatMayHaveChildren_WhenNoneHaveLoaded`, `TbDataGridLazyTreeTests.Toggle_ShouldDropTheButton_WhenTheRowTurnsOutToHaveNoChildren`, `TbDataGridLazyTreeTests.Toggle_ShouldKeepTheLoadedChildren_WhenTheRowOpensAgain`, `TbDataGridLazyTreeTests.ExpandAsync_ShouldReturnOnceTheChildrenHaveLoaded_WhenTheyLoadSlowly`
- [x] Rows `ExpandedParents` opens load their children too, also while the page prerenders. `TbDataGridLazyTreeTests.ExpandedParents_ShouldLoadTheChildren_WhenTheAppOpensAnUnloadedRow`, `TbDataGridLazyTreeTests.Render_ShouldShowTheChildrenOfRowsTheAppOpens_WhenRenderedStatically`
- [x] A row opened again before its canceled load ends starts a new load. Under a filter, a row whose children haven't loaded stays closed until it is opened. `TbDataGridLazyTreeTests.Toggle_ShouldStartANewLoad_WhenTheRowOpensAgainBeforeItsCanceledLoadEnds`, `TbDataGridLazyTreeTests.Filter_ShouldLeaveAnUnloadedRowClosedUntilItOpens_WhenFiltersApply`
- [x] With a `DataProvider`, the provider gives the top rows a page at a time, each open row's children show under it on its page, the children sort among their siblings, and an export lists every top row with the children loaded under it. `TbDataGridProviderTreeTests.ExpandAsync_ShouldLoadTheChildrenUnderAProvidersRow_WhenTheRowOpens`, `TbDataGridProviderTreeTests.PageSize_ShouldPageTheTopRows_WhenTheyComeFromAProvider`, `TbDataGridProviderTreeTests.Sort_ShouldOrderTheChildrenAndLeaveTheTopRowsToTheProvider_WhenAColumnIsSorted`, `TbDataGridProviderTreeTests.ExportCsvAsync_ShouldListEachTopRowWithTheChildrenLoadedUnderIt_WhenCalled`, `TbDataGridProviderTreeTests.GoToPageAsync_ShouldAskForTheNextTopRows_WhenTheSecondPageShows`, `TbDataGridProviderTreeTests.State_ShouldPageTheTopRows_WhenItHasAPageSizeForAProvidersTree`
- [x] Filters go to the provider and leave the loaded children as they are, and a row closes under a filter as it does without one; a row the app opens again shows the children it loaded. `TbDataGridProviderTreeTests.Filter_ShouldGoToTheProviderAndKeepTheLoadedChildren_WhenAFilterApplies`, `TbDataGridProviderTreeTests.ExpandedParents_ShouldShowTheLoadedChildren_WhenTheAppOpensARowAgain`
- [x] Leaving the page while children load cancels the load, and so does closing the row from code. `TbDataGridLazyTreeTests.Dispose_ShouldCancelTheLoad_WhenTheGridGoesWhileChildrenLoad`, `TbDataGridLazyTreeTests.ExpandedParents_ShouldCancelTheLoad_WhenTheAppClosesALoadingRow`
- [x] Opening a row from the keyboard or with a helper waits for its children, even when the app's `ExpandedParentsChanged` draws the grid again first, and a row that loads none loses its button. `TbDataGridLazyTreeTests.ExpandAsync_ShouldWaitForTheChildren_WhenTheAppDrawsTheGridAgainFirst`, `TreeGridTests.ExpandAsync_ShouldReturn_WhenALazyRowLoadsNoChildren`

## Touch

- [x] A tap on a row's button opens or closes it, and the button is at least 24 pixels square. `trailblazor.css`
- [x] A tree grid wider than its container fades at its sides as a data grid does. `data-grid.js`

## Focus

- [x] Focus stays on a row's button as the rows under it show or hide. `TreeGridTests.Keys_ShouldOpenAndCloseRowsAndGoToTheirParent_WhenPressedOnTheFirstCell`
- [x] `ScrollToAsync` opens the rows a row is nested under before going to it. `TbDataGridTreeTests.ScrollToAsync_ShouldOpenTheRowsAboveTheRow_WhenTheyAreClosed`
- [x] A row is `aria-busy` while its children load, its button shows a spinner, and closing it cancels the load. `TbDataGridLazyTreeTests.Toggle_ShouldCancelTheLoadAndMarkTheRowBusyMeanwhile_WhenTheRowClosesFirst`
- [x] A load that fails is announced and raised on `OnLoadFailed`, and the row closes, so opening it tries again, also while a filter applies. `TbDataGridLazyTreeTests.Toggle_ShouldCloseTheRowAndSaySo_WhenTheLoadFails`, `TbDataGridLazyTreeTests.Toggle_ShouldCloseTheRow_WhenTheLoadFailsWhileFiltersApply`
- [x] In forced colors, rows, cells and marks look as they do in a data grid, which a tree grid is. `GridSelectTests.SelectedRow_ShouldBeOutlinedInTheHighlight_WhenColorsAreForced`, `GridBatchTests.ChangedCell_ShouldHaveADashedOutline_WhenColorsAreForced`

## Not applicable

- Focus on whole rows, which the pattern allows: the data grid moves between cells, so a tree grid's keys work the same way on its first cell.
- Moving rows between parents: a tree grid's rows can't move; a tree view's drag and drop moves nested items.
- Pages of `Items`: a page could start with a row whose parent is on the page before, so a long tree uses `Virtualize`, or a `DataProvider` that pages its top rows.
