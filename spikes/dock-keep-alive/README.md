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

**Verdict: the keep-alive host works. Criterion 5 fails, and its fallback was not built.** Criterion 3 also fails
for a cold first move in WebAssembly. Docking is phase 4 work, after 1.0, so this doesn't block phase 1. The
tab-order design must be settled before `TbDockManager` gets an API.

On 2026-10-28 all 45 Playwright runs passed, twice in a row. The criterion 5 test records the failure as observed
behavior (see below), so the suite stays green.

Measured on Windows 11, .NET SDK 10.0.403 and Playwright 1.64.1 with its bundled engines (Chromium build 1248,
Firefox build 1555, WebKit build 2370), headless. Each criterion ran in Interactive Server and WebAssembly. The
tests are in `spikes/Trailblazor.Spikes.Tests/Dock`.

| # | Criterion | Result |
| --- | --- | --- |
| 1 | State survives moves | Pass in all engines and modes |
| 2 | Geometry | Pass in all engines and modes |
| 3 | Re-dock cost | Pass when warm. **Fail** once for a cold first move in WebAssembly: a 98 ms long task. |
| 4 | Hidden panels | Pass. The accessibility-tree part was measured in Chromium only. |
| 5 | Focus and reading order | **Fail** in all engines. The fallback was not built (see below). |
| 6 | Overlays in panels | Popovers pass with native anchoring and fail with the JS fallback. Dialogs not measured. |

**How the host works.**
- `DockHosts` renders one keyed host per panel, in a fixed order.
- The layout renders empty slots marked with the active panel's id.
- `dock.js` positions each host over its slot whenever a `MutationObserver` (layout re-renders) or a
  `ResizeObserver` (slot and dock sizes) fires.
- A move is a `[JSInvokable]` call that changes the layout model, then re-renders the page.
- Panel content never re-renders after a move, because Blazor skips a child whose parameters are unchanged
  primitives.

**Criterion 1.** After 100 scripted moves across groups, the floating window and the auto-hide strip:
- Every panel had exactly 1 initialization, 0 disposals and the same render count as before the moves.
- Typed text, a 300 px list scroll and a counter's value were unchanged.
- Iframe load counts were unchanged.

**Criterion 2.** A ResizeObserver created after the dock's own sampled every frame before paint. Through a
150 px splitter drag and two window resizes, no host was ever more than 1 px from its slot. With the dock's
ResizeObserver disabled, the same test reported a 180 px misalignment, so it can fail.

**Criterion 3.** On Chromium with 4× CPU throttling:
- Every warm re-dock settled within 1 to 2 painted frames, with no long task, and made exactly one layout call to
  .NET. The calls are counted on the .NET side, in each `[JSInvokable]` entry point. A deliberate extra call made the
  test report 2, so the count can fail.
- **Cold first move: fail.** In one earlier run, the first WebAssembly re-dock had a 98 ms long task, over the 50 ms
  limit. The four recorded runs since then had none. The test measures every first move but only asserts the warm
  ones, so recorded runs keep the cold numbers. Phase 4
  must warm this path when the dock mounts or keep the first move off interactions that need a fast response.

**Criterion 4.**
- Inactive panels kept their state (criterion 1) and couldn't take focus in any engine. In Chromium they are absent
  from the accessibility tree; Firefox's and WebKit's trees can't be read from Playwright.
- `KeepAlive="false"` disposed the panel when it was hidden.
- Layout cost: 50 inherited font-size changes, median of 5 samples on Chromium.

  | Scenario | Server | WebAssembly |
  | --- | --- | --- |
  | One visible heavy panel | 0.104 to 0.113 s | 0.110 to 0.118 s |
  | Plus 18 hidden heavy panels | 0.104 to 0.105 s | 0.101 to 0.108 s |
  | Control: `content-visibility` off | 3.55 to 3.59 s | 3.50 to 3.54 s |

  Hidden panels add no measurable layout time. The control, more than 30× slower, shows the measurement would catch
  that cost if it existed. The criterion names 19 hidden panels, but the measurement hid 18: p19 has KeepAlive off,
  so its content isn't rendered while hidden.

**Criterion 5.**
- **Fail.** Tab visits every tab strip and splitter first, then panel content in host order, in every engine. That
  happens because the hosts live after the layout in the DOM, not inside it.
- **Fallback not built.** The written fallback moves each host into or beside its slot in the DOM. Only its
  precondition was measured, on a static page outside Blazor:
  - `Element.moveBefore` keeps iframe and input state in Chromium and Firefox.
  - The bundled WebKit has no `moveBefore`. An ordinary DOM move reloads iframes, by the HTML spec; this run
    didn't measure that.
- **Open risk for that fallback.** Slots are rendered by Blazor, which removes them on some re-renders. Closing the
  auto-hide strip removes its flyout and slot, for example. A host moved into such a slot would be detached with it,
  losing exactly the state criterion 1 protects.
- Phase 4 has to choose one of these:
  - Move hosts with `moveBefore` next to slots that Blazor never removes, and accept iframe reloads in WebKit until
    `moveBefore` ships there.
  - Manage focus order explicitly, e.g. a roving model across the dock's regions that moves focus from a group's tab
    strip into its host.

**Criterion 6.**
- **Native anchor positioning** kept a popover on its trigger after the panel moved, in every engine and mode.
- **Dialogs** opened from a panel were not measured. A modal dialog sits in the top layer, centered, not anchored to
  the panel, so a move shouldn't affect it, but that was not checked.
- **The JS fallback** lost the popover in 22 of 30 runs over five full runs. A move changes the trigger's position
  without a resize or a scroll, so nothing tells the fallback to recompute. Its outcome is nondeterministic, so it is
  reported here, not kept as a test.
- Phase 1's JS fallback needs a position-change trigger; the dock can notify anchored overlays after a re-dock.

**Other findings.**
- Mapping a Playwright `evaluate` result straight onto `JsonElement` failed for nested objects. The tests read them
  through `JSON.stringify`.
