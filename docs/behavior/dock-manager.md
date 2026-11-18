# Dock manager

Pattern: [Tabs](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/) for each group, and
[Window splitter](https://www.w3.org/WAI/ARIA/apg/patterns/windowsplitter/) for each separator

Familiar apps: VS Code's editor groups and panel, Visual Studio's tool windows, browser developer tools

## Keyboard

- [x] Right moves to the next tab of the group and Left to the previous, wrapping at the ends and never leaving the group; each tab the keys reach shows its panel. `DockManagerTests.Dock_ShouldMoveBetweenAGroupsTabsAndShowEach_WhenTheArrowKeysAreUsed`
- [x] Home and End go to the group's first and last tabs. `DockManagerTests.Dock_ShouldMoveBetweenAGroupsTabsAndShowEach_WhenTheArrowKeysAreUsed`
- [x] In right-to-left text, Left moves forward and Right moves back, as on `TbTabs`, whose key handling the dock shares. `tabs.js`
- [x] Enter and Space on a tab select it, since each tab is a real button. `TbDockManager.razor`
- [x] Delete or Backspace closes a focused tab that can close, and the tab names both keys in `aria-keyshortcuts`. `DockManagerTests.Dock_ShouldCloseATabAndFocusItsNeighbor_WhenDeleteIsPressed`, `TbDockManagerTests.Render_ShouldTellScreenReadersHowToCloseATab_WhenItIsClosable`
- [x] Delete does nothing on a tab with `Closable="false"`. `DockManagerTests.Dock_ShouldKeepATabOpen_WhenItIsNotClosable`, `TbDockManagerTests.Close_ShouldLeaveAPanelOpen_WhenItIsNotClosable`
- [x] A held Delete closes only the one tab. `dock-manager.js`
- [x] Left and Right move a separator between side-by-side parts by 1% of the two parts' space, and Shift with them by 10%, also where the two parts take only part of the dock. `DockManagerResizeTests.Dock_ShouldResizeWithinTheSmallestGroupSize_WhenASeparatorsKeysAreUsed`, `DockManagerDriverTests.Dock_ShouldMoveASeparatorByAPercentOfItsTwoSides_WhenTheyTakeOnlyPartOfTheDock`
- [x] Up and Down move a separator between stacked parts, and Left and Right do nothing on it. `DockManagerResizeTests.Dock_ShouldResizeWithUpAndDown_WhenTheSeparatorLiesBetweenStackedGroups`
- [x] Home and End take the part before the separator to its smallest and largest, where no group is under `MinGroupSize`. `DockManagerResizeTests.Dock_ShouldResizeWithinTheSmallestGroupSize_WhenASeparatorsKeysAreUsed`
- [x] Every key press counts, even when presses come faster than the server takes the sizes. `DockManagerResizeTests.Dock_ShouldHandTheAppTheNewSizes_WhenASeparatorsKeysAreUsed`
- [x] In right-to-left text, Left grows the first part, now on the right. `DockManagerResizeTests.Dock_ShouldGrowTheFirstGroupWithTheLeftArrow_WhenTheTextIsRightToLeft`
- [x] Alt, Ctrl or Cmd with these keys stays with the browser. `DockManagerResizeTests.Dock_ShouldResizeWithUpAndDown_WhenTheSeparatorLiesBetweenStackedGroups`
- [x] Keys pressed in tabs inside a panel stay with those tabs. `DockManagerTests.Dock_ShouldLeaveTheKeysToTabsInsideAPanel_WhenTheyAreUsedThere`
- [ ] A panel moves to another group, or to a new group beside one, from the keyboard. #75

## Pointer

- [x] Clicking a tab shows its panel and raises `LayoutChanged`; clicking the showing tab raises nothing. `TbDockManagerTests.Select_ShouldShowTheTabsPanelAndKeepTheOtherAlive_WhenATabIsClicked`, `TbDockManagerTests.Select_ShouldRaiseNothing_WhenTheShowingTabIsClicked`
- [x] Clicking a tab's close mark closes the panel without showing it first. `DockManagerTests.Dock_ShouldCloseATab_WhenItIsMiddleClickedOrItsMarkIsClicked`, `TbDockManagerTests.Close_ShouldRemoveTheTabAndDisposeItsContent_WhenItsMarkIsClicked`
- [x] A middle click closes a tab that can close, as in a browser. `DockManagerTests.Dock_ShouldCloseATab_WhenItIsMiddleClickedOrItsMarkIsClicked`
- [x] A group whose last panel closes gives its space to its neighbors. `TbDockManagerTests.Close_ShouldGiveTheGroupsSpaceToTheOthers_WhenItsLastPanelCloses`
- [x] Dragging a separator resizes everything on both sides at once, groups inside other splits too, and the app gets the sizes on release. `DockManagerDragTests.Dock_ShouldShowTheNewSizesAtOnceAndReportThemOnRelease_WhenASeparatorIsDragged`
- [x] A drag stops where a group would get smaller than `MinGroupSize`. `DockManagerDragTests.Dock_ShouldStopAtTheSmallestGroupSize_WhenASeparatorIsDraggedPastIt`
- [x] In right-to-left text a drag follows the pointer from the right. `DockManagerDragTests.Dock_ShouldFollowThePointerFromTheRight_WhenASeparatorIsDraggedInRightToLeftText`
- [x] A separator moved before the app has rendered another one's move starts from where things now are. `DockManagerDragTests.Dock_ShouldMoveAnotherSeparatorFromWhereADragHasPutThings_WhenTheAppHasNotRenderedYet`
- [x] A drag across a panel's iframe isn't lost to it. `DockManagerDragTests.Dock_ShouldShowTheNewSizesAtOnceAndReportThemOnRelease_WhenASeparatorIsDragged`
- [x] Hover highlights a separator's line and shows a resize cursor. `trailblazor.css`
- [x] A tab too long for its row is cut short with an ellipsis, and a row with too many tabs scrolls sideways. `trailblazor.css`
- [x] The showing tab scrolls into view when its row overflows, without moving the page, also when it joins a group as its showing tab. `DockManagerTests.Dock_ShouldBringTheShowingTabIntoView_WhenItsRowOverflows`, `DockManagerTests.Dock_ShouldBringATabIntoView_WhenItJoinsAGroupAsItsShowingTab`
- [x] A middle click on a tab, or a click on its close mark, leaves the focus where it is, unless the tab or its panel holds it. `DockManagerTests.Dock_ShouldLeaveTheFocusInAField_WhenAnotherPanelsTabIsMiddleClicked`, `DockManagerTests.Dock_ShouldLeaveTheFocusInAField_WhenAnotherPanelsCloseMarkIsClicked`, `TbDockManagerTests.Close_ShouldLeaveTheFocusWhereItIs_WhenNeitherTheTabNorItsPanelHeldIt`, `TbDockManagerTests.Close_ShouldMoveTheFocus_WhenTheMarkIsClickedAndTheScriptSaysTheTabHoldsIt`
- [ ] A separator moves in steps from a click or a tap, for people who can't drag (WCAG 2.5.7). #75
- [ ] Dragging a tab moves its panel to another group, or to an edge to split a group, with a mark where it will land. #75
- [ ] A tab has a menu with what dragging does, for people who can't drag (WCAG 2.5.7). #75
- [ ] Double-clicking a tab makes its group fill the dock, and again puts it back. #75

## Touch

- [x] Tapping a tab selects it, since selection listens for the click. `TbDockManager.razor`
- [x] A finger drags a separator, through pointer events, without scrolling the page. `dock-manager.js`
- [x] A close mark is a 24px target. `trailblazor.css`
- [ ] A separator is as easy to catch with a finger as with a mouse. UNCONFIRMED
- [ ] A screen reader user on a touch screen can close a panel, though the close mark is hidden from screen readers and there is no Delete key. #75

## Focus

- [x] Each group's tabs are one tab stop, on the showing tab; then come its panel, what the panel holds, and the separator after the group. `DockManagerTests.Dock_ShouldKeepAHiddenPanelOutOfReachAndAtItsScrollPosition_WhenAnotherTabShows`
- [x] Tab and screen readers meet the groups in reading order, and still do after a panel changes group. `DockManagerTests.Dock_ShouldPutEachPanelAfterItsGroupsTabs_WhenAPanelChangesGroup`, `TbDockManagerTests.Render_ShouldPutEachPanelAfterItsGroupsTabsAndASeparatorBetweenGroups_WhenFirstRendered`
- [x] A hidden panel keeps its content, its scroll position and what was typed in it, and nothing in it can be reached. `DockManagerTests.Dock_ShouldKeepAHiddenPanelOutOfReachAndAtItsScrollPosition_WhenAnotherTabShows`, `TbDockManagerTests.Select_ShouldShowTheTabsPanelAndKeepTheOtherAlive_WhenATabIsClicked`
- [x] A panel that changes group keeps its content, its text, its scroll positions and its focus. `DockManagerTests.Dock_ShouldKeepAPanelsTextScrollAndFocus_WhenThePanelChangesGroup`, `TbDockManagerTests.Layout_ShouldLeaveEveryRenderedPartWhereItWasAmongItsSiblings_WhenAPanelChangesGroup`
- [x] Closing a tab moves focus to the next tab of its group, or else the one before; with the group gone, to the next group's showing tab, or else the previous group's. `DockManagerTests.Dock_ShouldCloseATabAndFocusItsNeighbor_WhenDeleteIsPressed`, `TbDockManagerTests.Close_ShouldFocusTheNextTabOrElseTheOneBefore_WhenATabOfAGroupCloses`, `TbDockManagerTests.Close_ShouldFocusTheNextGroupsShowingTabOrElseTheOneBefore_WhenTheGroupGoes`
- [x] Closing the last panel moves focus to the dock itself. `DockManagerTests.Dock_ShouldTakeTheFocusItself_WhenItsLastPanelCloses`, `TbDockManagerTests.Close_ShouldFocusTheDock_WhenItsLastPanelCloses`
- [x] Pressing a separator focuses it, and only keyboard focus highlights it. `dock-manager.js`, `trailblazor.css`
- [x] Screen readers hear each group's tabs as "Panel group" and its number, each separator as "Resize" and the title of the panel showing in the group just before it, with its share in `aria-valuenow`, and how to close a closable tab. `TbDockManagerTests.Render_ShouldGiveEachGroupANamedTabListInItsBox_WhenGivenALayout`, `TbDockManagerTests.Render_ShouldPutANamedSeparatorOnEachSharedEdge_WhenGivenALayout`, `TbDockManagerTests.Render_ShouldNameEachSeparatorByTheGroupJustBeforeIt_WhenASplitComesFirst`, `TbDockManagerTests.Render_ShouldTellScreenReadersHowToCloseATab_WhenItIsClosable`
- [x] `Labels` replaces those words. `TbDockManagerTests.Labels_ShouldWordTheGroupsSeparatorsAndCloseHint_WhenGiven`
- [x] In forced colors, the showing tab and a focused separator are marked in the system's highlight color. `DockManagerResizeTests.Dock_ShouldMarkTheShowingTabAndAFocusedSeparatorInTheHighlight_WhenColorsAreForced`

## Not applicable

- F6 between groups: the pattern makes it optional, and Tab already goes from group to group.
- Ctrl+Tab between tabs: browsers keep it for their own tabs.
- Enter on a separator: no part of a dock collapses; a panel closes from its tab.
- Escape to cancel a separator's drag: familiar docks keep the size where it's released.
- A wheel turned over a row of tabs: the row scrolls with a sideways wheel or swipe, as `TbTabs` does.
