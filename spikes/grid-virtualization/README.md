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

- Criteria 1, 2, 4 and 5 run on Chromium with 4× CPU throttling. As built: frame intervals from
  `requestAnimationFrame`, long tasks from a `PerformanceObserver`, and the managed heap from `GC.GetTotalMemory`.
  These replace the full performance traces that were planned.
- Criteria 3 and 6 use render-count and request-count assertions in the prototype, read by Playwright.
- Criteria 7 and 8 are Playwright tests on Chromium, Firefox and WebKit.

## If a criterion fails

- If 1 or 3 fails with Blazor's `Virtualize`, the fallback is a custom virtualizer that keeps a fixed pool of row
  elements and recycles them from JS-driven scroll positions, so .NET only updates row data.
- If 2 fails, Server mode keeps a larger overscan and prefetches a page ahead in the scroll direction.

## Results

**Verdict: the planned model on Blazor's `Virtualize` fails criterion 1, and misses criteria 2 and 5 in some runs.
Its rendering model, data handling, accessibility and alignment pass.** The fallbacks:
- a custom virtualizer with a fixed pool of row elements, recycled from JS-driven scroll positions (criteria 1 and 5)
- a larger overscan with prefetching in Server mode, on top of the row pool (criterion 2)

Grid virtualization is phase 3 work, so this doesn't block phase 1.

Measured on 2026-10-28 on Windows 11, .NET SDK 10.0.403 and Playwright 1.64.1 with its bundled engines (Chromium
build 1248, Firefox build 1555, WebKit build 2370), headless. Timing ran on Chromium with 4× CPU throttling. The
tests are in `spikes/Trailblazor.Spikes.Tests/Grid`.
- **The deterministic criteria (3, 6, 7, 8)** are regular tests, and they passed in every run.
- **The timing criteria (1, 2, 4, 5)** are explicit tests, run with `-- --explicit only`. They are noisy and slow,
  so the counts below cover every run.

| # | Criterion | Result |
| --- | --- | --- |
| 1 | Scrolling, WebAssembly | **Fail**: flings drop to about 20 to 30 fps |
| 2 | Scrolling, Interactive Server | **Fail, intermittently**: 2 of 6 calibrated runs missed (see below) |
| 3 | Rendering | Pass in both modes |
| 4 | Memory | Pass: the heap grew 0.3% |
| 5 | Jumping | **Fail, narrowly**: 1 of 10 WebAssembly runs took 104 ms. Server always passed. |
| 6 | Data provider | Pass in both modes |
| 7 | Accessibility | Pass in all engines and modes |
| 8 | Alignment | Pass in all engines and modes |

**The prototype.**
- Rows: 100,000 rows of 8 typed columns, with one `GridRow` component per row, keyed by row index and with all cells
  inline. `ShouldRender` is gated on the row's index, version and active state.
- Virtualization: Blazor's `Virtualize` with an overscan of 10 rows.
- Layout: a sticky header and a sticky first column.
- Keyboard: `aria-activedescendant`, so focus stays on the grid element.

**Criterion 1.** Each run is a slow stretch (120 px a frame, about 3 rows, 600 frames), then a fling (4,000 px a frame)
to the last row.
- The fling failed in all seven runs: median frame 33 to 50 ms, p95 83 to 100 ms, long tasks of 80 to 122 ms.
- The likely cause, reasoned rather than measured: each fling frame lands on rows that are all new. With 13 visible
  rows and an overscan of 10 on each side, `Virtualize` builds about 33 row components at once, which costs far
  more than a frame on a throttled CPU.
- The slow stretch mostly held 60 fps: median 16.7 ms in every run, p95 16.8 ms in 4 of 7 runs and 33 ms in 3.
  Three slow stretches also had one long task, of 65 to 84 ms.
- The fallback's row pool keeps the same row elements and components and updates their data in place. That removes
  component creation and DOM insertion, but a fling still updates about 33 rows a frame, roughly ten times a slow
  scroll's work. Whether that fits in a frame is unmeasured, and is the first thing phase 3 must check.
