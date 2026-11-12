# Tabs

Pattern: [Tabs](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/)

Familiar apps: Windows property dialogs, browser developer tools, VS Code's settings

## Keyboard

- [x] Right moves to the next enabled tab and Left to the previous, wrapping at the ends and skipping disabled tabs. `TabsTests.Tabs_ShouldMoveFocusAndSelectSkippingDisabledTabs_WhenArrowKeysArePressed`
- [x] Home and End go to the first and last enabled tabs. `TabsTests.Tabs_ShouldMoveFocusAndSelectSkippingDisabledTabs_WhenArrowKeysArePressed`
- [x] With automatic activation, the tab the keys reach is selected and shows its panel. `TabsTests.Tabs_ShouldMoveFocusAndSelectSkippingDisabledTabs_WhenArrowKeysArePressed`
- [x] With manual activation, the keys only move focus, and Enter selects the focused tab. `TabsTests.Tabs_ShouldOnlyMoveFocusUntilEnter_WhenActivationIsManual`
- [x] Space selects the focused tab as Enter does, since each tab is a real button. `TbTabs.razor`
- [x] A vertical list moves with Up and Down, and leaves Left and Right alone. `TabsTests.Tabs_ShouldMoveWithUpAndDownButNotLeftAndRight_WhenVertical`
- [x] In right-to-left text, Left moves forward and Right moves back. `TabsTests.Tabs_ShouldMoveForwardWithTheLeftArrow_WhenThePageIsRightToLeft`
- [x] The arrows select without raising the tab's `OnClick`, while Enter raises it. `TabsTests.Tab_ShouldRaiseItsOnClickForClicksButNotArrowKeys_WhenItsHeaderIsClickedEvenWhileSelected`
- [x] Alt, Ctrl or Cmd with these keys stays with the browser, so Alt+Left still goes back. `TabsTests.Tabs_ShouldLeaveTheKeysToTheBrowser_WhenAltCtrlOrCmdIsHeld`
- [x] Delete or Backspace closes a focused tab that has an `OnClose`, but Ctrl+Delete doesn't; the tab names both keys in `aria-keyshortcuts`. `TabsTests.Tabs_ShouldCloseTheFocusedTabAndFocusTheNext_WhenDeleteOrBackspaceIsPressedAlone`, `TbTabsTests.Delete_ShouldRaiseOnCloseOnlyForAClosableTab_WhenTheScriptReportsIt`, `TbTabsTests.Render_ShouldShowACloseMarkAndNameItsKeys_WhenATabCanClose`

## Pointer

