# Mention input

Pattern: [Combobox](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/), on a text field that keeps focus

Familiar apps: mentioning someone with @ in GitHub, Slack or Google Docs comments

## Keyboard

- [x] A trigger at the start of a word opens suggestions under it as the text after it is typed; a trigger inside a word doesn't. `MentionInputTests.MentionInput_ShouldSuggestUnderTheTrigger_WhenItStartsAWord`
- [x] Down and Up move through the suggestions and wrap, with `aria-activedescendant` on the field. `MentionInputTests.MentionInput_ShouldChooseWithTheKeys_WhenSuggestionsShow`
- [x] Enter or Tab puts the active suggestion in, with a space after it. `MentionInputTests.MentionInput_ShouldChooseWithTheKeys_WhenSuggestionsShow`
- [x] Moving the caret out of the mention closes the suggestions. `MentionInputTests.MentionInput_ShouldStopSuggesting_WhenTheCaretLeavesTheMention`
- [x] Escape closes the suggestions until the text changes. `MentionInputTests.MentionInput_ShouldStayClosedUntilTheTextChanges_WhenEscapeIsPressed`
- [x] A choice from suggestions that newer typing replaced isn't counted. `TbMentionInputTests.OnChosen_ShouldNotCountTheChoice_WhenItComesFromSuggestionsALaterQueryReplaced`
- [x] Enter or Tab pressed while suggestions show and newer ones are on their way puts the first of the newer ones in once they arrive. `MentionInputTests.MentionInput_ShouldChooseTheFirstOfTheLatestSuggestions_WhenEnterOrTabIsPressedBeforeTheyArrive`
- [x] That waiting choice is dropped when the newer suggestions come back empty, when more is typed before they arrive, or when the focus leaves the field. `MentionInputTests.MentionInput_ShouldDropTheWaitingChoice_WhenNothingMatchesMoreIsTypedOrTheFocusLeaves`
- [x] Undo takes a mention out again. `MentionInputTests.MentionInput_ShouldTakeTheMentionOut_WhenUndone`

## Pointer

- [x] A click on a suggestion puts it in, and focus stays in the field. `MentionInputTests.Helper_ShouldTypeAndChooseAMention_WhenFoundByLabel`
- [x] Moving the pointer over a suggestion makes it the active one, and a pointer that rests over the list leaves the active suggestion to the keys when the list scrolls under it: a move that the browser sends for the scroll, with the pointer where it was, is left alone, such as the one WebKit sends a moment after a scroll. `RestingPointerTests.MentionInput_ShouldLeaveTheActiveOptionToTheKeys_WhenItsListScrollsUnderARestingPointer`
- [x] The pointer's place is kept for each window and each kind of pointer, so a pointer that rests over the list is still known to rest after a move elsewhere: by a finger while the mouse rests, or in another of the app's windows where each window has a pointer of its own, as in a test browser. The lists share the script that keeps it. `resting-pointer.js`, `RestingPointerTests.Autocomplete_ShouldLeaveTheActiveOptionToTheKeys_WhenAnotherPointerMovedInAnotherWindowSince`

## Touch

- [x] Tapping a suggestion does what clicking does, and nothing needs hover. `TbMentionInput.razor`

## Focus

- [x] With `Lines="1"`, the input is a combobox whose `aria-expanded` follows the list; a text area stays a text box. `TbMentionInputTests.Render_ShouldMakeAOneLineFieldACombobox_WhenLinesIsOne`
- [x] A search that fails is logged as a warning with its exception, and counted in `trailblazor.control.failed` when the field has a `TelemetryName`. `LoadFailureTests.MentionInput_ShouldLogAWarningAndCountTheFailure_WhenItsSearchThrows`
- [x] When the search fails, the list closes, screen readers hear "Couldn't load suggestions.", and the app hears the exception. `TbMentionInputTests.TypeAsync_ShouldCloseTheListAndReportTheFailure_WhenTheSearchFails`
- [x] The field is named by its label, and so is the list of suggestions. `TbMentionInputTests.Render_ShouldBeALabeledFieldThatControlsAListOfSuggestions_WhenCalled`
- [x] Each suggestion is named by its text and described by its description. `TbMentionInputTests.TypeAsync_ShouldSuggestTheItemsMatchingTheTextAfterTheTrigger_WhenATriggerStartsAWord`
- [x] The app hears each mention's item while its text stays, and loses it when the text goes. `TbMentionInputTests.ChooseAsync_ShouldPutTheMentionInAndReportIt_WhenASuggestionIsChosen`, `TbMentionInputTests.TypeAsync_ShouldDropAMention_WhenItsTextIsDeleted`

## Not applicable

- Telling a browser's own move from the user's before the pointer has really moved in the window: the library learns the pointer's place from its real moves, so the first move a kind of pointer makes in a window counts as the user's, whoever sent it. The move's own `movementX` and `movementY` don't help: the test browsers report zero for real moves too.
- Mentions in formatted text: the rich text editor suggests and keeps them itself, with `MentionTriggers`.
