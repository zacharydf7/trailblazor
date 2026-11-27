# Gauge

Pattern: [Meter](https://www.w3.org/WAI/ARIA/apg/patterns/meter/)

Familiar apps: a car's fuel gauge, Grafana's gauge panel, a phone's battery and storage meters

## Keyboard

Nothing of its own. It only shows a value, so it has no keys.

## Pointer

Nothing of its own: it has no tooltip and no script. A slider is for setting a value.

## Touch

Nothing of its own, for the same reason.

## Focus

- [x] It is never a tab stop. `TbGauge.razor`
- [x] It is a meter with its lowest, highest and current values, named by its `Label` or its `AriaLabel`, and it throws without either. `TbGaugeTests.Render_ShouldBeAMeterFilledUpToItsValue_WhenGivenAValue`, `TbGaugeTests.Render_ShouldBeNamedByItsAriaLabel_WhenItHasALabelToo`, `TbGaugeTests.Render_ShouldThrow_WhenItHasNoName`
- [x] Screen readers hear the text in its middle for its value, in its `Format` or else as the current culture writes the number. `TbGaugeTests.Render_ShouldWriteItsValueAndEndsInItsFormat_WhenGivenOne`, `TbGaugeTests.Render_ShouldWriteTextAsTheCultureDoesAndNumbersWithAPeriod_WhenTheCultureUsesACommaForDecimals`
- [x] The arc is filled up to the value, around the value's text, with the label under the dial. `GaugeTests.Gauge_ShouldFillItsArcUpToItsValueAroundItsText_WhenDrawn`
- [x] A value beyond an end is drawn at that end, and its text still says the value. `TbGaugeTests.Render_ShouldDrawWithinItsEndsAndSayTheValueItself_WhenGivenAnyValue`
- [x] Without a value, or with one that isn't finite, nothing is filled and it shows and says "No value". `TbGaugeTests.Render_ShouldSayThereIsNoValueAndFillNothing_WhenItHasNoValue`
- [x] `Labels` rewords "No value" and how a value is said with its range. `TbGaugeTests.Labels_ShouldRewordWhatTheGaugeShowsAndSays_WhenGiven`
- [x] `Max` has to be above `Min`, and both finite, or it throws. `TbGaugeTests.Render_ShouldThrow_WhenMaxIsNotAboveMinOrAnEndIsNotFinite`
- [x] A range's name is shown under the value and read out after it, and the filled part takes the range's tone. `TbGaugeTests.Ranges_ShouldNameTheValuesRangeAndGiveItsTone_WhenTheValueIsInOne`, `GaugeTests.Gauge_ShouldTakeTheNameAndToneOfTheRange_WhenItsValueMovesIntoIt`
- [x] A range holds the value it ends at, and the first and the last hold a value beyond their end of the dial. `TbGaugeTests.Ranges_ShouldHoldTheValueTheyEndAt_WhenTheValueIsOnTheLineBetweenTwo`
- [x] Each range is a band around the value's arc, and two bands that meet stop a little short of each other. `GaugeTests.Ranges_ShouldRunAroundTheValuesArc_WhenDrawn`, `TbGaugeTests.Ranges_ShouldStopShortOfEachOtherAndReachTheDialsEnds_WhenDrawn`
- [x] A value past the last range has no range name and the gauge's own tone. `TbGaugeTests.Ranges_ShouldLeaveTheValueUnnamedInItsOwnTone_WhenTheyStopShortOfIt`
- [x] A range without a tone or a name draws a neutral band and changes nothing else. `TbGaugeTests.Ranges_ShouldKeepTheGaugesToneAndSayOnlyTheValue_WhenARangeHasNoToneOrName`
- [x] Ranges go by their ends, whatever order they are written in or appear in. One that ends below `Min` has no length and holds no value, and one that ends past `Max` stops there. `TbGaugeTests.Ranges_ShouldRunInTheOrderOfTheirEnds_WhenWrittenInAnotherOrder`, `TbGaugeTests.Ranges_ShouldPlaceARangeByItsEnd_WhenItAppearsAfterTheOthers`
- [x] The gauge draws again when a range changes or leaves. `TbGaugeTests.Ranges_ShouldDrawAgain_WhenARangeChangesOrLeaves`
- [x] With `ShowEnds`, `Min` and `Max` are written under the arc's two ends. `GaugeTests.Ends_ShouldBeUnderTheArcsEndsWithTheLowestOnTheLeft_WhenThePageRunsEitherWay`
- [x] Right to left, the dial still runs clockwise from the lower left. `GaugeTests.Ends_ShouldBeUnderTheArcsEndsWithTheLowestOnTheLeft_WhenThePageRunsEitherWay`
- [x] It is 10rem wide by default, `Diameter` sets its width, and its text scales with it. `GaugeTests.Text_ShouldScaleWithTheGauge_WhenGivenADiameter`
- [x] `--tb-gauge-diameter` sets the width of every gauge under an element, and a gauge is never wider than the room it has. `GaugeTests.Gauges_ShouldTakeTheDiameterSetAroundThemAndNoMoreThanTheRoom_WhenInsideCards`
- [x] In forced-colors mode the filled part takes the highlight color and the bands the text color. `GaugeTests.Gauge_ShouldTakeSystemColors_WhenColorsAreForced`
- [x] A change of value moves the filled part over a fifth of a second, or at once with reduced motion. `GaugeTests.Gauge_ShouldMoveItsFilledPartAtOnce_WhenMotionIsReduced`

## Not applicable

- Saying each new value aloud: it would interrupt everything else, so screen readers read the value when they reach the gauge, and the app announces a change people must not miss.
- A needle, several values on one dial and dragging the value: a needle adds nothing the filled arc doesn't show, several values compare better as bars in a [chart](chart.md), and a [slider](slider.md) is for setting a value.
- A tooltip: the value and its range are already written in the dial.
