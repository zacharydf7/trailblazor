# Code field

Pattern: none in the Authoring Practices; a single text input like the [text field](text-field.md), not one per box, so the code has one name

Familiar apps: the sign-in code steps of banking apps, GitHub's two-factor prompt and the phone-number check in messaging apps

## Keyboard

- [x] Typing fills the boxes in turn, and a key outside the code's characters, such as a letter in a code of digits, does nothing, even with the caret mid-code. `CodeFieldTests.Field_ShouldTakeOnlyTheCodesCharacters_WhenTyped`
- [x] Backspace removes the character before the caret, whichever box it is in. `CodeFieldTests.Field_ShouldTakeOnlyTheCodesCharacters_WhenTyped`
- [x] Typing the last character raises `OnComplete`, and so does changing a full code to another or retyping its last character after deleting it, but not an input that leaves the full code unchanged. `TbCodeFieldTests.Input_ShouldKeepOnlyTheCodesCharactersAndRaiseOnComplete_WhenTyped`, `TbCodeFieldTests.Input_ShouldRaiseOnCompleteAgain_WhenAFullCodeChangesToAnother`, `TbCodeFieldTests.Input_ShouldRaiseOnCompleteAgain_WhenTheLastCharacterIsDeletedAndRetyped`
- [x] A code of letters and digits shows them in capitals. `TbCodeFieldTests.Input_ShouldTakeLettersInCapitals_WhenLettersAreAllowed`

## Pointer

- [x] A paste keeps only the code's characters, so "123-456" fills six digit boxes, and a read-only field takes none. `CodeFieldTests.Paste_ShouldKeepOnlyTheDigits_WhenTheCodeHasADash`, `TbCodeFieldTests.Input_ShouldLeaveTheValue_WhenTheFieldIsReadOnly`

## Touch

- [x] The input asks for a one-time code, so a phone offers one it received, and a code of digits brings up a number keyboard. `TbCodeFieldTests.Render_ShouldOfferOneInputForAOneTimeCode_WhenShown`
- [ ] A code offered from a text message fills the field on a real phone. UNCONFIRMED

## Focus

- [x] Clicking the label focuses the input. `TbCodeFieldTests.Render_ShouldOfferOneInputForAOneTimeCode_WhenShown`

## Screen readers

- [x] The whole code has one name, its label, and is read as one value. `TbCodeFieldTests.Render_ShouldOfferOneInputForAOneTimeCode_WhenShown`
- [x] A partly typed code is invalid and says how many characters it needs once the field commits, not while it is typed. `TbCodeFieldTests.Validate_ShouldAskForEveryCharacter_WhenAPartlyTypedCodeCommits`
- [x] A code that arrives without typing, such as from a static form post, is invalid when it's short or holds other characters. `TbCodeFieldTests.Form_ShouldBeInvalid_WhenTheCodeArrivesShortOrWithOtherCharacters`
- [x] Removing the field from a form takes its errors with it. `TbCodeFieldTests.Dispose_ShouldLeaveTheFormValid_WhenAFieldWithAnErrorIsRemoved`

## Not applicable

- A box per character, each its own input: screen readers would read six fields with one character each, and paste and autofill would need rebuilding by hand.
