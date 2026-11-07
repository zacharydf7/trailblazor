# Label

Pattern: none for a label itself; its info button opens a popover panel, so [popover](popover.md) applies to it too

Familiar apps: the "i" button beside a setting in cloud consoles and settings pages

## Keyboard

- [x] Tab stops on the info button, and the label's text is no tab stop. `TbLabel.razor`
- [x] Enter or Space on the info button opens the panel and moves focus into it. `PopoverTests.Popover_ShouldTakeFocusAndGiveItBack_WhenOpenedAndClosedWithTheKeyboard`
- [x] Escape closes the panel and returns focus to the button when focus was in the panel. `PopoverTests.Popover_ShouldTakeFocusAndGiveItBack_WhenOpenedAndClosedWithTheKeyboard`
- [x] Tab out of the panel closes it. `PopoverTests.Popover_ShouldClose_WhenTabMovesFocusOutOfThePanel`
- [x] Pressing the info button inside a form doesn't submit the form. `TbButtonBase.cs`

## Pointer

- [x] Clicking the text of a label with `For` focuses its control. `TbLabelTests.Render_ShouldBeALabelForTheControl_WhenForIsSet`
- [x] Clicking the text of a label without `For` does nothing. `TbLabelTests.Render_ShouldBePlainText_WhenThereIsNoFor`
- [x] Clicking the info button doesn't activate the control, since the button sits outside the `<label>`. `TbLabel.razor`
- [x] Resting the pointer on the info button opens the panel without taking focus. `LabelTests.Label_ShouldShowRichInfoFlowingAsMarkup_WhenThePointerRestsOnTheButton`
- [x] Moving the pointer onto the panel keeps it open. `PopoverTests.Popover_ShouldOpenWithoutTakingFocusAndCloseOnEscape_WhenHovered`
- [x] Clicking the button after a hover keeps the panel open and moves focus into it. `PopoverTests.Popover_ShouldStayOpenAndTakeFocus_WhenTheTriggerIsClickedAfterAHover`
- [x] Clicking elsewhere closes the panel. `PopoverTests.Popover_ShouldCloseFromCodeAndOnAClickElsewhere_WhenItsOpenIsBound`

## Touch

- [x] Tapping the info button opens the panel and moves focus into it: the touch's `pointerleave` cancels the hover timer, and the tap's click opens it. `popover.js`
- [x] Tapping elsewhere closes the panel, since it is a native `popover="auto"`. `TbPopover.razor`

## Focus

- [x] The panel's focus goes back to the info button when it closes with focus inside. `PopoverTests.Popover_ShouldStayOpenAndTakeFocus_WhenTheTriggerIsClickedAfterAHover`
- [x] The info button shows the button's focus ring. `trailblazor.css`

## Not applicable

- Required and validation behavior: the label only draws the marker, hidden from screen readers; the control does the rest.
- Clicking a label without `For`: it is a group's heading, and a group has no single control to focus.
