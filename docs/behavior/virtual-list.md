# Virtual list

Pattern: [Feed](https://www.w3.org/WAI/ARIA/apg/patterns/feed/), drawing only the items near the view

Familiar apps: a social feed or news reader that scrolls on and on, a chat history, a long activity log

## Keyboard

- [x] Page Down and Page Up move to the next or previous item, of any height, and bring it into view. `VirtualListTests.VirtualList_ShouldMoveBetweenItemsOfAnyHeight_WhenPageDownAndPageUpArePressed`
- [x] One item holds the tab stop: the one focus last rested on. `VirtualListTests.VirtualList_ShouldMoveBetweenItemsOfAnyHeight_WhenPageDownAndPageUpArePressed`
- [x] Ctrl+End moves to the first control after the list, and Ctrl+Home to the last one before it. `VirtualListTests.VirtualList_ShouldLeaveTheFeed_WhenCtrlEndOrCtrlHomeIsPressed`
- [x] These keys stay with a text field inside an item, and the feed leaves alone the tab stops of a component inside one, such as a toggle group. `VirtualListTests.VirtualList_ShouldLeaveTheKeysAndTabStopsOfItsItemsControls_WhenTheyHaveFocus`
- [x] When the focused item scrolls out and is no longer drawn, focus moves to the list, and Page Down goes on from that item. `VirtualListTests.VirtualList_ShouldKeepTheKeysWorking_WhenTheFocusedItemScrollsOutOfTheList`

## Pointer

- [x] Scrolling draws the items coming into view. `VirtualListTests.VirtualList_ShouldKeepTheKeysWorking_WhenTheFocusedItemScrollsOutOfTheList`
- [x] `ScrollToAsync` draws a far item, scrolls it into view and can focus it. `VirtualListTests.ScrollToAsync_ShouldDrawAndFocusAFarItem_WhenCalled`, `TbVirtualListTests.ScrollToAsync_ShouldAskTheBrowserToScrollAndFocus_WhenCalled`

## Touch

- [ ] Swiping scrolls the list and draws the items coming into view. UNCONFIRMED

## Focus

- [x] Screen readers hear a feed named by its label, and each item as an article with its place and the total, named by `ItemLabel`. `TbVirtualListTests.Render_ShouldBeANamedFeedOfArticlesWithTheirPlaces_WhenGivenItems`
- [x] The feed is busy while `ItemsProvider` loads, and the total comes from the provider. `TbVirtualListTests.Render_ShouldBeBusyWhileItLoadsAndThenShowTheTotal_WhenGivenAProvider`
- [x] Items changed in place show again on the next render. `TbVirtualListTests.Render_ShouldShowTheItemsAgain_WhenTheListChangesInPlace`
- [x] With no items, the empty content shows, in a group named like the list, since a feed holds only articles. `TbVirtualListTests.Render_ShouldShowTheEmptyContent_WhenThereAreNoItems`
- [x] Before the page is interactive, every item in `Items` shows. `VirtualListTests.VirtualList_ShouldShowEveryItem_WhenThePageIsStatic`
- [x] `ScrollToAsync` refuses a place past the last item. `TbVirtualListTests.ScrollToAsync_ShouldThrow_WhenThePlaceIsPastTheLastItem`

## Not applicable

- Choosing items: a feed is for reading; use a list box to choose.
- Arrow keys between items: the feed pattern leaves them to scrolling and to the controls inside each item.
