# Spike C: grid virtualization at 100,000 rows

**Question.** Can a Blazor data grid built on row virtualization scroll 100,000 rows at 60 fps, in WebAssembly and
over Interactive Server, with the planned rendering model?

The planned model renders one component per row with the cells inline (no per-cell components), and gates
`ShouldRender` on a row version.

## What gets built

- A grid prototype with 8 typed columns (text, number, date and enum), one row component per row, inline cells, a
  sticky header and one pinned (sticky) column.
- Two data sources:
  - 100,000 in-memory rows
  - a data provider with 50 ms simulated latency that honors cancellation
- Comfortable and compact densities, with fixed row heights.

## Pass criteria

1. **Scrolling, WebAssembly.** Continuous scrolling and flings through all 100,000 rows give:
   - median frame time of 16.7 ms or less
   - 95th percentile frame time of 20 ms or less
   - no long task over 50 ms
2. **Scrolling, Interactive Server.** Scrolling stays smooth on the client, and placeholder rows fill in within
   150 ms at a simulated 50 ms round trip.
3. **Rendering.**
   - Scrolling renders only the rows that became visible.
   - Changing an off-screen row renders 0 row components.
   - The grid's DOM stays under 3,000 nodes whatever the row count.
4. **Memory.** The WebAssembly heap grows less than 10% after a full scroll and a garbage collection.
5. **Jumping.** A jump to row 99,999 shows no blank area for more than 100 ms with in-memory data.
6. **Data provider.**
   - A new request cancels the one it supersedes.
   - At most one request is in flight.
   - Setting parameters never causes a duplicate load.
7. **Accessibility.**
   - `aria-rowcount` is correct, and so is `aria-rowindex` on every rendered row.
   - Arrow-key focus survives when the focused row scrolls out of view and its element is reused.
8. **Alignment.** The header and the pinned column never drift more than 1 px from the body during scrolling.

## How it is measured

- Criteria 1, 2, 4 and 5 use Chromium performance traces with 4× CPU throttling.
- Criteria 3 and 6 use render-count and request-count assertions in the prototype, read by Playwright.
- Criteria 7 and 8 are Playwright tests on Chromium, Firefox and WebKit.

## If a criterion fails

- If 1 or 3 fails with Blazor's `Virtualize`, the fallback is a custom virtualizer that keeps a fixed pool of row
  elements and recycles them from JS-driven scroll positions, so .NET only updates row data.
- If 2 fails, Server mode keeps a larger overscan and prefetches a page ahead in the scroll direction.

## Results

To be filled in after the spike runs.
