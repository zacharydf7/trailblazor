# Pagination

Pattern: none in the Authoring Practices; a named navigation landmark of buttons or links

Familiar apps: search results, GitHub's lists, the pager under a data table

## Keyboard

- [x] Tab moves through the controls, and Enter on one goes to its page. `PaginationTests.Pagination_ShouldGoToThePageAndKeepFocusOnIt_WhenAPageIsChosenWithTheKeyboard`
- [x] Without `PageHref`, Space goes to the page as Enter does, since each control is a real button. `TbButtonBase.razor`
- [x] In compact mode, Enter or leaving the box goes to the typed page, and a page past either end goes to that end. `PaginationTests.Pagination_ShouldGoToTheTypedPage_WhenAPageIsTypedInCompactMode`
- [x] In compact mode, Up goes to the next page and Down to the previous. `PaginationTests.Pagination_ShouldGoToTheNextPage_WhenUpIsPressedInCompactMode`
- [ ] Clearing the box, or typing past the end while already there, puts the current page back in the box. UNCONFIRMED
- [x] Enter in the box goes to the page without submitting a form around the pagination. `PaginationTests.PageBox_ShouldGoToTheTypedPageWithoutSubmittingTheForm_WhenEnterIsPressed`

## Pointer

- [x] Clicking a page or a step button goes to that page. `TbPaginationTests.Click_ShouldGoToThePage_WhenAPageOrStepButtonIsClicked`
- [x] Clicking the current page raises nothing. `TbPagination.razor`
- [x] First, Previous, Next and Last turn off at the ends. `TbPaginationTests.Render_ShouldTurnOffTheStepsAtEachEndButKeepThemFocusable_WhenOnTheFirstOrLastPage`
- [x] When the page count shrinks below the parent's current page, a step button can still bring it back. `TbPaginationTests.Click_ShouldBringTheParentBack_WhenItsPageIsPastAShrunkenCount`
- [x] The ellipsis isn't a control. `TbPagination.razor`
- [x] With `PageHref`, each control is a real link that goes to its page. `PaginationTests.Pagination_ShouldLinkEachPage_WhenGivenPageHref`
- [x] With `PageHref`, a typed page goes to its link. `TbPaginationTests.Change_ShouldNavigateToTheTypedPagesLink_WhenCompactWithPageHref`
- [x] With `PageHref`, Ctrl+click, Cmd+click, Shift+click or Alt+click leaves the link to the browser, which opens it in a new tab or window or saves it, and leaves this tab on its page. `TbPaginationTests.Click_ShouldLeaveThisTabOnItsPage_WhenALinkIsClickedWithAModifier`
- [x] `Disabled` turns every control off. `TbPaginationTests.Render_ShouldTurnEveryControlOff_WhenDisabled`
- [x] In right-to-left text, the step arrows turn around. `PaginationTests.Pagination_ShouldTurnItsArrowsAround_WhenTheTextIsRightToLeft`

## Touch

- [x] Tapping a control goes to its page, since it listens for the click. `TbButtonBase.razor`
- [x] Controls too wide for their row wrap, so none is out of reach. `trailblazor.css`

## Focus

- [x] The chosen page keeps focus when the numbers shift around it. `PaginationTests.Pagination_ShouldGoToThePageAndKeepFocusOnIt_WhenAPageIsChosenWithTheKeyboard`
- [x] A step button that reaches the end keeps focus, turned off. `PaginationTests.Pagination_ShouldStepStopAndKeepFocusAtTheEnds_WhenFirstPreviousNextAndLastAreUsed`
- [x] A focused control shows a focus ring. `trailblazor.css`
- [x] The navigation is named "Pagination", or by `AriaLabel`. `TbPaginationTests.Render_ShouldNameTheNavigationByAriaLabel_WhenGivenOne`

## Not applicable

- Arrow keys between pages: a pagination is a list of separate controls, each its own tab stop.
- Scrolling to the top of the list after a page change: the app owns the list and where it sits.
