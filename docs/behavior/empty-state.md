# Empty state

Pattern: none; it is a heading and text, with ordinary buttons and links as its actions

Familiar apps: the "No files yet" page of a new cloud drive folder, an inbox with no messages, a search with no results

## Keyboard

Nothing of its own: it handles no keys, and its actions are native buttons and links in the tab order.

## Pointer

Nothing of its own: its actions are ordinary buttons and links.

## Touch

Nothing of its own: it holds no gestures, only its actions.

## Focus

- [x] Screen readers find it by its title, a heading at level 3 unless `HeadingLevel` says otherwise. `TbEmptyStateTests.Render_ShouldShowTheTitleAsAHeadingWithItsDescriptionAndActions_WhenGivenThem`, `TbEmptyStateTests.Render_ShouldUseTheGivenHeadingLevel_WhenHeadingLevelIsSet`
- [x] The icon is decoration and hidden from screen readers. `EmptyStateTests.EmptyState_ShouldHideItsIconFromScreenReaders_WhenRendered`

## Not applicable

- Announcing itself as it appears: an empty state can be on the page from the start, so announcing it is the app's job when the user's own action empties a list.
