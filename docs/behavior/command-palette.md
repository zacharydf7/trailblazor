# Command palette

Pattern: [Combobox](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/) in a modal [dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/)

Familiar apps: VS Code's command palette, GitHub's and Linear's Ctrl+K search, Slack's quick switcher

## Keyboard

- [x] Ctrl+K, or ⌘K on Apple devices, opens the palette from anywhere on the page, and pressing it again closes it. `CommandPaletteTests.Shortcut_ShouldOpenThePaletteWithItsSearchFocused_WhenPressedAnywhere`
- [x] The shortcut is left alone when something else already handled the key, while it's held down, and while an input method composes text. `command-palette.js`
- [x] Typing narrows the commands to those whose letters appear in order, best matches first, and the first one is active. `CommandPaletteTests.Keys_ShouldMoveTheActiveCommandAndRunIt_WhenTheArrowsAndEnterArePressed`, `TbCommandPaletteTests.Input_ShouldPutTheBestMatchesFirstAndMarkTheirLetters_WhenTheUserTypes`
- [x] Down and Up move the active command, skip disabled ones and wrap at the ends. `CommandPaletteTests.Keys_ShouldSkipADisabledCommandAndWrapAround_WhenTheArrowsMovePastIt`
- [x] Enter runs the active command, closes the palette and returns focus to where it was. `CommandPaletteTests.Keys_ShouldMoveTheActiveCommandAndRunIt_WhenTheArrowsAndEnterArePressed`
- [x] Escape closes the palette without running anything and returns focus. `CommandPaletteTests.Escape_ShouldCloseThePaletteAndReturnFocus_WhenPressedInTheSearch`
- [x] Enter goes to an input method while it composes text, not to the list. `command-palette.js`
- [x] Home and End stay with the search box's text. `command-palette.js`

## Pointer

- [x] Hovering a command makes it the active one, and focus stays in the search box. `CommandPaletteTests.Click_ShouldRunTheCommandAndKeepFocusInTheSearchUntilThen_WhenAnOptionIsPressed`
- [x] Clicking a command runs it and closes the palette. `CommandPaletteTests.Click_ShouldRunTheCommandAndKeepFocusInTheSearchUntilThen_WhenAnOptionIsPressed`
- [x] Clicking a disabled command, or the message shown when nothing matches, does nothing. `TbCommandPaletteTests.Click_ShouldDoNothing_WhenTheCommandIsDisabled`
- [x] Clicking the backdrop closes the palette, as for any dialog. `dialog.js`

## Touch

- [ ] A tap on a command runs it through the same click handler. UNCONFIRMED

## Focus

- [x] Opening moves focus to the search box, which starts empty each time. `CommandPaletteTests.Shortcut_ShouldOpenThePaletteWithItsSearchFocused_WhenPressedAnywhere`, `TbCommandPaletteTests.Open_ShouldStartWithAnEmptySearch_WhenOpenedAgain`
- [x] The palette closes before `OnChoose` runs, so a dialog the command opens returns focus to where the user was, not to the closed palette. `TbCommandPaletteTests.Click_ShouldCloseTheDialogThenRaiseOnChooseAndGoToItsPage_WhenACommandIsChosen`
- [x] Focus is trapped in the palette while it's open, as in any modal dialog. `dialog.js`

## Not applicable

- Opening the list with Down or Alt+Down: the results always show while the palette is open.
- A value kept in the box after choosing: a palette runs a command, so it closes instead.
