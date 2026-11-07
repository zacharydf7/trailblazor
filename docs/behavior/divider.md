# Divider

Pattern: none; a static line, the non-focusable form of the `separator` role

Familiar apps: the lines between sections in any settings page

## Keyboard

Nothing of its own. It can't be focused or moved.

## Pointer

Nothing of its own. It can't be dragged.

## Touch

Nothing of its own. It can't be dragged.

## Focus

- [x] It takes no focus, as a horizontal rule or a vertical separator. `TbDivider.razor`
- [x] A line with content isn't a separator, so its content is read. `TbDividerTests.Render_ShouldShowContentInAPlainBlockRatherThanASeparator_WhenGivenContent`

## Not applicable

- Dragging to resize: a [splitter](splitter.md) does that.
