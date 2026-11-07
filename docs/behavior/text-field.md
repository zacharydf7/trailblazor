# Text field

Pattern: none in the Authoring Practices; a native `<input>` or `<textarea>` with its own browser behavior

Familiar apps: any web form, Windows and macOS text boxes, the search box in a mail app

## Keyboard

- [x] Enter in a single-line field commits the text and submits its form. `FormTests.Form_ShouldSubmit_WhenEnterIsPressedInASingleLineField`
- [x] Enter in a text area adds a line. `TbTextField.razor`
- [x] Tab out of the field commits the text. `TextFieldTests.TextField_ShouldUpdateAtOnce_WhenEnterOrLeavingTheFieldEndsTheDelay`
- [x] Keystrokes aren't lost while updates are slow to come back. `TextFieldTests.TextField_ShouldKeepEveryKeystroke_WhenEachUpdateIsSlowToComeBack`
- [x] Tab reaches the clear button, and Enter or Space presses it. `TbTextField.razor`
- [x] In a mask, typing or deleting mid-text keeps the caret in place. `TextFieldTests.TextField_ShouldKeepTheCaretInPlace_WhenADigitIsTypedOrDeletedMidMask`
- [x] In a mask, Backspace just after a literal removes the character before it. `text-mask.js`
- [x] In a read-only field, the keys move the caret but change nothing. `ReadOnlyTests.ReadOnlyField_ShouldKeepItsValueButTakeFocus_WhenTheUserTriesToEditIt`
- [ ] In Safari, Enter in a masked field commits the fitted text before the form submits. UNCONFIRMED

## Pointer

- [x] Clicking the label focuses the input. `TbTextFieldTests.Render_ShouldLinkTheLabelAndHelperToTheInput_WhenBothAreSet`
- [ ] Clicking anywhere in the box, such as its padding or a start icon, focuses the input. #303
- [x] The clear button empties the field and puts focus back in the input. `TextFieldTests.TextField_ShouldEmptyAndKeepFocus_WhenTheClearButtonIsPressed`
- [x] The clear button shows only for text that can change. `TbTextFieldTests.Render_ShouldShowTheClearButtonOnlyForTextThatCanChange_WhenClearable`
- [x] Pasted text fits the mask, and a short paste says so. `TextFieldTests.TextField_ShouldFitPastedTextAndFlagAShortOne_WhenItHasAMask`
- [x] Text that doesn't parse stays in the input with its message, so it can be fixed. `TextFieldTests.TextField_ShouldKeepTheTextAndExplain_WhenTheTextDoesNotParse`

## Touch

- [x] A tap focuses the native input and opens the on-screen keyboard, which `Type` chooses. `TbTextField.razor`
- [x] A mask of digits asks for the number keyboard. `TextFieldMaskTests.Render_ShouldShowTheValueInTheMaskAndHintItsShape_WhenGivenAMask`

## Focus

- [x] The whole box shows the focus ring while focus is inside it. `trailblazor.css`
- [x] A disabled field can't take focus. `TbTextField.razor`
- [x] A read-only field takes focus, and its text can be selected. `ReadOnlyTests.ReadOnlyField_ShouldKeepItsValueButTakeFocus_WhenTheUserTriesToEditIt`
- [x] `FocusAsync` moves focus to the input. `FieldFocusTests.FocusAsync_ShouldMoveFocusToTheFieldsInput_WhenCalled`
- [x] `SelectTextAsync` moves focus and selects all the text. `FieldFocusTests.SelectTextAsync_ShouldFocusTheFieldAndSelectAllItsText_WhenCalled`
- [x] `AutoFocus` takes focus as the field appears, unless the user is typing in another field. `FieldFocusTests.AutoFocus_ShouldLeaveFocusInTheFieldTheUserIsTypingIn_WhenAnotherFieldAppears`

## Not applicable

- Escape: a plain text box has no action for it. A browser's search box may clear on it, and `Clearable` adds a button.
- A show-password button: browsers and password managers add their own, and an app can put one in the `End` slot.
- Ctrl+Enter to submit from a text area: a chat convention, which an app can add.
