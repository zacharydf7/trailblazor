# Splitter

Pattern: [Window splitter](https://www.w3.org/WAI/ARIA/apg/patterns/windowsplitter/)

Familiar apps: VS Code's sidebar and panel, the navigation pane in File Explorer, browser developer tools

## Keyboard

- [x] Left and Right resize by 1% on a side-by-side separator. `SplitterTests.Splitter_ShouldResizeWithinTheLimits_WhenTheKeysAreUsed`
- [x] Up and Down resize a stacked separator, and a nested one leaves its outer panes alone. `SplitterTests.Splitter_ShouldResizeTheNestedPanes_WhenTheirSeparatorUsesUpAndDown`
- [x] Shift with an arrow resizes by 10%. `SplitterTests.Splitter_ShouldResizeWithinTheLimits_WhenTheKeysAreUsed`
- [x] Every key press counts, even when presses come faster than the server takes the sizes: a render of an earlier size doesn't undo later presses. `SplitterTests.ResizeAsync_ShouldMoveTheSeparatorWithItsKeys_WhenCalled`
- [x] Home and End take the pane to its smallest and largest, within its limits. `SplitterTests.Splitter_ShouldResizeWithinTheLimits_WhenTheKeysAreUsed`
- [x] Enter collapses a collapsible pane and restores it at its size. `SplitterTests.Splitter_ShouldCollapseAndRestoreThePane_WhenEnterIsPressed`
- [x] Enter collapses the next pane when the one before can't collapse. `SplitterTests.Splitter_ShouldCollapseTheNextPane_WhenThePaneBeforeTheSeparatorCannot`
- [x] Enter still collapses the pane before when a collapsed middle pane follows it, since the middle pane can come back from its own separator. `TbSplitterTests.Toggle_ShouldCollapseThePaneBefore_WhenAMiddlePaneAfterItIsCollapsed`
- [x] Enter restores a collapsed last pane, even when the pane before it can collapse. With both collapsed, the pane before comes back first. `SplitterTests.Splitter_ShouldRestoreTheLastPane_WhenThePaneBeforeItCanCollapseToo`, `TbSplitterTests.Toggle_ShouldRestoreThePaneBeforeFirst_WhenTheLastPaneIsCollapsedToo`
- [x] Enter does nothing when neither pane can collapse. `TbSplitter.razor`
- [x] The arrows, Home and End do nothing while either neighbor is collapsed. `splitter.js`
- [x] A pane collapsed while sizes are on their way to the server stays shut when another separator moves. `SplitterTests.Splitter_ShouldKeepACollapsedPaneShut_WhenAnotherSeparatorMovesWhileSizesAreOnTheirWay`
- [x] Left and Right move the separator the way they point in right-to-left text. `SplitterTests.Splitter_ShouldGrowThePaneWithTheLeftArrow_WhenTheTextIsRightToLeft`
- [x] Alt, Ctrl or Cmd with these keys stays with the browser, so Alt+Left still goes back. `SplitterTests.Splitter_ShouldLeaveTheKeysToTheBrowser_WhenAltCtrlOrCmdIsHeld`

## Pointer

- [x] Dragging the separator resizes the pair and stops at a limit, even past the splitter's edge. `SplitterDragTests.Splitter_ShouldResizeAndStopAtTheLimit_WhenTheSeparatorIsDragged`
- [x] Dragging the grip resizes without opening its menu. `SplitterDragTests.Splitter_ShouldResizeWithoutOpeningTheMenu_WhenTheGripIsDragged`
- [x] A click on the grip opens its menu without resizing, even if it wanders a few pixels. `SplitterDragTests.Splitter_ShouldOpenTheMenuWithoutResizing_WhenAClickOnTheGripWandersAFewPixels`
- [x] The grip's menu makes the pane larger, smaller, collapsed or restored. `SplitterTests.Splitter_ShouldResizeAndCollapseThePane_WhenTheGripsMenuIsClicked`, `TbSplitterTests.Grip_ShouldRestoreTheLastPane_WhenThePaneBeforeItCanCollapseToo`
- [x] The menu's steps turn off at a limit or while a neighbor is collapsed. `TbSplitterTests.Render_ShouldDisableTheGripsSteps_WhenAPaneIsAtItsLimitOrCollapsed`
- [x] Hover highlights the line and shows a resize cursor. `trailblazor.css`
- [x] With a `PersistenceKey`, the layout comes back after a reload. `SplitterTests.Splitter_ShouldKeepTheLayout_WhenThePageIsReloadedWithAPersistenceKey`
- [x] When the key arrives or changes after the splitter first shows, the splitter reads and saves the layout under the new key from then on. `TbSplitterTests.PersistenceKey_ShouldReadAndSaveUnderTheNewKey_WhenTheKeyArrivesOrChangesLater`
- [x] A drag doesn't select the text in the panes. `SplitterDragTests.Splitter_ShouldSelectNoText_WhenTheSeparatorIsDraggedOverThePanes`

## Touch

- [x] A finger drags the separator or the grip, through pointer events, without scrolling the page. `splitter.js`
- [x] A tap on the grip opens its menu, and a finger gets 10px of wander where a mouse gets 4. `splitter.js`
- [ ] A second finger on the splitter doesn't move or end a drag already running. UNCONFIRMED

## Narrow widths

- [x] With `StackBelow`, a splitter narrower than that breakpoint's width shows its panes one under the other, with a line where each separator was and nothing to drag, and side by side again, at the sizes it had, once it is wide. `SplitterTests.Splitter_ShouldStackItsPanesWithNothingToDrag_WhenItIsNarrowerThanStackBelow`, `TbSplitterTests.Render_ShouldGiveTheScriptTheBreakpointsWidth_WhenGivenStackBelow`
- [x] A stacked splitter keeps the height it has: its panes are at least as tall as what they hold, and it scrolls when they need more. `SplitterTests.Splitter_ShouldStackItsPanesWithNothingToDrag_WhenItIsNarrowerThanStackBelow`
- [x] A `Vertical` splitter that is a stacked pane's own child stacks with it, and a collapsed pane takes no room while stacked. `SplitterTests.Splitter_ShouldStackAVerticalSplitterInsideAndHideACollapsedPane_WhenItsOuterSplitterStacks`
- [x] A separator that has focus when the splitter stacks hands focus to the pane before it, or after it when that one is collapsed: to the pane's first control, or to the pane itself when it holds none, which stays focusable only until focus leaves it. `SplitterTests.Splitter_ShouldMoveFocusIntoThePaneBefore_WhenStackingHidesTheFocusedSeparator`, `SplitterTests.Splitter_ShouldMoveFocusIntoThePaneAfter_WhenStackingHidesASeparatorWhosePaneBeforeIsCollapsed`
- [x] Collapsing the pane that holds focus while the splitter is stacked moves focus into the next pane that shows, or into the last pane that shows when none follows it, since the separators are hidden. `SplitterTests.Splitter_ShouldMoveFocusIntoTheNextPane_WhenAStackedPaneHoldingFocusCollapses`

## Focus

- [x] Each separator is one tab stop. `TbSplitterPane.razor`
- [x] The grip stays out of the tab order, since the separator's keys do the same. `TbSplitterTests.Render_ShouldGiveEachSeparatorAGripMenuOutOfTheTabOrder_WhenGivenPanes`
- [x] `Labels` replaces the separator's and grip's names, the grip menu's items and the name of a pane with no `Label`. `TbSplitterTests.Labels_ShouldWordTheSeparatorsAndTheirMenus_WhenGiven`
- [x] Pressing a separator focuses it, and only keyboard focus highlights it. `SplitterDragTests.Separator_ShouldShowAHighlightOnlyForKeyboardFocus_WhenPressedThenGivenAKeyOrTabbedBackTo`
- [x] In forced colors, a focused separator's line turns the system's highlight color and an outline thickens it, since forced colors drop shadows. `ForcedColorsTests.Example_ShouldShowAFocusRingAtEachTabStop_WhenColorsAreForced`
- [x] Focus stays on the separator through a collapse and a restore. `SplitterTests.Splitter_ShouldCollapseAndRestoreThePane_WhenEnterIsPressed`
- [x] A collapsed pane's content is out of the tab order. `SplitterTests.Splitter_ShouldHideACollapsedPanesContentFromTheTabOrder_WhenCollapsedFromAButton`
- [x] A collapsed pane's content stays out of reach and hidden, even in an open accordion inside it. `HiddenContentTests.HiddenContent_ShouldBeSkippedByTab_WhenLoadingOrInACollapsedPane`, `HiddenContentTests.HiddenContent_ShouldHideAnOpenAccordionsPanel_WhenLoadingOrInACollapsedPane`, `TbSplitterTests.Render_ShouldGiveACollapsedPanesShareToTheOthers_WhenOneIsCollapsed`
- [x] While the panes are side by side or `Vertical`, collapsing the pane that holds focus, from a button inside it, moves focus to the separator after it, or before it for the last pane. `SplitterTests.Splitter_ShouldMoveFocusToTheSeparatorAfterThePane_WhenCollapsedFromInsideIt`, `SplitterTests.Splitter_ShouldMoveFocusToTheSeparatorAfterIt_WhenAMiddlePaneCollapsesFromInside`, `SplitterTests.Splitter_ShouldMoveFocusToTheSeparatorBeforeTheLastPane_WhenCollapsedFromInsideIt`
- [x] Collapsing a pane leaves focus alone when focus is outside that pane. `SplitterTests.Splitter_ShouldLeaveFocusAlone_WhenAnotherPaneCollapses`
- [x] With reduced motion, nothing on it moves: its transitions and animations take no time, apart from an animation that only fades. `ReducedMotionTests.Example_ShouldHaveNoMotion_WhenMotionIsReduced`, `AccessibilityStatusTests.Stylesheet_ShouldOverrideEveryOtherDuration_WhenMotionIsReduced`

## Not applicable

- F6 between panes: the pattern makes it optional, and a page's panes aren't the only regions F6 would need to visit.
- Double-click to reset or collapse: apps disagree on what it does, and the grip's menu says what it will do.
- Escape to cancel a drag: familiar splitters keep the size where it's released.
- Dragging a collapsed pane open: Enter or the grip's menu restores it at its own size.
