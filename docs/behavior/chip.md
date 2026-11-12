# Chip

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/), including its toggle form

Familiar apps: a mail app's recipient chips, the filter chips on a shopping or search page

## Keyboard

- [x] Enter or Space runs a chip that is a button, and turns a filter chip on or off. `TbChip.razor`
- [x] Tab reaches a chip's action and then its remove button. `TbChip.razor`
- [x] Enter on the remove button removes the chip. `ChipTests.Chip_ShouldPassFocusToTheChipInItsPlace_WhenRemovedWithTheKeyboard`
- [x] Delete or Backspace on either part removes the chip. `TbChipTests.KeyDown_ShouldAskToBeRemoved_WhenGivenARemovalKey`
- [x] Backspace on a chip that can be removed doesn't go back a page in the browsers that still would. `ChipTests.Chip_ShouldKeepBackspaceFromGoingBack_WhenItCanBeRemoved`
- [x] A disabled chip's keys do nothing. `TbChipTests.KeyDown_ShouldDoNothing_WhenTheChipIsDisabled`

## Pointer

- [x] A click turns a filter chip on or off, and a check shows while it's on. `TbChipTests.Click_ShouldToggleAndShowACheck_WhenBoundToSelected`
- [x] A click runs a chip with `OnClick`, and it isn't a toggle. `TbChipTests.Click_ShouldRunOnClickFromAPlainButton_WhenOnClickIsSet`
- [x] A click on the remove button asks to remove the chip and doesn't run the chip's action. `TbChipTests.Click_ShouldAskToBeRemovedWithANamedButton_WhenOnRemoveIsSet`
- [x] While an async `OnRemove` runs, another click on the remove button doesn't ask again, and a held Delete or Backspace asks only once. `TbChipTests.Click_ShouldAskOnce_WhenTheRemoveButtonIsClickedAgainWhileOnRemoveRuns`, `TbChipTests.KeyDown_ShouldAskOnce_WhenARemovalKeyIsHeld`
- [x] A link chip is a real link until it is disabled. `TbChipTests.Render_ShouldLinkToTheAddressUnlessDisabled_WhenHrefIsSet`
- [x] A chip with no action is plain text with nothing to click. `TbChipTests.Render_ShouldBePlainTextInASoftNeutralPill_WhenItHasNoActions`

## Touch

- [x] The remove button is at least 24 pixels across, and a tap on it or the chip is a click. `trailblazor.css`

## Focus

- [x] Removing a chip in a group moves focus to the chip now in its place, or the one before when the last went. `ChipTests.Chip_ShouldPassFocusToTheChipInItsPlace_WhenRemovedWithTheKeyboard`
- [x] Removing the group's last chip moves focus to the group. `ChipTests.Chip_ShouldPassFocusToTheChipInItsPlace_WhenRemovedWithTheKeyboard`
- [x] A chip in a group is a list item, and the group is a named list. `TbChipTests.Render_ShouldBeAListOfItems_WhenChipsAreInAGroup`
- [x] The remove button is named for its chip, and a filter chip says whether it's on. `TbChipTests.Click_ShouldAskToBeRemovedWithANamedButton_WhenOnRemoveIsSet`

## Not applicable

- Arrow keys between chips, as in a tag input: a group is a list of buttons, each its own tab stop, not one composite widget.
- Focus after removing a chip outside a group: it has no group to hand focus to, so removable chips belong in a `TbChipGroup`.
