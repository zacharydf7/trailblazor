# Back to top

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/)

Familiar apps: the back to top button on GitHub's docs pages, Ant Design's `FloatButton.BackTop` and MUI's scroll-to-top demo

## Keyboard

- [x] Enter and Space scroll to the top and move focus there, so the next Tab starts at the top. `BackToTopTests.BackToTop_ShouldScrollUpAndMoveFocusThere_WhenPressedWithTheKeyboard`
- [x] While the page is near the top, Tab passes over the hidden button. `BackToTopTests.BackToTop_ShouldShowOnlyOnceThePageHasScrolledDown_WhenScrolled`

## Pointer

- [x] The button shows once the page has scrolled past `Threshold`, and hides again near the top. `BackToTopTests.BackToTop_ShouldShowOnlyOnceThePageHasScrolledDown_WhenScrolled`
- [x] Clicking it scrolls to the top. `BackToTopTests.BackToTop_ShouldJumpToTheTop_WhenReducedMotionIsPreferred`
- [x] An element that scrolls on its own, given as `ScrollContainer`, is followed and taken back to its top. `BackToTopTests.BackToTop_ShouldFollowAndScrollTheContainer_WhenGivenOne`
- [x] A `ScrollContainer` that renders after the button is still followed. `BackToTopTests.BackToTop_ShouldFollowAContainerThatRendersLater_WhenGivenOne`
- [x] A changed `ScrollContainer` or `Threshold` is sent on to the script. `TbBackToTopTests.Render_ShouldSendTheScriptANewContainerOrThreshold_WhenTheyChange`
- [x] For someone who asks for reduced motion, the page goes to the top at once. `BackToTopTests.BackToTop_ShouldJumpToTheTop_WhenReducedMotionIsPreferred`

## Touch

- [x] Tapping does what clicking does, and nothing needs hover. `TbBackToTop.razor`

## Focus

- [x] The button is named "Back to top", unless `Label` names it. `TbBackToTopTests.Render_ShouldNameTheButton_WhenCalled`
- [x] In forced colors, the button keeps a border, though its shadow is dropped. `ForcedColorsTests.Example_ShouldDrawABorderAroundEachButtonAndSurface_WhenColorsAreForced`
