# Grid

Pattern: none; it's a layout helper, not the interactive [Grid](https://www.w3.org/WAI/ARIA/apg/patterns/grid/) pattern, which is the data grid's

Familiar apps: none; it isn't a control

## Keyboard

Nothing of its own. It only arranges its items, with no arrow keys between them.

## Pointer

Nothing of its own. It only arranges its items.

## Touch

Nothing of its own. It only arranges its items.

## Focus

- [x] It takes no focus, and the tab order follows the markup at every width, since spans and offsets don't reorder items. `TbGridItem.cs`

## Not applicable

- Reordering items at a breakpoint: it has no order setting, so what shows and what's read stay in the same order.
