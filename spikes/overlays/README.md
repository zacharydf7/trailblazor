# Spike A: overlays on native `<dialog>` and the Popover API

**Question.** Can every Trailblazor overlay (modal and non-modal dialogs, menus, selects, tooltips, popovers) sit in
the browser's top layer with native `<dialog>`, the Popover API and anchored positioning, so the library needs no
portal provider and no z-index stack?

## What gets built

- A modal dialog (`showModal()`), a non-modal dialog, and a modal nested two deep.
- A menu (`popover="auto"`) opened from inside a modal, and a tooltip (`popover="manual"`).
- Both positioning paths for the menu and tooltip: native CSS anchor positioning, and the JS fallback in
  `Trailblazor.Primitives` (`anchored-position.js`).
- Repro layouts for the two kinds of misalignment the first consumer hit after a library upgrade:
  - **Edge tooltips:** a toolbar of icon buttons flush with the left edge of the viewport, inside a container with
    `overflow: hidden`, each button with a tooltip.
  - **Selects inside dialogs:** select-style dropdowns inside a modal dialog whose body scrolls, including selects
    in the rows of a table, opened after the dialog body has been scrolled, plus one select near the bottom of the
    viewport.

## Pass criteria

1. **Modal dialog.**
   - Focus moves into the dialog when it opens, and Tab and Shift+Tab never leave it.
   - The page behind is inert: a click on it does nothing, and it is absent from the accessibility tree.
   - The page behind does not scroll while the dialog is open.
   - Focus returns to the element that opened the dialog when it closes.
2. **Escape.**
   - Escape closes a modal dialog.
   - With escape dismissal turned off, the dialog stays open after Escape is pressed twice in a row in every
     engine. Chromium's close-watcher rules are the known risk here.
3. **Nested modals, two deep.** Escape closes only the inner dialog, and focus returns to the element inside the
   outer dialog that opened it.
4. **Non-modal dialogs.**
   - The page stays clickable and focusable, and two non-modal dialogs can be open at once.
   - A non-modal dialog renders above an ancestor with `z-index: 9999`, `transform` and `overflow: hidden`.
     `<dialog>.show()` alone does not use the top layer, so this tests `<dialog popover="manual">`.
5. **Popover inside a modal.**
   - A menu opened inside a modal renders above the modal and is clickable.
   - A press outside the menu or Escape closes the menu but not the dialog.
6. **Scoped theming.** A popover inherits CSS custom properties set on its DOM ancestors, such as
   `--tb-color-primary` set on a wrapper element.
7. **Positioning.** For both paths, native and JS fallback:
   - Placement is within 1 px of the expected position on all four sides.
   - The overlay flips when its side has no room and shifts to stay inside the viewport.
   - It stays attached while a nested scroll container scrolls and while the window resizes.
   - In both repro layouts, every tooltip and dropdown lines up with its trigger within 1 px.
8. **Render modes.**
   - Prerendered markup contains the closed overlays and no script errors occur, in Interactive Server and
     WebAssembly.
   - Dropping the circuit while a dialog is open throws no unhandled exception on the server.

## How it is measured

Each criterion is a Playwright test in `spikes/Trailblazor.Spikes.Tests`, run on Chromium, Firefox and WebKit, in
both render modes. Accessibility-tree checks use Playwright's ARIA snapshots.

## If a criterion fails

- If 2 fails, Trailblazor handles Escape in its own keydown listener and calls `close()` itself instead of relying
  on the dialog's `cancel` event.
- If 4 fails, non-modal dialogs fall back to a single top-layer host element that Trailblazor manages.
- If the native path in 7 fails in any engine, the JS fallback becomes the only positioning path, and native
  anchoring waits for a later release.


## Results

**Verdict: pass, with the criterion 2 fallback adopted.** On 2026-10-28, all 247 Playwright runs passed, twice in a
row. The planned criterion 2 design is recorded as a test that asserts how the engines actually behave, so the suite
stays green while it documents why that design is rejected.

Measured on Windows 11, .NET SDK 10.0.403 and Playwright 1.64.1 with its bundled engines (Chromium build 1248,
Firefox build 1555, WebKit build 2370), headless. Every criterion ran in Interactive Server and WebAssembly. The
tests are in `spikes/Trailblazor.Spikes.Tests/Overlays`.

| # | Criterion | Result |
| --- | --- | --- |
| 1 | Modal dialog | Pass in all engines and modes. The accessibility-tree part was measured in Chromium only. |
| 2 | Escape | Planned design rejected, fallback passes (see below) |
| 3 | Nested modals | Pass |
| 4 | Non-modal dialogs | Pass: `<dialog popover="manual">` opened with `show()` then `showPopover()` |
| 5 | Popover inside a modal | Pass |
| 6 | Scoped theming | Pass |
| 7 | Positioning | Pass for the native and the JS path, on all four sides and in every repro layout |
| 8 | Render modes | Pass |

**Criterion 1.**
- Tab and Shift+Tab, pressed repeatedly from either end, never put focus on page content.
- Programmatic focus on page content fails while the modal is open, which shows the page is inert in every engine.
- Chromium's accessibility tree, read over the DevTools protocol, leaves the page out.
- Firefox's and WebKit's accessibility trees can't be read from Playwright, and its ARIA snapshot doesn't reflect
  inertness, so that part is unmeasured in those two engines.
- Native modals don't lock page scroll, so the library adds `:root:has(dialog:modal) { overflow: hidden }`.

**Criterion 2.**
- **Planned design rejected.** With only the dialog's `cancel` event cancelled, two Escape presses close the dialog in
  every engine, whatever the timing. The engines only let a close request be cancelled after
  fresh user activation, and Escape never grants activation. Users do press Escape repeatedly, so this design can't
  promise "Escape doesn't close".
- **Fallback adopted.** A capture-phase `keydown` listener on the document calls `preventDefault()` on Escape while
  the dialog is modal. That stops the close request from ever being made. The dialog stayed open for repeated
  presses, including with focus on `<body>`.
- Phase 1 must apply the block to the topmost modal only, so a nested dialog that allows Escape still closes.
- Blazor declares `oncancel` without `preventDefault` support, so this wiring lives in the library's JS.

**Criterion 7.**
- Both paths placed a probe within 1 px on the top, bottom, start and end of its anchor.
- Both flipped a listbox above a trigger at the viewport bottom.
- Both kept a listbox attached while a dialog body scrolled and while the window resized.
- Both lined up a listbox with a select in a scrolled dialog table.
- Edge tooltips stayed inside the viewport: the JS path shifts them to its 8 px padding, while native positioning
  lands them flush at x = 0, with no padding option. A tooltip with room was centered by both.

Native anchoring can become the default path once the oldest browser version Trailblazor supports has it. This run
only covered the newest engine builds, so the JS fallback stays until that is checked.

**Criterion 8.**
- Prerendered markup contains every dialog closed, in both modes.
- Neither spike page raised a script error in any measured flow.
- Navigating away with a dialog and an anchored listbox open tore that page's own circuit down with no new server
  errors. Disposal hit `JSDisconnectedException` and handled it.

**Other findings for phase 1.**
- The JS initializer has its first real job. Registering a `popovertoggle` custom event (the browser's `toggle` with
  `newState`) let .NET learn about a light-dismissed menu in both render modes.
- A client project's `wwwroot` is served at the app root, not under `_content/`. That matters for the docs site and
  any WebAssembly host.
- Playwright's `evaluate` grants the page user activation. A test of activation-gated behavior must not call it
  between the steps it measures. An early version of the criterion 2 test did, and wrongly showed slow Escape presses
  being blocked.
