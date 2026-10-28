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

To be filled in after the spike runs.
