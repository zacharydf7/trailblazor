# Rating

Pattern: [Radio group](https://www.w3.org/WAI/ARIA/apg/patterns/radio/), one radio for each star

Familiar apps: the star rating of a shop's review form, and of an app store

## Keyboard

- [x] The rating is one tab stop, landing on the chosen star, since its stars are native radios that share a name. `TbRatingTests.Render_ShouldBeALabeledGroupOfNamedStarsSharingAName_WhenCalled`
- [x] The arrow keys choose the next or previous star, and focus moves with them. `RatingTests.Rating_ShouldChooseTheNextStar_WhenTheRightArrowIsPressed`
- [x] The focused star shows a focus ring. `RatingTests.Rating_ShouldRingTheFocusedStar_WhenItHasKeyboardFocus`
- [x] With `Clearable`, Space on the chosen star takes the rating back. `RatingTests.Rating_ShouldTakeTheRatingBack_WhenSpaceIsPressedOnTheChosenStar`

## Pointer

- [x] Clicking a star chooses it and every star before it. `TbRatingTests.Change_ShouldRaiseValueChangedWithTheStarsNumber_WhenAStarIsChosen`, `TbRatingTests.Render_ShouldBeALabeledGroupOfNamedStarsSharingAName_WhenCalled`
- [x] Pointing at a star previews the rating it would give. `RatingTests.Rating_ShouldPreviewTheRatingAStarWouldGive_WhenPointedAt`
- [x] With `Clearable`, clicking the chosen star again takes the rating back to 0; without it, nothing changes. `RatingTests.Rating_ShouldTakeTheRatingBack_WhenTheChosenStarIsClickedAgain`, `TbRatingTests.Click_ShouldTakeTheRatingBackOnlyWhenClearable_WhenTheChosenStarIsChosenAgain`
- [x] A read-only rating can't be changed, and only its chosen star stays focusable; with nothing chosen, every star is off. `TbRatingTests.Render_ShouldTurnOffEveryStarButTheChosenOne_WhenReadOnly`
- [x] Chosen stars are filled and the rest are outlines, so the rating doesn't rely on color. `RatingTests.Rating_ShouldFillTheChosenStarsAndOutlineTheRest_WhenRendered`
- [x] The stars follow their radios, so they show a choice made before the page is interactive. `RatingTests.Rating_ShouldShowTheChosenStarsFromItsRadios_WhenThePageHasNotRenderedTheChoice`
- [x] In forced-colors mode, the chosen stars take the system's highlight. `RatingTests.Rating_ShouldHighlightTheChosenStars_WhenColorsAreForced`

## Touch

- [x] Tapping a star does what clicking does. The preview shows only on devices that can hover, so a tapped star doesn't stay lit. `trailblazor.css`

## Focus

- [x] Screen readers hear a group named by its label, and each star as "1 star", "2 stars" and so on, or by `StarLabel`. `TbRatingTests.Render_ShouldBeALabeledGroupOfNamedStarsSharingAName_WhenCalled`, `TbRatingTests.Render_ShouldShowTheGivenNumberOfStarsWithTheirNames_WhenMaxAndStarLabelAreSet`
- [x] A required rating with no stars chosen is invalid. `TbRatingTests.Click_ShouldMarkTheRatingInvalid_WhenARequiredRatingIsTakenBack`

## Not applicable

- Half stars, and showing an average such as 4.3: a rating is a choice of whole stars; show an average as text.
- Taking the rating back with a key other than Space: Space on the chosen star does it with `Clearable`.
