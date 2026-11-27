# Sparkline

Pattern: none; a sparkline is an image. It follows the [WAI tutorial on images](https://www.w3.org/WAI/tutorials/images/): a picture that says something has a text alternative, and one whose neighbor says the same is hidden.

Familiar apps: Excel and Google Sheets sparklines, GitHub's activity lines, stock tickers

## Keyboard

Nothing of its own: it is a picture, with nothing to focus or press.

## Pointer

Nothing of its own: it is a picture, with no tooltip and no script.

## Touch

Nothing of its own: it is a picture.

## Focus

- [x] It is one image named by its `AriaLabel`. `TbSparklineTests.Render_ShouldDrawALineFromItsLowestValueToItsHighest_WhenGivenValues`
- [x] Without an `AriaLabel` it is hidden from screen readers. `TbSparklineTests.Render_ShouldHideThePictureFromScreenReaders_WhenItHasNoName`
- [x] In forced-colors mode it is drawn in the text color, and an area keeps its line and loses its fill. `SparklineTests.Sparkline_ShouldBeDrawnInTheTextColor_WhenColorsAreForced`
- [x] It is 6rem wide and one and a half times its text's size high by default, in the flow of its text, and `Width` and `Height` set its size. `SparklineTests.Sparkline_ShouldBeALineOfTextHighInTheFlowOfItsText_WhenDrawn`, `SparklineTests.Sparkline_ShouldTakeItsToneColorAndSize_WhenTheyAreGiven`
- [x] A line runs from the left edge to the right one, with its lowest value at the bottom and its highest at the top. `SparklineTests.Sparkline_ShouldBeALineOfTextHighInTheFlowOfItsText_WhenDrawn`, `TbSparklineTests.Render_ShouldDrawALineFromItsLowestValueToItsHighest_WhenGivenValues`
- [x] Right to left, it runs from the right. `SparklineTests.Sparkline_ShouldRunFromTheRight_WhenThePageRunsRightToLeft`
- [x] An area is filled between its line and zero. `TbSparklineTests.Render_ShouldFillFromZeroUnderTheLine_WhenAnArea`
- [x] Bars stand on the zero line, or hang from it for a value below zero. `SparklineTests.Bars_ShouldStandOnTheZeroLineOrHangFromIt_WhenValuesAreOfBothSigns`, `TbSparklineTests.Render_ShouldDrawABarFromZeroForEachValue_WhenBars`
- [x] `Min` and `Max` set the bottom and the top, and a value beyond them is drawn at the edge, as every value is when an end is set beyond them all. `TbSparklineTests.Render_ShouldMeasureBetweenMinAndMax_WhenTheyAreGiven`, `TbSparklineTests.Render_ShouldDrawOnTheEdge_WhenAnEndIsSetBeyondEveryValue`
- [x] A missing value breaks the line and has no bar. `TbSparklineTests.Render_ShouldBreakTheLine_WhenAValueIsMissing`, `TbSparklineTests.Render_ShouldDrawABarFromZeroForEachValue_WhenBars`
- [x] For a line, values that are all the same run along the middle, and one value alone is a dot. `TbSparklineTests.Render_ShouldDrawAlongTheMiddle_WhenThereIsNothingToScaleBy`
- [x] With no values it draws nothing, and keeps its name. `TbSparklineTests.Render_ShouldDrawNothing_WhenThereAreNoValues`
- [x] A tone or a color takes the place of the first series color. `SparklineTests.Sparkline_ShouldTakeItsToneColorAndSize_WhenTheyAreGiven`

## Not applicable

- A tooltip, keys between values and a data table: a sparkline shows a shape, and a [chart](chart.md) is the component for values people read.
- Clicking a value: it is a picture. An app that wants a link puts the sparkline inside one.
