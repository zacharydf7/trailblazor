# Kanban board

Pattern: none of its own; each column is a sortable list, whose handles are menu buttons

Familiar apps: Trello, GitHub Projects' board view, Jira's board

## Keyboard

- [x] Up and Down on a card's handle move it within its column, and Left and Right to the column before or after. `KanbanTests.Kanban_ShouldMoveACardAcrossColumns_WhenLeftAndRightArePressedOnItsHandle`
- [x] Enter or Space on a handle opens its menu, which moves within the column or to each other column. `TbKanbanTests.MoveToColumn_ShouldRaiseOnMoveBetweenTheColumns_WhenACardMovesToAnother`

## Pointer

- [x] Dragging a card's handle onto another column moves it there. `KanbanTests.Kanban_ShouldMoveACardToAnotherColumn_WhenDraggedThere`
- [x] A click on a handle opens the same menu as Enter, the single-pointer way that replaces dragging. `TbKanbanTests.MoveToColumn_ShouldRaiseOnMoveBetweenTheColumns_WhenACardMovesToAnother`

## Touch

- [ ] Dragging a card by touch, which the sortable list handles, needs a real touch device to confirm; the handle's menu works by tap meanwhile. UNCONFIRMED

## Focus

- [x] A move into a full column is refused, and the reason is read out. `TbKanbanTests.MoveToColumn_ShouldBeRefusedAndReadOut_WhenTheColumnIsFull`
- [x] A column's count shows how full it is in words, not only by color, and forced colors keeps the card edges. `KanbanTests.Kanban_ShouldKeepItsCardEdges_WhenColorsAreForced`
- [x] Cards move only between one board's columns, even with two boards on a page. `TbKanbanTests.MoveToColumn_ShouldOfferOnlyItsOwnBoardsColumns_WhenTwoBoardsShowAtOnce`

## Not applicable

- Adding, editing and deleting cards: the app does that, in `CardTemplate` or `ColumnFooterTemplate`.
- Moving or collapsing columns: the columns come from the app, in its order.
