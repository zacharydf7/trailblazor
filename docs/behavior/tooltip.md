# Tooltip

Pattern: [Tooltip](https://www.w3.org/WAI/ARIA/apg/patterns/tooltip/)

Familiar apps: Windows and macOS toolbar tips, VS Code's toolbar, a browser's `title` tips

## Keyboard

- [x] Tabbing to the trigger shows the tooltip at once. `TooltipTests.Tooltip_ShouldShow_WhenTheKeyboardBringsFocusBackAfterAClick`
- [x] Tabbing away hides it. `TooltipTests.Tooltip_ShouldClose_WhenFocusMovesAway`
- [x] Escape hides it while the trigger has focus. `TooltipTests.Tooltip_ShouldDescribeTheTriggerAndCloseOnEscape_WhenTheTriggerHasFocus`
- [x] Escape also hides a tooltip the pointer opened, wherever focus is. `tooltip.js`
- [x] The Escape that hides a tooltip doesn't also close the dialog around it. `DialogTests.Dialog_ShouldHideTheTooltipFirstAndCancelNext_WhenEscapeIsPressedWithATooltipOpen`
- [x] A disabled button that stays focusable shows its tooltip on Tab. `ButtonTests.Button_ShouldStayInTheTabOrderAndShowItsTooltip_WhenDisabledButFocusable`

## Pointer

- [x] Resting the pointer on the trigger shows the tooltip after a delay. `TooltipTests.Tooltip_ShouldWaitBeforeAppearing_WhenThePointerArrives`
- [x] A pointer that leaves before the delay ends shows nothing. `tooltip.js`
- [x] Leaving the trigger hides the tooltip after a delay. `TooltipTests.Tooltip_ShouldClose_WhenThePointerLeaves`
- [x] Moving onto the bubble keeps it open, across the gap too. `TooltipTests.Tooltip_ShouldStayOpen_WhenThePointerMovesOntoIt`
- [x] Pressing the trigger hides the tooltip. `TooltipTests.Tooltip_ShouldClose_WhenTheTriggerIsPressedWhileItShows`
- [x] A click leaves it closed while the pointer stays on the trigger. `TooltipTests.Tooltip_ShouldStayClosed_WhenTheTriggerIsClicked`
- [x] It follows the trigger as the page scrolls. `AnchoredPositionTests.Scroll_ShouldKeepFloatingAttached_WhenScrollContainerScrolls`
- [x] It flips to the other side when there's no room. `AnchoredPositionTests.CreateAsync_ShouldFlipToTop_WhenThereIsNoRoomBelow`
- [x] Only one tooltip shows at a time, so hovering a trigger hides the one showing for keyboard focus or still waiting out its hide delay. `TooltipTests.Tooltip_ShouldHideTheFocusedTriggersTooltip_WhenAnotherTriggerIsHovered`, `TooltipTests.Tooltip_ShouldHideOneWaitingToHide_WhenAnotherShows`
- [ ] Hovering a natively disabled trigger shows its tooltip. UNCONFIRMED
- [x] A tooltip placed to the side in a narrow window stays on screen, overlapping its trigger if it must. `NarrowPlacementTests.Tooltip_ShouldStayInsideTheWindow_WhenNeitherSideHasRoom`
- [x] Long text wraps to fit beside the trigger when it can, and holds still there. `NarrowPlacementTests.Tooltip_ShouldWrapBesideItsTriggerAndHoldStill_WhenItsTextCanWrap`
- [x] A tooltip forced over its trigger lets a press through to the trigger, so it's the one case where moving onto the bubble doesn't keep it open. `NarrowPlacementTests.Trigger_ShouldTakeTheClick_WhenItsTooltipCoversIt`
- [x] A tooltip goes back to its natural width once the window has room for it. `NarrowPlacementTests.Tooltip_ShouldGoBackToItsNaturalWidth_WhenTheWindowWidens`
- [x] Placed at the start, a long tooltip wraps to fit there too. `NarrowPlacementTests.Tooltip_ShouldWrapBeforeItsTrigger_WhenPlacedAtTheStart`

## Touch

- [x] A tap on a button shows no tooltip: the trigger listens for pointer events, and the press cancels the delay the touch started. `tooltip.js`
- [x] A tap into a text field shows it, since a text field's focus counts as keyboard focus. `tooltip.js`

## Focus

- [x] Focus handed back after a mouse click, such as by a dialog closed with the mouse, shows no tooltip. `TooltipTests.Tooltip_ShouldStayClosed_WhenFocusReturnsToTheTriggerAfterAClickElsewhere`
- [x] A text field shows its tooltip on any focus. `DialogTests.Dialog_ShouldHideTheTooltipFirstAndCancelNext_WhenEscapeIsPressedWithATooltipOpen`
- [x] The tooltip's text is the trigger's description. `TooltipTests.Tooltip_ShouldDescribeTheTriggerWithItsContent_WhenGivenRichContent`
- [x] The description comes back when the trigger re-renders. `TooltipTests.Tooltip_ShouldStillDescribeTheTrigger_WhenTheTriggerRewritesItsDescription`
- [x] A tooltip turned off neither shows nor describes. `TooltipTests.Tooltip_ShouldNeitherShowNorDescribe_WhileItIsTurnedOff`

## Not applicable

- Enter or Space closing the tooltip: it still describes a focused control, and familiar libraries differ.
- Staying open while the pointer or focus is still on its trigger: it hides when either one leaves, which WCAG 1.4.13 allows.
- Links or buttons in the bubble: the keyboard can't reach it, so that content belongs in a popover.
- Long press to show the tooltip, as on Android: the docs send what touch users need to the page or a popover.
- A shorter wait when moving between neighboring triggers, as in OS toolbars: each tooltip waits its own `ShowDelay`.
