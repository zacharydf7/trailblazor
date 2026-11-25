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
- [x] While the task its `OnClick` returned is still running, a button ignores further clicks, including the second click of a double click, and prevents that click's default action, so a form isn't submitted twice. After 200 milliseconds it shows its spinner and `aria-busy`, for at least 400. A handler that throws frees it again. `TbButtonBusyTests.Click_ShouldCallOnClickOnce_WhenClickedTwiceWhileTheHandlerRuns`, `TbButtonBusyTests.Click_ShouldShowNoSpinner_WhenTheHandlerFinishesWithinTheShowDelay`, `TbButtonBusyTests.Click_ShouldKeepTheSpinnerForItsMinimum_WhenTheHandlerFinishesJustAfterTheShowDelay`, `TbButtonBusyTests.Click_ShouldAcceptClicksAgain_WhenTheHandlerThrew`, `ButtonTests.Button_ShouldCallOnClickOnceAndShowItsSpinner_WhenDoubleClickedWhileTheClickRuns`
- [x] With `Cooldown`, a button stays off for that long after its `OnClick` finishes: it ignores clicks but stays focusable, marked `aria-disabled`, and shows the seconds left after its text, hidden from screen readers. A handler that throws starts no cooldown, and toggles and links never cool down. It ends on time, even partway through a second. `TbButtonCooldownTests.Click_ShouldTurnTheButtonOffAndCountDown_WhenItHasACooldown`, `TbButtonCooldownTests.Click_ShouldNotCoolDown_WhenTheHandlerThrew`, `TbButtonCooldownTests.Click_ShouldNotCoolDown_WhenTheButtonIsAToggle`, `TbButtonCooldownTests.Click_ShouldNotCoolDown_WhenTheButtonIsALink`, `TbButtonCooldownTests.Click_ShouldEndTheCooldownOnTime_WhenItIsntWholeSeconds`, `TbButtonCooldownTests.Click_ShouldCoolDown_WhenTheButtonIsNotBusyWhileItRuns`
- [x] During a cooldown the button keeps its name, and the app's own `aria-describedby` stays first and is all that's left once it ends. `TbButtonCooldownTests.Button_ShouldKeepItsNameAndTheAppsDescription_WhenItCoolsDown`
- [x] `Labels` words the seconds left and the description. `TbButtonCooldownTests.Labels_ShouldWordTheSecondsAndTheDescription_WhenGiven`
- [x] As a cooldown starts, the button's description becomes "Available again in 30 seconds" (or "in 1 second"), and it's read out once; the description keeps up with the seconds left for whoever focuses the button later. `TbButtonCooldownTests.Click_ShouldTurnTheButtonOffAndCountDown_WhenItHasACooldown`, `TbButtonCooldownTests.Click_ShouldSayOneSecond_WhenTheCooldownIsASecond`, `TbButtonCooldownTests.Click_ShouldSayTheSecondsLeft_WhenTheButtonIsFocusedLater`
- [x] A slow click's spinner gives way to the seconds left as soon as the cooldown starts. `TbButtonCooldownTests.Click_ShouldTakeTheSpinnerAway_WhenACooldownStartsAfterASlowClick`
- [x] `BusyWhileRunning="false"`, or `ButtonSettings.BusyWhileRunning = false` for the app, lets every click through, and toggles and links always do. `TbButtonBusyTests.Click_ShouldCallOnClickTwice_WhenBusyWhileRunningIsOff`, `TbButtonBusyTests.Click_ShouldCallOnClickTwice_WhenTheAppTurnsBusyWhileRunningOff`, `TbButtonBusyTests.Click_ShouldToggleTwice_WhenPressedIsBoundAndClickedTwice`, `TbButtonBusyTests.Click_ShouldCallOnClickTwice_WhenTheButtonIsALink`
- [x] Hover and press each deepen the fill. `ButtonTests.Button_ShouldDeepenItsSoftFill_WhenHoveredAndThenPressed`
- [x] A disabled, loading or cooling-down button shows no hover or press change, and a not-allowed cursor. `trailblazor.css`
- [x] Pressing, then moving off before release, cancels the click. `TbButtonBase.razor`
- [x] A button with an `Href` navigates on click. `ButtonTests.Button_ShouldNavigateAndShowItsHover_WhenItIsALink`
- [x] Ctrl+click and middle-click open a button with an `Href` in a new tab. `TbButtonBase.razor`
- [x] A disabled link has no address to follow. `TbButtonTests.Render_ShouldDropTheAddressButKeepTheLinkRole_WhenALinkIsDisabled`
- [x] A loading link ignores clicks. `TbButtonTests.Click_ShouldIgnoreTheClick_WhenALinkIsLoading`
- [x] A submit button ignores clicks and shows a spinner while the `TbForm` it submits handles a submit: the one its `form` attribute names, or else the one around it. `TbFormOtherFormButtonTests.Button_ShouldShowBusy_WhenTheFormItsFormAttributeNamesSubmits`, `TbFormSubmittingTests.Click_ShouldBeIgnored_WhenTheFormIsHandlingASubmit`, `TbFormSubmittingTests.Submit_ShouldShowTheSubmitButtonsBusy_WhenTheHandlerIsRunning`
- [x] Turning an outlined toggle off shows at once, while the pointer is still on it. `ToggleItemsTests.Item_ShouldShowItTurnedOff_WhenClickedWithThePointerStillOnIt`
- [x] With `PressedLook.Quiet`, a pressed toggle shows a tint and a bar in its text color, and the bar stands out by at least 3:1 from the tint, the page and a hovered or selected tree row. `TokensTests.PressedToggle_ShouldStandOutAndKeepItsTextReadable_WhenAPaletteAndThemeAreApplied`, `TbButtonTests.Render_ShouldMarkTheQuietLook_WhenGivenAPressedLook`

