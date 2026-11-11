# Autocomplete

Pattern: [Editable combobox with list autocomplete](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/examples/combobox-autocomplete-list/)

Familiar apps: a browser's address bar, the search boxes in Google and GitHub

## Keyboard

- [x] Typing opens the list and searches. `AutocompleteTests.Autocomplete_ShouldSearchAndChooseWithTheKeys_WhenTheUserTypes`
- [x] With a `Search`, quick typing searches once, for the text typed before the pause, since the browser waits out `DebounceDelay`. `AutocompleteTests.Autocomplete_ShouldSearchOnceForTheTextTypedBeforeAPause_WhenGivenASearch`
- [x] Enter during that wait still commits everything typed before it, and the list stays closed. `AutocompleteTests.Autocomplete_ShouldTakeTheTextTypedJustBefore_WhenEnterIsPressedDuringTheWait`
- [x] Escape during that wait still searches the typed text, and the list stays closed. `AutocompleteTests.Autocomplete_ShouldStayClosedAndSearchTheTypedText_WhenEscapeIsPressedDuringTheWait`
- [x] Choosing a suggestion during that wait drops the waiting text instead of searching it. `AutocompleteTests.Autocomplete_ShouldChooseWithoutSearchingTheWaitingText_WhenASuggestionIsChosenDuringTheWait`
- [x] Down and Up move through an open list, and wrap at its ends. `autocomplete.js`
- [x] Alt+Down opens the list without moving to an option. `autocomplete.js`
- [x] Enter chooses the active option, closes the list and stays in the field. `AutocompleteTests.Autocomplete_ShouldSearchAndChooseWithTheKeys_WhenTheUserTypes`
- [x] Enter with no active option closes the list and settles the typed text: a matching option is chosen, `FreeText` keeps it, and anything else goes back to the chosen value. `AutocompleteTests.Autocomplete_ShouldTakeTypedTextAsTheValue_WhenFreeTextIsEnteredOrLeft`
- [x] Escape closes an open list and keeps the text. `AutocompleteTests.Autocomplete_ShouldCloseAndPutBackTheChosenText_WhenEscapeIsPressedAndTheFieldIsLeft`
- [x] Escape on a closed list is left for what surrounds it, such as a dialog. `autocomplete.js`
- [x] Arrows pass over the status message. `AutocompleteTests.Listbox_ShouldStayOpenWithNothingActive_WhenItsMessageIsClickedOrArrowedTo`
- [x] Tab closes the list and settles the typed text the same way. `AutocompleteTests.Autocomplete_ShouldTakeTypedTextAsTheValue_WhenFreeTextIsEnteredOrLeft`
- [x] Enter and Escape go to an input method while it composes text, not to the list. `AutocompleteTests.Autocomplete_ShouldLeaveTheKeyToTheIme_WhenItIsComposing`

## Pointer

