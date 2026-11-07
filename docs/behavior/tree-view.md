# Tree view

Pattern: [Tree view](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/)

Familiar apps: File Explorer, VS Code's file tree, Finder's list view

## Keyboard

- [x] Down and Up move through the visible rows and stop at the ends. `TreeViewTests.Keys_ShouldMoveOpenAndCloseLikeAWaiAriaTree_WhenPressed`, `tree-view.js`
- [x] Right opens a closed row, then moves to its first child; Left closes an open row, or moves to the parent of a closed one. `TreeViewTests.Keys_ShouldMoveOpenAndCloseLikeAWaiAriaTree_WhenPressed`
- [x] Home and End go to the first and last visible rows, rendered or not. `TreeVirtualizeTests.Tree_ShouldReachRowsThatArentRendered_WhenEndHomeAndTypingMoveFocus`
- [x] Typing moves to the next visible row that starts with the letters, unless `TypeAhead` is off. `TreeVirtualizeTests.Tree_ShouldReachRowsThatArentRendered_WhenEndHomeAndTypingMoveFocus`, `TreeViewTests.Tree_ShouldLeaveALetterToThePage_WhenTypeAheadIsOff`
- [x] `*` opens the row's siblings. `TbTreeViewTests.Keys_ShouldOpenCloseActivateAndCheck_WhenTheBrowserReportsThem`
- [x] Enter selects and activates the row, and opens or closes one with children. `TbTreeViewTests.OnKey_ShouldOpenAndCloseARowWithChildrenAndActivateIt_WhenEnterIsPressed`, `TbTreeView.cs`
- [x] Space selects the row, toggles it in multiple selection, or checks it in a checkable tree. `TbTreeView.cs`, `TreeViewTests.Space_ShouldCheckTheRowAndMarkItsParentMixed_WhenTheTreeIsCheckable`
- [x] With `SelectionFollowsFocus`, the keys select each row they reach, and Ctrl with a key moves focus alone. `TreeViewTests.Tree_ShouldSelectEachRowTheArrowsReachButNotWithCtrl_WhenSelectionFollowsFocus`
- [x] Shift+Up and Shift+Down extend a selection from the row last chosen. `TreeViewTests.ShiftArrows_ShouldExtendTheSelection_WhenTheTreeTakesSeveral`
- [ ] Shift+Down from a row with nothing chosen yet selects that row too, not only the next one. #323
- [ ] Shift+Home and Shift+End extend the selection to the first and last rows. #323
- [x] Shift+Space selects the range from the row last chosen, and Ctrl+A selects every visible row that can be. `TbTreeView.cs`
- [x] Alt+Shift with an arrow moves the focused row and keeps focus on it. `TreeReorderTests.Tree_ShouldMoveTheFocusedRowAndKeepFocusOnIt_WhenAltShiftAndAnArrowArePressed`
- [x] In right-to-left text, the arrows mirror. `TreeReorderTests.Tree_ShouldMirrorTheArrowKeys_WhenItIsLaidOutRightToLeft`
- [x] The arrows reach the failed row a lazy load leaves, and Enter there tries again. `TreeViewTests.Keyboard_ShouldReachAFailedRowRetryItAndFollowTheBranch_WhenALazyLoadFails`
- [x] Pressing the same letter quickly cycles through the rows that start with it, and "aa" still finds a row that starts with that letter doubled, unless the first press landed on it. Typing on, as in "aar", always finds it. All of this holds with or without `Virtualize`. `TypeAheadTests.Tree_ShouldStepThroughTheRowsStartingWithALetter_WhenItIsPressedQuickly`, `TypeAheadTests.Tree_ShouldFindTheRowWithADoubledLetter_WhenOneStartsWithIt`, `TreeVirtualizeTests.OnFind_ShouldStepOrFindTheDoubledLetter_WhenALetterIsRepeated`, `TreeVirtualizeTests.OnFind_ShouldFindTheRowWithADoubledLetter_WhenTypingGoesOn`, `TreeVirtualizeTests.OnFind_ShouldStayOnTheRow_WhenDifferentLettersMatchNothing`

## Pointer

