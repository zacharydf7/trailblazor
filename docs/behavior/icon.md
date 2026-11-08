# Icon

Pattern: none; it's a picture, named only when it carries meaning

Familiar apps: none; it isn't a control

## Keyboard

Nothing of its own. It isn't a control.

## Pointer

- [x] An icon set to mirror turns around on a right-to-left page; any other keeps its direction. `IconTests.Icon_ShouldTurnAroundOnlyRightToLeft_WhenItMirrorsInRightToLeft`, `TbIconTests.Render_ShouldMarkTheIconToTurnAroundRightToLeft_WhenTheIconSaysSo`

An icon that acts is an [icon button](icon-button.md).

## Touch

Nothing of its own. It isn't a control.

## Focus

- [x] It takes no focus, and without a `Label` it's hidden from assistive technology. `TbIconTests.Render_ShouldHideTheIconFromAssistiveTechnology_WhenItHasNoLabel`
- [x] With a `Label` it's exposed as a named image. `TbIconTests.Render_ShouldExposeTheIconAsAnImage_WhenItHasALabel`

## Not applicable

- A tooltip on hover: a [tooltip](tooltip.md) can wrap it where the app wants one.
