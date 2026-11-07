# List

Pattern: none; it's a native list of links or buttons. For a list people choose from with the arrow keys, see [Listbox](https://www.w3.org/WAI/ARIA/apg/patterns/listbox/)

Familiar apps: a settings page's list, a mail app's folder list

## Keyboard

- [x] Tab reaches each item, then the item's own buttons, in order. `ListTests.List_ShouldReachEachItemThenItsOwnButton_WhenTabbing`
- [x] Enter or Space runs an item that is a button, and Enter follows one that is a link. `TbListItem.razor`
- [x] Tab skips a disabled item. `ListTests.List_ShouldSkipTheItem_WhenItIsDisabled`

## Pointer

- [x] A click anywhere on an item runs it. `ListTests.List_ShouldMarkTheChosenItemCurrent_WhenItIsClicked`
- [x] A button in an item's end runs alone, without choosing the item. `ListTests.List_ShouldRunTheItemsOwnButtonAlone_WhenItIsPressed`
- [x] A link item is a real link, so Ctrl+click and a middle click open a new tab. `TbListTests.Render_ShouldKeepTheEndOutsideTheAction_WhenTheItemIsALink`
- [x] A disabled link stops being a link. `TbListTests.Render_ShouldDropTheLink_WhenDisabled`
- [x] Items show hover and press states. `trailblazor.css`

## Touch

Nothing of its own. Items are native links and buttons, so a tap is a click.

## Focus

- [x] A focused item shows a ring inside its edges. `trailblazor.css`
- [x] The current item says so to screen readers, as the page on a link and as current on a button. `TbListTests.Render_ShouldMarkTheActionCurrent_WhenSelected`
- [x] An item is named by its text and then its description. `ListTests.List_ShouldNameEachItemByItsTextThenItsDescription_WhenItHasOne`

## Not applicable

- Arrow keys and typing to jump: those belong to a listbox, and each item here is its own tab stop.
- Selecting several items: that belongs to a listbox too.
- Focus after a button in an item's end removes the item: the button and the removal are the app's, so the app places focus.
