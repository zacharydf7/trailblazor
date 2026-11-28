# Surface

Pattern: none; it's a panel around content

Familiar apps: none; it isn't a control

## Keyboard

Nothing of its own. It only holds content.

## Pointer

Nothing of its own. It only holds content.

- [x] In the dark theme, where its shadow barely shows, a surface at the `Low` elevation with no border has a hairline edge. `StatesTests.Surface_ShouldDrawAnEdgeAroundItsLowElevation_WhenThePageIsDark`

## Touch

Nothing of its own. It only holds content.

## Focus

- [x] It takes no focus, and the tab order follows its content. `TbSurface.cs`
- [x] In forced colors, a surface has a border in the text color, since its shadow and background are dropped there. `ForcedColorsTests.Example_ShouldDrawABorderAroundEachButtonAndSurface_WhenColorsAreForced`

## Not applicable

- A clickable card: it has no click of its own, so a link or button inside gets a real name and a real tab stop.
