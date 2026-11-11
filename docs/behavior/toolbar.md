# Toolbar

Pattern: [Toolbar](https://www.w3.org/WAI/ARIA/apg/patterns/toolbar/)

Familiar apps: Google Docs' toolbar and its overflow menu, the editor actions in VS Code

## Keyboard

- [x] Tab enters and Shift+Tab leaves in one stop. `ToolbarTests.Toolbar_ShouldBeOneTabStop_WhenTabbedThrough`
- [x] Left and Right move across every control, toggle group items included, and Home and End go to the first and last. `ToolbarTests.Toolbar_ShouldMoveAcrossEveryControlWithTheArrows_WhenControlsAreNested`
- [x] The arrows wrap at the ends, and Left and Right swap in right-to-left text. `roving-focus.js`
- [x] A vertical toolbar moves with Up and Down. `ToolbarTests.Toolbar_ShouldMoveWithUpAndDown_WhenVertical`
- [x] The arrows skip buttons moved into the menu and reach the More button. `ToolbarTests.Toolbar_ShouldSkipMovedButtonsAndReachTheMoreButton_WhenUsingTheArrows`
- [x] The arrows skip disabled controls. `roving-focus.js`
- [x] A text field keeps its own arrows, Home and End. `ToolbarTests.Toolbar_ShouldLeaveTheArrowsToATextField_WhenItHasFocus`
- [x] Alt, Ctrl or Cmd with an arrow stays with the browser, on a menu button too. `ToolbarTests.Toolbar_ShouldLeaveTheKeysToTheBrowser_WhenAltCtrlOrCmdIsHeldOnItsMenuButton`
- [x] Escape closes the More menu and returns focus to its button. `MenuTests.Menu_ShouldCloseAndRefocusTheTrigger_WhenEscapeIsPressed`
- [x] The arrows reach a `TbSelect`, and it isn't a second tab stop. A turned-off select stays out. `ToolbarTests.Toolbar_ShouldReachASelectWithTheArrowsAsOneTabStop_WhenItHoldsOne`, `ToolbarTests.Toolbar_ShouldSkipATurnedOffSelect_WhenTheArrowsMove`, `ToolbarTests.Select_ShouldKeepItsOwnKeys_WhenInAToolbar`
- [x] The arrows reach a `FocusableWhenDisabled` button, even one with an `Href`, so a keyboard user can find it and learn why it's off. They skip one that's simply disabled, and a link that turned off while shown. `ToolbarTests.Toolbar_ShouldReachButtonsAndLinksKeptFocusable_WhenTheyreOff`, `ToolbarTests.Toolbar_ShouldSkipLinksThatAreOff_WhenOneStartsOffAndOneTurnsOff`
- [x] A link that turns off gives up the tab stop, and one that becomes focusable while off doesn't add another. `ToolbarTests.Toolbar_ShouldKeepOneTabStop_WhenTheLinkHoldingItTurnsOff`, `ToolbarTests.Toolbar_ShouldKeepOneTabStop_WhenALinkBecomesFocusableWhileOff`
- [x] A link that turns off while the toolbar is hidden gives up the tab stop once the toolbar shows again, in a vertical toolbar too. `ToolbarTests.Toolbar_ShouldKeepOneTabStop_WhenTheLinkHoldingItTurnsOffWhileHidden`
- [x] A `Loading` link that turns back on doesn't add a tab stop. `ToolbarTests.Toolbar_ShouldKeepOneTabStop_WhenABusyLinkTurnsBackOn`
- [x] A hidden toolbar keeps its tab stop through a change inside it. `ToolbarTests.Toolbar_ShouldKeepItsTabStop_WhenSomethingChangesWhileItsHidden`
- [x] A select that turns off while focused keeps focus. `ToolbarTests.Toolbar_ShouldKeepFocusOnASelect_WhenItTurnsOffWhileFocused`
- [x] The arrows keep working from a button that turned `Loading` while focused, and the tab stop stays on it. `ToolbarTests.Toolbar_ShouldKeepItsKeysAndTabStop_WhenTheFocusedButtonTurnsLoading`

## Pointer

- [x] A click on a toolbar button runs `OnClick`. `TbToolbarButton.razor`
- [x] When it narrows, the last buttons move into the More menu, still work, and move back when it widens. `ToolbarTests.Toolbar_ShouldMoveButtonsIntoTheMoreMenuAndBack_WhenItNarrowsAndWidens`
- [x] The More menu keeps each button's icon, disabled state and order. `TbToolbarTests.Overflow_ShouldGiveTheMenuItemsTheButtonsIconsAndStateInTheOrderReported_WhenButtonsMove`
- [x] A moved button's menu item follows its text. `TbToolbarTests.Overflow_ShouldFollowAMovedButtonsText_WhenTheButtonRendersOnItsOwn`
- [x] Resting on an icon button shows its text in a tooltip, and a click doesn't leave it open. `tooltip.js`

## Touch

- [x] A tap presses a button or opens the More menu through the same click a mouse sends. `TbToolbarButton.razor`
- [x] The toolbar's `Size` sizes its toolbar buttons and More button, and each size steps with density, keeping a small button at least 24px high. A button's own `Size` wins. `TbToolbarTests.Render_ShouldKeepAButtonsOwnSize_WhenTheToolbarHasAnother`, `SizeTests.Size_ShouldStepThePartsHeightWithDensity_WhenSmallMediumOrLarge`
- [x] A tap doesn't leave an icon button's tooltip open, since the press hides it and cancels its timer. `tooltip.js`

## Focus

- [x] The tab stop stays on the last focused control, which a click also moves it to, and is still there after the toolbar is hidden and shown again. `ToolbarTests.Toolbar_ShouldKeepTheTabStopOnTheLastFocusedControl_WhenHiddenAndShownAgain`
- [x] It stays one tab stop when a toggle group inside changes from outside. `ToolbarTests.Toolbar_ShouldKeepOneTabStop_WhenAToggleGroupInsideItChangesFromOutside`
- [x] Controls in a popup inside the toolbar aren't part of its keys. `ToolbarTests.ToggleGroup_ShouldKeepItsOwnKeys_WhenInAPopupInsideAToolbar`
- [x] Keyboard focus on an icon button shows its text, which isn't read twice. `ToolbarTests.Toolbar_ShouldNotDescribeAnIconButtonByItsOwnName_WhenItsTooltipRepeatsIt`
- [x] When the focused button moves into the More menu, or the More button goes away as the toolbar widens, focus moves to a control still in view. `ToolbarTests.Toolbar_ShouldMoveFocusToItsMenu_WhenTheFocusedButtonMovesIntoIt`, `ToolbarTests.Toolbar_ShouldMoveFocusToItsLastControl_WhenItsFocusedMenuGoesAway`

## Not applicable

- Up and Down moving on from a vertical toolbar's menu button: they open the menu, and Escape comes back to the button.
- Keeping plain disabled controls focusable: the pattern allows it, but a native disabled button can't take focus, so the arrows skip them.
- Moving other content into the menu: only a `TbToolbarButton` moves, since a toggle group or a field has no menu item form.
- Overflow in a vertical toolbar: it grows with its content.
