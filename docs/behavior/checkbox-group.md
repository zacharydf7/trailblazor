# Checkbox group

Pattern: [Checkbox](https://www.w3.org/WAI/ARIA/apg/patterns/checkbox/)

Familiar apps: the filter lists on shopping sites, notification settings pages

## Keyboard

- [x] Tab stops on each enabled box in turn, since each is a native checkbox. `TbCheckboxItem.razor`
- [x] Space toggles the focused box and updates the value in the order boxes were checked. `CheckboxGroupTests.CheckboxGroup_ShouldBindTheCheckedOptionsInTheOrderChecked_WhenClickedOrToggledWithSpace`
- [x] Enter doesn't toggle a box, and submits the form as a native checkbox does. `TbCheckboxItem.razor`
- [x] Space on a box in a read-only group changes nothing. `read-only.js`

## Pointer

- [x] Clicking a box or its label toggles it. `CheckboxGroupTests.CheckboxGroup_ShouldBindTheCheckedOptionsInTheOrderChecked_WhenClickedOrToggledWithSpace`
- [x] A disabled option ignores clicks while the others stay usable. `TbCheckboxGroupTests.Render_ShouldDisableTheGroupOrOneOption_WhenEitherIsDisabled`
- [x] A disabled group turns off every box. `TbCheckboxGroupTests.Render_ShouldDisableTheGroupOrOneOption_WhenEitherIsDisabled`
- [x] A click on a box in a read-only group changes nothing. `read-only.js`
- [x] Clicking a link in an option's label opens the link without toggling, since the browser sends the click to the link. `TbCheckboxItem.razor`
- [x] Unchecking the last box in a required group shows its error at once. `CheckboxGroupTests.CheckboxGroup_ShouldShowItsErrorAndMarkTheGroupInvalid_WhenTheLastOptionIsUnchecked`

## Touch

- [x] A tap on a box or its label toggles it, since each is a native checkbox inside its `<label>`. `TbCheckboxItem.razor`

## Focus

- [x] `FocusAsync` moves focus to the first enabled box. `TbCheckboxGroupTests.FocusAsync_ShouldAskTheScriptToFocusACheckboxInTheGroup_WhenCalled`
- [x] A failed submit moves focus to the first enabled box when the group holds the first error. `form.js`
- [x] `AutoFocus` sends focus to the first enabled box. `focus-scope.js`
- [x] Boxes in a read-only group stay focusable. `TbCheckboxGroupTests.Render_ShouldMarkEachCheckboxReadOnlyButNotDisabled_WhenReadOnly`
- [x] Tabbing between boxes, or pressing a box itself, doesn't raise `OnFocus` or `OnBlur`. `TbFieldBase.cs`
- [x] A click on another option's label doesn't raise them either, and a link in a label still takes focus. `GroupFocusEventsTests.CheckboxGroup_ShouldRaiseNoBlurOrFocus_WhenAnotherOptionsLabelIsClicked`, `GroupFocusEventsTests.CheckboxGroup_ShouldLetALinkInALabelTakeFocus_WhenItIsPressed`
- [x] Each box shows a focus ring for keyboard focus. `trailblazor.css`

## Not applicable

- Arrow keys and a single tab stop: each checkbox is its own control, unlike radios, and the pattern asks for no more.
- A built-in "select all": an app can add a `TbCheckbox` whose null value shows the mixed state.
