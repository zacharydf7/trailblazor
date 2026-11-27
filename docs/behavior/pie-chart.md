# Pie chart

Pattern: none; the WAI-ARIA Authoring Practices have no chart pattern. The picture and its table follow the [WAI tutorial on complex images](https://www.w3.org/WAI/tutorials/images/complex/), and the keys are the ones the [chart](chart.md) uses.

Familiar apps: Excel and Google Sheets pie charts, Grafana pie panels

## Keyboard

- [x] The pie is one tab stop, on the slice last visited or else the first. `PieChartTests.Table_ShouldOpenFromTheKeyboard_WhenItsDisclosureHasFocus`, `PieChartTests.Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus`
- [x] Right and Down move to the next slice, and Left and Up to the previous one. `PieChartTests.Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus`
- [x] Left and Right swap in right-to-left layouts, where the pie is mirrored. `PieChartTests.Keys_ShouldSwapLeftAndRight_WhenThePieRunsRightToLeft`
- [x] Home and End move to the first and last slice. `PieChartTests.Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus`
- [x] An item with no slice has no point for the keys to stop on, and the script passes over a place with none. `TbPieChartTests.Render_ShouldLeaveAnItemOutOfThePieAndTheLegend_WhenItsValueIsMissingOrNotAboveZero`, `ChartTests.Keys_ShouldStepOverAGap_WhenASeriesHasNoValueAtAPlace`
- [x] Enter or Space on a slice raises `OnSliceClick`. `PieChartTests.Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus`
- [x] Escape hides the tooltip and leaves focus where it is. `PieChartTests.Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus`
- [x] Tab reaches the data table's disclosure, and Enter opens it. `PieChartTests.Table_ShouldOpenFromTheKeyboard_WhenItsDisclosureHasFocus`

## Pointer

- [x] Moving over a slice shows a tooltip with its label, its value and its share, level with the slice, with a key in the slice's color and no line through the plot. `PieChartTests.Tooltip_ShouldShowTheSliceUnderThePointerAndHideOffThePie_WhenThePointerMoves`
- [x] While a slice has focus, the tooltip goes back to it when the pointer leaves the slices. `PieChartTests.Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus`
- [x] Off the pie, and in a donut's hole, there is no tooltip. `PieChartTests.Tooltip_ShouldShowTheSliceUnderThePointerAndHideOffThePie_WhenThePointerMoves`, `PieChartTests.Donut_ShouldShowItsContentInTheHoleAndHaveNoSliceThere_WhenDrawn`
- [x] A click on a slice raises `OnSliceClick` with its item, its label and its value. `PieChartScenarioTests.RescuesByCause_ShouldShowTheRangersAdvice_WhenACausesSliceIsClicked`, `TbPieChartTests.Click_ShouldTellTheAppTheItemItsLabelAndItsValue_WhenASliceIsActivated`
- [x] Moving over the pie sends nothing to the server. `chart.js`
- [x] The pie is a circle that fills a square plot, 16rem wide by default, with its legend beside it. `PieChartTests.Pie_ShouldBeARoundDrawingBesideItsLegend_WhenThereIsRoom`
- [x] On a narrow page the pie is as wide as its container, still square, and its legend is under it. `PieChartTests.Pie_ShouldStaySquareAboveItsLegend_WhenThePageIsNarrow`
- [x] A donut shows the app's content in the middle of its hole. `PieChartTests.Donut_ShouldShowItsContentInTheHoleAndHaveNoSliceThere_WhenDrawn`
- [x] A slice with a tone is drawn in the tone's color, as are its legend key and its tooltip's key. `PieChartTests.Tone_ShouldColorItsSliceItsLegendKeyAndItsTooltipKey_WhenAnItemHasOne`
- [x] An item whose value is missing, zero or below zero has no slice and no legend entry, and the others keep their colors. `TbPieChartTests.Render_ShouldLeaveAnItemOutOfThePieAndTheLegend_WhenItsValueIsMissingOrNotAboveZero`, `ChartArcTests.Slices_ShouldLeaveAValueOut_WhenItIsMissingOrNotAboveZero`

## Touch

- [x] A tap on a slice shows its tooltip and activates it as a click does, and the tooltip stays until a tap elsewhere. `PieChartTests.Tap_ShouldShowTheTooltipAndPickTheSlice_WhenASliceIsTouched`

## Focus

- [x] The focus ring is drawn in the middle of the slice that has focus, right to left too. `PieChartTests.Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus`, `PieChartTests.Keys_ShouldSwapLeftAndRight_WhenThePieRunsRightToLeft`
- [x] Each slice that takes focus is named by its label, its value, its share and its position, such as "Week: 260 (33%), 2 of 3". `PieChartTests.Keys_ShouldMoveBetweenNamedSlicesAndChooseOne_WhenThePieHasFocus`
- [x] The drawing is one image named by its kind and its slices, inside a figure named by the chart's title. `TbPieChartTests.Render_ShouldNameTheFigureByItsTitleAndTheDrawingByItsKindAndSlices_WhenGivenATitle`
- [x] The legend lists each slice with its share, and its keys are hidden from screen readers. `TbPieChartTests.Render_ShouldListEachSliceWithItsShareInTheLegend_WhenDrawn`
- [x] The data table under the chart holds every item's value and share. `TbPieChartTests.Render_ShouldHoldEveryValueAndShareInATableUnderTheChart_WhenDrawn`
- [x] On a static page the pie, its legend and its table are drawn, and no script is attached. `TbPieChartTests.Render_ShouldDrawWithoutItsScript_WhenThePageIsStatic`
- [x] In forced-colors mode slices are filled, hollow, dashed or dotted in the text color, as their legend keys are. `PieChartTests.Slices_ShouldDifferWithoutColor_WhenColorsAreForced`
- [ ] A screen reader speaks a slice's name as focus moves between slices. UNCONFIRMED #332
- [ ] A screen reader's own command for activating a slice reaches it. UNCONFIRMED #332

## Not applicable

- Hiding a slice from the legend: the legend is text. An app that lets people leave a part out takes it out of `Items`, and the other shares follow.
- Selecting several slices: a chart reports the slice activated, and an app keeps what is selected.
- A context menu: the chart has none of its own.
