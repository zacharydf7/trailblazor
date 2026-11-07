# Number field

Pattern: [Spinbutton](https://www.w3.org/WAI/ARIA/apg/patterns/spinbutton/)

Familiar apps: the browser's number box, the Windows number up-down control, the spin controls in Excel

## Keyboard

- [x] Up and Down step the value by `Step`. `NumberFieldTests.NumberField_ShouldStepWithTheArrowKeys_WhenFocused`
- [x] A field with a `Format` steps the same way, and the caret stays put. `NumberFieldTests.NumberField_ShouldShowTheValueInItsFormat_WhenTypedOrSteppedWithAFormat`
- [x] An arrow steps from text typed but not yet committed. `NumberFieldTests.NumberField_ShouldStepFromTheTypedText_WhenAnArrowIsPressedBeforeItIsCommitted`
- [x] Arrow presses aren't lost while updates are slow to come back. `NumberFieldTests.NumberField_ShouldKeepEveryArrowStep_WhenEachUpdateIsSlowToComeBack`
- [x] Arrows stop at `Min` and `Max`. `TbNumberField.razor`
- [x] Enter commits the text and submits the field's form. `TbNumberField.razor`
- [x] Tab commits the text and skips the step buttons. `NumberFieldTests.NumberField_ShouldSkipItsStepButtons_WhenTabbing`, `TbNumberField.razor`
- [x] In a read-only field the arrows change nothing and the step buttons are off. `TbNumberFieldTests.Render_ShouldTurnOffTheStepButtonsAndMakeTheInputReadOnly_WhenReadOnly`, `ReadOnlyTests.ReadOnlyField_ShouldKeepItsValueButTakeFocus_WhenTheUserTriesToEditIt`
- [x] Text that isn't a number stays in the box with a message. `NumberFieldTests.NumberField_ShouldSayItNeedsANumber_WhenAnEmptyFieldGetsTextTheBrowserCannotRead`
- [x] A typed value outside the limits explains them and keeps the old value. `NumberFieldTests.NumberField_ShouldExplainTheLimits_WhenATypedValueIsOutsideThem`
- [ ] Arrows and step buttons land on the same number from a value that is off the step. UNCONFIRMED

## Pointer

- [x] A step button changes the value by `Step`. `TbNumberFieldTests.Click_ShouldStepTheValue_WhenAStepButtonIsClicked`
- [x] A step button stops at the limit and turns off there. `TbNumberFieldTests.Click_ShouldStopAtTheLimitAndDisableTheButton_WhenAStepWouldPassIt`
- [x] A step in an empty field starts from the minimum. `TbNumberFieldTests.Click_ShouldStartFromTheMinimum_WhenTheFieldIsEmpty`
- [x] A step button doesn't submit the form. `TbButtonBase.cs`
- [x] Clicking the label focuses the input. `TbFieldBase.razor`
- [ ] Holding a step button keeps stepping, as the browser's own spinner does. #148
- [ ] Clicking anywhere in the box, such as its padding or a start icon, focuses the input. #303
- [ ] Scrolling the wheel over a focused field leaves its value alone. UNCONFIRMED

## Touch

- [x] The step buttons are plain buttons, so a tap is a click. `TbNumberField.razor`
- [x] A field with a `Format` asks for the decimal keyboard. `TbNumberFieldTests.Render_ShouldBeASpinButtonShowingTheFormattedValue_WhenGivenAFormat`
- [ ] The on-screen keyboard offers a minus sign when the field allows negative numbers. UNCONFIRMED
- [ ] Quick taps on a step button step each time without zooming the page. UNCONFIRMED

## Focus

- [ ] Clicking a step button leaves focus in the input, as the browser's own spinner does. #315
- [x] A disabled field can't take focus. `TbNumberField.razor`
- [x] A read-only field takes focus. `ReadOnlyTests.ReadOnlyField_ShouldKeepItsValueButTakeFocus_WhenTheUserTriesToEditIt`
- [x] `FocusAsync` moves focus to the input. `FieldFocusTests.FocusAsync_ShouldMoveFocusToTheFieldsInput_WhenCalled`
- [x] The whole box shows the focus ring while focus is inside it. `trailblazor.css`

## Not applicable

- Home, End, Page Up and Page Down: optional in the pattern, and the browser's number box doesn't use them to step either.
- The browser's own spinners: hidden, since the field draws its own step buttons.
