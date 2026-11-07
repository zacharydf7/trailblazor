# Button group

Pattern: none of its own; each button follows [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/)

Familiar apps: GitHub's merge button with its menu, the Back and Forward pair in a browser

## Keyboard

- [x] Each button takes its own keys, and Tab visits each enabled one in turn. `TbButtonGroup.razor`

## Pointer

- [x] Each button takes its own click. `TbButtonGroup.razor`
- [x] A button and a menu's trigger join with one seam between them. `ButtonGroupTests.ButtonGroup_ShouldJoinAButtonAndAMenusTriggerWithOneSeam_WhenTheyAreFilled`
- [x] `Disabled` turns every button off. `TbButtonGroupTests.Render_ShouldDisableEveryButton_WhenTheGroupIsDisabled`
- [ ] `Disabled` turns a menu's or a popover's trigger off too, including a custom trigger that stays focusable, so it can't open. #335

## Touch

Nothing of its own. A tap reaches each button as it does outside a group.

## Focus

- [x] A focused button is lifted over its neighbors, so all of its ring shows. `trailblazor.css`
- [x] A disabled group's buttons are out of the tab order. `TbButtonGroupTests.Render_ShouldDisableEveryButton_WhenTheGroupIsDisabled`

## Not applicable

- Arrow keys and a single tab stop: a [toolbar](toolbar.md) does that, and a group of two or three buttons has no need for it.
