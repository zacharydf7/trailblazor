# Transfer list

Pattern: [Listbox](https://www.w3.org/WAI/ARIA/apg/patterns/listbox/), two of them with buttons between

Familiar apps: the "Customize toolbar" and "Choose columns" dialogs in desktop apps

## Keyboard

- [x] In either list, the arrows move through the items and Space marks the active one, as in a multiple list box. `TransferListTests.TransferList_ShouldMoveTheMarkedItem_WhenItsMarkedAndMovedWithTheKeys`
- [x] Tab goes from a list to its move buttons, and Enter on a button moves the marked items. `TransferListTests.TransferList_ShouldMoveTheMarkedItem_WhenItsMarkedAndMovedWithTheKeys`
- [x] A move button with nothing to move is turned off but stays in the tab order. `TbTransferListTests.Render_ShouldListTheItemsNotChosenAndTheChosenOnes_WhenGivenAValue`

## Pointer

- [x] Clicking items marks them, and a move button moves them; the chosen items keep the order of `Items`. `TbTransferListTests.MoveAsync_ShouldMoveTheMarkedItemsKeepingTheItemsOrderAndSaySo_WhenCalled`, `TransferListTests.MoveAsync_ShouldMoveTheItemsAndWaitForThem_WhenCalled`
- [x] Moving every item moves only the ones a filter shows. `TransferListTests.TransferList_ShouldMoveOnlyTheItemsShown_WhenTheListIsFilteredAndAllAreMoved`, `TbTransferListTests.MoveAll_ShouldMoveOnlyTheItemsTheFilterShows_WhenTheListIsFiltered`
- [x] Double-clicking an item moves it to the other list, unless the transfer list is read-only; like any move, it clears the marks, and the moved item doesn't stay the active option in the list it left. `TbTransferListTests.Option_ShouldMoveToTheOtherList_WhenDoubleClicked`, `TransferListTests.Option_ShouldMoveToTheOtherList_WhenDoubleClicked`, `TbTransferListTests.Option_ShouldStayInItsList_WhenDoubleClickedInAReadOnlyList`, `TransferListTests.List_ShouldHaveNoActiveOption_WhenItsLastItemIsDoubleClickedAway`

## Touch

- [x] A tap marks an item and a tap on a button moves it, through the same click events. `TbTransferList.razor`

## Focus

- [x] After a move, focus stays on the button that made it, though that button is now turned off. `TransferListTests.TransferList_ShouldMoveTheMarkedItem_WhenItsMarkedAndMovedWithTheKeys`

## Screen readers

- [x] The lists are named by their titles and the buttons by what they do and where to, such as "Move to Chosen". `TbTransferListTests.Render_ShouldListTheItemsNotChosenAndTheChosenOnes_WhenGivenAValue`
- [x] A move is read out with how many items moved and to which list. `TbTransferListTests.MoveAsync_ShouldMoveTheMarkedItemsKeepingTheItemsOrderAndSaySo_WhenCalled`

## States

- [x] Right to left, the lists swap sides and the move arrows turn around to point at their lists. `TransferListTests.TransferList_ShouldTurnItsArrowsAround_WhenTheTextIsRightToLeft`
- [x] Disabled or read-only, no button moves anything. `TbTransferListTests.Render_ShouldOfferNoMoves_WhenDisabledOrReadOnly`
- [x] A chosen item loaded again matches its item through `Comparer`. `TbTransferListTests.Value_ShouldMatchItsItems_WhenTheComparerSaysTheyAreTheSame`
