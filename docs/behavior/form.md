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
- [x] A field bound to a `[Required]` text or nullable property is marked required, but not one bound to a list or a value type that can't be null. A file upload counts as nullable, since removing its last file leaves null. An empty required field shows one message whether its `Required`, the model's `[Required]` or both apply, even with a rule like `[EmailAddress]` that rejects empty text. `FieldRequiredTests.TextField_ShouldMarkItselfRequired_WhenTheBoundPropertyHasRequired`, `FieldRequiredTests.TextField_ShouldShowOneMessage_WhenItsRequiredAndTheModelsRequiredBothApply`, `FieldRequiredTests.TextField_ShouldShowOnlyTheRequiredMessage_WhenClearedWithAnEmailRuleOnTheModel`
- [x] Clicking a `Loading` submit button doesn't submit again. `ButtonTests.Button_ShouldNotSubmitTheForm_WhenLoadingOrDisabledButFocusable`
- [x] A button with `Type="ButtonType.Reset"` resets the fields and their bound values together and hides their messages. `FormTests.Form_ShouldPutFieldsAndTheirValuesBack_WhenItsResetButtonIsPressed`, `TbFormTests.Reset_ShouldPutTheValueBackAndHideTheMessages_WhenTheFormResets`
- [x] A reset goes back to the last value the parent set, even one set in answer to an edit. `TbFormTests.Reset_ShouldGoBackToTheParentsLatestValue_WhenTheParentSetOneAfterTheFirstRender`, `TbFormTests.Reset_ShouldGoBackToAValueTheParentRewrote_WhenItAnsweredAnEdit`
- [x] A field reset to a value that would fail shows no message, from its own checks or from a validator that wasn't showing one, even beside a parent's `Error`. `TbFormTests.Reset_ShouldShowNoMessage_WhenTheFirstValueWouldFail`, `TbFormTests.Reset_ShouldNotAddAValidatorsMessage_WhenItWasntShowingOne`, `TbFormTests.Reset_ShouldNotAddAValidatorsMessage_WhenOnlyTheParentsErrorIsShowing`
- [x] A reset that changes a field's value lets a validator inside the form check again, so its message clears when the value is good. `TbFormTests.Reset_ShouldLetAValidatorCheckAgain_WhenTheValueChanged`
- [x] A reset that removes a later field doesn't fail. `TbFormTests.Reset_ShouldNotFail_WhenAnEarlierFieldsResetRemovesAnAutocomplete`
- [x] A reset reads out nothing. `TbFormTests.Reset_ShouldReadOutNothing_WhenTheValueItGoesBackToWouldFail`
- [x] A reset drops text a field is still holding: an autocomplete's unreadable text and a text field's input waiting out `UpdateDelay`. `TbFormTests.Reset_ShouldClearAnAutocompletesUnreadableText_WhenItsValueStaysTheSame`, `TbTextFieldTests.Reset_ShouldDropTextWaitingOutTheDelay_WhenTheFormResets`
- [x] A reset clears a field's unreadable text and the message about it, from code too. `FormTests.Form_ShouldClearUnreadableText_WhenItsResetButtonIsPressed`, `TbFormTests.Reset_ShouldClearUnreadableDates_WhenCalledFromCode`
- [x] A reset empties a file upload's picker and its rejections. `TbFormTests.Reset_ShouldEmptyAFileUploadsPickerAndItsRejections_WhenCalled`
- [x] A reset marks the edit context unmodified. `TbFormTests.Reset_ShouldMarkTheFormUnmodified_WhenAFieldHadChanged`
- [x] On a static page, the browser's own reset puts the inputs back. `FormTests.Form_ShouldLeaveTheResetToTheBrowser_WhenTheFormIsOnAStaticPage`

## Touch

Nothing of its own: taps reach the form as clicks on its fields and buttons.

## Focus

- [x] A failed submit moves focus to the first field with an error, once its message shows. `FormTests.Form_ShouldShowEveryErrorAndFocusTheFirst_WhenASubmitFails`
- [x] When that field is a radio group, focus goes to its checked or first enabled radio. `FormTests.Form_ShouldFocusTheGroupsFirstRadio_WhenASubmitFailsOnARadioGroupFirst`
- [x] When that field is a checkbox group, focus goes to its first enabled box. `form.js`
- [x] A field only marked `Invalid`, with no message to fix, is passed over. `FormTests.Form_ShouldFocusTheFieldWithAnError_WhenAFieldAboveItIsOnlyMarkedInvalid`
- [x] With `OnSubmit`, focus is left for the handler, for example to move to an error summary. `FormTests.FormExample_ShouldMoveFocusToTheErrorSummary_WhenSubmittedWithErrors`
- [x] A successful submit leaves focus where it was. `TbForm.razor`
- [x] A reset leaves focus on its button when pressed from the keyboard. `FormTests.Form_ShouldPutFieldsAndTheirValuesBack_WhenItsResetButtonIsPressed`

## Not applicable

- Blocking a second submit while the handler runs: the app sets `Loading` on the submit button, which covers clicks and Enter.
- Checking a field as focus leaves it: the fields check on change and on submit, and an app can use a field's `OnBlur`.
- Moving focus after a static form post: there is no script, and the page reloads.
- Warning about unsaved changes on leaving the page: that belongs to the app, which can read the edit context.
