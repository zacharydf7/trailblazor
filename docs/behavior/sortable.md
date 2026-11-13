# Sortable

Pattern: none for reordering; each handle is a [menu button](https://www.w3.org/WAI/ARIA/apg/patterns/menu-button/) whose arrow keys also move its item

Familiar apps: Trello's lists, Jira's backlog, a mail app's filter rules

## Keyboard

- [x] Up and Down on a handle move the item one place. `SortableTests.Keys_ShouldMoveTheItemAndKeepFocusOnItsHandle_WhenPressedOnTheHandle`
- [x] Home and End on a handle move the item to the start and the end. `TbSortableTests.Keys_ShouldMoveTheItemAndSaySo_WhenPressedOnItsHandle`
- [x] A move key at the end of the list does nothing and says nothing. `ItemMover.cs`
- [x] Keys pressed quickly in a row each move the same item, before the last move shows. `TbSortableTests.Keys_ShouldMoveTheItemTheyWerePressedOn_WhenTheyArriveBeforeTheLastMoveShows`
- [x] A move key pressed while an async `CanMove` or `OnMove` runs is ignored, and a move isn't made when the list changes, shrinks or is replaced while `CanMove` runs and the move no longer fits it. `TbSortableTests.Keys_ShouldAskAndMoveOnce_WhenPressedAgainWhileCanMoveRuns`, `TbSortableTests.Keys_ShouldLeaveTheList_WhenItChangesWhileCanMoveRuns`, `TbSortableTests.Keys_ShouldLeaveTheList_WhenItsEndIsRemovedWhileCanMoveRuns`, `TbSortableTests.Keys_ShouldLeaveTheList_WhenTheParentReplacesItWhileCanMoveRuns`
- [x] Up and Down with Alt, Ctrl, Shift or Cmd leave a handle's menu closed, as its other move keys do. `SortableTests.Keys_ShouldLeaveTheHandlesMenuClosed_WhenUpOrDownIsPressedWithAModifier`
- [x] A refused move leaves the order and reads out why. `ItemMover.cs`
- [x] Enter or Space on a handle opens its menu of moves, for screen readers that keep the arrow keys. `MoveHandle.cs`
- [x] A list inside an item moves only its own items. `SortableTests.Keys_ShouldMoveOnlyTheInnerItem_WhenPressedOnAListInsideAGridRow`
- [x] A disabled list's handles are off and no key moves anything. `TbSortableTests.Moves_ShouldBeIgnored_WhenDisabled`
- [x] In a list with a `Group`, Left and Right on a handle move the item to the same place in the list before or after, or to the end of a shorter one, and focus follows it; at the first or last list they do nothing. `SortableBoardTests.Keys_ShouldMoveTheItemToTheListBesideAndKeepFocusOnItsHandle_WhenLeftOrRightIsPressed`
- [x] In right-to-left text, Left moves the item to the next list and Right to the one before. `SortableBoardTests.Keys_ShouldMoveTheItemTheOtherWay_WhenThePageIsRightToLeft`
- [x] The handle's menu offers each other list of the group, and choosing one moves the item to the end of that list. `TbSortableGroupTests.Menu_ShouldOfferEachOtherListAndMoveTheItemToItsEnd_WhenOneIsChosen`
- [x] The list an item goes to answers `CanMove`, and a disabled list takes nothing. `TbSortableGroupTests.Drop_ShouldAskTheListItGoesToAndSayWhy_WhenThatListRefuses`, `TbSortableGroupTests.Drop_ShouldBeRefused_WhenTheListItGoesToIsDisabled`

## Pointer

- [x] Dragging a handle a few pixels starts a drag, and a line marks where the item will land. `SortableTests.Handle_ShouldMoveTheItemAfterTheOneItIsDroppedOn_WhenDraggedPastItsMiddle`
- [x] A place the item may not go shows why, and a drop there changes nothing. `SortableTests.Handle_ShouldShowWhyAndLeaveTheOrder_WhenDraggedWhereTheItemMayNotGo`
- [x] Letting go outside the list changes nothing. `sortable.js`
- [x] Dragged over another list of its group, the item lands before or after the item under the pointer, or at the nearest gap between that list's items; a line marks the spot, or an outline marks the list when it's empty. `SortableBoardTests.Handle_ShouldMoveTheItemIntoAnotherListBeforeTheItemDroppedOn_WhenDraggedThere`, `SortableBoardTests.Handle_ShouldMoveTheItemIntoAnEmptyList_WhenDraggedOntoIt`
- [x] A list of the group that refuses the item shows why beside the drop, and a drop there changes neither list. `SortableBoardTests.Handle_ShouldShowWhyAndLeaveBothLists_WhenTheListItGoesToRefuses`
- [x] Escape during a drag cancels it. `sortable.js`
- [x] A click on a handle opens its menu, but a drag's release doesn't. `sortable.js`
- [x] The menu moves the item in one click, and offers no move that goes nowhere. `SortableTests.Menu_ShouldMoveTheItemWithOneClick_WhenAMoveIsChosenFromItsHandle`
- [x] Only the primary button starts a drag. `pointer-drag.js`
- [x] A drag near the top or bottom of the scrolling area scrolls it. `pointer-drag.js`
- [x] A drag held still near the top or bottom keeps scrolling until the pointer leaves the edge or the drag ends, and a drop lands at the row the scrolling brought under the pointer. `DragScrollTests.Sortable_ShouldKeepScrolling_WhenADragIsHeldStillNearTheBottom`, `DragScrollTests.Sortable_ShouldKeepScrollingUp_WhenADragIsHeldStillNearTheTop`, `DragScrollTests.Sortable_ShouldStopScrolling_WhenThePointerLeavesTheEdge`, `DragScrollTests.Sortable_ShouldStopScrolling_WhenTheDragEnds`, `DragScrollTests.Sortable_ShouldDropWhereTheScrollingBroughtIt_WhenReleasedAfterHoldingStill`, `DragScrollTests.Sortable_ShouldDropWhereTheScrollingBroughtIt_WhenTheBoxScrollsSmoothly`
- [x] Losing the window cancels the drag, since its release would never arrive. `DragScrollTests.Sortable_ShouldCancelTheDrag_WhenTheWindowLosesFocus`
- [x] A box that only starts to overflow during the drag scrolls too. `DragScrollTests.Sortable_ShouldScrollABox_WhenItStartsToOverflowDuringTheDrag`

## Touch

- [x] A finger held on a handle for 300 ms starts a drag, through pointer events, and the page doesn't scroll under it. `pointer-drag.js`
- [x] A tap on a handle opens its menu, which moves the item with no drag. `MoveHandle.cs`
- [x] A swipe on an item's content scrolls the page, since only the handle sets `touch-action: none`. `trailblazor.css`

## Focus

- [x] Focus stays on the handle after a key moves its item. `SortableTests.Keys_ShouldMoveTheItemAndKeepFocusOnItsHandle_WhenPressedOnTheHandle`
- [x] Focus goes back to the handle after a move chosen from its menu with the keyboard. `TbSortable.cs`
- [x] Each handle is named for its item, and says how the keys work. `TbSortableTests.Render_ShouldNameTheListAndEachHandle_WhenGivenItems`
- [x] Screen readers hear where the item landed. `TbSortableTests.Keys_ShouldMoveTheItemAndSaySo_WhenPressedOnItsHandle`
- [x] After a move to another list, screen readers hear that list's name with the item's place there. `TbSortableGroupTests.ListKey_ShouldMoveTheItemToTheSamePlaceInTheListBesideAndSaySo_WhenPressed`
- [x] `Labels` replaces the handle's name and description, its menu's items and the announcement after a move. `TbSortableTests.Labels_ShouldWordTheHandlesTheirMenuAndTheMove_WhenGiven`

## Not applicable

- Pressing Space to lift an item before moving it, as some drag libraries do: the arrows move it at once, so there is no mode to enter or leave.
- Dragging from anywhere on an item: only the handle starts a drag, so an item's text and controls keep their clicks.
- A copy of the item following the pointer: the item dims in place and a line marks the drop.
- Left and Right in a list without a `Group`: the list is vertical, and there's no other list to move to.