- [x] Clicking a tab selects it and raises `ActiveIndexChanged`. `TbTabsTests.Click_ShouldSelectTheTabAndRaiseActiveIndexChanged_WhenAnotherTabIsClicked`
- [x] Clicking the selected tab keeps it selected and raises its `OnClick` again. `TbTabsTests.Click_ShouldRaiseTheTabsOnClickOnceItIsSelected_WhenItsHeaderIsClickedSelectedOrNot`
- [x] Clicking a disabled tab does nothing. `TbTabs.razor`
- [x] With `OnAdd`, an add button ends the row, outside the tab list, and clicking it raises `OnAdd`. It takes its name from `Labels.Add`, or "Add tab", and isn't shown on a static page. `TbTabsTests.Render_ShouldEndTheRowWithAnAddButtonThatRaisesOnAdd_WhenGivenOnAdd`, `TbTabsTests.Render_ShouldNameTheAddButtonByTheLabels_WhenGivenOne`, `TbTabsTests.Render_ShouldShowNoAddButton_WhenNoOnAddIsGivenOrThePageIsStatic`, `NavigationExampleTests.TabsAddExample_ShouldAddADayAndShowIt_WhenTheAddButtonIsClicked`
- [x] The add button stays in view at the row's end while a long list scrolls, and sits under the tabs in a vertical list, even with no panel beside them. `TabsTests.Tabs_ShouldKeepTheAddButtonInViewAtTheRowsEnd_WhenTheListScrolls`, `TabsTests.Tabs_ShouldPutTheAddButtonUnderTheTabs_WhenTheListIsVertical`, `TabsTests.Tabs_ShouldDrawNoLineUnderTheAddButton_WhenTheTabsArePills`
- [x] Clicking a tab's close mark raises `OnClose` without selecting the tab. Only a tab with an `OnClose` shows the mark, and a disabled tab, or one on a static page, shows none. `TabsTests.Tabs_ShouldCloseATabWithoutSelectingIt_WhenItsCloseMarkIsClicked`, `TbTabsTests.Close_ShouldRaiseOnCloseWithoutSelectingTheTab_WhenItsMarkIsClicked`, `TbTabsTests.Render_ShouldShowNoCloseMark_WhenThePageIsStatic`
- [x] While an async `OnClose` runs, another click on the close mark doesn't ask again, and a held Delete closes only the one tab. `TbTabsTests.Close_ShouldAskOnce_WhenTheMarkIsClickedAgainWhileOnCloseRuns`, `TabsTests.Tabs_ShouldCloseOneTab_WhenDeleteIsHeld`
- [x] A parent rendering again with the old index doesn't undo a click. `TbTabsTests.Render_ShouldKeepSelectionFromAClick_WhenTheParentRendersAgainWithTheSameIndex`
- [x] Before the page is interactive, every panel renders, and a click, or an arrow key with automatic activation, still selects a tab and shows its panel. Tab from the selected tab reaches only its panel. `TbTabsTests.Render_ShouldRenderEveryPanelHiddenUntilFound_WhenThePageIsStatic`, `TabsTests.Tabs_ShouldSwitchByClickOrArrowKey_WhenThePageIsStatic`, `TabsTests.Tabs_ShouldTabFromThePanelOutOfTheTabs_WhenThePageIsStatic`
- [x] Before the page is interactive, the browser's find selects the tab whose panel holds a match. `TabsTests.Tabs_ShouldSelectTheTabHoldingTheText_WhenTheBrowserFindsItOnAStaticPage`

## Touch

- [x] Tapping a tab selects it, since selection listens for the click. `TbTabs.razor`
- [x] A close mark is a 24px target that leaves its tab as tall as the others. `TabsTests.Tabs_ShouldCloseATabWithoutSelectingIt_WhenItsCloseMarkIsClicked`
- [x] Each `Size` steps the tabs' height with density, and a small tab stays at least 24px high. `SizeTests.Size_ShouldStepThePartsHeightWithDensity_WhenSmallMediumOrLarge`
- [x] Tabs wider than their row swipe sideways. `trailblazor.css`
- [x] The selected tab scrolls fully into view when the row overflows, without moving the page. That holds on first render, from code and on a click, and in right-to-left text. `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenItStartsOutOfSight`, `TabsOverflowTests.Tabs_ShouldShowTheSelectedTabAndLeaveThePageAlone_WhenSelectedFromCode`, `TabsOverflowTests.Tabs_ShouldShowAllOfATab_WhenAPartlyHiddenOneIsClicked`, `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenTheTextIsRightToLeft`
- [x] The selected tab also scrolls into view when its tabs load after the first render, when it moves as tabs arrive, when its row is hidden until later, and in a `Centered` row. `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenTheTabsLoadLate`, `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenItMovesToItsPlaceInTheRow`, `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenItsRowIsShownLater`, `TabsOverflowTests.Tabs_ShouldReachTheFirstTab_WhenACenteredRowOverflows`
- [x] A tab wider than its row shows the start of its label, in right-to-left text too. `TabsOverflowTests.Tabs_ShouldShowATabsStart_WhenItIsWiderThanItsRow`, `TabsOverflowTests.Tabs_ShouldShowATabsStart_WhenItIsWiderThanItsRowInRightToLeftText`
- [x] With `ScrollButtons`, a button at each end of an overflowing row scrolls it by most of its width, turns off at its own end, works in right-to-left text, and leaves focus where it was. They hide while the row fits and show once it overflows, including when they're turned on or the row grows too long later. They sit before the add button, and a vertical list or a static page has none. `TabsTests.Tabs_ShouldScrollTheRowAndTurnEachButtonOffAtItsEnd_WhenItsScrollButtonsAreClicked`, `TabsTests.Tabs_ShouldHideTheScrollButtons_WhenTheRowFits`, `TabsTests.Tabs_ShouldShowTheScrollButtons_WhenTurnedOnLaterOrTheRowStartsToOverflow`, `TabsTests.Tabs_ShouldKeepTheScrollButtonsAtTheRowsEndsBeforeTheAddButton_WhenBothShow`, `TbTabsTests.Render_ShouldPutAHiddenScrollButtonAtEachEndOfTheRow_WhenScrollButtonsIsSet`, `TbTabsTests.Render_ShouldPutNoScrollButtons_WhenTheListIsVerticalOrThePageIsStatic`
- [x] A row the user has scrolled stays put when tabs are added after the selected one, until the selection changes. `TabsOverflowTests.Tabs_ShouldLeaveAScrolledRowAlone_WhenATabIsAdded`, `TabsOverflowTests.Tabs_ShouldShowATabAgain_WhenItIsSelectedAgainAfterNothingWasSelected`

