# Select

Pattern: [Select-only combobox](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/examples/combobox-select-only/)

Familiar apps: a native `<select>`, the dropdowns in Windows and macOS settings

## Keyboard

- [x] Down, Up, Enter and Space open a closed list on the chosen option, or the first. `select.js`
- [x] Down and Up move through the open list and skip disabled options. `SelectTests.Select_ShouldMoveSkippingDisabledOptionsAndChooseWithEnter_WhenUsingTheKeyboard`
- [x] Home and End open the list if needed and go to the first or last option. `SelectTests.Select_ShouldJumpToTheEndsAndChooseWithAltUp_WhenHomeOrEndIsPressed`
- [x] Page Up and Page Down move through the open list by as many options as it shows, stopping at its ends, instead of scrolling the page behind it. They also work from the filter. `SelectLongTests.Select_ShouldMoveByTheRowsShownAndLeaveThePageAlone_WhenPageKeysArePressed`, `SelectLongTests.Select_ShouldStopAtTheEndsOfTheList_WhenPageKeysReachThem`, `SelectLongTests.Select_ShouldMoveByTheRowsShownAndLeaveThePageAlone_WhenPageKeysArePressedInTheFilter`, `SelectLongTests.Select_ShouldMoveByAboutAPage_WhenOptionsArriveAfterTheListOpensWithNoneActive`
- [x] Typing letters opens the list on the option that starts with them. `SelectTests.Select_ShouldOpenOnTheMatchingOption_WhenALetterIsTypedWhileClosed`
- [x] Pressing the same letter quickly cycles through the options that start with it, and "aa" still finds an option that starts with that letter doubled, unless the first press landed on it. Typing on, as in "aar", always finds it. `TypeAheadTests.Select_ShouldStepThroughTheOptionsStartingWithALetter_WhenItIsPressedQuickly`, `TypeAheadTests.Select_ShouldFindTheOptionWithADoubledLetter_WhenOneStartsWithIt`
- [x] A Space typed mid-word is part of the search and doesn't choose. `SelectTests.Select_ShouldTreatSpaceAsPartOfTheText_WhenTypedMidWord`
- [x] Enter or Space chooses the active option, closes a single select and keeps focus. `SelectTests.Select_ShouldMoveSkippingDisabledOptionsAndChooseWithEnter_WhenUsingTheKeyboard`
- [x] Alt+Up chooses the active option and closes a single select. `SelectTests.Select_ShouldJumpToTheEndsAndChooseWithAltUp_WhenHomeOrEndIsPressed`
- [x] Escape closes without choosing. `SelectTests.Select_ShouldCloseWithoutChoosing_WhenEscapeIsPressedOrTheComboboxIsClickedAgain`
- [x] Escape in a dialog closes only the list. `SelectTests.Select_ShouldCloseOnlyItsList_WhenEscapeIsPressedInsideADialog`
- [x] Tab keeps the active option in a single select, closes the list and moves on. `SelectTests.Select_ShouldKeepTheActiveOptionAndMoveOn_WhenTabIsPressedWhileOpen`
- [x] In a multi-select, Space and Enter toggle and stay open, and Tab and Alt+Up close without toggling. `SelectTests.MultiSelect_ShouldToggleWithSpaceAndEnterAndCloseWithoutToggling_WhenTabOrAltUpIsPressed`
- [x] Backspace removes the last chip. `SelectExtrasTests.Backspace_ShouldRemoveTheLastChip_WhenTheComboboxHasFocus`
- [x] Typing on a filterable select opens it and starts the filter, and Enter chooses the first match. `SelectExtrasTests.Filter_ShouldStartWithTheTypedTextAndChooseTheFirstMatch_WhenTypingOnTheSelect`
- [x] In the filter, Down and Up move through the matches, and Home and End stay with the text. `SelectExtrasTests.Filter_ShouldMoveThroughTheMatchesWithTheArrows_WhenFocusIsInTheFilter`

## Pointer

