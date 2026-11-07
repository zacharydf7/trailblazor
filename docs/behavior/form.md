# Form

Pattern: [Form landmark](https://www.w3.org/WAI/ARIA/apg/patterns/landmarks/examples/form.html)

Familiar apps: sign-in and checkout forms on the web, the settings forms in cloud consoles

## Keyboard

- [x] Enter in a single-line field commits its text and submits the form. `FormTests.Form_ShouldSubmit_WhenEnterIsPressedInASingleLineField`
- [x] Enter in a text area adds a line instead of submitting. `TbTextField.razor`
- [x] Enter doesn't submit again while the submit button is `Loading`. `ButtonTests.Button_ShouldNotSubmitTheForm_WhenLoadingOrDisabledButFocusable`
- [x] Pressing a Trailblazor button that isn't a submit button doesn't submit, since buttons default to `type="button"`. `TbButtonBase.cs`

## Pointer

- [x] Clicking submit on a form with problems shows every field's message. `FormTests.Form_ShouldShowEveryErrorAndFocusTheFirst_WhenASubmitFails`
- [x] The submit button stays on while the form is invalid, so a click can show what to fix. `FormExampleTests.FormExample_ShouldLeaveSignUpOnAndCountWhatToFix_WhenSubmittedEmpty`
- [x] A form nobody has touched shows no messages. `TbFormTests.Render_ShouldReportInvalidWithoutShowingMessages_WhenAFieldWouldFail`
- [x] A field fixed after a failed submit lets the next submit through. `TbTextFieldTests.Submit_ShouldGoThrough_WhenTheFieldIsFixedAfterAFailedSubmit`
- [x] An empty required field that is disabled or read-only doesn't block a submit. `TbFormTests.Submit_ShouldSubmit_WhenAnEmptyRequiredFieldIsDisabledOrReadOnly`
- [x] Clicking a `Loading` submit button doesn't submit again. `ButtonTests.Button_ShouldNotSubmitTheForm_WhenLoadingOrDisabledButFocusable`
- [ ] A button with `Type="ButtonType.Reset"` resets the fields and their bound values together and hides their messages. #314

## Touch

Nothing of its own: taps reach the form as clicks on its fields and buttons.

## Focus

- [x] A failed submit moves focus to the first field with an error, once its message shows. `FormTests.Form_ShouldShowEveryErrorAndFocusTheFirst_WhenASubmitFails`
- [x] When that field is a radio group, focus goes to its checked or first enabled radio. `FormTests.Form_ShouldFocusTheGroupsFirstRadio_WhenASubmitFailsOnARadioGroupFirst`
- [x] When that field is a checkbox group, focus goes to its first enabled box. `form.js`
- [x] A field only marked `Invalid`, with no message to fix, is passed over. `FormTests.Form_ShouldFocusTheFieldWithAnError_WhenAFieldAboveItIsOnlyMarkedInvalid`
- [x] With `OnSubmit`, focus is left for the handler, for example to move to an error summary. `FormTests.FormExample_ShouldMoveFocusToTheErrorSummary_WhenSubmittedWithErrors`
- [x] A successful submit leaves focus where it was. `TbForm.razor`

## Not applicable

- Blocking a second submit while the handler runs: the app sets `Loading` on the submit button, which covers clicks and Enter.
- Checking a field as focus leaves it: the fields check on change and on submit, and an app can use a field's `OnBlur`.
- Moving focus after a static form post: there is no script, and the page reloads.
- Warning about unsaved changes on leaving the page: that belongs to the app, which can read the edit context.
