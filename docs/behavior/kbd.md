# Kbd

Pattern: none; it is the HTML `kbd` element, which marks text as keyboard input

Familiar apps: the shortcut hints in GitHub's and VS Code's menus, keyboard help pages

## Keyboard

Nothing of its own: it is text, with nothing to focus or press.

## Pointer

Nothing of its own: it is text.

## Touch

Nothing of its own: it is text.

## Focus

- [x] Screen readers read each key as text, with "plus" between the keys of a combination. `TbKbdTests.Render_ShouldShowEachKeyInItsOwnKbd_WhenGivenACombination`
- [x] Each keycap keeps its edges in forced-colors mode. `KbdTests.Kbd_ShouldKeepItsKeycapEdges_WhenColorsAreForced`

## Not applicable

- Showing Cmd on a Mac and Ctrl elsewhere: the page can't know the device before it renders, so the app passes the keys it wants shown.
