# Tree select

Pattern: a field whose button opens a [Dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) popup holding a [Tree view](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/), as a select opens its list; `TbMultiTreeSelect` holds a tree with checkboxes

Familiar apps: the folder picker in Google Drive's move dialog, the category picker in online stores and the location picker in issue trackers

## Keyboard

- [x] Enter or Space on the field opens the tree, with focus in its filter box when it has one, and Tab moves from the filter box into the tree. `TreeSelectTests.TreeSelect_ShouldChooseFromTheKeyboardAndReturnToTheField_WhenOpened`
- [x] The tree takes its own keys: the arrows move and open or close branches. `TreeSelectTests.TreeSelect_ShouldChooseFromTheKeyboardAndReturnToTheField_WhenOpened`
- [x] Enter on an item chooses it, closes the tree and returns focus to the field. `TreeSelectTests.TreeSelect_ShouldChooseFromTheKeyboardAndReturnToTheField_WhenOpened`
- [x] Escape closes the tree without choosing and returns focus to the field. `TreeSelectTests.TreeSelect_ShouldCloseWithoutChoosing_WhenEscapeIsPressed`
- [x] Down Arrow in the filter box moves into the tree, to the first match of what's typed, or else to the row Tab would reach: the last one focused, the chosen item or the first one. `TreeSelectTests.Filter_ShouldMoveToTheFirstMatch_WhenDownArrowIsPressed`, `TreeSelectTests.Filter_ShouldMoveToTheChosenItemOrTheFirst_WhenDownArrowIsPressedWithoutAFilter`
- [x] The tree filters once typing pauses for 200 ms. Down Arrow is the only one of its keys that reaches the server, and it sends any text still waiting first, so it moves to a match of everything typed. `TreeSelectTests.Filter_ShouldMoveToAMatchOfAllTheText_WhenDownArrowFollowsTypingAtOnce`
- [x] In a multi tree select, Enter or Space checks or unchecks the focused item and the tree stays open; Escape closes it and returns focus to the field. `TreeSelectTests.MultiTreeSelect_ShouldCheckFromTheKeyboardAndStayOpen_WhenEnterOrSpaceIsPressed`
- [x] In a multi tree select, Down Arrow in the filter box moves to the first match, which Space then checks. `TreeSelectTests.MultiTreeSelect_ShouldMoveToTheFirstMatchAndCheckIt_WhenDownArrowThenSpaceIsPressed`

## Pointer

- [x] A click on an item chooses it and shows its text in the field. `TbTreeSelectTests.Choose_ShouldSetTheValueAndShowItsText_WhenAnItemIsClicked`
- [x] A click on the item already chosen closes the tree without reporting a change. `TbTreeSelectTests.Choose_ShouldCloseTheTreeWithoutRaisingAChange_WhenTheChosenItemIsClickedAgain`
- [x] An item that can't be chosen, such as a region, opens and closes but isn't chosen. `TbTreeSelectTests.Activate_ShouldLeaveTheValue_WhenTheItemCantBeChosen`
- [x] Typing in the filter box keeps the items that match, ignoring case and accents, and the items they sit under. `TbTreeSelectTests.Filter_ShouldKeepTheMatchingItemsAndTheirParents_WhenTyped`, `TbTreeSelectTests.Filter_ShouldIgnoreAccents_WhenTyped`
- [x] A render that changes nothing keeps the filter and the field's index of items, so the tree isn't laid out or walked again; `RefreshAsync` reads items changed in place. `TbTreeSelectTests.Filter_ShouldKeepItsFilter_WhenTheFieldRendersAgain`, `TbTreeSelectTests.Render_ShouldNotWalkTheTreeAgain_WhenTheItemsAreTheSame`, `TbTreeSelectTests.Render_ShouldShowTheNewItemsText_WhenGivenANewItemsCollection`, `TbTreeSelectTests.RefreshAsync_ShouldShowAnItemChangedInPlace_WhenCalled`
- [x] A filter that matches a heading keeps the items under it, so a region's name finds its trails. `TbTreeSelectTests.Filter_ShouldKeepTheItemsUnderAMatchingHeading_WhenTyped`
- [x] A filter that matches nothing shows and announces "No matches", once until something matches again. `TbTreeSelectTests.Filter_ShouldShowAndAnnounceThatNothingMatches_WhenNoItemDoes`, `TbTreeSelectTests.Filter_ShouldStaySilent_WhenSomethingMatches`, `TbTreeSelectTests.Filter_ShouldStaySilent_WhenOnlyALoadedChildMatches`, `TbTreeSelectTests.Filter_ShouldAnnounceNothingMatchesOnce_WhenTheUserKeepsTypingWithoutAMatch`
- [x] `Labels` words the filter box, the no-match message, the clear button, the count of checked items the field doesn't name, and the tree's own text. `TbTreeSelectTests.Labels_ShouldWordTheFilterNoMatchesAndClear_WhenGiven`, `TbTreeSelectTests.Labels_ShouldReachTheTree_WhenGivenTreeLabels`, `TbMultiTreeSelectTests.Render_ShouldNameUpToMaxShownItemsAndCountTheRest_WhenGivenMaxShown`
- [x] In a multi tree select, a click on a row checks it and the items inside it; the value lists them in tree order, the field shows their text, and the tree stays open. `TbMultiTreeSelectTests.Click_ShouldCheckTheItemAndAddItToTheValue_WhenARowIsClicked`, `TreeSelectTests.MultiTreeSelect_ShouldCheckAParentsItemsAndShowTheirText_WhenItsRowIsClicked`
- [x] With `CascadeChecks` off, a click checks only the row clicked. `TbMultiTreeSelectTests.Check_ShouldCheckOnlyTheRowClicked_WhenCascadeChecksIsOff`
- [x] When `IsSelectable` leaves out a heading, its checkbox still checks the items inside it, but the heading isn't part of the value; with `CascadeChecks` off it has no checkbox, as its check would reach nothing. `TbMultiTreeSelectTests.Check_ShouldLeaveAHeadingOutOfTheValue_WhenIsSelectableExcludesIt`, `TbMultiTreeSelectTests.Check_ShouldOfferNoCheckOnAHeading_WhenIsSelectableExcludesItAndCascadeChecksIsOff`
- [x] A heading above another heading checks everything inside it and unchecks it all on a second click, and a checked heading checks the items that load inside it later. `TbMultiTreeSelectTests.Check_ShouldUncheckEverythingInside_WhenAHeadingAboveAnotherIsClickedTwice`, `TbMultiTreeSelectTests.Check_ShouldReachItemsThatLoadLater_WhenAHeadingAboveThemIsChecked`
- [x] A checked heading unchecks on the first click when the value came from outside. `TbMultiTreeSelectTests.Check_ShouldUncheckANestedHeadingOnTheFirstClick_WhenTheValueCameFromOutside`
- [x] When `IsSelectable` leaves out a heading that holds nothing, it has no checkbox. `TbMultiTreeSelectTests.Check_ShouldOfferNoCheckOnAHeading_WhenIsSelectableExcludesItAndItHoldsNothing`
- [x] A form's reset clears a heading's check, even one that hadn't changed the value. `TbMultiTreeSelectTests.Reset_ShouldUncheckAHeadingWhoseItemsHaventLoaded_WhenTheFormResets`
- [x] Closing the tree without a choice clears the filter. `TbTreeSelectTests.Filter_ShouldStartAfresh_WhenTheTreeClosesWithoutAChoice`
- [x] With `Clearable`, a button beside the field clears the choice and goes away with it; a disabled or read-only field has none. `TbTreeSelectTests.Clear_ShouldEmptyTheValueAndRemoveTheButton_WhenClearableAndClicked`, `TbTreeSelectTests.Render_ShouldOfferNoClearButton_WhenThereIsNothingToClearOrItCantChange`, `TbMultiTreeSelectTests.Clear_ShouldUncheckEveryRow_WhenClearableAndClicked`, `TbMultiTreeSelectTests.Clear_ShouldUncheckAHeadingWhoseItemsHaventLoaded_WhenTheValueWasEmptyBefore`
- [x] `@bind-Open` opens and closes the tree from code and reports each open and close by the user, including a choice that closes it; a disabled or read-only field stays closed and reports `false` back to the parent. `TbTreeSelectTests.Open_ShouldOpenTheTree_WhenTheParentSetsIt`, `TbTreeSelectTests.Open_ShouldCloseTheTreeAndItsFilter_WhenTheParentClearsIt`, `TbTreeSelectTests.Open_ShouldStayClosedAndSaySo_WhenTheFieldCantChange`, `TbTreeSelectTests.OpenChanged_ShouldReportEachToggle_WhenTheUserOpensAndChooses`

