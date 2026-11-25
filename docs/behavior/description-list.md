# Description list

Pattern: none; it is a `dl` of `dt` and `dd` pairs, each pair in a `div`

Familiar apps: an order's details page with "Placed", "Shipped to" and "Total", a file's properties panel

## Keyboard

Nothing of its own: it is text, and links or buttons in its values work as anywhere.

## Pointer

Nothing of its own: it is text.

## Touch

Nothing of its own: it is text.

## Focus

- [x] Each pair is a `dt` and a `dd` in a `div` inside the `dl`, so screen readers hear the term with its value. `TbDescriptionListTests.TbDescriptionList_ShouldRenderTermsAndValues_WhenGivenItems`, `DescriptionListTests.TbDescriptionList_ShouldStackColumns_WhenContainerIsNarrow`
- [x] A blank `Value` shows a dash, which screen readers hear as the list's `EmptyText`, "Not set" by default, also after `EmptyText` changes. `TbDescriptionListTests.TbDescriptionItem_ShouldShowEmptyPlaceholder_WhenValueIsBlank`, `TbDescriptionListTests.EmptyText_ShouldBeWhatScreenReadersHear_WhenGivenForTheList`, `TbDescriptionListTests.EmptyText_ShouldReachTheItems_WhenItChangesAfterTheFirstRender`
- [x] `TermContent` and `ChildContent` show formatted content for a term and a value. `TbDescriptionListTests.Item_ShouldShowItsContentAndTermContent_WhenGivenThem`

## Layout

- [x] `Columns` puts pairs side by side, and a narrow container falls back to one column, whatever the window's width. `DescriptionListTests.TbDescriptionList_ShouldStackColumns_WhenContainerIsNarrow`
- [x] `Inline` puts the terms in a column beside their values; `Stacked`, the default, puts each above its value. `TbDescriptionListTests.Render_ShouldSetTheLayoutAndColumns_WhenGiven`, `DescriptionListTests.TbDescriptionList_ShouldStackColumns_WhenContainerIsNarrow`

## Not applicable

- Announcements: it shows fixed text, so nothing changes to announce.
