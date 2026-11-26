# Spike E: charts as static markup

**Question.** Can a chart be complete as the markup .NET renders, with no measuring and no script needed to show it,
and still look right at any size, follow the pointer without a round trip, and work from the keyboard?

Most chart libraries measure their container in script and then draw. A Blazor chart that did the same would show
nothing on a static page, would draw twice on an interactive one, and over Interactive Server would wait on a round
trip for every resize. The planned model lets the browser do the sizing instead:

- **Marks the browser can place by percentage** (bars, markers, grid lines) are SVG shapes with percentage
  coordinates and pixel sizes, in an SVG with no `viewBox`, so a radius or a stroke is the same at every size.
- **Lines and areas**, whose paths can't take percentages, sit in a nested SVG with a fixed `viewBox` and
  `preserveAspectRatio="none"`, stretched over the plot, with `vector-effect: non-scaling-stroke` so the stroke
  isn't stretched with it.
- **Text** (tick labels, axis titles, the legend) is HTML around the plot in a CSS grid, so the column of value
  labels is as wide as its widest label and nothing is measured in .NET.
- **Pointer tracking** (crosshair, tooltip, highlight) runs in one script module that reads the points from the
  markup. .NET hears only a click, Enter or Space on a point.

## What gets built

- A prototype chart component drawing from in-memory data:
  - a line chart: 3 series of 30 dated points with markers, one series dashed
  - a column chart: 12 categories with 3 series side by side, and the same data stacked
  - a donut of 5 parts
  - a large line chart: 5 series of 1,000 points
- A data table for each, drawn on opening for the large chart, and a legend of buttons that hide and show a
  series.
- One script module for the crosshair, tooltip and keys.

## Pass criteria

1. **No script.** With JavaScript off, each chart shows all of its marks, grid lines, tick labels and legend, and its
   data table can be opened.
2. **Shapes at any size.** At plot sizes from 280 by 160 to 1,600 by 480 pixels:
   - a line is 2 pixels thick whether it runs across, up or at a slant, within half a pixel
   - a marker is a circle, as wide as it is tall within 1 pixel
   - a dash is as long at the widest size as at the narrowest, within 1 pixel
   - a column's rounded end has the same radius at every size
3. **Alignment.** The middle of each tick label is within 1 pixel of its grid line, and each column is within
   1 pixel of its place on the line chart's scale, left to right and right to left.
4. **Labels fit.** At 280 pixels wide, with 12 categories or 30 dates, no two tick labels overlap, with and without
   JavaScript.
5. **Pointer.** In Interactive Server, two seconds of pointer movement over the plot sends no message to the server,
   and the tooltip shows the nearest point's values within one frame of each move at 4× CPU throttling.
6. **Keyboard and names.** The chart is one tab stop. Arrow keys move between points and between series, the
   tooltip follows, and what has focus is named with the series, the category or date and the value. Escape hides
   the tooltip. Nothing goes to the server until Enter.
7. **Size.** The large chart:
   - has fewer than 600 elements
   - is under 120 kB of markup
   - renders in under 100 ms in WebAssembly at 4× CPU throttling, and adding one point to each series re-renders
     in under 50 ms
   - sends under 40 kB over Interactive Server when one point is added to each series
8. **Colors.** In forced colors every series is still visible and two neighboring series can be told apart without
   color, and in both themes the marks have 3:1 contrast against the surface.

## How it is measured

- Criteria 1 to 4, 6 and 8 are Playwright tests on Chromium, Firefox and WebKit. Thickness, roundness and dash length
  are read from screenshots of a chart whose marks are drawn in one known color; alignment is read from bounding
  boxes.
- Criterion 5 counts WebSocket frames in Chromium, and times the tooltip from `pointermove` to the next frame.
- Criterion 7 runs on Chromium with 4× CPU throttling: render time from a mark before the state change to the frame
  after it, and the batch size from the WebSocket frames.

## If a criterion fails

- If 2 or 3 fails for stretched paths in one engine, then in that engine lines and areas are drawn from a width
  measured in script, and the static page keeps the stretched drawing.
- If 4 fails without script, tick labels are thinned in .NET for a stated minimum width, and script thins them
  further.
