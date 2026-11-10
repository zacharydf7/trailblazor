# Tag input

Pattern: none; the WAI-ARIA Authoring Practices have no tag input, so it is a labeled text box beside a list of tags
with native remove buttons

Familiar apps: the topics field of a GitHub repository, the recipient field of an email app, a blog editor's tags

## Keyboard

- [x] Enter adds what was typed, trimmed, as a tag. `TagInputTests.TagInput_ShouldAddTheTypedText_WhenEnterOrACommaIsPressed`
- [x] A separator, a comma by default, adds what was typed too, and isn't typed itself. `TagInputTests.TagInput_ShouldAddTheTypedText_WhenEnterOrACommaIsPressed`, `TagInputTests.TagInput_ShouldAddTheTypedText_WhenOneOfItsOwnSeparatorsIsTyped`
- [x] Enter in the empty input submits the form. `TagInputTests.TagInput_ShouldSubmitTheForm_WhenEnterIsPressedInTheEmptyInput`
- [x] Backspace in the empty input removes the last tag. `TagInputTests.TagInput_ShouldRemoveTheLastTag_WhenBackspaceIsPressedInTheEmptyInput`
- [x] Left at the start of the input moves to the last tag's remove button, Left and Right move along the tags, and Right from the last returns to the input. `TagInputTests.TagInput_ShouldMoveAlongTheTagsAndRemoveThem_WhenArrowAndDeleteKeysArePressed`
- [x] On a tag, Home moves to the first tag and End back to the input. `TagInputTests.TagInput_ShouldMoveAlongTheTagsAndRemoveThem_WhenArrowAndDeleteKeysArePressed`
- [x] On a tag, Delete removes it and moves to the next, and Backspace removes it and moves to the one before. `TagInputTests.TagInput_ShouldMoveAlongTheTagsAndRemoveThem_WhenArrowAndDeleteKeysArePressed`, `TagInputTests.TagInput_ShouldRemoveTheTagAndMoveBack_WhenBackspaceIsPressedOnATag`, `TbTagInputTests.RemoveEvent_ShouldFocusTheTagThatTakesItsPlace_WhenATagIsRemovedByKey`
- [x] A key pressed before a removal has rendered leaves focus where the key put it, even on another tag, instead of the removal pulling it back. The removal moves focus only when its render takes away the button that still had focus, which is the removed tag's own. `TagInputLateRenderTests.TagInput_ShouldKeepFocusWhereAKeyMovedIt_WhenTheRemovalRendersAfterTheKey`, `TagInputLateRenderTests.TagInput_ShouldLeaveFocusOutOfTheField_WhenAKeyMovedItOutBeforeTheRender`, `TagInputLateRenderTests.TagInput_ShouldFocusTheNextTag_WhenAKeyLeftFocusOnTheRemovedTagsButton`, `TagInputLateRenderTests.TagInput_ShouldFocusTheTagAfterTheRemovedOne_WhenAKeyMovedToATagBeforeTheRender`, `TagInputLateRenderTests.TagInput_ShouldKeepFocusOnTheTagBefore_WhenAKeyMovedBackToItBeforeTheRender`, `TagInputLateRenderTests.TagInput_ShouldKeepFocusOnTheFirstTag_WhenHomeIsPressedAfterRemovingAMiddleOne`, `TagInputLateRenderTests.TagInput_ShouldKeepFocusOnTheLastTag_WhenTwoArrowsReachItBeforeTheRender`
- [x] Enter or Space on a tag presses its remove button, which removes it and moves to the next tag. `TbTagInputTests.RemoveAsync_ShouldMoveOnToTheNextTag_WhenTheButtonIsPressedFromTheKeyboard`
- [x] On a right-to-left page, Right moves from the input to the last tag. `TagInputTests.TagInput_ShouldMoveToTheLastTagWithRight_WhenThePageIsRightToLeft`
- [x] The remove buttons aren't tab stops, so the field is one stop in the tab order. `TbTagInputTests.Render_ShouldListEachTagWithANamedRemoveButtonOutOfTheTabOrder_WhenGivenTags`

## Pointer

- [x] Clicking a tag's remove button removes it and puts focus back in the input. `TagInputTests.TagInput_ShouldRemoveTheTagAndGoBackToTyping_WhenItsButtonIsClicked`
- [x] Clicking anywhere in the box, between the tags, focuses the input. `field-box.js`
- [x] Pasting text with separators or line breaks adds each part as a tag, and refused parts go back into the input, still separate, so adding them again checks each one on its own. If the field's separators are set to none, only the first refused part goes back. `TagInputTests.TagInput_ShouldAddEachPart_WhenAListIsPasted`, `TagInputTests.TagInput_ShouldCheckEachPutBackPartAgain_WhenTheyAreAddedAgain`, `TbTagInputTests.AddEvent_ShouldSplitTheTextAndPutBackTheRefusedParts_WhenItHoldsSeveralTags`
- [x] Leaving the field adds what was typed, but switching to another window doesn't. `TagInputTests.TagInput_ShouldAddTheTypedText_WhenFocusLeavesTheField`, `TagInputTests.TagInput_ShouldKeepTheTypedText_WhenTheWholeWindowLosesFocus`

## Touch

- [ ] Tapping a remove button removes its tag, and the on-screen keyboard's Enter or comma adds one. UNCONFIRMED

## Focus

- [x] The label names the input and the list of tags, and each remove button is named "Remove" and its tag. `TbTagInputTests.Render_ShouldListEachTagWithANamedRemoveButtonOutOfTheTabOrder_WhenGivenTags`
- [x] Screen readers hear how to add a tag and reach the tags as the input's description. `TbTagInputTests.Render_ShouldDescribeTheInputWithHowToAddAndReachTags_WhenEditable`
- [x] Adding and removing a tag are read out. `TbTagInputTests.AddAsync_ShouldAddTheTrimmedTextAndAnnounceIt_WhenTextIsEntered`, `TbTagInputTests.RemoveAsync_ShouldRemoveTheTagAnnounceItAndFocusTheInput_WhenItsButtonIsClicked`
- [x] A tag that's already there, past the limit or refused by the check stays in the input with its message, and the input is marked invalid. `TagInputTests.TagInput_ShouldKeepARefusedTagInTheInputWithItsMessage_WhenItIsAlreadyThere`, `TbTagInputTests.AddAsync_ShouldRefuseTheTag_WhenTheFieldHasMaxTags`, `TbTagInputTests.AddAsync_ShouldShowTheChecksMessage_WhenTagValidationRefusesTheTag`
- [x] A disabled or read-only field shows its tags without remove buttons. `TbTagInputTests.Render_ShouldOfferNoWayToChangeTheTags_WhenDisabledOrReadOnly`
- [x] A static form posts the tags. `TagInputTests.TagInput_ShouldPostItsTags_WhenUsedInAStaticForm`

## Not applicable

- Suggestions while typing: a tag input takes any text. To choose from a known list, use a multi-select with chips.
- Editing a tag in place: remove it and type it again, as most tag fields do.
- Reordering tags by dragging: tags keep the order they were added in.
