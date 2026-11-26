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
- [x] A `PageProvider` that doesn't count its items loads a page more as the list scrolls near the end, with each item's `aria-setsize` -1 until a page says no more follow and the items are counted, and `ScrollToAsync`, Page Down and the Playwright helper's `ItemAsync` reach as far as a page past the items loaded; `RefreshDataAsync` starts the count again. It takes the place of `Items` and `ItemsProvider`, and only one of the three may be set. `VirtualListTests.PageProvider_ShouldLoadMoreItemsAsTheyScrollIntoView_WhenItDoesntCountThem`, `TbVirtualListTests.PageProvider_ShouldCountTheItems_WhenThePageItAskedForSaysNoMoreFollow`, `TbVirtualListTests.Render_ShouldThrow_WhenGivenItemsAndAPageProvider`, `TbVirtualListTests.RefreshDataAsync_ShouldLeaveTheCountUnknownAgain_WhenThePagesNowSayMoreFollow`, `VirtualListTests.ItemAsync_ShouldReachAnItemPastThoseLoaded_WhenThePageProviderHasntSaidWhereTheyEnd`, `UncountedReachTests.Take_ShouldEndTheItemsWhereAnEmptyPageStarts_WhenThePageBeforeFilledUpToIt`
- [x] The feed is busy while a provider loads, and the total comes from the provider. `TbVirtualListTests.Render_ShouldBeBusyWhileItLoadsAndThenShowTheTotal_WhenGivenAProvider`
- [x] Once the page is interactive, the feed stays busy until its first items are drawn, so it's never empty without being busy. `VirtualListTests.Feed_ShouldBeBusyUntilItsFirstItemsAreDrawn_WhenThePageTurnsInteractive`
- [x] After each provider load, such as a jump to a far item, the feed stays busy until the loaded items replace the placeholders. A load that gives no items ends the wait itself. `VirtualListTests.Feed_ShouldBeBusyUntilTheItemsItJumpedToAreDrawn_WhenAProviderLoadsThem`, `TbVirtualListTests.Render_ShouldStopBeingBusy_WhenAProviderLoadGivesNoItems`
- [x] Items changed in place show again on the next render. `TbVirtualListTests.Render_ShouldShowTheItemsAgain_WhenTheListChangesInPlace`
- [x] With no items, the empty content shows, in a group named like the list, since a feed holds only articles. `TbVirtualListTests.Render_ShouldShowTheEmptyContent_WhenThereAreNoItems`
- [x] Before the page is interactive, every item in `Items` shows. `VirtualListTests.VirtualList_ShouldShowEveryItem_WhenThePageIsStatic`
- [x] `ScrollToAsync` refuses a place past the last item, or with a `PageProvider` whose end isn't known, past a page beyond the items loaded. `TbVirtualListTests.ScrollToAsync_ShouldThrow_WhenThePlaceIsPastTheLastItem`, `TbVirtualListTests.ScrollToAsync_ShouldReachAPagePastTheItemsLoaded_WhenThePageProviderHasntSaidWhereTheyEnd`

## Not applicable

- Choosing items: a feed is for reading; use a list box to choose.
- Arrow keys between items: the feed pattern leaves them to scrolling and to the controls inside each item.
