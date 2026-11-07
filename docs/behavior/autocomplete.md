# Autocomplete

Pattern: [Editable combobox with list autocomplete](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/examples/combobox-autocomplete-list/)

Familiar apps: a browser's address bar, the search boxes in Google and GitHub

## Keyboard

- [x] Typing opens the list and searches. `AutocompleteTests.Autocomplete_ShouldSearchAndChooseWithTheKeys_WhenTheUserTypes`
- [x] Down and Up move through an open list, and wrap at its ends. `autocomplete.js`
- [x] Alt+Down opens the list without moving to an option. `autocomplete.js`
- [x] Enter chooses the active option, closes the list and stays in the field. `AutocompleteTests.Autocomplete_ShouldSearchAndChooseWithTheKeys_WhenTheUserTypes`
- [x] Enter with no active option closes the list and settles the typed text: a matching option is chosen, `FreeText` keeps it, and anything else goes back to the chosen value. `AutocompleteTests.Autocomplete_ShouldTakeTypedTextAsTheValue_WhenFreeTextIsEnteredOrLeft`
- [x] Escape closes an open list and keeps the text. `AutocompleteTests.Autocomplete_ShouldCloseAndPutBackTheChosenText_WhenEscapeIsPressedAndTheFieldIsLeft`
- [x] Escape on a closed list is left for what surrounds it, such as a dialog. `autocomplete.js`
- [x] Arrows pass over the status message. `AutocompleteTests.Listbox_ShouldStayOpenWithNothingActive_WhenItsMessageIsClickedOrArrowedTo`
- [x] Tab closes the list and settles the typed text the same way. `AutocompleteTests.Autocomplete_ShouldTakeTypedTextAsTheValue_WhenFreeTextIsEnteredOrLeft`

## Pointer

- [x] Clicking a suggestion chooses it and keeps focus in the field. `AutocompleteTests.Autocomplete_ShouldKeepFocusInTheInput_WhenAnOptionIsClicked`
- [x] Hovering a suggestion makes it the active one. `autocomplete.js`
- [x] Clicking the status message does nothing and leaves the list open. `AutocompleteTests.Listbox_ShouldStayOpenWithNothingActive_WhenItsMessageIsClickedOrArrowedTo`
- [x] Clicking elsewhere closes the list, since focus leaves the field. `autocomplete.js`
- [ ] A list opened from code closes on a click elsewhere or Escape, even if focus never reached the field. #307

## Touch

- [x] A tap on a suggestion chooses it through the same pointer and click handlers. `autocomplete.js`

## Focus

- [x] Focus stays in the field while the list is open, and `aria-activedescendant` names the active option. `AutocompleteTests.Autocomplete_ShouldSearchAndChooseWithTheKeys_WhenTheUserTypes`
- [x] Typing again clears the active option. `autocomplete.js`
- [x] Leaving the field puts back the chosen value's text, unless it's `FreeText`. `AutocompleteTests.Autocomplete_ShouldCloseAndPutBackTheChosenText_WhenEscapeIsPressedAndTheFieldIsLeft`
- [x] Clearing moves focus to the field. `TbAutocompleteTests.Click_ShouldClearTheFieldAndFocusIt_WhenTheClearButtonIsClicked`
- [x] A disabled or read-only field doesn't open the list. `autocomplete.js`
- [ ] A read-only field can't take a suggestion left over from before it became read-only. #307

## Not applicable

- Down moving to an option on a list not yet loaded: it only opens the list, as the docs say, since the suggestions aren't there yet.
- Alt+Up closing an open list: it moves up instead; the pattern lists closing as optional, and few people know it in a text field.
- A click on the field opening the list: it suggests as the user types, and a known list belongs in a select.
- A tap on the field opening the list: the same reason as a click.
- Page Up, Page Down, Home and End in the list: they're optional in the pattern, and Home and End stay with the text.
- Tab choosing the active option: the pattern's example doesn't, and the field settles what was typed.
- Escape clearing a closed field: it's optional in the pattern.
