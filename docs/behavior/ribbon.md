# Ribbon

Pattern: [Tabs](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/) for the tabs, and [Toolbar](https://www.w3.org/WAI/ARIA/apg/patterns/toolbar/) for each tab's commands

Familiar apps: the ribbon in Word and Excel, the command bar in File Explorer

## Keyboard

- [x] The tabs take the keys tabs do: Left and Right move along them and select, and Home and End go to the first and last. `RibbonTests.Ribbon_ShouldShowAnotherTabsCommands_WhenTheArrowKeysMoveAlongItsTabs`, `tabs.js`
- [x] Tab moves from the selected tab into its commands, which are one tab stop, and then out of the ribbon. The panel around the commands is no stop of its own. `RibbonTests.Ribbon_ShouldBeTwoTabStops_WhenTabbedThrough`, `TbRibbonTests.Render_ShouldLeaveThePanelOutOfTheTabOrder_WhenCalled`
- [x] Shift+Tab comes back to the control that last had the focus, and from there to the selected tab. `RibbonTests.Ribbon_ShouldBeTwoTabStops_WhenTabbedThrough`
- [x] Left and Right move across every control of the tab's groups, from one group into the next, and Home and End go to the tab's first and last control. `RibbonTests.Ribbon_ShouldMoveAcrossItsGroupsWithTheArrows_WhenACommandHasFocus`
- [x] The arrows reach a select and a menu's button, and pass over a button that is turned off. `RibbonTests.Ribbon_ShouldMoveAcrossItsGroupsWithTheArrows_WhenACommandHasFocus`, `RibbonTests.Ribbon_ShouldShowAnotherTabsCommands_WhenTheArrowKeysMoveAlongItsTabs`
- [x] The arrows wrap at the ends and swap in right-to-left text, and a text field, select or editable area keeps its own arrows, Home and End. `roving-focus.js`
- [x] The commands of a tab shown later get their one tab stop and their keys once it shows. `RibbonTests.Ribbon_ShouldShowAnotherTabsCommands_WhenTheArrowKeysMoveAlongItsTabs`
- [x] Enter and Space press a button, and turn a toggle button on or off. `TbButtonBase.razor`
- [ ] Ctrl+F1 collapses the ribbon to its tabs and shows it again. #566
- [ ] Alt shows key tips, a letter on each tab and command. #567

## Pointer

- [x] A click on a tab shows its commands. The commands of a tab shown before stay rendered, hidden, so what their controls hold survives. `TbRibbonTests.Tabs_ShouldStartOnTheFirstAndFollowTheUser_WhenNoSelectionIsBound`
- [x] `ActiveKey` or `ActiveIndex` sets the selected tab and reports the user's choice; with neither, the ribbon starts on its first tab and follows the user. A key alone, or its callback alone, selects by key too. `TbRibbonTests.Tabs_ShouldSelectByKeyAndReportTheKey_WhenActiveKeyIsBound`, `TbRibbonTests.Tabs_ShouldSelectByKey_WhenOnlyAKeyOrOnlyItsCallbackIsGiven`, `TbRibbonTests.Tabs_ShouldSelectByPositionAndReportIt_WhenActiveIndexIsBound`, `TbRibbonTests.Tabs_ShouldStartOnTheFirstAndFollowTheUser_WhenNoSelectionIsBound`
- [x] A click on a button runs its `OnClick`, and a button bound with `@bind-Pressed` toggles and says whether it is on. `TbRibbonTests.Button_ShouldRunItsCommand_WhenClicked`, `TbRibbonTests.Button_ShouldToggleAndSayWhetherItIsOn_WhenPressedIsBound`, `RibbonTests.Ribbon_ShouldRunCommandsAndToggle_WhenUsedThroughItsHelper`
- [x] A group's controls fill columns of up to three rows, a `Large` button is as tall as the group with its icon above its text, and the group's name sits under its controls. `RibbonTests.Group_ShouldStackItsSmallControlsBesideALargeButton_WhenItHoldsBoth`
- [x] Resting on an `IconOnly` button shows its name in a tooltip. `TbRibbonTests.Button_ShouldShowItsIconBesideAboveOrAlone_WhenGivenLargeOrIconOnly`, `tooltip.js`
- [x] An `IconOnly` button toggles, turns off and takes attributes as the other buttons do. `TbRibbonTests.Button_ShouldToggleTurnOffAndTakeAttributes_WhenItIsIconOnly`
- [x] A `Disabled` tab can't be selected, a tab shows its `Icon`, and a tab's `Class`, `Style` and other attributes go on its panel. `TbRibbonTests.Tab_ShouldPassWhatItIsGivenToItsTabAndPanel_WhenGiven`
- [x] A `TbRibbonButton` works as a menu's trigger. `TbRibbonTests.Button_ShouldTakeAMenusTriggerAttributes_WhenUsedAsATrigger`
- [x] A row too narrow for its groups scrolls sideways, and the page doesn't. `RibbonTests.Ribbon_ShouldScrollItsCommandsAndBringTheFocusedOneIntoView_WhenItIsTooNarrow`
- [x] On a static page, a click or an arrow key on a tab shows its commands, the panel still isn't a tab stop, and the testing helpers refuse to click a button, which does nothing there. `RibbonTests.Ribbon_ShouldSwitchTabsAndRefuseAHelpersClick_WhenThePageIsStatic`, `TabsTests.Tabs_ShouldSwitchByClickOrArrowKey_WhenThePageIsStatic`, `TbRibbonTests.Driver_ShouldRefuseToClick_WhenThePageIsStatic`
- [ ] When the ribbon is too narrow, its groups collapse into buttons that open them. #565
- [ ] A double click on a tab collapses the ribbon to its tabs. #566

## Touch

- [x] A tap presses a button or selects a tab through the same click a mouse sends, and a swipe scrolls a row that is too narrow. `TbButtonBase.razor`, `trailblazor.css`
- [x] Each button is at least 24px high and wide. `RibbonTests.Group_ShouldStackItsSmallControlsBesideALargeButton_WhenItHoldsBoth`

## Focus

- [x] A control that takes focus in a row scrolled out of view is brought into view. `RibbonTests.Ribbon_ShouldScrollItsCommandsAndBringTheFocusedOneIntoView_WhenItIsTooNarrow`
- [x] In forced colors the line between two groups is drawn in a system color. `RibbonTests.Ribbon_ShouldKeepTheLinesBetweenItsGroups_WhenForcedColorsAreOn`
- [x] A screen reader hears the tabs as a `tablist` named by `AriaLabel`, a tab's commands as a `toolbar` named by its `Title`, and each group as a `group` named by its `Label`. `TbRibbonTests.Render_ShouldNameItsTabsCommandsAndGroups_WhenCalled`
- [x] A toggle button says whether it is on in `aria-pressed`. `TbRibbonTests.Button_ShouldToggleAndSayWhetherItIsOn_WhenPressedIsBound`
- [x] An `IconOnly` button's tooltip repeats its name, so it isn't read again as its description. `tooltip.js`

## Not applicable

- A menu for tabs that don't fit: the row of tabs scrolls, as tabs do.
- Drag and drop: nothing in a ribbon is moved by dragging.
