# Viewport

Pattern: none; a focusable, named `region` that scrolls, with zoom buttons and a [menu button](https://www.w3.org/WAI/ARIA/apg/patterns/menu-button/)

Familiar apps: Google Maps, Figma's and Miro's canvases, a PDF reader's zoom, Windows Photos

## Keyboard

- [x] With focus on the box, + and - zoom in and out by the step, and 0 goes back to 100%. `ViewportTests.Keys_ShouldZoomInOutAndBackToActualSize_WhenPressedOnTheBox`
- [x] The arrows, Page Up and Page Down, Home and End scroll the box, as they do any box that scrolls. `viewport.js`
- [x] The zoom keys work only while the box itself has focus, so a field inside the content keeps them. `viewport.js`
- [x] Each zoom from the keys, the buttons or the menu is read out. `TbViewportTests.Report_ShouldTakeTheZoomAndSaySo_WhenTheKeysZoom`, `TbViewportTests.ZoomInAsync_ShouldZoomByTheStepAndSaySo_WhenItsButtonIsClicked`

## Pointer

- [x] Ctrl with the wheel zooms around the pointer, which stays over the same spot of the content. `ViewportTests.Wheel_ShouldZoomAroundThePointer_WhenCtrlIsHeld`
- [x] A drag pans the content and focuses the box. `ViewportTests.Drag_ShouldPanTheContent_WhenTheMouseDragsTheBox`
- [x] A click on a button inside the content clicks it rather than starting a pan. `ViewportTests.Click_ShouldReachAButtonInTheContent_WhenItIsPressed`
- [x] The menu fits the content's width or the whole content, and a fit follows the box as it resizes until the user zooms. `ViewportTests.Fit_ShouldFitTheContentAndFollowTheBox_WhenPickedAndTheBoxResizes`
- [x] The zoom stays within its limits, and a zoom button at its limit is marked disabled but keeps focus. `TbViewportTests.Render_ShouldKeepTheZoomWithinItsLimits_WhenGivenOneOutside`

## Touch

- [x] Two fingers pinch to zoom. `ViewportTests.Pinch_ShouldZoomTheContent_WhenTwoFingersSpreadApart`
- [x] One finger pans the content, as a mouse drag does. `viewport.js`

## Focus

- [x] The box is a stop for Tab even when its content has nothing focusable. `TbViewportTests.Render_ShouldShowTheContentInANamedBoxUnderItsControls_WhenGivenALabel`

## Not applicable

- Rotating the content: a viewport scales and moves what it shows; the app can rotate the content itself.
- Arrow keys that pan by a set distance: the box scrolls with the browser's own keys, which every user already knows.
