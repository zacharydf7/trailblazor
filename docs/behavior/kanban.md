# Kanban board

Pattern: none of its own; each column is a sortable list, whose handles are menu buttons

Familiar apps: Trello, GitHub Projects' board view, Jira's board

## Keyboard

- [x] Up and Down on a card's handle move it within its column, and Left and Right to the column before or after. `KanbanTests.Kanban_ShouldMoveACardAcrossColumns_WhenLeftAndRightArePressedOnItsHandle`
- [x] Enter or Space on a handle opens its menu, which moves within the column or to each other column. `TbKanbanTests.MoveToColumn_ShouldRaiseOnMoveBetweenTheColumns_WhenACardMovesToAnother`
- [x] Enter on a column's collapse button collapses it and opens it again, as on any button, and a card's Left and Right skip a collapsed column. `KanbanTests.Collapse_ShouldKeepFocusOnTheButtonAndOpenAgain_WhenPressed`, `sortable.js`

## Pointer

- [x] Dragging a card's handle onto another column moves it there. `KanbanTests.Kanban_ShouldMoveACardToAnotherColumn_WhenDraggedThere`
- [x] A click on a handle opens the same menu as Enter, the single-pointer way that replaces dragging. `TbKanbanTests.MoveToColumn_ShouldRaiseOnMoveBetweenTheColumns_WhenACardMovesToAnother`
- [x] With `Collapsible`, a column's header button collapses it to its title and count, with no cards or footer, and a collapsed column takes no card. `TbKanbanTests.Collapse_ShouldHideTheCardsAndKeepTheTitleAndCount_WhenTheColumnsButtonIsPressed`, `TbKanbanTests.MoveToColumn_ShouldNotOfferACollapsedColumn_WhenOneIsCollapsed`

## Touch

- [x] A board wider than its container, with no scrollbar of its own to show that it scrolls, fades at each side that has more columns past it, in either direction of text, and a scroll that ends near a column's start comes to rest on that column. `KanbanTests.Kanban_ShouldFadeTheSideWithMoreColumns_WhenItScrollsWithNoScrollbarOfItsOwn`, `trailblazor.css`
- [x] A scrollbar that takes room already shows that the board scrolls, so a board with one has no fade. `scroll-edges.js`
- [ ] Dragging a card by touch, which the sortable list handles, needs a real touch device to confirm; the handle's menu works by tap meanwhile. UNCONFIRMED

## Focus

- [x] A move into a full column is refused, and the reason is read out. `TbKanbanTests.MoveToColumn_ShouldBeRefusedAndReadOut_WhenTheColumnIsFull`
- [x] A column's count shows how full it is in words, not only by color, and forced colors keeps the card edges. `KanbanTests.Kanban_ShouldKeepItsCardEdges_WhenColorsAreForced`
- [x] Cards move only between one board's columns, even with two boards on a page. `TbKanbanTests.MoveToColumn_ShouldOfferOnlyItsOwnBoardsColumns_WhenTwoBoardsShowAtOnce`
- [x] Focus stays on a column's collapse button as Enter collapses and opens it, and its `aria-expanded` says which. `KanbanTests.Collapse_ShouldKeepFocusOnTheButtonAndOpenAgain_WhenPressed`
- [x] `@bind-CollapsedColumns` holds the collapsed ids, and a parent's render that passes the same collection keeps the user's choices. `TbKanbanTests.CollapsedColumns_ShouldCollapseTheColumnsGiven_WhenSet`, `TbKanbanTests.CollapsedColumns_ShouldKeepTheUsersToggles_WhenTheParentRendersTheSameOnes`
- [x] A column that opens, even one that started collapsed, takes its place in the cards' move menus. `SortableGroupsTests.Join_ShouldPutAListBackInItsPlace_WhenItLeftAndJoinsAgain`, `TbKanbanTests.Expand_ShouldOfferTheColumnInTheBoardsOrder_WhenItStartedCollapsed`

## Not applicable

- Adding, editing and deleting cards: the app does that, in `CardTemplate` or `ColumnFooterTemplate`.
- Moving columns: the columns come from the app, in its order.
