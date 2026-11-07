# Table of contents

Pattern: none in the Authoring Practices; a named navigation landmark of links, the current one marked `aria-current="location"`

Familiar apps: the "On this page" panel of documentation sites, GitHub's outline of a README

## Keyboard

- [x] Tab moves through the links in order, since they are plain links. `TbTableOfContents.razor`
- [x] Enter on a link scrolls to its section and moves focus there, as a click does. `table-of-contents.js`

## Pointer

- [x] Clicking a link scrolls to the section and updates the address. `TableOfContentsTests.TableOfContents_ShouldScrollToTheSectionAndFocusIt_WhenANestedLinkIsFollowed`
- [x] With reduced motion, it jumps instead of scrolling smoothly. `TableOfContentsTests.TableOfContents_ShouldJumpWithoutScrollingSmoothly_WhenReducedMotionIsPreferred`
- [x] Ctrl, Cmd, Shift, Alt and middle clicks stay with the browser. `table-of-contents.js`
- [x] Before the script attaches, the links are real addresses that still jump. `TbTableOfContentsTests.Render_ShouldListLinksToEachSectionInANamedLandmark_WhenGivenItems`
- [x] Scrolling marks the link to the section in view, in the page or a scrolling panel. `TableOfContentsTests.TableOfContents_ShouldMarkTheSectionInView_WhenThePanelScrolls`
- [x] Scrolled to the end, the last section showing is marked. `TableOfContentsTests.TableOfContents_ShouldMarkTheLastSectionShowing_WhenThePanelIsScrolledToTheEnd`
- [ ] Following a link marks that link, even when a later section is also near the top or the scroll can't bring it there. #326
- [x] `ActiveChanged` reports each section that comes into view. `TableOfContentsTests.TableOfContents_ShouldReportTheSection_WhenAnotherComesIntoView`

## Touch

- [x] Tapping a link follows it, since it listens for the click. `table-of-contents.js`
- [x] A swipe marks the section in view, since it listens for scrolling, whatever caused it. `table-of-contents.js`

## Focus

- [x] Following a link moves focus to the section, so Tab goes on from there. `TableOfContentsTests.TableOfContents_ShouldScrollToTheSectionAndFocusIt_WhenANestedLinkIsFollowed`
- [x] A focused link shows a focus ring. `trailblazor.css`

## Not applicable

- Back after following a link: the address is replaced, not pushed, because Blazor's router can't read an entry it didn't make. Back leaves the page.
- Keeping the marked link in view: the list has no scrolling of its own, and an app that makes it scroll owns that.
