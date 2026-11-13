# Hotkeys

Pattern: none; WAI-ARIA describes keyboard shortcuts in its [developing a keyboard interface](https://www.w3.org/WAI/ARIA/apg/practices/keyboard-interface/) practice

Familiar apps: Gmail's and GitHub's shortcuts with their "?" list, VS Code's keybindings

## Keyboard

- [x] A hotkey's keys run it anywhere on the page, and the browser's own action for them doesn't happen. `HotkeysTests.Hotkey_ShouldFireAndPreventTheBrowsersAction_WhenItsKeysArePressedOnThePage`
- [x] Mod is ⌘ on Apple devices and Ctrl elsewhere, and Space and other keys that aren't letters work too. `HotkeysTests.Hotkey_ShouldFire_WhenItsKeyIsSpace`
- [x] A character key hotkey, a key people type without Mod, Ctrl, Alt or Meta, must be allowed, and never fires while someone types in a field; a named key such as Escape needs no opt-in and fires in a field. `HotkeysTests.ParentHotkey_ShouldFireANamedKeyInsideAField_WhenFocusIsInIt`, `TbHotkeyTests.Render_ShouldThrow_WhenTheKeysCantBeReadOrAreACharacterKeyNotAllowed`, `HotkeysTests.CharacterKey_ShouldFireOnThePageButNotInAField_WhenAllowed`
- [x] A disabled hotkey leaves its keys to the page. `HotkeysTests.Hotkey_ShouldLeaveTheKeysAlone_WhenDisabledAndFireOnceEnabled`
- [x] A `Parent` hotkey fires only with focus inside its element, and there it wins over a page hotkey with the same keys, even one deeper in the page. `HotkeysTests.ParentHotkey_ShouldWinInsideItsElementAndNotFireOutside_WhenItsKeysArePressed`
- [x] While a modal dialog is open, only the hotkeys inside it fire. `HotkeysTests.Hotkey_ShouldFireOnlyInsideTheDialog_WhenAModalDialogIsOpen`
- [x] The help's keys open the list of hotkeys, and the same keys close it from inside. Mod shows as ⌘ on Apple devices and Ctrl elsewhere. `HotkeysTests.Help_ShouldListTheHotkeysByGroupAndClose_WhenItsKeyIsPressedTwice`
- [x] Keys held down fire a hotkey once, and keys pressed while composing text in an input method fire nothing. `shortcuts.js`

## Pointer

Nothing of its own: hotkeys are keys, and their actions need a button or menu item of their own.

## Touch

Nothing of its own: hotkeys are keys, and their actions need a button or menu item of their own.

## Focus

- [x] The help lists each described hotkey that isn't disabled under its group, with the ones that have no group first. `TbHotkeyTests.Help_ShouldListTheDescribedHotkeysThatFireByGroup_WhenOpen`

## Not applicable

- Sequences such as "g then t": each hotkey is one combination.
- Letting people change the keys: the app sets `Keys`, so it can offer that with a setting.
