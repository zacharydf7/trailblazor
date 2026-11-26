# Chart

Pattern: none; the WAI-ARIA Authoring Practices have no chart pattern. The picture and its table follow the [WAI tutorial on complex images](https://www.w3.org/WAI/tutorials/images/complex/), and the keys follow the ones shared by chart libraries that support the keyboard.

Familiar apps: Excel and Google Sheets charts, Grafana panels, Highcharts' accessibility module

## Keyboard

- [x] Tab reaches the plot once, at the point last visited or the first point. `ChartTests.Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus`
- [x] Right and Left move to the next and previous point of the series, and swap in right-to-left layouts. `ChartTests.Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus`, `ChartTests.Keys_ShouldSwapLeftAndRight_WhenTheChartRunsRightToLeft`
- [x] A place where the series has no value is passed over. `ChartTests.Keys_ShouldStepOverAGap_WhenASeriesHasNoValueAtAPlace`
- [x] Down and Up move to the same place in the next and previous series that has a value there, in the legend's order. `ChartTests.Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus`, `ChartTests.Escape_ShouldHideTheTooltipAndLeaveTheDialogOpen_WhenTheChartIsInADialog`
- [x] Home and End move to the first and last point of the series. `ChartTests.Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus`
- [x] Enter or Space activates the point, as a click does. `ChartTests.Point_ShouldTellTheApp_WhenClickedOrChosenWithTheKeys`
- [x] Escape hides the tooltip and leaves focus where it is. `ChartTests.Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus`
- [x] Escape hides a tooltip the pointer shows while focus is elsewhere, and a dialog around the chart stays open for that press. `ChartTests.Escape_ShouldHideTheTooltipAndLeaveTheDialogOpen_WhenTheChartIsInADialog`
- [x] Escape goes first to a field being edited in the same dialog, even while the pointer shows the tooltip. `ChartTests.Escape_ShouldCancelAnEditFirst_WhenThePointerRestsOnAChartInTheSameDialog`
- [x] Tab reaches each legend button, and Enter or Space hides or shows its series, with focus left on the button. `ChartTests.Legend_ShouldHideAndShowASeries_WhenItsButtonIsPressedWithTheKeys`
- [x] Tab reaches the data table's disclosure, and Enter or Space opens it. `ChartTests.Table_ShouldOpenFromTheKeyboard_WhenItsDisclosureHasFocus`

## Pointer

- [x] Moving over the plot shows a line at the nearest place and a tooltip with every series' value there. `ChartTests.Tooltip_ShouldShowEverySeriesValueAtThePlace_WhenThePointerIsOverThePlot`
- [x] Moving over the plot sends nothing to the server. `chart.js`
- [x] The tooltip stays while the pointer is over the plot and goes when the pointer leaves. `ChartTests.Tooltip_ShouldShowEverySeriesValueAtThePlace_WhenThePointerIsOverThePlot`
- [x] The tooltip has no timeout. `chart.js`
- [x] The tooltip stays inside the plot, and a name too long for the room beside the line wraps. `ChartTests.Tooltip_ShouldStayInsideThePlot_WhenThePlotIsNarrow`
- [x] A click on the plot activates the bar under the pointer, or else the nearest point at that place. A line takes no click for itself, so every browser picks the same point. `ChartTests.Point_ShouldTellTheApp_WhenClickedOrChosenWithTheKeys`, `ChartTests.Point_ShouldBeTheNearest_WhenAreasOverlap`, `ChartTests.Point_ShouldBeTheBarUnderThePointer_WhenBarsStandSideBySide`
- [x] In a stack of areas, a click inside a band and off any point activates that band's point at the place, even where another line is nearer, and a click on a point activates that point. `ChartTests.Point_ShouldBeTheBandUnderThePointer_WhenAreasAreStacked`
- [x] Areas that overlap take no click for themselves: a click goes to the nearest point. `ChartTests.Point_ShouldBeTheNearest_WhenAreasOverlap`
- [x] An area's fill is drawn under the bars and every line, so it covers no other series' marks. `TbChartTests.Render_ShouldDrawEveryFillUnderTheBarsAndTheLines_WhenAreasShareAChartWithThem`
- [x] A click on a legend button hides or shows its series. `TbChartLegendTests.Legend_ShouldHideItsSeriesAndFitTheAxisToTheRest_WhenItsButtonIsPressed`, `TbChartLegendTests.Legend_ShouldShowItsSeriesAgain_WhenItsButtonIsPressedTwice`
- [x] With a series hidden, the axis fits the ones that show, each keeps its color, the tooltip lists only them, and the data table keeps every value. `TbChartLegendTests.Legend_ShouldHideItsSeriesAndFitTheAxisToTheRest_WhenItsButtonIsPressed`, `ChartTests.Legend_ShouldHideAndShowASeries_WhenItsButtonIsPressedWithTheKeys`
- [x] With every series hidden, the plot says so and the legend stays. `TbChartLegendTests.Plot_ShouldSayEverySeriesIsHidden_WhenNoneShows`
- [x] A hidden series' legend entry is struck through as well as dimmed. `ChartTests.Legend_ShouldHideAndShowASeries_WhenItsButtonIsPressedWithTheKeys`
- [x] With `CanHideSeries` off, and on a static page, the legend is plain text, and a hidden series' entry is still marked. `TbChartLegendTests.Legend_ShouldHaveNoButtons_WhenSeriesCannotBeHiddenOrThePageIsStatic`
- [x] What the user chooses in the legend holds until the app gives the series' `Hidden` a new value, and the app setting it tells the app nothing. `TbChartLegendTests.Hidden_ShouldHideTheSeriesAndTellTheApp_WhenTheAppOnlyListens`, `TbChartLegendTests.Hidden_ShouldTakeTheAppsValueAgain_WhenTheAppSetsItAfterTheUserChoseAnother`, `TbChartLegendTests.Hidden_ShouldLeaveTheSeriesOutAndNotTellTheApp_WhenTheAppSetsIt`
- [x] A click or a key on a point of a series hidden meanwhile is passed over. `TbChartLegendTests.Activate_ShouldPassOverAHiddenSeries_WhenTheScriptNamesOne`
- [x] The chart is as wide as its container at any width, with each label on its grid line or its mark. `ChartTests.Labels_ShouldSitOnTheirGridLinesAndMarks_WhenTheChartIsAnyWidthAndDirection`
- [x] An axis shows only the labels that fit, evenly spaced, and none overlap. `ChartTests.PlaceLabels_ShouldNotOverlap_WhenTheChartIsNarrow`
- [x] In forced-colors mode, every mark shows, lines differ by their dashes and bars by their fill, and each legend key is drawn as its series' marks are. `ChartTests.Series_ShouldDifferWithoutColor_WhenColorsAreForced`
- [x] In forced-colors mode an area keeps its line, which differs by its dashes, and loses its fill, in the plot and in its legend key. `ChartTests.Area_ShouldKeepItsLineAndLoseItsFill_WhenColorsAreForced`
- [x] In forced-colors mode a click inside a band of a stack still activates that band's point. `ChartTests.Point_ShouldBeTheBandUnderThePointer_WhenColorsAreForcedAndFillsAreNotPainted`
- [x] A bar stops at the plot's edge when the axis's `Min` or `Max` is short of it, and a bar whose value is beyond the axis is cut square there. `ChartPlanTests.Build_ShouldStopABarAtThePlotsEdge_WhenTheAxisStartsAboveZero`, `ChartPlanTests.Build_ShouldCutABarSquareAtThePlotsEdge_WhenItsValueIsBeyondTheAxis`
- [x] An area is filled from its line down to zero, or to the area under it in a stack, and no farther than the plot's edge. `ChartTests.Area_ShouldBeFilledFromItsLineDownToZero_WhenDrawn`, `ChartPlanTests.Build_ShouldPileAreasOnAreasAndBarsOnBars_WhenStacked`, `ChartPlanTests.Build_ShouldFillPastThePlotsEdge_WhenTheAxisStartsAboveZero`

## Touch

- [x] A tap on the plot shows the tooltip for the nearest place and activates a point as a click there does, and the tooltip stays until a tap elsewhere. `ChartTests.Tap_ShouldShowTheTooltipAndPickThePoint_WhenThePlotIsTouched`
- [ ] A vertical swipe that starts on the plot scrolls the page. UNCONFIRMED #332

## Focus

- [x] The focus ring is drawn around the point that has focus. `trailblazor.css`
- [x] When the data changes, the tab stop stays on its point if the point is still there, or else goes to the first one, and an empty chart has no tab stop. `ChartTests.Points_ShouldFollowTheData_WhenTheItemsChange`
- [x] Each point that takes focus is named by its place, its series, its value and its position, such as "March, Completed: 42, 3 of 12". `ChartTests.Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus`
- [x] The tab stop stays with its own series when another is hidden or shown, and goes to the first point when its own series is hidden. `ChartTests.Legend_ShouldHideAndShowASeries_WhenItsButtonIsPressedWithTheKeys`
- [x] In a legend without buttons, a hidden series' entry reads "(hidden)" to screen readers. `TbChartLegendTests.Legend_ShouldHaveNoButtons_WhenSeriesCannotBeHiddenOrThePageIsStatic`
- [x] A legend button is pressed while its series shows, and the drawing's name lists only the series that show. `TbChartLegendTests.Legend_ShouldHideItsSeriesAndFitTheAxisToTheRest_WhenItsButtonIsPressed`
- [ ] A screen reader speaks that name as focus moves between points. UNCONFIRMED #332
- [x] A click with no position on the point that has focus, as a screen reader sends, activates that point. `ChartTests.Point_ShouldTellTheApp_WhenClickedOrChosenWithTheKeys`
- [ ] A screen reader's own command for activating the point reaches it. UNCONFIRMED #332
- [x] The data table under the chart holds every value. `TbChartTests.Render_ShouldHoldEveryValueInATableUnderTheChart_WhenDrawn`
- [x] On a static page the chart draws and its table opens. `ChartTests.Chart_ShouldDrawAndOpenItsTable_WhenThePageIsStatic`
- [x] The drawing is one image named by its kind and its series, inside a figure named by the chart's title. `TbChartTests.Render_ShouldNameTheFigureByItsTitleAndTheDrawingByItsKindAndSeries_WhenGivenATitle`
- [x] On an interactive page the point that has the tab stop is the only other image, and a point focus has left is no longer one. `ChartTests.Keys_ShouldMoveBetweenNamedPoints_WhenThePlotHasFocus`

## Not applicable

- Zooming and panning: not in the first release; when they come, they will have buttons and keys as well as dragging.
- Selecting several points: a chart reports the point activated, and an app keeps what is selected.
- A context menu: the chart has none of its own.
