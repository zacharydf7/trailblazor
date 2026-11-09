# Breadcrumbs

Pattern: [Breadcrumb](https://www.w3.org/WAI/ARIA/apg/patterns/breadcrumb/)

Familiar apps: the folder trail above a file list, and the category trail on a shop's product page

## Keyboard

- [x] Tab moves between the links, each its own tab stop, and skips plain-text items and separators. `TbBreadcrumb.razor`
- [x] Enter follows the focused link, since it is a real anchor. `TbBreadcrumb.razor`

## Pointer

- [x] Clicking a link goes to its address. `TbBreadcrumbsTests.Render_ShouldListTheLinksAndMarkTheCurrentPage_WhenGivenATrail`
- [x] Ctrl, Cmd, Shift, middle and right clicks do what they do on any link, since nothing cancels them. `TbBreadcrumb.razor`
- [x] A separator shows only between items, never before the first. `BreadcrumbsTests.Breadcrumbs_ShouldShowSeparatorsOnlyBetweenItems_WhenRendered`
- [x] A trail too long for its line wraps onto the next. `trailblazor.css`

## Touch

- [x] Tapping does what clicking does, and nothing depends on hover. `TbBreadcrumb.razor`

## Focus

- [x] Screen readers hear a named navigation landmark holding a list of links, without the separators. `BreadcrumbsTests.Breadcrumbs_ShouldReadAsAListOfLinksWithoutTheSeparators_WhenRendered`, `TbBreadcrumbsTests.Render_ShouldHideEachSeparatorFromScreenReaders_WhenCalled`
- [x] The current item is marked `aria-current="page"`, whether it is a link or plain text. `TbBreadcrumbsTests.Render_ShouldListTheLinksAndMarkTheCurrentPage_WhenGivenATrail`, `TbBreadcrumbsTests.Render_ShouldMarkTheLinkAsTheCurrentPage_WhenTheCurrentItemHasAnAddress`
- [x] In right-to-left text the trail runs from right to left, and the chevrons point left. `BreadcrumbsTests.Breadcrumbs_ShouldRunFromRightToLeftWithMirroredChevrons_WhenTheTextIsRightToLeft`

## Not applicable

- Collapsing a long trail behind a "…" button: the trail wraps instead, and the app can render fewer steps.
- Choosing the current page from the address: the app marks it with `Current`.