- [x] Options made from `Items` work as `TbOption`s do: a click chooses one, a template shows in place of its text, a disabled one can't be chosen, a static page lists them as native options, chips show and remove them, a list filled in place shows, and the keys reach them. `SelectItemsTests.Click_ShouldChooseTheItem_WhenItsOptionIsClicked`, `SelectItemsTests.Click_ShouldLeaveTheValue_WhenTheItemIsDisabled`, `SelectItemsTests.Render_ShouldShowTheTemplateAndNameItWithItsText_WhenGivenAnItemTemplate`, `SelectItemsTests.SelectAll_ShouldChooseEveryEnabledItem_WhenTheMultiSelectHasItems`, `SelectItemsTests.Render_ShouldListTheItemsAsNativeOptions_WhenThePageIsStatic`, `SelectItemsTests.RemoveChip_ShouldTakeOutTheItem_WhenItsChipIsRemoved`, `SelectItemsTests.Render_ShouldShowItemsAddedToTheList_WhenItIsFilledInPlace`, `SelectTests.Select_ShouldReachAndChooseItsItemsByKey_WhenItsOptionsComeFromItems`
- [x] Clicking the field or its chevron opens the list, and clicking it again closes it. `SelectTests.Select_ShouldCloseWithoutChoosing_WhenEscapeIsPressedOrTheComboboxIsClickedAgain`
- [x] Clicking the field before its script attaches, as on a slow link, opens the list once the script is ready, unless focus has moved on. `SelectTests.Select_ShouldOpenItsList_WhenClickedBeforeItsScriptHasLoaded`, `SelectTests.Select_ShouldStayClosed_WhenFocusMovesOnBeforeItsScriptHasLoaded`, `SelectTests.Select_ShouldStayClosed_WhenClickedTwiceBeforeItsScriptHasLoaded`
- [x] With `AttachOnFirstUse`, the select's script starts when it is first focused or pointed at, or its list is opened from code, or `AttachOnFirstUse` is turned off, so a select on every row costs no call to the browser until it's used. `TbSelectTests.AttachOnFirstUse_ShouldStartTheScriptOnlyOnceTheComboboxTakesFocus_WhenSet`, `TbSelectTests.AttachOnFirstUse_ShouldStartTheScript_WhenThePointerReachesTheCombobox`, `TbSelectTests.AttachOnFirstUse_ShouldStartTheScriptAndOpen_WhenTheParentOpensItBeforeUse`, `TbSelectTests.AttachOnFirstUse_ShouldStartTheScript_WhenTurnedOffBeforeUse`
- [x] With `AttachOnFirstUse`, a click beside the combobox, such as on its chevron, starts the script and opens the list. `SelectTests.Select_ShouldOpenItsList_WhenClickedBesideTheComboboxBeforeItsFirstUse`
- [x] With `AttachOnFirstUse`, a click on the label before first use focuses the combobox. `SelectTests.Select_ShouldFocusTheCombobox_WhenItsLabelIsClickedBeforeItsFirstUse`
- [x] A key that opens the list, pressed before the script attaches, opens it once the script is ready. `SelectTests.Select_ShouldOpenItsList_WhenAKeyIsPressedBeforeItsScriptHasLoaded`
- [x] With `AttachOnFirstUse`, the testing helper focuses the select to set up its script before it chooses. `SelectTests.Select_ShouldChooseThroughTheTestingHelper_WhenItAttachesOnFirstUse`
- [x] Clicking an option chooses it and closes a single select. `SelectTests.Select_ShouldChooseAClickedOptionAndRefocusTheCombobox_WhenOpenedByClicking`
- [x] Clicking options in a multi-select toggles them and leaves the list open. `SelectTests.MultiSelect_ShouldToggleOptionsAndStayOpen_WhenOptionsAreClicked`
- [x] Clicking a disabled option leaves the value alone and the list open. `SelectTests.Select_ShouldStayOpenAndUnchanged_WhenADisabledOptionIsClicked`
- [x] Hovering an option makes it the active one. `SelectTests.Select_ShouldFocusTheComboboxAndFollowThePointer_WhenTheLabelIsClickedAndAnOptionHovered`
- [x] Clicking elsewhere closes the list without choosing, since it's a native `popover="auto"`. `TbSelectBase.razor`
- [x] Clicking a chip's remove button removes that choice without opening the list. `SelectExtrasTests.Chips_ShouldRemoveAChoiceWithoutOpeningTheList_WhenItsButtonIsClicked`

## Touch

- [x] Tapping uses the same click handlers as the pointer, so a tap opens the list and chooses an option. `select.js`

## Focus

- [x] Focus stays on the combobox while the list is open, and `aria-activedescendant` names the active option. `SelectTests.Select_ShouldMoveSkippingDisabledOptionsAndChooseWithEnter_WhenUsingTheKeyboard`
- [x] Clicking the label focuses the combobox and doesn't open the list. `SelectTests.Select_ShouldFocusTheComboboxAndFollowThePointer_WhenTheLabelIsClickedAndAnOptionHovered`
- [x] The chosen option is scrolled into view as the list opens, unless it is disabled. `select.js`
- [x] Clearing or removing a chip moves focus to the combobox. `SelectTests.Select_ShouldClearTheChoice_WhenTheClearButtonIsClicked`
- [x] A filterable select moves focus to the filter and keeps it there in a multi-select. `SelectExtrasTests.Filter_ShouldStayOpenWithFocusAndText_WhenChoosingInAMultiSelect`
- [x] Escape in the filter closes the list and returns focus to the combobox. `SelectExtrasTests.Filter_ShouldHideEmptyGroupsSayNoMatchesAndClearOnClose_WhenNothingMatches`
- [x] A disabled select is out of the tab order and offers no clear button. `TbSelectTests.Render_ShouldHideTheClearButtonAndLeaveTheComboboxOutOfTheTabOrder_WhenDisabled`
- [x] A read-only select keeps its value and doesn't open. `TbSelectTests.Click_ShouldKeepTheValueAndHideClear_WhenReadOnly`
- [x] Pressing a disabled option or a group heading keeps focus on the combobox or filter, so the keys keep working. `SelectLongTests.Select_ShouldKeepFocusAndItsKeys_WhenADisabledOptionIsPressed`, `SelectLongTests.Select_ShouldKeepFocusInTheFilter_WhenAGroupHeadingIsPressed`
- [x] A press in the filter's text box still moves its caret. `SelectLongTests.Filter_ShouldMoveItsCaret_WhenItsTextBoxIsPressed`

## Not applicable

- Choosing without opening, as a native select's arrows do on Windows: the pattern opens the list first.
- A custom list when rendered statically: it's a native select, which brings the browser's own behavior.