## Focus

- [x] Tab reaches the list once, on the selected tab. `TbTabsTests.Render_ShouldLinkEachTabToItsPanel_WhenCalled`
- [x] With the selected tab disabled, or none selected, Tab reaches the first enabled tab. `TbTabsTests.Render_ShouldMakeTheFirstEnabledTabReachable_WhenTheSelectedTabIsDisabled`
- [x] Tab from the selected tab moves into its panel. `TabsTests.Tabs_ShouldMoveFocusIntoThePanel_WhenTabIsPressedOnTheSelectedTab`
- [x] A focused tab shows a focus ring. `trailblazor.css`
- [x] A tab that can close is described by "Press Delete to close", or `Labels.CloseHint`, since nothing else tells a screen reader user how. A screen reader on a touch screen has no Delete key, so an app with closable tabs should offer another way to close them, such as a close button in the panel. `TbTabsTests.Render_ShouldDescribeAClosableTabByHowToCloseIt_WhenATabCanClose`, `TbTabsTests.Labels_ShouldWordTheCloseHint_WhenGiven`
- [x] Focus stays on the same tab when a tab renders before it. `TabsTests.Tabs_ShouldKeepFocusOnTheSameTab_WhenALateTabRendersBeforeIt`
- [ ] When the focused tab goes away, focus moves to the tab selected in its place. #148
- [x] Once a closed tab is removed, focus moves to the next enabled tab, or to the one before it when no enabled tab follows. A tab the parent keeps open leaves focus alone, and so does one it removes later after focus has left it. `NavigationExampleTests.TabsClosableExample_ShouldRemoveTheTabAndFocusTheNextOne_WhenATabIsClosed`, `NavigationExampleTests.TabsClosableExample_ShouldFocusTheTabBefore_WhenTheLastTabIsClosed`, `TbTabsTests.Close_ShouldLeaveFocusAlone_WhenTheParentKeepsTheTabAndRemovesItLater`, `TbTabsTests.Close_ShouldFocusTheEnabledTabBefore_WhenTheNextTabIsDisabled`, `TbTabsTests.Close_ShouldFocusTheNextTab_WhenOnCloseRemovesTheTabAfterAwaiting`
- [x] A focused tab the parent removes by any other route, such as a key of its own, also hands focus to the next enabled tab, or to the one before it when no enabled tab follows. A tab removed with it, or a disabled one, is passed over. Removing a tab after focus has already left it, for a field or the page, leaves focus where it is. `TabsRemoveTests.Tabs_ShouldFocusTheNextTab_WhenTheParentRemovesTheFocusedOne`, `TabsRemoveTests.Tabs_ShouldFocusTheNextTab_WhenTheParentRemovesTheFocusedOneAfterAFocusoutThatKeptFocus`, `TabsRemoveTests.Tabs_ShouldPassOverTabsRemovedWithItAndDisabledOnes_WhenTheParentRemovesTheFocusedOne`, `TabsRemoveTests.Tabs_ShouldFocusTheEnabledTabBefore_WhenTheParentRemovesTheFocusedLastOne`, `TabsRemoveTests.Tabs_ShouldLeaveFocusAlone_WhenTheParentRemovesATabAfterFocusLeftItForAField`, `TabsRemoveTests.Tabs_ShouldLeaveFocusOnThePage_WhenTheParentRemovesATabAfterFocusLeftItForThePage`

## Not applicable

- Tab leaving the list from an unselected tab, with manual activation: when that tab comes before the selected one, Tab stops on the selected tab first, since that tab holds the list's one tab stop.
