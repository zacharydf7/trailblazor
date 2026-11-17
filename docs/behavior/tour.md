# Product tour

Pattern: [Dialog (modal)](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/), one for each step

Familiar apps: the first-run walkthroughs in Figma and Notion, GitHub's tours of new features

## Keyboard

- [x] Each new step gets focus, and Tab stays in it. `TourTests.Tour_ShouldKeepFocusInTheStep_WhenTabbing`
- [x] Escape skips the tour, and focus goes back to the button that started it. `TourTests.Tour_ShouldGiveFocusBack_WhenEscapeSkipsIt`
- [x] Next, Back, Done and Skip tour move through the steps or end the tour. `TbTourTests.NextAsync_ShouldMoveThroughTheStepsAndShowEach_WhenPressed`, `TbTourTests.NextAsync_ShouldEndTheTourAsCompleted_WhenPressedOnTheLastStep`, `TbTourTests.SkipAsync_ShouldEndTheTourAsSkipped_WhenPressed`

## Pointer

- [x] Each step sits beside its target, which the shade rings while dimming the rest of the page. `TourTests.Tour_ShouldPlaceTheStepByItsTargetAndRingIt_WhenShown`
- [x] The page around the step can't be clicked while the tour shows. `TourTests.Tour_ShouldPlaceTheStepByItsTargetAndRingIt_WhenShown`
- [x] A step whose target isn't on the page, or whose selector isn't valid, shows in the middle of the window. `TourTests.Tour_ShouldCenterTheStep_WhenItsTargetIsMissingOrNotValid`
- [x] For someone who asks for reduced motion, the highlight moves to each target at once. `TourTests.Tour_ShouldMoveTheHighlightAtOnce_WhenReducedMotionIsPreferred`

## Touch

- [x] Tapping the buttons does what clicking does, and nothing needs hover. `TbTour.razor`

## Focus

- [x] Each step is a modal dialog named by its title and described by its body. `TbTourTests.Render_ShouldShowTheStepAsADialogNamedByItsTitle_WhenOpen`
- [x] The page behind the tour is inert until it ends, so screen readers and Tab can't reach it. `TourTests.Tour_ShouldMakeThePageBehindItInert_WhenShown`
- [x] The app hears whether the tour was completed or skipped. `TbTourTests.NextAsync_ShouldEndTheTourAsCompleted_WhenPressedOnTheLastStep`, `TbTourTests.Escape_ShouldEndTheTourAsSkipped_WhenTheScriptHearsIt`
