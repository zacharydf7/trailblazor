# Checkbox

Pattern: [Checkbox](https://www.w3.org/WAI/ARIA/apg/patterns/checkbox/)

Familiar apps: the browser's own checkbox, "Accept the terms" boxes, select-all boxes in mail apps

## Keyboard

- [x] Space toggles it. `CheckboxTests.Checkbox_ShouldToggle_WhenSpaceIsPressedOrTheLabelIsClicked`
- [x] Enter doesn't toggle it, and submits its form as a native checkbox does. `TbCheckbox.razor`
- [x] Space on a read-only checkbox changes nothing. `ReadOnlyTests.ReadOnlyCheckbox_ShouldStayChecked_WhenClickedOrToggledWithSpace`
- [x] Fast presses leave it showing the value the server holds. `CheckboxTests.Controls_ShouldShowTheStateTheServerHolds_WhenEachUpdateIsSlowToComeBack`

## Pointer

- [x] Clicking the box or its label toggles it. `CheckboxTests.Checkbox_ShouldToggle_WhenSpaceIsPressedOrTheLabelIsClicked`
- [x] Clicking a mixed checkbox checks it. `CheckboxTests.Checkbox_ShouldBeMixedThenChecked_WhenANullValueIsClicked`
- [x] A mixed checkbox shows mixed again when the parent keeps the value null. `TbCheckboxTests.Change_ShouldShowMixedAgain_WhenTheParentKeepsTheValueNull`
- [x] Clicking a link in a rich label opens the link without toggling. `CheckboxGroupTests.Checkbox_ShouldOpenTheLinkWithoutChecking_WhenALinkInItsRichLabelIsClicked`
- [x] Clicking the helper text or error message doesn't toggle, since they sit outside the `<label>`. `TbCheckbox.razor`
- [x] A click on a read-only checkbox changes nothing. `ReadOnlyTests.ReadOnlyCheckbox_ShouldStayChecked_WhenClickedOrToggledWithSpace`
- [x] A disabled checkbox ignores clicks. `CheckboxExampleTests.StatesExample_ShouldNameTheReadOnlyBoxSoItPostsAndDisableTheOther_WhenRendered`

## Touch

- [x] A tap on the box or label toggles it, since it is a native checkbox inside its `<label>`, at least 24px tall. `TbCheckbox.razor`

## Focus

- [x] It shows a focus ring for keyboard focus. `trailblazor.css`
- [x] A read-only checkbox keeps focus when pressed. `ReadOnlyTests.ReadOnlyCheckbox_ShouldStayChecked_WhenClickedOrToggledWithSpace`
- [x] A disabled checkbox can't take focus. `TbCheckbox.razor`
- [x] `FocusAsync` moves focus to the box. `FieldFocusTests.FocusAsync_ShouldMoveFocusToTheFieldsInput_WhenCalled`
- [x] A checkbox that reveals a field with `AutoFocus` gives focus up to that field. `FieldFocusTests.AutoFocus_ShouldFocusTheField_WhenACheckboxRevealsIt`

## Not applicable

- Cycling back to mixed by clicking: mixed only comes from a null value, which the app sets, usually from the state of a group.
- Arrow keys: a checkbox is its own tab stop, so only a radio group uses them.