- If 6 fails with one focusable element whose name changes, each point becomes an element of its own, and criterion
  7's element count rises to the number of points.
- If 7 fails, long series are thinned with largest-triangle sampling to as many points as the plot's width can show.

## Results

**Verdict: go with the planned model.** A chart drawn this way shows in full with no script, keeps its shapes at
every size in three engines, and follows the pointer and the keys with nothing sent to the server. Criterion 7 fails
for the 5,000-point chart, on drawing time in WebAssembly and on what an update sends. With 500 points a series, an
update sends less than its target and takes about half as long, but the drawing time still misses its target.

Measured on 2026-11-26 on Windows 11, .NET SDK 10.0.403 and Playwright 1.64.1 with its bundled engines (Chromium
build 1248, Firefox build 1555, WebKit build 2370), headless, in a Debug build. The tests are in
`spikes/Trailblazor.Spikes.Tests/Charts`. The timing tests are explicit; the full run included them with
`-explicit on`, and passed 106 of 108. The two that failed are criterion 7's.

| # | Criterion | Result |
| --- | --- | --- |
| 1 | No script | Pass in all engines, except the large chart's table, which needs script (see below) |
| 2 | Shapes at any size | Pass in all engines |
| 3 | Alignment | Pass in all engines and both directions, after two fixes for WebKit (see below) |
| 4 | Labels fit | Pass in all engines, with and without script, after one fix for a long axis (see below) |
| 5 | Pointer | No message sent: pass. Time is at the limit: 16 to 17 ms at the 95th percentile |
| 6 | Keyboard and names | Pass in all engines. How screen readers speak it is still to check by hand. |
| 7 | Size | **Fail** on drawing time and on what an update sends. Pass on elements and markup, table left out |
| 8 | Colors | Pass in all engines |

**No script (criterion 1).** The line, column, stacked and donut charts show everything with JavaScript off, and
their tables open. The large chart shows its marks, labels and legend, but its table isn't in its markup: the
prototype draws it when the reader opens it, which a test checks in all three engines and which needs script. A
static page would have to carry that table in its markup, about 7,000 more elements for this chart, which criterion
7's count leaves out.

**Shapes (criterion 2).** On plots of 280 by 160, 874 by 300 and 1,634 by 480 pixels, in every engine:

- A level line and an upright one are 2.00 pixels thick, and a slanted one 1.83 to 2.11.
- Every dash is 6 pixels and every gap 4.
- The stretched line starts and ends within 0.73 pixels of the middle of its first and last places.
- Every marker's box on the screen is as wide as it is tall, within 1 pixel.
- The column's rounded corner reads as a radius of 3.3 to 3.4 pixels in Chromium, 3.8 to 4.2 in Firefox and 3.7 to
  3.9 in WebKit, with no trend by size. The engines soften the arc differently. A radius stretched with the plot
  would be about six times larger at the widest size than at the narrowest.

**Alignment (criterion 3).** Place labels are within 0.24 pixels of their markers, value labels within 1 pixel of
their grid lines, the stretched line's corners within 0.05 pixels of their markers, and stacked columns within
0.18 pixels of their place labels. The first run failed in WebKit for two reasons:

- **A percentage `top` didn't move a label inside a grid cell** whose height came from the grid. Value labels are now
  placed over an unseen copy of the longest label, which gives the column its width.
- **A grid item that is itself a container-query container was sized by the whole grid**, left to right only, so the
  date labels drifted up to 25 pixels. The query container now sits inside the grid item.

**Labels (criterion 4).** On a 280-pixel plot, 4 of 30 dates and 6 of 12 months show, the same in every engine and
with script off. The thinning is CSS alone: each label is either full width or zero width, depending on whether
labels at its stride have room. That is worked out from container-query units and the label's length. The code
review found that the widest stride was capped at 64 places, so the 1,000-date axis showed no labels on a plot
narrower than about 1,100 pixels. The first label now has the widest stride the axis holds, and the large chart
shows 4 dates on a 271-pixel plot.

