# Carousel

Pattern: [Carousel](https://www.w3.org/WAI/ARIA/apg/patterns/carousel/), with buttons for each slide

Familiar apps: the featured stories at the top of a news site, a store's home page banners, a photo gallery's slides

## Keyboard

- [x] Tab reaches the stop button first, when there is one, then previous, next, the shown slide's content and each slide's button. `CarouselTests.Carousel_ShouldStopItsSlidesAtTheStopButton_WhenTabbedInto`, `CarouselTests.Carousel_ShouldTabThroughItsControlsInOrder_WhenTabbedInto`
- [x] Enter or Space on next shows the next slide, and focus stays on the button. `CarouselTests.Carousel_ShouldShowTheNextSlideAndReadItOut_WhenNextIsPressedFromTheKeyboard`
- [x] Keyboard focus anywhere in the carousel stops slides that turn by themselves until the start button is pressed, even after a click put focus there first. `CarouselTests.Carousel_ShouldStopItsSlidesAtTheStopButton_WhenTabbedInto`, `CarouselTests.Carousel_ShouldStopItsSlides_WhenTabMovesFocusOnFromAClickedButton`, `TbCarouselTests.AutoPlayInterval_ShouldStayStoppedUntilStartIsPressed_WhenKeyboardFocusEntersAndLeaves`
- [x] At the end of a carousel that doesn't loop, next turns off but keeps focus. `CarouselTests.Carousel_ShouldKeepFocusOnNext_WhenTheKeysReachTheLastSlideOfOneThatDoesntLoop`

## Pointer

- [x] Previous and next move between slides and wrap around the ends, or stop there without `Loop`. `TbCarouselTests.Buttons_ShouldMoveBetweenSlidesAndWrapAround_WhenPressed`, `TbCarouselTests.Render_ShouldTurnOffTheButtonsAtTheEnds_WhenItDoesntLoop`
- [x] A slide's button shows that slide and is marked as the current one. `TbCarouselTests.Picker_ShouldShowTheSlideItNamesAndMarkIt_WhenItsButtonIsPressed`
- [x] Turning slides pause while the pointer is over the carousel or focus from a click is in it, and go on once the pointer and focus have both left. `CarouselTests.Carousel_ShouldPauseItsTurningSlides_WhenThePointerIsOverIt`, `CarouselTests.Carousel_ShouldKeepItsSlidesTurning_WhenAClickFocusesAButtonAndThePointerLeaves`, `TbCarouselTests.AutoPlayInterval_ShouldPauseAndGoOn_WhenThePointerOrAPointersFocusComesAndGoes`
- [x] In forced-colors mode, the shown slide's button takes the system's highlight. `CarouselTests.Carousel_ShouldMarkTheShownSlidesButtonInTheSystemHighlight_WhenColorsAreForced`
- [x] The stop button stops the turning slides until it's pressed again. `TbCarouselTests.AutoPlayInterval_ShouldTurnTheSlidesAndStopWhenAsked_WhenGivenAnInterval`

## Touch

- [ ] Tapping the buttons does what clicking does. UNCONFIRMED

## Focus

- [x] Screen readers hear a carousel named by its label, and each slide as a slide named "2 of 3", or by its own label. `TbCarouselTests.Render_ShouldBeANamedCarouselShowingItsFirstSlide_WhenCalled`
- [x] A slide the user brings in is read out; slides that turn by themselves aren't. `TbCarouselTests.AutoPlayInterval_ShouldTurnTheSlidesAndStopWhenAsked_WhenGivenAnInterval`
- [x] Removing an earlier slide keeps the shown one shown and reports its new position. `TbCarouselTests.Remove_ShouldKeepTheShownSlideAndReportItsNewIndex_WhenAnEarlierSlideGoes`
- [x] Slides don't turn by themselves for someone who asks for reduced motion. `CarouselTests.Carousel_ShouldStartWithItsSlidesStill_WhenTheUserAsksForReducedMotion`, `TbCarouselTests.AutoPlayInterval_ShouldStartStopped_WhenTheUserAsksForReducedMotion`

## Not applicable

- Arrow keys between slides: this follows the pattern's basic carousel, where each button is its own tab stop. Arrow keys belong to the tabbed carousel, whose slide picker is a tab list.
- Swiping: slides change only with the buttons, which work for touch, mouse and keyboard alike; a swipe is a hidden gesture people would have to discover.
