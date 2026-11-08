# Radio group

Pattern: [Radio group](https://www.w3.org/WAI/ARIA/apg/patterns/radio/)

Familiar apps: the browser's own radio buttons, the shipping choice at a checkout, Windows dialog options

## Keyboard

- [x] Tab lands on the chosen radio and is one stop for the group. `RadioGroupTests.RadioGroup_ShouldBeOneTabStopOnTheCheckedOption_WhenTabbedInto`
- [x] With nothing chosen, Tab lands on the first enabled radio, since the radios share a native name. `TbRadio.razor`
- [x] Arrows move to the next or previous option and choose it. `RadioGroupTests.RadioGroup_ShouldMoveTheSelection_WhenAnArrowKeyIsPressed`
- [x] Arrows skip disabled options. `TbRadio.razor`
- [x] Arrows wrap at the ends of the group in Chromium and Firefox. `TbRadioGroup.razor`
- [x] Space chooses the focused radio when nothing is chosen yet. `TbRadio.razor`
- [x] Fast presses leave the group showing the value the server holds. `CheckboxTests.Controls_ShouldShowTheStateTheServerHolds_WhenEachUpdateIsSlowToComeBack`
- [x] Arrows in a read-only group change nothing. `ReadOnlyTests.ReadOnlyRadioGroup_ShouldKeepItsChoice_WhenTheUserTriesAnother`
- [ ] In Safari, arrows wrap at the ends of the group too. UNCONFIRMED
- [ ] In right-to-left text, Left moves to the next option and Right to the previous one. UNCONFIRMED

## Pointer

- [x] Clicking a radio or its label chooses it. `RadioGroupExampleTests.RadioGroupExample_ShouldShowTheChosenValue_WhenAnOptionIsChosen`
- [x] All of an option's content, such as a badge, is part of its click target and name. `RadioGroupTests.RadioGroup_ShouldNameAnOptionByAllItsContent_WhenItHoldsABadge`
- [x] A disabled option ignores clicks while the others stay usable. `TbRadioGroupTests.Render_ShouldDisableOneOption_WhenTheOptionIsDisabled`
- [x] A disabled group turns off every radio. `TbRadioGroupTests.Render_ShouldDisableTheWholeGroup_WhenDisabledIsSet`
- [x] Clicking another option in a read-only group changes nothing. `ReadOnlyTests.ReadOnlyRadioGroup_ShouldKeepItsChoice_WhenTheUserTriesAnother`

## Touch

- [x] A tap on a radio or its label chooses it, since each is a native radio inside its `<label>`. `TbRadio.razor`

## Focus

- [x] `FocusAsync` moves focus to the chosen radio, or else the first enabled one. `FieldFocusTests.FocusAsync_ShouldMoveFocusToTheFieldsInput_WhenCalled`
- [x] A failed submit moves focus the same way when the group holds the first error. `FormTests.Form_ShouldFocusTheGroupsFirstRadio_WhenASubmitFailsOnARadioGroupFirst`
- [x] `AutoFocus` marks the group, and focus goes to the chosen radio, or else the first one. `FieldFocusAndNameTests.Render_ShouldMarkTheGroupAndNotEachItem_WhenARadioGroupHasAutoFocus`
- [x] A read-only group's chosen radio takes focus. `ReadOnlyTests.ReadOnlyRadioGroup_ShouldKeepItsChoice_WhenTheUserTriesAnother`
- [x] Each radio shows a focus ring for keyboard focus. `trailblazor.css`
- [x] A click on another option's label doesn't raise `OnFocus` or `OnBlur`, in a dialog or tab panel too. `GroupFocusEventsTests.RadioGroup_ShouldRaiseNoBlurOrFocus_WhenAnotherOptionsLabelIsClicked`

## Not applicable

- Focus on a read-only group with nothing chosen: there is nothing to read or copy, as its page says.
- Clearing a choice by clicking it again: native radios don't, and the pattern doesn't ask for it. A null option can stand for no choice.
- Home and End: the pattern has no use for them in a radio group.
