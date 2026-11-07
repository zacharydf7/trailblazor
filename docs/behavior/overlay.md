# Overlay

Pattern: none; a scrim has no pattern of its own, and `Blocking` applies the inert page of a [modal dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/)

Familiar apps: loading scrims over a card or a page, the shade behind a lightbox

## Keyboard

- [x] With `CloseOnClick`, Escape closes the overlay. `OverlayTests.Overlay_ShouldCloseOnAClickOnTheScrimOrEscape_WhenCloseOnClickIsSet`
- [x] Without `CloseOnClick`, Escape does nothing. `TbOverlay.razor`
- [x] Escape closes only the topmost layer of several. `DismissableLayerTests.EscapeKey_ShouldDismissOnlyTopmostLayer_WhenLayersAreNested`
- [ ] Escape goes first to a menu, popover or dialog open above the overlay. #330
- [x] With `Blocking`, Tab can't reach the page behind. `OverlayTests.Overlay_ShouldKeepFocusFromThePageAndGiveItBack_WhenAPageOverlayIsBlocking`
- [x] A contained `Blocking` overlay makes only its region inert. `OverlayTests.Overlay_ShouldMakeOnlyItsRegionInert_WhenAContainedOverlayIsBlocking`
- [x] Without `Blocking`, the keyboard still reaches what's behind. `TbOverlayTests.Render_ShouldLeaveThePageAlone_WhenNotBlocking`

## Pointer

- [x] With `CloseOnClick`, a click on the scrim closes it, and a click on its content doesn't. `OverlayTests.Overlay_ShouldCloseOnAClickOnTheScrimOrEscape_WhenCloseOnClickIsSet`
- [x] A drag from the content that ends on the scrim doesn't close it, since the click lands on neither. `TbOverlay.razor`
- [x] Without `CloseOnClick`, a click is only reported. `TbOverlayTests.Click_ShouldOnlyReportTheClick_WhenCloseOnClickIsOff`
- [x] A contained overlay catches clicks over its region only. `OverlayTests.Overlay_ShouldCoverOnlyItsRegionAndCatchClicksThere_WhenContained`
- [x] With `LockScroll`, the page behind can't scroll. `OverlayTests.Overlay_ShouldStopThePageScrolling_WhileAPageOverlayLocksScroll`
- [ ] The page doesn't shift sideways as `LockScroll` removes its scrollbar. #301

## Touch

- [x] A tap on the scrim works as a click. `TbOverlay.razor`
- [ ] With `LockScroll`, the page doesn't scroll under a finger. UNCONFIRMED

## Focus

- [x] Focus that was behind a `Blocking` overlay goes back when it closes. `OverlayTests.Overlay_ShouldKeepFocusFromThePageAndGiveItBack_WhenAPageOverlayIsBlocking`
- [x] Focus the user moved elsewhere stays there. `overlay.js`
- [x] What a second `Blocking` overlay covers stays inert when the first closes. `OverlayTests.Overlay_ShouldKeepThePageBlocked_WhenTheFirstOfTwoBlockingOverlaysClosesFirst`
- [x] Dialogs, toasts and announcements stay usable above a `Blocking` overlay. `overlay.js`

## Not applicable

- Moving focus into the overlay or trapping it there: use a dialog for content that needs an answer.
- Blocking the keyboard by default: an overlay blocks only the pointer unless it has `Blocking`, as for a map locked against clicks.