## Touch

- [x] A tap activates it through the same click a mouse sends. `TbButtonBase.razor`
- [x] The smallest size keeps a target at least 26px high. `trailblazor.css`
- [ ] The hover color doesn't stay on after a tap. UNCONFIRMED

## Focus

- [x] Keyboard focus shows a ring. `ButtonTests.Button_ShouldShowAFocusRing_WhenFocusedByKeyboard`
- [x] A mouse click doesn't show the ring. `trailblazor.css`
- [x] A disabled button is out of the tab order. `TbButtonTests.Click_ShouldNotInvokeOnClick_WhenDisabled`
- [x] A focusable disabled button stays in the tab order and shows its tooltip. `ButtonTests.Button_ShouldStayInTheTabOrderAndShowItsTooltip_WhenDisabledButFocusable`
- [x] A button that turns itself off with a reason keeps focus, in a toolbar too and when the reason arrives with `Disabled`, and keeps its own place in the layout: full width, or fixed for a floating button, whose tooltip sits next to it. `ButtonTests.Button_ShouldKeepFocus_WhenItTurnsItselfOffWithAReason`, `ButtonTests.Button_ShouldKeepItsOwnPlaceAndTooltip_WhenOffWithAReason`
- [x] On a static page, a submit button that's off with a reason doesn't post its form, from a click or from Enter in a field. `ButtonTests.Button_ShouldNotPostItsForm_WhenOffWithAReasonOnAStaticPage`
- [x] A focusable disabled link stays in the tab order. `TbButtonTests.Render_ShouldKeepTheLinkFocusable_WhenDisabledButFocusable`
- [x] A disabled button with a `DisabledReason` stays in the tab order and is described by the reason, which a tooltip shows on hover, keyboard focus and a tap. A press does nothing but count `pressed_off`, and an enabled button ignores the reason. `ButtonTests.Button_ShouldSayWhyItIsOff_WhenHoveredFocusedOrTapped`, `DisabledReasonTests.Render_ShouldDescribeTheButtonWithItsReason_WhenDisabled`, `DisabledReasonTests.Click_ShouldStayFocusableAndCountThePress_WhenDisabledWithAReason`, `DisabledReasonTests.Render_ShouldIgnoreTheReason_WhenEnabled`
- [x] A loading button keeps focus while the work runs. `TbButtonTests.Click_ShouldNotInvokeOnClickButStayFocusable_WhenLoading`
- [x] `FocusAsync` moves focus to it. `TbButtonTests.FocusAsync_ShouldFocusTheButton_WhenCalled`

## Not applicable

- Space on a button with an `Href`: it's a link, and links only take Enter.
- Keeping focus when `Disabled` turns on: a native disabled button can't hold focus, so `Loading` or `FocusableWhenDisabled` is the way to keep it.
