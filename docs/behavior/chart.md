# Chart

Pattern: none; the WAI-ARIA Authoring Practices have no chart pattern. The picture and its table follow the [WAI tutorial on complex images](https://www.w3.org/WAI/tutorials/images/complex/), and the keys follow the ones shared by chart libraries that support the keyboard.

Familiar apps: Excel and Google Sheets charts, Grafana panels, Highcharts' accessibility module

## Keyboard

- [ ] Tab reaches the plot once, at the point last visited or the first point. #199
- [ ] Right and Left move to the next and previous point of the series, and swap in right-to-left layouts. #199
- [ ] Down and Up move to the same place in the next and previous series that shows. #199
- [ ] Home and End move to the first and last point of the series. #199
- [ ] Enter or Space activates the point, as a click does. #199
- [ ] Escape hides the tooltip and leaves focus where it is. #199
- [ ] Tab reaches each legend button, and Enter or Space hides or shows its series. #199
- [x] Tab reaches the data table's disclosure, and Enter or Space opens it. `ChartTests.Table_ShouldOpenFromTheKeyboard_WhenItsDisclosureHasFocus`

## Pointer

- [ ] Moving over the plot shows a line at the nearest place and a tooltip with every series' value there, with nothing sent to the server. #199
- [ ] The tooltip stays while the pointer is over the plot, has no timeout, and goes when the pointer leaves. #199
- [ ] A click on the plot activates the nearest point. #199
- [ ] A click on a legend button hides or shows its series. #199
- [x] The chart is as wide as its container at any width, with each label on its grid line or its mark. `ChartTests.Labels_ShouldSitOnTheirGridLinesAndMarks_WhenTheChartIsAnyWidthAndDirection`
- [x] An axis shows only the labels that fit, evenly spaced, and none overlap. `ChartTests.PlaceLabels_ShouldNotOverlap_WhenTheChartIsNarrow`
- [x] In forced-colors mode, every mark shows, lines differ by their dashes and bars by their fill, and each legend key is drawn as its series' marks are. `ChartTests.Series_ShouldDifferWithoutColor_WhenColorsAreForced`

## Touch

- [ ] A tap on the plot shows the tooltip for the nearest place and activates the nearest point. #199
- [ ] A vertical swipe that starts on the plot scrolls the page. #199

## Focus

- [ ] The focus ring is drawn around the point that has focus. #199
- [ ] A screen reader hears the chart's name on entering the plot, then each point as its place, its series, its value and its position, such as "March, Completed: 42, 3 of 12". UNCONFIRMED #332
- [x] The data table under the chart holds every value. `TbChartTests.Render_ShouldHoldEveryValueInATableUnderTheChart_WhenDrawn`
- [x] On a static page the chart draws and its table opens. `ChartTests.Chart_ShouldDrawAndOpenItsTable_WhenThePageIsStatic`
- [x] Until the plot takes focus, the drawing is one image named by its kind and its series, inside a figure named by the chart's title. `TbChartTests.Render_ShouldNameTheFigureByItsTitleAndTheDrawingByItsKindAndSeries_WhenGivenATitle`

## Not applicable

- Zooming and panning: not in the first release; when they come, they will have buttons and keys as well as dragging.
- Selecting several points: a chart reports the point activated, and an app keeps what is selected.
- A context menu: the chart has none of its own.
