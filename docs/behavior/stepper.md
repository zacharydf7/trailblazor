# Stepper

Pattern: none in WAI-ARIA; a list of steps, with `aria-current="step"` on the current one

Familiar apps: the checkout steps of a shop, and the setup of a new phone

## Keyboard

- [x] Tab moves between the steps people can go to, and skips the ones they can't, which aren't buttons. `StepperTests.Stepper_ShouldSkipTheStepsAheadWithTab_WhenItIsLinear`
- [x] Enter or Space on a finished step goes back to it, and focus stays on it. `StepperTests.Stepper_ShouldGoBackToAFinishedStepAndKeepFocusOnIt_WhenEnterIsPressed`

## Pointer

- [x] Clicking a completed step goes to it; in a linear stepper, steps ahead can't be chosen. `TbStepperTests.StepButton_ShouldMoveToACompletedStep_WhenPressed`
- [x] With `Linear="false"`, any step can be chosen. `TbStepperTests.Render_ShouldLetPeopleGoToAnyStep_WhenNotLinear`
- [x] A disabled step can't be chosen. `TbStepperTests.Render_ShouldKeepPeopleFromAStep_WhenItIsDisabled`
- [x] The steps are numbered in the order they appear, even when the app adds one later. `trailblazor.css`
- [x] A vertical stepper shows the current step's content under it, and only that step's. `TbStepperTests.Render_ShouldShowTheCurrentStepsContentOnlyInAColumn_WhenGivenALayout`
- [x] On a phone, or wherever a horizontal stepper's steps get narrow, each title goes under its number, so it can use the step's whole width. `StepperTests.Stepper_ShouldPutEachTitleUnderItsNumber_WhenTheScreenIsAPhone`, `StepperTests.Stepper_ShouldPutEachTitleUnderItsNumber_WhenSixStepsShareAMidWidth`
- [x] A vertical stepper squeezed beside other content in a row breaks its titles only between words. `StepperTests.Stepper_ShouldBreakTitlesOnlyBetweenWords_WhenVerticalInARow`
- [x] Clicking the current step does nothing. `TbStepperTests.StepButton_ShouldDoNothing_WhenItIsTheCurrentStep`

## Touch

- [x] Tapping does what clicking does, and nothing depends on hover. `TbStep.razor`

## Focus

- [x] Screen readers hear a list named "Progress" whose current step has `aria-current="step"`, and "Completed" after a finished step's title. `TbStepperTests.Render_ShouldListTheStepsAndMarkTheCurrentOne_WhenGivenSteps`
- [x] A step's button is named by every word it shows, then "Completed" for a finished step, so a name spoken to voice control matches what is on screen. `TbStepperTests.StepButton_ShouldEndItsTextWithItsState_WhenItIsCompleted`
- [x] When the step changes, screen readers hear "Current step: Gear", since moving on doesn't move focus. `TbStepperTests.ActiveKey_ShouldAnnounceTheNewStep_WhenItChangesAfterTheFirstRender`
- [x] `Labels` replaces the "Optional" note, the "Completed" word and the "Current step" announcement. `TbStepperTests.Labels_ShouldWordTheOptionalCompletedAndCurrentStep_WhenGiven`
- [x] A step with an error shows its message, and a warning in place of its number or check. `TbStepperTests.Render_ShouldShowTheErrorInPlaceOfTheCheck_WhenAStepHasOne`
- [x] In forced-colors mode, the current step's number takes the system's highlight, and the lines between steps stay drawn. `StepperTests.Stepper_ShouldHighlightTheCurrentStepAndKeepTheLines_WhenColorsAreForced`

## Not applicable

- Arrow keys between the steps: the steps aren't tabs, and Tab reaches every step people can go to.
- Back and Next buttons, and checking a step before moving on: the app owns both, since only it knows what a step needs.
