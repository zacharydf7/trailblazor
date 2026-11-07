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

## Pointer

- [x] Clicking a tab selects it and raises `ActiveIndexChanged`. `TbTabsTests.Click_ShouldSelectTheTabAndRaiseActiveIndexChanged_WhenAnotherTabIsClicked`
- [x] Clicking the selected tab keeps it selected and raises its `OnClick` again. `TbTabsTests.Click_ShouldRaiseTheTabsOnClickOnceItIsSelected_WhenItsHeaderIsClickedSelectedOrNot`
- [x] Clicking a disabled tab does nothing. `TbTabs.razor`
- [x] A parent rendering again with the old index doesn't undo a click. `TbTabsTests.Render_ShouldKeepSelectionFromAClick_WhenTheParentRendersAgainWithTheSameIndex`

## Touch

- [x] Tapping a tab selects it, since selection listens for the click. `TbTabs.razor`
- [x] Tabs wider than their row swipe sideways. `trailblazor.css`
- [x] The selected tab scrolls fully into view when the row overflows, without moving the page. That holds on first render, from code and on a click, and in right-to-left text. `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenItStartsOutOfSight`, `TabsOverflowTests.Tabs_ShouldShowTheSelectedTabAndLeaveThePageAlone_WhenSelectedFromCode`, `TabsOverflowTests.Tabs_ShouldShowAllOfATab_WhenAPartlyHiddenOneIsClicked`, `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenTheTextIsRightToLeft`
- [x] The selected tab also scrolls into view when its tabs load after the first render, when it moves as tabs arrive, when its row is hidden until later, and in a `Centered` row. `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenTheTabsLoadLate`, `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenItMovesToItsPlaceInTheRow`, `TabsOverflowTests.Tabs_ShouldShowTheSelectedTab_WhenItsRowIsShownLater`, `TabsOverflowTests.Tabs_ShouldReachTheFirstTab_WhenACenteredRowOverflows`
- [x] A tab wider than its row shows the start of its label, in right-to-left text too. `TabsOverflowTests.Tabs_ShouldShowATabsStart_WhenItIsWiderThanItsRow`, `TabsOverflowTests.Tabs_ShouldShowATabsStart_WhenItIsWiderThanItsRowInRightToLeftText`
- [x] A row the user has scrolled stays put when tabs are added after the selected one, until the selection changes. `TabsOverflowTests.Tabs_ShouldLeaveAScrolledRowAlone_WhenATabIsAdded`, `TabsOverflowTests.Tabs_ShouldShowATabAgain_WhenItIsSelectedAgainAfterNothingWasSelected`

## Focus

- [x] Tab reaches the list once, on the selected tab. `TbTabsTests.Render_ShouldLinkEachTabToItsPanel_WhenCalled`
- [x] With the selected tab disabled, or none selected, Tab reaches the first enabled tab. `TbTabsTests.Render_ShouldMakeTheFirstEnabledTabReachable_WhenTheSelectedTabIsDisabled`
- [x] Tab from the selected tab moves into its panel. `TabsTests.Tabs_ShouldMoveFocusIntoThePanel_WhenTabIsPressedOnTheSelectedTab`
- [x] A focused tab shows a focus ring. `trailblazor.css`
- [x] Focus stays on the same tab when a tab renders before it. `TabsTests.Tabs_ShouldKeepFocusOnTheSameTab_WhenALateTabRendersBeforeIt`
- [ ] When the focused tab goes away, focus moves to the tab selected in its place. #148

## Not applicable

- Delete to close a tab: an optional key in the pattern, and these tabs have no close action of their own.
- Tab leaving the list from an unselected tab, with manual activation: when that tab comes before the selected one, Tab stops on the selected tab first, since that tab holds the list's one tab stop.
