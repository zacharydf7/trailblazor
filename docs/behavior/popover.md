# Popover

Pattern: none of its own; the closest are a non-modal [Dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) opened by a [Disclosure](https://www.w3.org/WAI/ARIA/apg/patterns/disclosure/)

Familiar apps: filter panels in Google Sheets and GitHub, Slack's profile cards

## Keyboard

- [x] Enter on the trigger opens the panel and moves focus to its first control. `PopoverTests.Popover_ShouldTakeFocusAndGiveItBack_WhenOpenedAndClosedWithTheKeyboard`
- [x] Space on the trigger does the same. `TbPopover.razor`
- [x] Escape closes the panel and focus goes back to the trigger. `PopoverTests.Popover_ShouldTakeFocusAndGiveItBack_WhenOpenedAndClosedWithTheKeyboard`
- [x] Escape closes a panel the pointer opened. `PopoverTests.Popover_ShouldOpenWithoutTakingFocusAndCloseOnEscape_WhenHovered`
- [x] Tab past the last control closes the panel. `PopoverTests.Popover_ShouldClose_WhenTabMovesFocusOutOfThePanel`
- [x] Shift+Tab from the first control goes to the trigger and leaves the panel open. `popover.js`
- [x] A natively disabled trigger opens nothing. `PopoverTests.Popover_ShouldOpenNeitherOnAClickNorOnHover_WhenDisabled`
- [ ] Tab past the last control closes the panel when nothing on the page follows it. UNCONFIRMED

## Pointer

- [x] A click on the trigger opens the panel, and a second click closes it. `TbPopover.razor`
- [x] A click elsewhere closes the panel. `PopoverTests.Popover_ShouldCloseFromCodeAndOnAClickElsewhere_WhenItsOpenIsBound`
- [x] A click in the panel keeps it open. `PopoverTests.Popover_ShouldStayOpen_WhenTheUserWorksInAHoverOpenedPanelAndThePointerLeaves`
- [x] With `OpenOnHover`, resting on the trigger opens the panel without taking focus. `PopoverTests.Popover_ShouldOpenWithoutTakingFocusAndCloseOnEscape_WhenHovered`
- [x] With `OpenOnHover`, the panel closes a moment after the pointer leaves both it and the trigger. `popover.js`
- [x] A click on the trigger keeps a panel the pointer opened, and moves focus in. `PopoverTests.Popover_ShouldStayOpenAndTakeFocus_WhenTheTriggerIsClickedAfterAHover`
- [x] Resting on a trigger doesn't close another popover that's open. `PopoverTests.Popover_ShouldNotCloseAnotherPopover_WhenAHoverTriggerIsRestedOn`
- [x] A popover inside an open one opens on hover and leaves the outer one open. `PopoverTests.Popover_ShouldOpenOnHover_WhenItSitsInsideAnotherOpenPopover`
- [x] The panel flips when there's no room. `AnchoredPositionTests.CreateAsync_ShouldFlipToTop_WhenThereIsNoRoomBelow`
- [x] A panel placed to the side in a narrow window stays on screen, overlapping its trigger if it must. `NarrowPlacementTests.Popover_ShouldStayInsideTheWindow_WhenNeitherSideHasRoom`
- [x] A panel wrapped to fit that scrolls keeps its place when the page moves it. `NarrowPlacementTests.Popover_ShouldKeepItsScrollPlace_WhenPlacedAgainWhileWrapped`
- [x] A trigger that's off opens on no hover, including a custom one that stays focusable. `OffTriggerTests.Popover_ShouldStayClosed_WhenTheMouseRestsOnACustomTriggerThatIsOff`

## Touch

- [x] A tap on the trigger opens the panel and moves focus in, as a click does. `TbPopover.razor`
- [x] A tap elsewhere closes it, through the browser's popover handling. `TbPopover.razor`
- [ ] A long press on an `OpenOnHover` trigger doesn't open a panel that closes again on release: the hover listeners don't check for a mouse. UNCONFIRMED

## Focus

- [x] A panel with no controls takes focus itself. `PopoverTests.Popover_ShouldStayOpenAndTakeFocus_WhenTheTriggerIsClickedAfterAHover`
- [x] A panel clicked open before its script has loaded is placed and takes focus once it has, and an opening reported twice doesn't move focus a second time. `PopoverTests.Popover_ShouldPlaceItselfAndTakeFocus_WhenClickedBeforeItsScriptHasLoaded`, `PopoverTests.Popover_ShouldLeaveFocusWhereItIs_WhenAnOpeningIsReportedTwice`
- [x] With `AttachOnFirstUse`, the popover's script starts when its trigger is first focused, pointed at or clicked, or the panel is opened from code, so a popover on every row costs no call to the browser until it's used. `TbPopoverTests.AttachOnFirstUse_ShouldStartTheScriptOnlyOnceTheTriggerTakesFocus_WhenSet`, `TbPopoverTests.AttachOnFirstUse_ShouldStartTheScript_WhenThePointerReachesTheTrigger`, `TbPopoverTests.AttachOnFirstUse_ShouldStartTheScript_WhenTheTriggerIsClickedWithoutFocus`, `TbPopoverTests.AttachOnFirstUse_ShouldStartTheScriptAndOpen_WhenTheParentOpensItBeforeUse`, `TbPopoverTests.AttachOnFirstUse_ShouldStartTheScript_WhenTurnedOffBeforeUse`
- [x] With `AttachOnFirstUse`, a popover that opens on hover opens on the hover that sets up its script. `PopoverTests.Popover_ShouldOpenOnTheFirstHover_WhenItAttachesOnFirstUseAndOpensOnHover`
- [x] With `AttachOnFirstUse`, the testing helper focuses the trigger to set up the script before it opens the popover. `PopoverTests.Popover_ShouldOpenThroughTheTestingHelper_WhenItAttachesOnFirstUse`
- [x] A panel opened from code and closed before its opening was reported still tells the parent it closed. `PopoverTests.Popover_ShouldTellTheParentItClosed_WhenItClosesBeforeTheOpeningFromCodeIsReported`
- [x] A panel opened from code takes focus too. `PopoverTests.Popover_ShouldOpenFromAnIconTriggerAndFromCode_WhenAskedEitherWay`
- [x] Closing returns focus to the trigger only if it was in the panel. `popover.js`
- [x] Focus in a panel the pointer opened keeps it open after the pointer leaves. `PopoverTests.Popover_ShouldStayOpen_WhenTheUserWorksInAHoverOpenedPanelAndThePointerLeaves`
- [x] A button click closing the panel from code returns focus to the trigger. `popover.js`
- [x] Opening puts focus on a radio group's checked radio, not its first. `RadioStopsTests.Popover_ShouldTakeFocusOnTheCheckedRadio_WhenItOpens`

## Not applicable

- Keeping Tab inside the panel: it's non-modal, so Tab moves on and closes it. For a decision that blocks the page, use a dialog.
- Opening on keyboard focus: a hover trigger opens with Enter or Space, so moving through the page doesn't open panels.
