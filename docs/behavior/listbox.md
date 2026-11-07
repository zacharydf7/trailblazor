# List box

Pattern: [Listbox](https://www.w3.org/WAI/ARIA/apg/patterns/listbox/)

Familiar apps: a native `<select size>`, the list panes in Windows dialogs and macOS Finder

## Keyboard

- [x] Down and Up move through the options, skip disabled ones, and choose in a single-selection list. `ListBoxTests.ListBox_ShouldChooseTheOptionItMovesTo_WhenTheArrowKeysAreUsed`
- [x] Home and End go to the first and last option, and choose in a single-selection list. `ListBoxTests.ListBox_ShouldChooseTheOptionItMovesTo_WhenTheArrowKeysAreUsed`
- [x] Page Up and Page Down move the active option by as many options as the list shows, instead of only scrolling the list away from it, and choose in a single-selection list. `ListBoxKeysTests.ListBox_ShouldMoveByTheRowsShownAndChoose_WhenPageKeysArePressed`, `ListBoxKeysTests.MultiListBox_ShouldMoveWithoutChoosing_WhenPageKeysArePressed`, `ListBoxKeysTests.ListBox_ShouldStopAtTheEndsOfTheList_WhenPageKeysReachThem`
- [x] Typing letters jumps to the option that starts with them. `ListBoxTests.ListBox_ShouldJumpToTheOptionTyped_WhenLettersAreTyped`
- [x] Pressing the same letter quickly cycles through the options that start with it, and "aa" still finds an option that starts with that letter doubled, unless the first press landed on it. Typing on, as in "aar", always finds it. `TypeAheadTests.ListBox_ShouldStepThroughTheOptionsStartingWithALetter_WhenItIsPressedQuickly`, `TypeAheadTests.ListBox_ShouldFindTheOptionWithADoubledLetter_WhenOneStartsWithIt`, `TypeAheadTests.ListBox_ShouldStepPastTheOptionWithADoubledLetter_WhenItIsAlreadyOnIt`, `TypeAheadTests.ListBox_ShouldFindTheOptionWithADoubledLetter_WhenTypingGoesOnFromIt`
- [x] Different letters that match nothing leave the active option where it is. `TypeAheadTests.ListBox_ShouldStayOnTheOption_WhenDifferentLettersMatchNothing`
- [x] A Space typed mid-word is part of the search. `listbox.js`
- [x] In a multiple-selection list, the arrows move without choosing, and Space toggles the active option. `ListBoxTests.MultiListBox_ShouldToggleWithSpaceAndExtendWithShift_WhenTheKeysAreUsed`
- [x] In a multiple-selection list, Shift+Down and Shift+Up also choose the option they move to. `ListBoxTests.MultiListBox_ShouldToggleWithSpaceAndExtendWithShift_WhenTheKeysAreUsed`
- [x] In a multiple-selection list, Ctrl+A or Cmd+A chooses every enabled option as one change to the value. A read-only list stays as it is, and a single-selection list leaves the keys to the page. `ListBoxKeysTests.MultiListBox_ShouldChooseEveryEnabledOptionAsOneChange_WhenCtrlAIsPressed`, `ListBoxKeysTests.MultiListBox_ShouldLeaveTheValueAlone_WhenItIsReadOnlyAndCtrlAIsPressed`, `ListBoxKeysTests.ListBox_ShouldLeaveCtrlAToThePage_WhenOnlyOneOptionCanBeChosen`, `TbListBoxTests.ChooseAll_ShouldKeepWhatIsChosenAndSkipDisabledOptions_WhenCalled`, `TbListBoxTests.ChooseAll_ShouldChooseNothing_WhenTheListIsDisabledOrReadOnly`, `ListBoxKeysTests.MultiListBox_ShouldLeaveCtrlShiftAToThePage_WhenItIsPressed`
- [x] A disabled list ignores the keys. `listbox.js`

## Pointer

- [x] Clicking an option chooses it. `ListBoxTests.ListBox_ShouldChooseTheOption_WhenItIsClicked`
- [x] Clicking the chosen option of a single-selection list keeps it. `TbListBox.cs`
- [x] Clicking an option of a multiple-selection list toggles it. `TbListBoxTests.Click_ShouldToggleEachOption_WhenAMultipleListboxsOptionsAreClicked`
- [x] In a multiple-selection list, Shift+click adds the enabled options from the active one through the clicked one, in either direction, as one change to the value. `ListBoxKeysTests.MultiListBox_ShouldChooseTheRangeAsOneChange_WhenAnOptionIsShiftClicked`, `ListBoxKeysTests.MultiListBox_ShouldChooseTheRangeUpward_WhenTheShiftClickedOptionIsAboveTheLastOne`, `ListBoxKeysTests.MultiListBox_ShouldKeepWhatIsChosen_WhenARangeIsShiftClickedOverIt`, `TbListBoxTests.ChooseRange_ShouldMakeOneChangeOnceTheLastBatchIsIn_WhenTheRangeArrivesInBatches`
- [x] On a list of a thousand options, Ctrl+A and a Shift+click across them all work over a server circuit. `ListBoxKeysTests.MultiListBox_ShouldChooseEveryOption_WhenCtrlAIsPressedOnAVeryLongList`, `ListBoxKeysTests.MultiListBox_ShouldChooseTheRange_WhenItCrossesAVeryLongList`
- [x] Clicking a disabled option does nothing. `TbOption.razor`
- [x] A disabled list's options can't be chosen. `TbListBoxTests.Click_ShouldLeaveTheValue_WhenTheListboxIsDisabled`
- [x] A read-only list keeps its value. `TbListBoxTests.Click_ShouldKeepTheValue_WhenReadOnly`

## Touch

- [x] A tap chooses through the same click handler, and a swipe scrolls the list without choosing. `listbox.js`

## Focus

- [x] The first focus puts the active option on the chosen one, or the first, without choosing it. `listbox.js`
- [x] In a single-selection list, focus coming back puts the active option on the chosen one when the value changed while focus was away, unless a press on an enabled option brought focus back. With the value unchanged, focus coming back leaves the active option and the scroll position alone. `ListBoxKeysTests.ListBox_ShouldPutTheActiveOptionOnTheChosenOne_WhenFocusComesBackAfterTheValueChanged`, `ListBoxKeysTests.ListBox_ShouldChooseThePressedOption_WhenAPressBringsFocusBackAfterTheValueChanged`, `ListBoxKeysTests.ListBox_ShouldPutTheActiveOptionOnTheChosenOne_WhenADisabledOptionIsPressed`, `ListBoxKeysTests.ListBox_ShouldPutTheActiveOptionOnTheChosenOne_WhenFocusComesBackAfterAnEarlierPress`, `ListBoxKeysTests.ListBox_ShouldPutTheActiveOptionOnTheChosenOne_WhenAnEarlierPressBecameAScroll`, `ListBoxKeysTests.ListBox_ShouldKeepItsScrollPosition_WhenFocusComesBackWithTheValueUnchanged`
- [x] Focus stays on the list, and `aria-activedescendant` names the active option. `ListBoxTests.ListBox_ShouldChooseTheOptionItMovesTo_WhenTheArrowKeysAreUsed`
- [x] The active option scrolls into view. `ListBoxTests.ListBox_ShouldScrollTheActiveOptionIntoView_WhenItIsBelowTheRowsShown`
- [x] Pressing an option makes it active before focus arrives, so the list doesn't scroll away from it. `listbox.js`
- [x] The active option's ring goes when focus leaves. `ListBoxTests.ListBox_ShouldDropTheActiveOptionsRing_WhenFocusLeaves`
- [x] Clicking the label focuses the list. `ListBoxTests.ListBox_ShouldTakeFocus_WhenItsLabelIsClicked`
- [x] A disabled list is out of the tab order. `TbListBoxBase.razor`

## Not applicable

- Plain click clearing the other choices, as in a native multiple-selection list: each click toggles, like the boxes it shows.
- A custom list when rendered statically: it's a native list, which brings the browser's own behavior. `ListBoxTests.ListBox_ShouldPostTheChosenOptions_WhenUsedInAStaticForm`
