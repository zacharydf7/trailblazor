# List box

Pattern: [Listbox](https://www.w3.org/WAI/ARIA/apg/patterns/listbox/)

Familiar apps: a native `<select size>`, the list panes in Windows dialogs and macOS Finder

## Keyboard

- [x] Down and Up move through the options, skip disabled ones, and choose in a single-selection list. `ListBoxTests.ListBox_ShouldChooseTheOptionItMovesTo_WhenTheArrowKeysAreUsed`
- [x] Home and End go to the first and last option, and choose in a single-selection list. `ListBoxTests.ListBox_ShouldChooseTheOptionItMovesTo_WhenTheArrowKeysAreUsed`
- [ ] Page Up and Page Down move the active option by a page, as a native list does, instead of only scrolling the list away from it. #306
- [x] Typing letters jumps to the option that starts with them. `ListBoxTests.ListBox_ShouldJumpToTheOptionTyped_WhenLettersAreTyped`
- [ ] Pressing the same letter quickly cycles through the options that start with it. #304
- [x] A Space typed mid-word is part of the search. `listbox.js`
- [x] In a multiple-selection list, the arrows move without choosing, and Space toggles the active option. `ListBoxTests.MultiListBox_ShouldToggleWithSpaceAndExtendWithShift_WhenTheKeysAreUsed`
- [x] In a multiple-selection list, Shift+Down and Shift+Up also choose the option they move to. `ListBoxTests.MultiListBox_ShouldToggleWithSpaceAndExtendWithShift_WhenTheKeysAreUsed`
- [ ] In a multiple-selection list, Ctrl+A chooses every option. #306
- [x] A disabled list ignores the keys. `listbox.js`

## Pointer

- [x] Clicking an option chooses it. `ListBoxTests.ListBox_ShouldChooseTheOption_WhenItIsClicked`
- [x] Clicking the chosen option of a single-selection list keeps it. `TbListBox.cs`
- [x] Clicking an option of a multiple-selection list toggles it. `TbListBoxTests.Click_ShouldToggleEachOption_WhenAMultipleListboxsOptionsAreClicked`
- [ ] In a multiple-selection list, Shift+click chooses the range from the last clicked option. #306
- [x] Clicking a disabled option does nothing. `TbOption.razor`
- [x] A disabled list's options can't be chosen. `TbListBoxTests.Click_ShouldLeaveTheValue_WhenTheListboxIsDisabled`
- [x] A read-only list keeps its value. `TbListBoxTests.Click_ShouldKeepTheValue_WhenReadOnly`

## Touch

- [x] A tap chooses through the same click handler, and a swipe scrolls the list without choosing. `listbox.js`

## Focus

- [x] The first focus puts the active option on the chosen one, or the first, without choosing it. `listbox.js`
- [ ] Focus coming back puts the active option on the chosen one when the value changed while focus was away. #306
- [x] Focus stays on the list, and `aria-activedescendant` names the active option. `ListBoxTests.ListBox_ShouldChooseTheOptionItMovesTo_WhenTheArrowKeysAreUsed`
- [x] The active option scrolls into view. `ListBoxTests.ListBox_ShouldScrollTheActiveOptionIntoView_WhenItIsBelowTheRowsShown`
- [x] Pressing an option makes it active before focus arrives, so the list doesn't scroll away from it. `listbox.js`
- [x] The active option's ring goes when focus leaves. `ListBoxTests.ListBox_ShouldDropTheActiveOptionsRing_WhenFocusLeaves`
- [x] Clicking the label focuses the list. `ListBoxTests.ListBox_ShouldTakeFocus_WhenItsLabelIsClicked`
- [x] A disabled list is out of the tab order. `TbListBoxBase.razor`

## Not applicable

- Plain click clearing the other choices, as in a native multiple-selection list: each click toggles, like the boxes it shows.
- A custom list when rendered statically: it's a native list, which brings the browser's own behavior. `ListBoxTests.ListBox_ShouldPostTheChosenOptions_WhenUsedInAStaticForm`
