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
- [x] Tab goes from a group's tabs to its menu button, and the menu moves the group's showing panel along its row, to another group, or to a new group beside its own or along an edge of the dock. `DockManagerMoveTests.Dock_ShouldMoveAPanelToAnotherGroupAndKeepTheFocusOnItsTab_WhenItsGroupsMenuIsUsed`, `DockManagerMoveTests.Dock_ShouldMoveATabAlongItsRow_WhenForwardOrBackIsChosen`, `DockManagerMoveTests.Dock_ShouldGiveAPanelANewGroup_WhenASideOfItsGroupOrOfTheDockIsChosen`
- [x] The menu offers only what the panel can do: moving back is disabled for the first tab and forward for the last, and the menu leaves out moving to a group when there is no other, a new group beside its own for a panel alone in its group, a new group at the dock's edge for the only panel docked, and Close for a panel that can't close. `TbDockManagerTests.Menu_ShouldOfferWhatItsGroupsShowingPanelCanDo_WhenRendered`, `TbDockManagerTests.Menu_ShouldHaveNoWayToMove_WhenTheDockHoldsOnePanel`
- [x] A separator's grip stays out of the tab order, since the separator's keys do what its menu does. `TbDockManagerTests.Grip_ShouldStepItsSeparatorThroughTheScript_WhenLargerOrSmallerIsChosen`

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
- [x] A separator moves in steps of 10% from the menu of the grip on its line, for people who can't drag (WCAG 2.5.7), and stops at `MinGroupSize` as its keys do. `DockManagerMoveTests.Dock_ShouldResizeInSteps_WhenASeparatorsGripMenuIsUsed`
- [x] The grip shows while the pointer is on its separator or on the grip, and while its menu is open. `DockManagerMoveTests.Dock_ShouldResizeInSteps_WhenASeparatorsGripMenuIsUsed`, `trailblazor.css`
- [x] A drag that starts on the grip resizes as one on the line does, without opening the menu, and the grip goes with its line while the drag runs. A click on the grip that wanders a few pixels still opens the menu. `DockManagerGripDragTests.Dock_ShouldResizeWithoutOpeningTheMenu_WhenASeparatorsGripIsDragged`, `DockManagerGripDragTests.Dock_ShouldOpenTheGripsMenuWithoutResizing_WhenAClickOnItWandersAFewPixels`
- [ ] Dragging a tab moves its panel to another group, or to an edge to split a group, with a mark where it will land. #75
- [x] Each group has a menu that moves its showing panel with single clicks, for people who can't drag (WCAG 2.5.7), and closes it. `DockManagerMoveTests.Dock_ShouldMoveAPanelToAnotherGroupAndKeepTheFocusOnItsTab_WhenItsGroupsMenuIsUsed`, `DockManagerMoveTests.Dock_ShouldCloseAPanel_WhenCloseIsChosenFromItsGroupsMenu`
- [ ] Double-clicking a tab makes its group fill the dock, and again puts it back. #75

## Touch

- [x] Tapping a tab selects it, since selection listens for the click. `TbDockManager.razor`
- [x] A finger drags a separator, through pointer events, without scrolling the page. `dock-manager.js`
- [x] A close mark is a 24px target. `trailblazor.css`
- [ ] A separator is as easy to catch with a finger as with a mouse. UNCONFIRMED
- [x] Where the main pointer is a finger, as on a phone or a tablet, the grips always show, since nothing hovers there. `trailblazor.css`
- [x] A screen reader user on a touch screen can close a panel from Close in its group's menu, though the close mark is hidden from screen readers and there is no Delete key. `DockManagerMoveTests.Dock_ShouldCloseAPanel_WhenCloseIsChosenFromItsGroupsMenu`

## Focus

