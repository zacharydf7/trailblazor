# Text

Pattern: none; it's text in the type scale

Familiar apps: none; it isn't a control

## Keyboard

Nothing of its own. It isn't a control.

## Pointer

Nothing of its own. The text can be selected as usual.

## Touch

Nothing of its own. It isn't a control.

## Focus

- [x] It takes no focus, and `MaxLines` cuts off only what shows. `TextTests.Text_ShouldShowOnlyItsLinesWithAnEllipsis_WhenGivenMaxLines`

## Not applicable

- Revealing cut-off text on hover: a tooltip wouldn't reach keyboard or touch users, so text that matters shouldn't be cut off, or the app adds its own control.