- The median check allows 16.75 ms, because rAF timestamps are rounded to 0.1 ms and an on-time 60 Hz frame reads
  as 16.7 ms.

**Criterion 2.** A TCP proxy in the test delays every chunk 25 ms in each direction, covering both HTTP and the
Blazor WebSocket. It spins through the last timer tick so Windows timer resolution doesn't stretch the delay, and a
run counts only if its measured round trip is 50 to 56 ms.
- Six runs qualified, at 54 to 55 ms. Four passed: client-side median 16.7 ms and p95 16.8 ms, with every fill after
  jumps to rows 1,000, 50,000 and 99,000 between 104 and 138 ms.
- Two failed. Client-side p95 rose to 33 ms, and one fill in each took 151 ms and 202 ms.
- Earlier runs, through a cruder proxy (57 to 72 ms round trips) and before a stale-render bug in the row's
  `ShouldRender` was fixed, aren't counted.
- The written fallback is a larger overscan with a page prefetched in the scroll direction. The row pool from
  criterion 1 also cuts the client's DOM work per batch, which is what pushed p95 to 33 ms.

**Criterion 3.**
- Scrolling past the overscan rendered exactly the rows that became visible, and created exactly that many row
  components. That held in both modes, and in Server mode at a 50 ms round trip.
- That latency run doesn't prove the `ShouldRender` fix (see Other findings). With the old code put back, it still
  passed, because a single jump renders each new row only once inside the acknowledgement window. The fix rests on
  how Blazor Server acknowledges batches, not on this test.
- Changing an off-screen row rendered 0 rows. Changing an on-screen row rendered exactly 1.
- The grid used 344 DOM nodes (comfortable) and 380 (compact), the same at the first and the last row.

**Criterion 4.** Over a full scroll, measured with `GC.GetTotalMemory(forceFullCollection: true)` in WebAssembly:
- The managed heap went from 17,769,416 to 17,830,208 bytes in the final runs.
- Before the `ShouldRender` fix it went from 17,824,016 to 17,880,784 bytes.
- Both are about 0.3% growth.

**Criterion 5.**
- WebAssembly filled the view after a jump to the last row in 64 to 104 ms over ten runs; one was over 100 ms.
  The measurement checks once per painted frame, and a throttled frame takes 17 to 33 ms, so 104 ms is within one
  frame of the limit. It still counts as a fail.
- Server filled it in 48 to 84 ms over ten runs, with no added latency.
- **Fallback.** A jump renders an entirely new set of rows in one frame, like one frame of a fling. So the row pool
  from criterion 1's fallback is also criterion 5's fallback, and phase 3 measures both with it.

**Criterion 6.**
- During ten rapid jumps, every superseded provider request was cancelled (5 to 7 a run), and only one request was
  ever active.
- Every started request either completed or was cancelled.
- Re-rendering the page three times with unchanged parameters started no new request.

**Criterion 7.**
- `aria-rowcount` is 100,001 (with the header row).
- Every rendered row's `aria-rowindex` matched the index computed from where the row sits on screen, at the top and
  after a jump. Rendering it off by one made the test fail in every engine and mode.
- Arrow keys kept focus on the grid when the active row's element had been removed by scrolling. The next key press
  moved the active row on and scrolled it back into view.

**Criterion 8.** During vertical and horizontal scrolling, the header and the pinned column stayed within 1 px of the
body's edges in every frame. That holds for layout geometry. Compositor-only jitter in the painted frame isn't
visible to these DOM measurements.

**Other findings for phase 3.**
- The keyboard model from criterion 7 (focus on the grid, `aria-activedescendant` for the active row) keeps focus
  through virtualization, and should be kept. One gap remains: while the active row is scrolled out of view,
  `aria-activedescendant` points at an element that no longer exists, so a screen reader has nothing to announce.
  Phase 3 must keep the active row rendered while it is off-screen, or otherwise handle the broken reference.
- `Virtualize`'s `Context` names the placeholder template's context too, so a placeholder needs its own `Context`.
- A row's `ShouldRender` must save its rendered state when it renders, not in `OnAfterRender`. Server mode runs
  `OnAfterRender` only after the client acknowledges the batch, a round trip later.