- [x] With a `Comparer`, such as one matching entities by id, a value loaded again still marks its suggestion as chosen, and choosing that suggestion again leaves the form clean. A value loaded again with new text puts that text in the field. `ComparerTests.Autocomplete_ShouldMarkTheReloadedValuesSuggestion_WhenGivenAComparer`, `ComparerTests.Autocomplete_ShouldLeaveTheFormClean_WhenTheReloadedValuesSuggestionIsChosenAgain`, `ComparerTests.Autocomplete_ShouldShowTheNewText_WhenTheParentPassesTheValueAgainRenamed`
- [x] Clicking anywhere in the box outside its controls, such as its padding, focuses the input. `FieldBoxTests.Box_ShouldFocusItsInput_WhenItsPaddingIsPressed`
- [x] A press on the box's padding leaves an open list open. `FieldBoxTests.Box_ShouldLeaveAnOpenListOpen_WhenItsPaddingIsPressed`
- [x] A press on a control inside the box closes an open list when focus was in the input. A list opened from code, with focus elsewhere, stays open until a click outside the box or Escape. `FieldBoxTests.Box_ShouldCloseAnOpenList_WhenItsClearButtonIsPressedWithFocusInTheInput`
- [x] Clicking a suggestion chooses it and keeps focus in the field. `AutocompleteTests.Autocomplete_ShouldKeepFocusInTheInput_WhenAnOptionIsClicked`
- [x] Hovering a suggestion makes it the active one. `autocomplete.js`
- [x] Clicking the status message does nothing and leaves the list open. `AutocompleteTests.Listbox_ShouldStayOpenWithNothingActive_WhenItsMessageIsClickedOrArrowedTo`
- [x] Clicking elsewhere closes the list once the click's own handlers have run. A right-click, a middle-click or a press on a disabled control elsewhere fires no click event, so the list closes when focus leaves the field instead, and a press that turns into a drag or a scroll closes it as soon as the press is canceled, if focus has left. `autocomplete.js`, `AutocompleteTests.Listbox_ShouldStillCloseOnTab_WhenAMiddleClickElsewhereCameFirst`, `AutocompleteTests.Listbox_ShouldStillCloseOnTab_WhenADisabledButtonElsewhereWasPressed`, `AutocompleteTests.Listbox_ShouldClose_WhenAPressElsewhereTurnsIntoADrag`
- [x] A list opened from code closes on a click elsewhere or Escape, even if focus never reached the field. `AutocompleteTests.Listbox_ShouldClose_WhenOpenedFromCodeAndClickedElsewhere`, `AutocompleteTests.Listbox_ShouldClose_WhenOpenedFromCodeAndEscapeIsPressed`
- [x] A drag that starts in the field and ends outside it leaves the list open. `AutocompleteTests.Listbox_ShouldStayOpen_WhenADragFromTheFieldEndsOutsideIt`
- [x] While focus is elsewhere, a button that toggles a list opened from code closes it, and doesn't reopen it. `AutocompleteTests.Listbox_ShouldStayClosed_WhenAButtonElsewhereTogglesItShut`
- [x] With focus in the field, a button that toggles the list closes it, and doesn't reopen it. `AutocompleteTests.Listbox_ShouldStayClosed_WhenAButtonTogglesItShutWithFocusInTheField`
- [x] A press that opens the list isn't taken for a click elsewhere when it's released. `AutocompleteTests.Listbox_ShouldStayOpen_WhenAPressThatOpensItIsReleasedAfterAnEarlierClickElsewhere`
- [x] A button elsewhere activated from the keyboard leaves the list open, and Escape closes it. `autocomplete.js`

## Touch

- [ ] A tap on the box's padding focuses the input. UNCONFIRMED
- [x] A tap on a suggestion chooses it through the same pointer and click handlers. `autocomplete.js`

## Focus

- [x] Focus stays in the field while the list is open, and `aria-activedescendant` names the active option. `AutocompleteTests.Autocomplete_ShouldSearchAndChooseWithTheKeys_WhenTheUserTypes`
- [x] Typing again clears the active option. `autocomplete.js`
- [x] Leaving the field puts back the chosen value's text, unless it's `FreeText`. `AutocompleteTests.Autocomplete_ShouldCloseAndPutBackTheChosenText_WhenEscapeIsPressedAndTheFieldIsLeft`
- [x] Clearing moves focus to the field. `TbAutocompleteTests.Click_ShouldClearTheFieldAndFocusIt_WhenTheClearButtonIsClicked`
- [x] A disabled or read-only field doesn't open the list. `autocomplete.js`
- [x] A read-only field can't take a suggestion left over from before it became read-only. `AutocompleteTests.Autocomplete_ShouldNotActivateAnOption_WhenAnArrowIsPressedAfterTheFieldTurnedReadOnly`, `TbAutocompleteTests.Click_ShouldLeaveTheValue_WhenTheFieldTurnedReadOnlyOrDisabledWithTheListShowing`
- [x] Text typed before the field turned read-only isn't committed, and the value's text comes back. `TbAutocompleteTests.Change_ShouldPutBackTheValuesText_WhenTheFieldTurnedReadOnlyWithTextTyped`

## Not applicable

- Down moving to an option on a list not yet loaded: it only opens the list, as the docs say, since the suggestions aren't there yet.
- Alt+Up closing an open list: it moves up instead; the pattern lists closing as optional, and few people know it in a text field.
- A click on the field opening the list: it suggests as the user types, and a known list belongs in a select.
- A tap on the field opening the list: the same reason as a click.
- Page Up, Page Down, Home and End in the list: they're optional in the pattern, and Home and End stay with the text.
- Tab choosing the active option: the pattern's example doesn't, and the field settles what was typed.
- Escape clearing a closed field: it's optional in the pattern.
