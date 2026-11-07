# Table

Pattern: [Table](https://www.w3.org/WAI/ARIA/apg/patterns/table/)

Familiar apps: tables in documentation and on Wikipedia

## Keyboard

Nothing of its own. It is a native table with no keys, and links or buttons written in it keep theirs.

## Pointer

- [x] With `StickyHeader`, the header stays in view as the rows scroll. `TableTests.Table_ShouldKeepItsHeaderInView_WhenItsRowsScrollWithStickyHeader`

## Touch

- [x] A wide table in an area with `overflow: auto` scrolls sideways there, and the page doesn't. `TableTests.Table_ShouldScrollInItsAreaAndLeaveThePageAlone_WhenItIsWiderThanAPhone`

## Focus

- [x] The table itself takes no focus. `TbTable.razor`
- [x] A link or button in a row scrolls clear of a sticky header when it takes focus, at any `Size` and wherever `--tb-table-sticky-top` puts it. `TableTests.Table_ShouldScrollAFocusedLinkClearOfTheHeader_WhenItLandsBehindIt`
- [x] Items in a row's menu get no room for the header, since the menu scrolls on its own. `TableTests.Table_ShouldLeaveAMenusItemsOutOfTheHeadersRoom_WhenARowHoldsAMenu`

## Not applicable

- Arrow keys between cells: the table pattern has none, and the data grid is for rows people act on.
- Sorting, selecting and row clicks: those belong to the data grid too.
- A tab stop on a scrolling area: the area is the app's markup, so the docs ask the app to give it `tabindex="0"`, a role and a name.
