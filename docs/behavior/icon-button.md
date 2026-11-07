# Icon button

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/)

Familiar apps: the icon bars in Gmail and Google Docs, GitHub's issue actions

## Keyboard

- [x] Enter and Space activate it, as on a [button](button.md). `TbButtonBase.razor`
- [x] Enter and Space turn it on or off when `Pressed` is bound. `TbButtonBase.razor`
- [x] Escape hides a shown tooltip without moving focus. `TooltipTests.Tooltip_ShouldDescribeTheTriggerAndCloseOnEscape_WhenTheTriggerHasFocus`

## Pointer

- [x] A click runs `OnClick`, and a disabled or loading one ignores it. `TbButtonBase.cs`
- [x] A loading one shows a spinner in place of its icon and keeps its name. `TbIconButtonTests.Render_ShouldShowTheSpinnerInsteadOfTheIcon_WhenLoading`
- [x] With `ShowTooltip`, resting on it shows its name after a short delay. `TooltipTests.Tooltip_ShouldWaitBeforeAppearing_WhenThePointerArrives`
- [x] Without `ShowTooltip`, no tooltip shows. `TbIconButtonTests.Render_ShouldShowNoTooltip_WhenShowTooltipIsNotSet`
- [x] A click doesn't leave the tooltip open. `TooltipTests.Tooltip_ShouldStayClosed_WhenTheTriggerIsClicked`
- [x] The tooltip closes when the pointer leaves. `TooltipTests.Tooltip_ShouldClose_WhenThePointerLeaves`

## Touch

- [x] A tap activates it and doesn't leave a tooltip open, since the press hides it and cancels its timer. `tooltip.js`
- [x] The smallest size keeps a square target of at least 26px. `trailblazor.css`
- [ ] The hover color doesn't stay on after a tap. UNCONFIRMED

## Focus

- [x] Keyboard focus shows a ring. `trailblazor.css`
- [x] Keyboard focus shows the tooltip, and its name isn't read twice. `ButtonTests.IconButton_ShouldShowItsNameWithoutDescribingItself_WhenFocusedWithShowTooltip`
- [x] Focus left by a click doesn't bring the tooltip back. `TooltipTests.Tooltip_ShouldStayClosed_WhenFocusReturnsToTheTriggerAfterAClickElsewhere`
- [x] A focusable disabled one stays in the tab order, and a loading one keeps focus. `TbButtonBase.razor`

## Not applicable

- A tooltip by default: it's opt-in with `ShowTooltip`, since a familiar icon in a labeled row doesn't need one.
- A name on touch: a finger can't hover, so people on touch only get the name through a screen reader.
