# Tree select

Pattern: a field whose button opens a [Dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) popup holding a [Tree view](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/), as a select opens its list

Familiar apps: the folder picker in Google Drive's move dialog, the category picker in online stores and the location picker in issue trackers

## Keyboard

- [x] Enter or Space on the field opens the tree, with focus in its search when it has one, and Tab moves from the search into the tree. `TreeSelectTests.TreeSelect_ShouldChooseFromTheKeyboardAndReturnToTheField_WhenOpened`
- [x] The tree takes its own keys: the arrows move and open or close branches. `TreeSelectTests.TreeSelect_ShouldChooseFromTheKeyboardAndReturnToTheField_WhenOpened`
- [x] Enter on an item chooses it, closes the tree and returns focus to the field. `TreeSelectTests.TreeSelect_ShouldChooseFromTheKeyboardAndReturnToTheField_WhenOpened`
- [x] Escape closes the tree without choosing and returns focus to the field. `TreeSelectTests.TreeSelect_ShouldCloseWithoutChoosing_WhenEscapeIsPressed`
- [x] Down Arrow in the search moves into the tree, to the first match of what's typed, or else to the row Tab would reach: the last one focused, the chosen item or the first one. `TreeSelectTests.Search_ShouldMoveToTheFirstMatch_WhenDownArrowIsPressed`, `TreeSelectTests.Search_ShouldMoveToTheChosenItemOrTheFirst_WhenDownArrowIsPressedWithoutASearch`

## Pointer

- [x] A click on an item chooses it and shows its text in the field. `TbTreeSelectTests.Choose_ShouldSetTheValueAndShowItsText_WhenAnItemIsClicked`
- [x] A click on the item already chosen closes the tree without reporting a change. `TbTreeSelectTests.Choose_ShouldCloseTheTreeWithoutRaisingAChange_WhenTheChosenItemIsClickedAgain`
- [x] An item that can't be chosen, such as a region, opens and closes but isn't chosen. `TbTreeSelectTests.Activate_ShouldLeaveTheValue_WhenTheItemCantBeChosen`
- [x] Typing in the search keeps the items that match and the items they sit under. `TbTreeSelectTests.Search_ShouldKeepTheMatchingItemsAndTheirParents_WhenTyped`
- [x] A search that matches a heading keeps the items under it, so a region's name finds its trails. `TbTreeSelectTests.Search_ShouldKeepTheItemsUnderAMatchingHeading_WhenTyped`
- [x] A search that matches nothing shows and announces "No matches", once until something matches again. `TbTreeSelectTests.Search_ShouldShowAndAnnounceThatNothingMatches_WhenNoItemDoes`, `TbTreeSelectTests.Search_ShouldStaySilent_WhenSomethingMatches`, `TbTreeSelectTests.Search_ShouldStaySilent_WhenOnlyALoadedChildMatches`, `TbTreeSelectTests.Search_ShouldAnnounceNothingMatchesOnce_WhenTheUserKeepsTypingWithoutAMatch`
- [x] Closing the tree without a choice clears the search. `TbTreeSelectTests.Search_ShouldStartAfresh_WhenTheTreeClosesWithoutAChoice`

## Touch

- [x] The button fills the field's box, and the tree's rows are the same size as a tree view's. `trailblazor.css`

## Focus

- [x] Clicking the label opens the tree, as a click on the field's button does. `TbTreeSelectTests.Render_ShouldNameTheButtonByItsLabelAndChoice_WhenShown`
- [x] A read-only field stays focusable and shows its choice, but opens nothing; a disabled one can't be focused. `TbTreeSelectTests.Render_ShouldShowAFocusableButtonThatOpensNothing_WhenReadOnly`, `TbTreeSelectTests.Render_ShouldDisableTheButton_WhenDisabled`

## Screen readers

- [x] The button is named by the label and the chosen item's text, and says it opens a dialog. `TbTreeSelectTests.Render_ShouldNameTheButtonByItsLabelAndChoice_WhenShown`
- [x] A chosen item from a branch that loaded when opened keeps its text in the field. `TbTreeSelectTests.Choose_ShouldShowTheItemsText_WhenItsBranchWasLoaded`
- [x] A value whose item hasn't loaded still shows a name, from `UnmatchedText`. `TbTreeSelectTests.Render_ShouldAskForTheText_WhenTheValuesItemIsntLoaded`
- [x] A key type's default, such as 0 for an `int`, counts as nothing chosen and shows the placeholder. `TbTreeSelectTests.Render_ShouldShowThePlaceholder_WhenAnIntKeyHoldsItsDefault`
- [x] A required field with nothing chosen leaves the form invalid. `TbTreeSelectTests.Form_ShouldBeInvalid_WhenARequiredTreeSelectIsEmpty`

## Not applicable

- Typing into the field itself to filter, as a combobox does: the tree takes its own keys, including type-ahead, so the search sits in the popup instead.
- Down Arrow on the field to open it: the field is a button that opens a dialog, which Enter and Space open, as the dialog pattern has it.
