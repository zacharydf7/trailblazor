# Text field

Pattern: none in the Authoring Practices; a native `<input>` or `<textarea>` with its own browser behavior

Familiar apps: any web form, Windows and macOS text boxes, the search box in a mail app

## Keyboard

- [x] Enter in a single-line field commits the text and submits its form. `FormTests.Form_ShouldSubmit_WhenEnterIsPressedInASingleLineField`
- [x] Enter in a single-line field raises `OnEnter` after the value updates, and only for an Enter pressed in the field. `TextFieldTests.OnEnter_ShouldSeeTheTypedValue_WhenEnterIsPressedBeforeTheFieldLosesFocus`, `TextFieldTests.OnEnter_ShouldStaySilent_WhenEnterWasPressedOutsideTheField`
- [x] Enter in a text area adds a line. `TbTextField.razor`
- [x] Tab out of the field commits the text. `TextFieldTests.TextField_ShouldUpdateAtOnce_WhenEnterOrLeavingTheFieldEndsTheDelay`
- [x] With `UpdateDelay`, quick typing sends the server only the text typed before the pause. `TextFieldTests.TextField_ShouldSendOnlyTheTextTypedBeforeAPause_WhenGivenAnUpdateDelay`
- [x] Keystrokes aren't lost while updates are slow to come back. `TextFieldTests.TextField_ShouldKeepEveryKeystroke_WhenEachUpdateIsSlowToComeBack`
- [x] Tab reaches the clear button, and Enter or Space presses it. `TbTextField.razor`
- [x] Tab reaches the show-password button, and Enter or Space shows or hides the password. `TbTextField.razor`
- [x] In a mask, typing or deleting mid-text keeps the caret in place. `TextFieldTests.TextField_ShouldKeepTheCaretInPlace_WhenADigitIsTypedOrDeletedMidMask`
- [x] In a mask, Backspace just after a literal removes the character before it. `text-mask.js`
- [x] In a mask, Enter that ends an input method's composition commits nothing; the next Enter commits. `TextFieldTests.TextField_ShouldCommitOnlyAfterTheComposition_WhenEnterEndsOneInAMaskedField`
- [x] In a mask, leaving the field commits the text the mask rewrote, even after Enter committed earlier in the same focus, so a short number pasted after Enter still says it's short. If the field already had focus when the mask attached, leaving it unchanged commits nothing. `TextFieldTests.TextField_ShouldCommitTheTextTheMaskRewrote_WhenEnterCommittedEarlierInTheSameFocus`, `TextFieldTests.Mask_ShouldCommitNothing_WhenItAttachesToAFocusedFieldThatIsLeftUnchanged`
- [x] In a read-only field, the keys move the caret but change nothing. `ReadOnlyTests.ReadOnlyField_ShouldKeepItsValueButTakeFocus_WhenTheUserTriesToEditIt`
- [ ] In Safari, Enter in a masked field commits the fitted text before `OnEnter` runs or the form submits; `TextFieldTests.OnEnter_ShouldSeeTheFittedText_WhenEnterIsPressedInAMaskedField` checks it once WebKit runs it. UNCONFIRMED

## Pointer

