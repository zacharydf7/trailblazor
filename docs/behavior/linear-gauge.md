# Linear gauge

Pattern: [Meter](https://www.w3.org/WAI/ARIA/apg/patterns/meter/)

Familiar apps: a phone's storage bar, a bullet graph in Tableau or Power BI, a budget bar in a banking app

## Keyboard

Nothing of its own. It only shows a value, so it has no keys.

## Pointer

Nothing of its own: it has no tooltip and no script. A slider is for setting a value.

## Touch

Nothing of its own, for the same reason.

## Focus

- [x] It is never a tab stop. `TbLinearGauge.razor`
- [x] It is a meter with its lowest, highest and current values, named by its `Label` or its `AriaLabel`, and it throws without either. `TbLinearGaugeTests.Render_ShouldBeAMeterWithItsBarFilledUpToItsValue_WhenGivenAValue`, `TbLinearGaugeTests.Render_ShouldBeNamedByItsAriaLabelWithNoLabelPart_WhenGivenOnlyAnAriaLabel`, `TbLinearGaugeTests.Render_ShouldThrowInItsOwnName_WhenItHasNoNameOrItsEndsAreOutOfOrder`
- [x] The bar is as wide as the room it has and filled from its start up to the value, with the label above its start and the value's text above its end. `LinearGaugeTests.Gauge_ShouldFillItsBarUpToItsValueUnderItsLabelAndText_WhenDrawn`
- [x] Without a label, the value's text stays above the bar's end. `LinearGaugeTests.Value_ShouldStayAboveTheBarsEnd_WhenTheGaugeHasNoLabel`
- [x] A label too long for one line wraps beside the value's text, which keeps its one line. `LinearGaugeTests.Label_ShouldWrapBesideTheValuesText_WhenItIsTooLongForOneLine`
- [x] Where the gauge is too narrow for the value's text and the range's name side by side, the name goes under the value, each still on one line. `LinearGaugeTests.RangeName_ShouldGoUnderTheValue_WhenTheGaugeIsTooNarrowForBoth`, `LinearGaugeTests.RangeName_ShouldStayBesideTheValue_WhenALongLabelWrapsAndThereIsRoomForBoth`
- [x] A value beyond an end is drawn at that end, and its text still says the value. `TbLinearGaugeTests.Render_ShouldDrawWithinItsEndsAndSayTheValueItself_WhenGivenAnyValue`, `LinearGaugeTests.Gauge_ShouldFillNothing_WhenItsValueIsBelowItsLowestOrMissing`
- [x] Without a value, or with one that isn't finite, nothing is filled and it shows and says "No value", or what `Labels.NoValue` says. `TbLinearGaugeTests.Render_ShouldSayThereIsNoValueAndFillNothing_WhenItHasNoValue`, `LinearGaugeTests.Gauge_ShouldFillNothing_WhenItsValueIsBelowItsLowestOrMissing`
- [x] A range's name is shown after the value and read out after it, and the filled part takes the range's tone. `TbLinearGaugeTests.Ranges_ShouldNameTheValuesRangeAndDrawABandForEach_WhenTheValueIsInOne`, `LinearGaugeTests.Gauge_ShouldTakeTheNameAndToneOfTheRange_WhenItsValueMovesIntoIt`
- [x] Each range is a band under the bar between its two values, with the surface showing where two bands meet. `LinearGaugeTests.Bands_ShouldRunBetweenTheirValuesUnderTheBar_WhenDrawn`
- [x] A range without a tone draws a neutral band and leaves the filled part its own tone. `TbLinearGaugeTests.Ranges_ShouldKeepTheGaugesToneAndDrawANeutralBand_WhenARangeHasNoTone`
- [x] Ranges go by their ends and hold the value they end at, as a [gauge](gauge.md)'s do. `TbGaugeTests.Ranges_ShouldRunInTheOrderOfTheirEnds_WhenWrittenInAnotherOrder`, `TbGaugeTests.Ranges_ShouldHoldTheValueTheyEndAt_WhenTheValueIsOnTheLineBetweenTwo`
- [x] A value past the last range has no range name and the gauge's own tone, and the gauge draws again when a range changes or leaves, as a gauge does. `TbGaugeTests.Ranges_ShouldLeaveTheValueUnnamedInItsOwnTone_WhenTheyStopShortOfIt`, `TbGaugeTests.Ranges_ShouldDrawAgain_WhenARangeChangesOrLeaves`
- [x] `Labels` rewords "No value", how a value is said with its range, and how a target is said after them. `TbGaugeTests.Labels_ShouldRewordWhatTheGaugeShowsAndSays_WhenGiven`, `TbLinearGaugeTests.Target_ShouldFollowTheRangeAndKeepItsMark_WhenTheGaugeHasRangesOrNoValue`
- [x] A target is a mark across the bar at its value, and is read out after the value and its range, in the value's format and also when the gauge has no value. `TbLinearGaugeTests.Target_ShouldBeMarkedOnTheBarAndReadAfterTheValue_WhenGiven`, `TbLinearGaugeTests.Target_ShouldFollowTheRangeAndKeepItsMark_WhenTheGaugeHasRangesOrNoValue`, `LinearGaugeTests.Target_ShouldBeMarkedAcrossTheBarAtItsValue_WhenThePageRunsEitherWay`
- [x] A target beyond an end is marked at that end and read out as it is, and one that isn't finite is left out. `TbLinearGaugeTests.Target_ShouldBeDrawnAtTheEndAndSaidAsItIs_WhenItIsBeyondAnEnd`, `TbLinearGaugeTests.Target_ShouldNotBeDrawnOrSaid_WhenItIsNotFinite`
- [x] With `ShowEnds`, `Min` and `Max` are written under the bar's two ends in the value's format. `TbLinearGaugeTests.Render_ShouldWriteItsEndsUnderTheBarInItsFormat_WhenShowingThem`
- [x] Right to left, the bar fills from the right, with `Min` under its right end and the target mirrored. `LinearGaugeTests.Target_ShouldBeMarkedAcrossTheBarAtItsValue_WhenThePageRunsEitherWay`
- [x] Text is written as the current culture writes it, and the numbers in the markup with a period. `TbLinearGaugeTests.Render_ShouldWriteNumbersWithAPeriod_WhenTheCultureUsesACommaForDecimals`
- [x] `--tb-linear-gauge-height` sets how high the bar is, 0.5rem by default. `LinearGaugeTests.Bar_ShouldTakeTheHeightSetForIt_WhenTheTokenIsGiven`
- [x] In forced-colors mode the bar has an outline, the filled part takes the highlight color, and the bands and the target the text color. `LinearGaugeTests.Gauge_ShouldTakeSystemColors_WhenColorsAreForced`
- [x] A change of value moves the filled part over a fifth of a second, or at once with reduced motion. `LinearGaugeTests.Gauge_ShouldMoveItsFilledPartAtOnce_WhenMotionIsReduced`

## Not applicable

- Saying each new value aloud: it would interrupt everything else, so screen readers read the value when they reach the gauge, and the app announces a change people must not miss.
- A vertical bar, several values on one bar and dragging the value: a [chart](chart.md)'s bars compare several values, and a [slider](slider.md) is for setting one.
- A tooltip: the value and its range are already written above the bar.