- [x] Clicking a row selects it, and clicking its chevron opens or closes it without selecting. `TreeViewTests.Mouse_ShouldSelectOnClickAndActivateOnDoubleClick_WhenUsed`
- [x] Ctrl+click adds or removes a row, and Shift+click selects a range. `TbTreeViewTests.Click_ShouldAddWithCtrlAndRangeWithShift_WhenTheModeIsMultiple`
- [x] Double-clicking a row with children opens or closes it and activates it, and double-clicking a leaf activates it. `TreeViewTests.Row_ShouldOpenAndCloseAndBeActivated_WhenARowWithChildrenIsDoubleClicked`, `TbTreeViewTests.DoubleClick_ShouldOpenAndActivateARowWithChildrenButOnlyActivateALeaf_WhenEachIsDoubleClicked`
- [ ] Double-clicking or Shift+clicking a row doesn't highlight its text. #323
- [ ] Clicking a parent row that can't be selected opens or closes it, rather than doing nothing. #324
- [x] Clicks and keys in a row's end slot stay with its controls. `TreeViewTests.EndSlot_ShouldKeepItsClicksFromTheRow_WhenAButtonInItIsPressed`, `tree-view.js`
- [x] A drag drops before, after or inside the row under the pointer, scrolling near the edges. `TreeReorderTests.Tree_ShouldMoveARowBeforeAnotherAndSaySo_WhenItIsDraggedOntoTheOthersTopEdge`, `TreeReorderTests.Tree_ShouldMoveARowIntoAnother_WhenItIsDraggedOntoTheOthersMiddle`, `pointer-drag.js`
- [x] A refused drop shows why and moves nothing. `TreeReorderTests.Tree_ShouldShowWhyAndMoveNothing_WhenARowIsDraggedIntoOneThatCantHoldIt`
- [x] Escape cancels a drag. `TreeReorderTests.Tree_ShouldDropNothing_WhenEscapeIsPressedDuringADrag`
- [ ] Holding a drag over a closed row opens it, so a drop can reach the rows inside. #324
- [x] A drag held still near the top or bottom keeps scrolling. `DragScrollTests.Tree_ShouldKeepScrolling_WhenADragIsHeldStillNearTheBottom`, `DragScrollTests.Tree_ShouldKeepScrolling_WhenAFingerIsHeldStillNearTheBottom`

## Touch

- [x] Tapping a row selects it, even on the hidden edge of an insert slot. `TreeReorderTests.Tree_ShouldSelectTheRowAndAddNothing_WhenAHiddenSlotsEdgeIsTappedOnATouchScreen`
- [x] A finger held still starts a drag and a swipe scrolls, through pointer events. `pointer-drag.js`, `TreeReorderTests.Tree_ShouldLetTheNextKeyAndClickThrough_WhenATouchDragEndsWithoutAClick`
- [ ] Double-tapping a row opens and activates it. UNCONFIRMED
- [ ] A long press starts a drag without the system's own menu getting in the way. UNCONFIRMED

## Focus

- [x] The tree is one tab stop that remembers the last row focused or clicked. `TreeViewTests.Tree_ShouldBeOneTabStopThatRemembersTheRow_WhenTabbedThrough`, `TreeViewTests.Tree_ShouldMakeAClickedRowTheTabStop_WhenTheArrowsMovedFocusFirst`
- [x] Before any row has focus, the first selected row is the tab stop. `TbTreeViewTests.Render_ShouldGiveTheFirstSelectedRowTheTabStop_WhenNoRowHasBeenFocused`
- [x] Tab from a row reaches its end slot's controls, and only that row's. `tree-view.js`
- [x] A disabled row can still be focused, and so can the rows of a disabled tree. `TbTreeView.cs`
- [x] In a virtualized tree, the focused row stays rendered when scrolled away. `TreeVirtualizeTests.Tree_ShouldKeepTheFocusedRowAndMoveOnFromIt_WhenTheTreeIsScrolledAway`
- [x] A tab stop stays in the tree when a loading row goes away. `TreeViewTests.Keyboard_ShouldKeepATabStopInTheTree_WhenFocusLeavesALoadingRowBeforeItGoes`
- [x] A focused row shows a focus ring. `trailblazor.css`
- [ ] When the focused row goes away, as when a button in its end slot deletes it, focus moves to the nearest row left. #324

## Not applicable

- Context menus: the tree has no menu of its own, and an app can attach one through a row's template or attributes.
- Page Up and Page Down: they scroll as the browser does, and the pattern doesn't ask for more.
- Plain arrows moving a multiple selection, as in File Explorer: in a multiple-selection tree the arrows only move focus, as in the pattern's recommended model.
