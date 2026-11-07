# Button

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/)

Familiar apps: the buttons in any native dialog, Gmail's and GitHub's action buttons

## Keyboard

- [x] Enter and Space activate it. `ButtonTests.Button_ShouldActivate_WhenEnterOrSpaceIsPressed`
- [x] Space turns a toggle on or off, and its state is exposed as pressed or not. `ButtonTests.Button_ShouldTogglePressed_WhenSpaceIsPressedOnAToggle`
- [x] Enter in a form field submits through a submit button. `ButtonTests.Button_ShouldSubmitTheForm_WhenClickedOrEnterIsPressedInAField`
- [x] A button that doesn't set `Type` never submits the form around it. `TbButtonTests.Render_ShouldRenderAFilledPrimaryButton_WhenNoParametersAreSet`
- [x] A loading or focusable disabled submit button doesn't submit, from a click or from Enter in a field. `ButtonTests.Button_ShouldNotSubmitTheForm_WhenLoadingOrDisabledButFocusable`
- [x] Enter follows a button with an `Href`, as on any link. `TbButtonBase.razor`

## Pointer

- [x] A click runs `OnClick`. `TbButtonTests.Click_ShouldInvokeOnClick_WhenEnabled`
- [x] A disabled button ignores clicks. `TbButtonTests.Click_ShouldNotInvokeOnClick_WhenDisabled`
- [x] A focusable disabled button ignores clicks. `TbButtonTests.Click_ShouldIgnoreTheClickButStayFocusable_WhenDisabledButFocusable`
- [x] A loading button ignores clicks and shows a spinner. `TbButtonTests.Click_ShouldNotInvokeOnClickButStayFocusable_WhenLoading`
- [x] Hover and press each deepen the fill. `ButtonTests.Button_ShouldDeepenItsSoftFill_WhenHoveredAndThenPressed`
- [x] A disabled or loading button shows no hover or press change, and a not-allowed cursor. `trailblazor.css`
- [x] Pressing, then moving off before release, cancels the click. `TbButtonBase.razor`
- [x] A button with an `Href` navigates on click. `ButtonTests.Button_ShouldNavigateAndShowItsHover_WhenItIsALink`
- [x] Ctrl+click and middle-click open a button with an `Href` in a new tab. `TbButtonBase.razor`
- [x] A disabled link has no address to follow. `TbButtonTests.Render_ShouldDropTheAddressButKeepTheLinkRole_WhenALinkIsDisabled`
- [x] A loading link ignores clicks. `TbButtonTests.Click_ShouldIgnoreTheClick_WhenALinkIsLoading`
- [ ] Turning an outlined toggle off shows at once, while the pointer is still on it. #310

## Touch

- [x] A tap activates it through the same click a mouse sends. `TbButtonBase.razor`
- [x] The smallest size keeps a target at least 26px high. `trailblazor.css`
- [ ] The hover color doesn't stay on after a tap. UNCONFIRMED

## Focus

- [x] Keyboard focus shows a ring. `ButtonTests.Button_ShouldShowAFocusRing_WhenFocusedByKeyboard`
- [x] A mouse click doesn't show the ring. `trailblazor.css`
- [x] A disabled button is out of the tab order. `TbButtonTests.Click_ShouldNotInvokeOnClick_WhenDisabled`
- [x] A focusable disabled button stays in the tab order and shows its tooltip. `ButtonTests.Button_ShouldStayInTheTabOrderAndShowItsTooltip_WhenDisabledButFocusable`
- [x] A focusable disabled link stays in the tab order. `TbButtonTests.Render_ShouldKeepTheLinkFocusable_WhenDisabledButFocusable`
- [x] A loading button keeps focus while the work runs. `TbButtonTests.Click_ShouldNotInvokeOnClickButStayFocusable_WhenLoading`
- [x] `FocusAsync` moves focus to it. `TbButtonTests.FocusAsync_ShouldFocusTheButton_WhenCalled`

## Not applicable

- Space on a button with an `Href`: it's a link, and links only take Enter.
- Keeping focus when `Disabled` turns on: a native disabled button can't hold focus, so `Loading` or `FocusableWhenDisabled` is the way to keep it.
- Ignoring a double click: every click counts unless the app sets `Loading`, since only the app knows when the work ends.