- [x] Each group's tabs are one tab stop, on the showing tab; then come the group's menu button, its panel, what the panel holds, and the separator after the group. `DockManagerTests.Dock_ShouldKeepAHiddenPanelOutOfReachAndAtItsScrollPosition_WhenAnotherTabShows`
- [x] Tab and screen readers meet the groups in reading order, and still do after a panel changes group. `DockManagerTests.Dock_ShouldPutEachPanelAfterItsGroupsTabs_WhenAPanelChangesGroup`, `TbDockManagerTests.Render_ShouldPutEachPanelAfterItsGroupsTabsAndASeparatorBetweenGroups_WhenFirstRendered`
- [x] A hidden panel keeps its content, its scroll position and what was typed in it, and nothing in it can be reached. `DockManagerTests.Dock_ShouldKeepAHiddenPanelOutOfReachAndAtItsScrollPosition_WhenAnotherTabShows`, `TbDockManagerTests.Select_ShouldShowTheTabsPanelAndKeepTheOtherAlive_WhenATabIsClicked`
- [x] A panel that changes group keeps its content, its text, its scroll positions and its focus. `DockManagerTests.Dock_ShouldKeepAPanelsTextScrollAndFocus_WhenThePanelChangesGroup`, `TbDockManagerTests.Layout_ShouldLeaveEveryRenderedPartWhereItWasAmongItsSiblings_WhenAPanelChangesGroup`
- [x] Closing a tab, with its keys or from its group's menu, moves focus to the next tab of its group, or else the one before; with the group gone, to the next group's showing tab, or else the previous group's. `DockManagerTests.Dock_ShouldCloseATabAndFocusItsNeighbor_WhenDeleteIsPressed`, `DockManagerMoveTests.Dock_ShouldCloseAPanel_WhenCloseIsChosenFromItsGroupsMenu`, `TbDockManagerTests.Close_ShouldFocusTheNextTabOrElseTheOneBefore_WhenATabOfAGroupCloses`, `TbDockManagerTests.Close_ShouldFocusTheNextGroupsShowingTabOrElseTheOneBefore_WhenTheGroupGoes`
- [x] A move from a group's menu puts focus on the moved panel's tab, and screen readers hear where the panel went, as in "Trails moved to panel group 2." `DockManagerMoveTests.Dock_ShouldMoveAPanelToAnotherGroupAndKeepTheFocusOnItsTab_WhenItsGroupsMenuIsUsed`, `TbDockManagerTests.Menu_ShouldMoveTheTabAlongItsRowAndSayWhereItWent_WhenForwardIsChosen`
- [x] A panel that holds an iframe stays in place in the document while other parts move around it, so the frame keeps what was typed in it, with or without `Element.moveBefore`. `DockManagerTests.Dock_ShouldKeepAPanelsTextScrollAndFocus_WhenThePanelChangesGroup`
- [x] When two panels that hold iframes trade places, one of them has to move, and its frame loads again where the browser has no `Element.moveBefore`. `dock-manager.js`
- [x] Closing the last panel moves focus to the dock itself. `DockManagerTests.Dock_ShouldTakeTheFocusItself_WhenItsLastPanelCloses`, `TbDockManagerTests.Close_ShouldFocusTheDock_WhenItsLastPanelCloses`
- [x] Pressing a separator focuses it, and only keyboard focus highlights it. `dock-manager.js`, `trailblazor.css`
- [x] Screen readers hear each group's tabs as "Panel group" and its number, each separator as "Resize" and the title of the panel showing in the group just before it, with its share in `aria-valuenow`, and how to close a closable tab. `TbDockManagerTests.Render_ShouldGiveEachGroupANamedTabListInItsBox_WhenGivenALayout`, `TbDockManagerTests.Render_ShouldPutANamedSeparatorOnEachSharedEdge_WhenGivenALayout`, `TbDockManagerTests.Render_ShouldNameEachSeparatorByTheGroupJustBeforeIt_WhenASplitComesFirst`, `TbDockManagerTests.Render_ShouldTellScreenReadersHowToCloseATab_WhenItIsClosable`
- [x] Each group's menu button is named "Options for" and the title of the showing panel, and each grip "Size of" and the title that names its separator. `TbDockManagerTests.Menu_ShouldOfferWhatItsGroupsShowingPanelCanDo_WhenRendered`, `TbDockManagerTests.Grip_ShouldStepItsSeparatorThroughTheScript_WhenLargerOrSmallerIsChosen`
- [x] `Labels` replaces those words, the items of both menus and what is said after a move. `TbDockManagerTests.Labels_ShouldWordTheGroupsSeparatorsAndCloseHint_WhenGiven`, `TbDockManagerTests.Labels_ShouldWordTheMenusAndGrips_WhenGiven`, `TbDockManagerTests.Labels_ShouldWordWhatIsSaidAfterAMove_WhenGiven`
- [x] In forced colors, the showing tab and a focused separator are marked in the system's highlight color. `DockManagerResizeTests.Dock_ShouldMarkTheShowingTabAndAFocusedSeparatorInTheHighlight_WhenColorsAreForced`

## Remembering

- [x] With a `PersistenceKey`, the layout is saved in the browser after each change and comes back after a reload, and the app hears it through `LayoutChanged`. `DockManagerMoveTests.Dock_ShouldKeepItsLayout_WhenThePageIsReloadedWithAPersistenceKey`, `TbDockManagerTests.PersistenceKey_ShouldShowTheSavedLayoutAndHandItToTheApp_WhenTheBrowserHoldsOne`
- [x] A layout the app sets later replaces the saved one, so a Reset button works. `DockManagerMoveTests.Dock_ShouldKeepItsLayout_WhenThePageIsReloadedWithAPersistenceKey`, `TbDockManagerTests.PersistenceKey_ShouldSaveTheLayout_WhenTheUserOrTheAppChangesIt`
- [x] Each change is saved once, also when the app binds the layout and hands it back. `TbDockManagerTests.PersistenceKey_ShouldSaveEachChangeOnce_WhenTheLayoutIsBound`
- [x] When the key arrives or changes after the dock first shows, the dock reads and saves the layout under the new key from then on. `TbDockManagerTests.PersistenceKey_ShouldReadAndSaveUnderTheNewKey_WhenTheKeyArrivesOrChangesLater`
- [x] The menus and grips appear once the page is interactive, and a static page shows the tabs, panels and separators without them. `TbDockManagerTests.Render_ShouldLeaveOutTheMenusAndGrips_WhenThePageIsStatic`
- [x] Saved text that isn't a layout is ignored, and the app's layout shows. `TbDockManagerTests.PersistenceKey_ShouldKeepTheAppsLayout_WhenTheBrowserHoldsNoneOrSomethingElse`
- [x] Where storage is turned off or full, the dock works and the layout just isn't kept. `dock-manager.js`
- [x] Without a `PersistenceKey` nothing is saved. `TbDockManagerTests.PersistenceKey_ShouldSaveNothing_WhenNoKeyIsGiven`

## Not applicable

- A prerendered page showing the saved layout from the start: the layout is kept in the browser's storage, which the server can't read while it prerenders, so the app's layout shows until the page is interactive.
- F6 between groups: the pattern makes it optional, and Tab already goes from group to group.
- Ctrl+Tab between tabs: browsers keep it for their own tabs.
- Enter on a separator: no part of a dock collapses; a panel closes from its tab.
- Escape to cancel a separator's drag: familiar docks keep the size where it's released.
- A wheel turned over a row of tabs: the row scrolls with a sideways wheel or swipe, as `TbTabs` does.