## Touch

- [x] The button fills the field's box, and the tree's rows are the same size as a tree view's. `trailblazor.css`
- [x] The tree's rows stand off the popup's edges, as a select's options do. `TreeSelectTests.TreeSelect_ShouldSetItsRowsOffThePopupsEdges_WhenOpened`

## Focus

- [x] Clicking the label opens the tree, as a click on the field's button does. `TbTreeSelectTests.Render_ShouldNameTheButtonByItsLabelAndChoice_WhenShown`
- [x] A read-only field stays focusable and shows its choice, but opens nothing; a disabled one can't be focused. `TbTreeSelectTests.Render_ShouldShowAFocusableButtonThatOpensNothing_WhenReadOnly`, `TbTreeSelectTests.Render_ShouldDisableTheButton_WhenDisabled`

## Screen readers

- [x] The button is named by the label and the chosen item's text, and says it opens a dialog. `TbTreeSelectTests.Render_ShouldNameTheButtonByItsLabelAndChoice_WhenShown`
- [x] A chosen item from a branch that loaded when opened keeps its text in the field. `TbTreeSelectTests.Choose_ShouldShowTheItemsText_WhenItsBranchWasLoaded`
- [x] A value whose item hasn't loaded still shows a name, from `UnmatchedText`. `TbTreeSelectTests.Render_ShouldAskForTheText_WhenTheValuesItemIsntLoaded`
- [x] A key type's default, such as 0 for an `int`, counts as nothing chosen and shows the placeholder. `TbTreeSelectTests.Render_ShouldShowThePlaceholder_WhenAnIntKeyHoldsItsDefault`
- [x] A multi tree select's field shows the checked items' text, separated by commas, or the placeholder when none is checked. `TbMultiTreeSelectTests.Render_ShouldShowTheCheckedItemsTextOrThePlaceholder_WhenGivenAValue`
- [x] Past `MaxShown` items (three unless set), the field counts the rest, so its accessible name stays short. `TbMultiTreeSelectTests.Render_ShouldNameUpToMaxShownItemsAndCountTheRest_WhenGivenMaxShown`, `TbMultiTreeSelectTests.Render_ShouldNameThreeItemsAndCountTheRest_WhenMaxShownIsLeftAlone`
- [x] A required field with nothing chosen leaves the form invalid. `TbTreeSelectTests.Form_ShouldBeInvalid_WhenARequiredTreeSelectIsEmpty`

## Not applicable

- Typing into the field itself to filter, as a combobox does: the tree takes its own keys, including type-ahead, so the filter box sits in the popup instead.
- Down Arrow on the field to open it: the field is a button that opens a dialog, which Enter and Space open, as the dialog pattern has it.
