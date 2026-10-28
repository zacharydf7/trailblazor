# Spike B: docking keep-alive host

**Question.** Can a docking manager move panels between tab groups, splits, floating windows and auto-hide strips
without Blazor disposing or re-rendering the panel content?

Blazor disposes a component whose position in the render tree changes parent. The proposed design renders every
panel's content once, in a stable keyed host that never moves in the render tree. The layout tree renders empty
placeholder slots, and a small JS module keeps each host sized and positioned over its slot.

## What gets built

- A 20-panel layout with tab groups, horizontal and vertical splits, one floating window and one auto-hide strip.
- Stateful panel content:
  - a counter component that records how many times it was initialized, rendered and disposed
  - a text input with typed text
  - a scrolled list
  - an `<iframe>` that records how many times it loaded
- A scripted "move panel" API that changes the layout model directly, so moves don't depend on drag-and-drop UI.

## Pass criteria

1. **State survives moves.** After 100 scripted moves across groups, splits, the floating window and the auto-hide
   strip:
   - Panel content was initialized once and disposed 0 times.
   - Panel content re-rendered 0 times because of a move.
   - Typed text and scroll positions are unchanged.
   - Each iframe's `load` event fired exactly once.
2. **Geometry.** Each panel host stays within 1 px of its slot in every frame of a scripted splitter drag and a
   window resize.
3. **Re-dock cost.** Re-docking one panel in the 20-panel layout:
   - causes no long task over 50 ms
   - settles within 2 frames of the drop
   - sends exactly one interop message to .NET
4. **Hidden panels.**
   - Inactive tabs and collapsed auto-hide panels keep their state.
   - They are out of the tab order and out of the accessibility tree.
   - 19 hidden heavy panels add at most 10% to layout time compared with 1 visible panel.
   - `KeepAlive="false"` disposes a panel's content when it is hidden.
5. **Focus and reading order.** Tab order and screen-reader reading order follow the visual layout, not the order
   of the stable hosts in the DOM. This is the criterion most likely to fail.
6. **Overlays in panels.** Popovers and dialogs opened from inside a panel position correctly after the panel has
   moved.

## How it is measured

- Criteria 1, 4, 5 and 6 are Playwright tests on Chromium, Firefox and WebKit, in both render modes.
- Criteria 2 and 3 use Chromium performance traces with 4× CPU throttling.
- Interop messages are counted on the .NET side with a counting `JSInvokable` wrapper.

## If a criterion fails

- If 1 fails, the fallback design externalizes panel state: panels save state on move and restore it after a
  re-render.
- If 5 fails, the hosts are reordered in the DOM to match the layout on each re-dock, accepting one move of the DOM
  node per re-dock, and the spike measures whether that still keeps iframe and input state.

## Results

To be filled in after the spike runs.