- [x] Clicking the label focuses the input. `TbTextFieldTests.Render_ShouldLinkTheLabelAndHelperToTheInput_WhenBothAreSet`
- [x] Clicking anywhere in the box outside its controls, such as its padding or a start icon, focuses the input, also when something around the field can take the focus itself, such as a tab's or a dock's panel. `FieldBoxTests.Box_ShouldFocusItsInput_WhenItsPaddingIsPressed`, `FieldBoxTests.Box_ShouldFocusItsInput_WhenSomethingAroundTheFieldCanTakeTheFocusToo`, `FieldBoxTests.Box_ShouldFocusItsInput_WhenItsStartIconIsPressed`
- [x] The clear button empties the field and puts focus back in the input. `TextFieldTests.TextField_ShouldEmptyAndKeepFocus_WhenTheClearButtonIsPressed`
- [x] Typing and pasting stop at `MaxLength`, and the counter follows each keystroke with no round trip, even while `UpdateDelay` holds the text back. `TextFieldTests.TextField_ShouldCountEachKeystrokeAndStopAtTheLimit_WhenItShowsACount`
- [x] As the text nears the limit, the field announces the characters left at 20 and 0, once each. `TextFieldTests.TextField_ShouldAnnounceTheCharactersLeft_WhenTheTextReachesAThreshold`
- [x] The counter follows the server's text after typing, such as when the clear button empties the field, a commit writes the value back or the app sets new text. `TextFieldTests.TextField_ShouldCountTheServersText_WhenTheServerChangesItAfterTyping`, `TextFieldTests.TextField_ShouldCountTheServersText_WhenACommitKeepsTheValue`, `TextFieldTests.TextField_ShouldCountTheServersText_WhenTheAppSetsTextOfTheSameLengthWhileTyping`
- [x] A longer value set from code shows an error and blocks the form. `TbTextFieldTests.Submit_ShouldBlockTheFormAndExplain_WhenAValueSetFromCodeIsLongerThanMaxLength`
- [x] The clear button shows only for text that can change. `TbTextFieldTests.Render_ShouldShowTheClearButtonOnlyForTextThatCanChange_WhenClearable`
- [x] The show-password button shows and hides the password, leaving focus and the selection in the input. `TextFieldTests.TextField_ShouldKeepFocusAndTheSelection_WhenTheShowPasswordButtonIsPressed`
- [x] A read-only or disabled field hides its password and disables the show-password button. `TbTextFieldTests.Render_ShouldHideThePasswordAndTurnTheButtonOff_WhenTheFieldCantChange`
- [x] On a static page, the show-password button shows and hides the password and updates its pressed state and icon. `TextFieldTests.TextFieldLocator_ShouldShowAndHideThePassword_WhenThePageIsStatic`, `TbTextFieldTests.Render_ShouldAddAShowPasswordButton_WhenRevealableOnAStaticPage`
- [x] A field shows Edge's own show-password button only when it has none of its own and nothing else in the `End` slot. `trailblazor.css`
- [x] Pasted text fits the mask, and a short paste says so. `TextFieldTests.TextField_ShouldFitPastedTextAndFlagAShortOne_WhenItHasAMask`
- [x] Text that doesn't parse stays in the input with its message, so it can be fixed. `TextFieldTests.TextField_ShouldKeepTheTextAndExplain_WhenTheTextDoesNotParse`

## Touch

- [ ] A tap on the box's padding focuses the input. UNCONFIRMED
- [x] A tap focuses the native input and opens the on-screen keyboard, which `Type` chooses. `TbTextField.razor`
- [ ] A tap on the show-password button keeps the on-screen keyboard open. UNCONFIRMED
- [x] A mask of digits asks for the number keyboard. `TextFieldMaskTests.Render_ShouldShowTheValueInTheMaskAndHintItsShape_WhenGivenAMask`

## Focus

- [x] The whole box shows the focus ring while focus is inside it. `trailblazor.css`
- [x] A disabled field can't take focus. `TbTextField.razor`
- [x] A read-only field takes focus, and its text can be selected. `ReadOnlyTests.ReadOnlyField_ShouldKeepItsValueButTakeFocus_WhenTheUserTriesToEditIt`
- [x] `FocusAsync` moves focus to the input. `FieldFocusTests.FocusAsync_ShouldMoveFocusToTheFieldsInput_WhenCalled`
- [x] `SelectTextAsync` moves focus and selects all the text. `FieldFocusTests.SelectTextAsync_ShouldFocusTheFieldAndSelectAllItsText_WhenCalled`
- [x] `AutoFocus` takes focus as the field appears, unless the user is typing in another field. `FieldFocusTests.AutoFocus_ShouldLeaveFocusInTheFieldTheUserIsTypingIn_WhenAnotherFieldAppears`
- [x] `AutoFocus` takes focus as a static page loads, with no circuit. `FieldFocusTests.AutoFocus_ShouldFocusTheField_WhenThePageIsStatic`, `FieldFocusTests.AutoFocus_ShouldFocusTheField_WhenEnhancedNavigationReachesAStaticPage`

## Not applicable

- Escape: a plain text box has no action for it. A browser's search box may clear on it, and `Clearable` adds a button.
- Ctrl+Enter to submit from a text area: a chat convention, which an app can add.