**Pointer (criterion 5).** Over 101 moves at 4× CPU throttling the chart sent no message. Each update is timed from
the first move it answers to the end of its script work; style, layout and paint come after and aren't in it. Over
two runs that took 7 to 9 ms at the median, 16 to 17 ms at the 95th percentile and 18 to 21 ms at most, against a
frame of 16.7 ms. The work itself took 3 to 4 ms at the median and 11 ms at most, for the large chart too, and
rebuilds the tooltip's rows on every move; keeping the rows and changing their text would cut it.

**Keyboard (criterion 6).** Focus goes back and forth between two elements, each named for its point before it takes
focus, so every move is a real change of focus. A name gives the place, the series, the value and the position,
in the form "Mar 2, Failed: 58, 2 of 30" (the value there is made up). Playwright reads the role and the name of
the focused element as expected in all three engines. These tests can't show what NVDA, JAWS and VoiceOver say.

**Size (criterion 7).** The first run measured the large chart at 1,038 elements and 160 kB of markup. Two changes
brought it down to 74 elements and 72 kB, without its table: a long axis renders at most 48 label elements, and a
line's path gives each point as a whole number of steps from the last on a 10,000 grid.

| Chart | First draw | A point added to each series | Of which the chart's own .NET work |
| --- | --- | --- | --- |
| 5 series of 1,000, WebAssembly, 4× throttle | 717 ms | 386 to 520 ms | 482 ms |
| 5 series of 1,000, WebAssembly, no throttle | 95 ms | 63 to 65 ms | 56 ms |
| 5 series of 1,000, Interactive Server, 4× throttle | 61 ms | 18 to 32 ms | under 1 ms |
| 5 series of 500, WebAssembly, 4× throttle | 448 ms | 210 to 260 ms | 194 ms |
| 5 series of 500, WebAssembly, no throttle | 65 ms | 32 to 63 ms | 27 ms |
| 3 series of 30 with markers, WebAssembly, 4× throttle | 294 ms | 61 to 67 ms | 7 ms |
| 3 series of 12 columns, WebAssembly, 4× throttle | 284 ms | 44 to 77 ms | 5 ms |

- The targets were 100 ms for the first draw and 50 ms for an added point, at 4× throttle. The large chart misses
  both by a wide margin, and with 500 points a series it still does. First draws varied by about a fifth between
  runs.
- The time goes to .NET, not to the browser: over the server the same update paints in about 30 ms. About four
  fifths of it is turning each value into text for the tooltip, and the rest is building the paths.
- A chart of ordinary size spends under 10 ms on chart work; the rest of its 44 to 77 ms is Blazor rendering a few
  hundred elements in WebAssembly at 4× throttle.
- Adding a point over Interactive Server sent 70.7 kB against a target of 40 kB, since every position changes when
  the scale does. With 500 points a series it sent 37.6 kB.
- The 500-point rows use a 500-point data set. Thinning a longer series to 500 points wasn't built or measured.

**Colors (criterion 8).** In forced colors every line draws in the text color with a dash pattern of its own, and
neighboring bars differ by fill or outline, in all three engines. Against the surface, the eight series colors have
3.24:1 to 7.65:1 in the light theme and 3.19:1 to 5.21:1 in the dark one.

## What this means for the design

- Draw with the planned model: percentage shapes, stretched paths with strokes that don't scale, HTML text in a
  grid, and one script module that reads its points from the markup.
- Place value labels over an unseen copy of the longest one, and keep the label row's query container inside its
  grid item, as WebKit needs.
- Write paths as whole steps on a 10,000 grid. Render no more label elements than a plot could ever show, and give
  the first label the widest stride the axis holds.
- Thin a series longer than 500 points to 500 for drawing and for the tooltip, keep every point in the table, and
  build each series' path and value text again only when its data changes. Say on the docs page what a very long
  series costs in WebAssembly.
- On an interactive page, draw the table when it is opened. A static page has to carry it in its markup. The cost
  is that an interactive page that never starts can't open its table.
- Tell .NET which series a point belongs to by the series' own number, not its position among the ones that show:
  the prototype mixed the two up after a series was hidden, which the code review caught.
- In tests, check a mark's shape by its box on the screen, and allow for the stroke: Firefox's
  `getBoundingClientRect` includes it.
- Check with NVDA, JAWS and VoiceOver that a point is spoken when focus moves between the two elements (#332).
