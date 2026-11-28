# Lightbox

Pattern: [Dialog (modal)](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/)

Familiar apps: photo viewers in Google Photos, GitHub's image previews, online shops' product photos

## Keyboard

- [x] Left and Right show the previous and next image, swapped in right-to-left text, and a new `Index` from the app shows that image fitted. `TbLightboxTests.Render_ShouldShowTheParentsIndexFitted_WhenItChangesWhileOpen`, `LightboxTests.Lightbox_ShouldMoveBetweenImagesWithTheArrowKeys_WhenFocusIsInIt`
- [x] On a zoomed image, the arrow keys scroll it instead. `LightboxTests.Lightbox_ShouldPanAZoomedImage_WhenItsStageHasFocus`
- [x] + and - zoom a step, 0 fits the image again, R and Shift+R turn it. `LightboxTests.Lightbox_ShouldZoomAndTurnWithKeys_WhenFocusIsInIt`
- [x] Escape closes the lightbox, and focus goes back to the thumbnail that opened it. `LightboxTests.Lightbox_ShouldGiveFocusBack_WhenClosedWithEscape`
- [x] Tab never reaches the page behind the lightbox, which is a modal dialog. `DialogTests.Dialog_ShouldFocusTheFirstFieldAndKeepFocusOffThePage_WhenModal`

## Pointer

- [x] A double click zooms to twice the fitted size and back. `TbLightboxTests.DoubleClick_ShouldZoomToTwiceTheFittedSizeAndBack_WhenOnTheStage`
- [x] Dragging a zoomed image pans it. `LightboxTests.Lightbox_ShouldPanAZoomedImage_WhenDraggedWithTheMouse`
- [x] Each image fits the window at its own shape, never past its own size, including after a quarter turn. `LightboxTests.Lightbox_ShouldFitTheImageToTheWindow_WhenShownOrTurned`

## Touch

- [x] A swipe moves to the previous or next image while the image isn't zoomed, one image per swipe, even after a touch the browser took over for a scroll or pinch. `LightboxTests.Lightbox_ShouldMoveBetweenImages_WhenSwiped`, `LightboxTests.Lightbox_ShouldMoveOneImage_WhenSwipedAfterACanceledTouch`

## Screen readers

- [x] Each new image is read out with its alt text and position, as focus stays on the button. `TbLightboxTests.NextAsync_ShouldShowTheNextImageAndWrapAround_WhenPressed`, `LightboxTests.Lightbox_ShouldMoveBetweenImagesWithTheArrowKeys_WhenFocusIsInIt`
- [x] A zoomed image's scrolling area is a `group` named by its alt text, in the tab order. `TbLightboxTests.ZoomInAsync_ShouldStepUpToFourTimesAndMakeTheStageATabStop_WhenPressed`
- [x] Zooming back out while the image's area has focus keeps focus there, so the keys go on working. `LightboxTests.Lightbox_ShouldKeepFocusOnTheStage_WhenItsImageIsZoomedBackOut`
- [x] In forced colors, the lightbox has a border on every side, which sets it off from the page. `ForcedColorsTests.Popup_ShouldHaveABorderOnEverySide_WhenColorsAreForced`

## Not applicable

- Pinching to zoom the image: the zoom buttons, keys and a double click zoom it, and a pinch zooms the page as it does anywhere.
