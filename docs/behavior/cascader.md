# Cascader

Pattern: [Editable combobox with list autocomplete](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/examples/combobox-autocomplete-list/), with one listbox for each level

Familiar apps: the country, region and city pickers on shipping forms, Ant Design's and Element's cascaders

## Keyboard

- [x] Down and Up open the lists, then move within the active list. `CascaderTests.Cascader_ShouldChooseAPathWithTheKeys_WhenRightOpensEachLevel`
- [x] Right opens the active choice's list and moves into it, and Left goes back to the choice that opened it. `CascaderTests.Cascader_ShouldChooseAPathWithTheKeys_WhenRightOpensEachLevel`
- [x] Enter on a choice with more opens its list, and with `AllowParents` also chooses it; on the end of a path, Enter chooses it and closes the lists. `CascaderTests.Cascader_ShouldChooseAPathWithTheKeys_WhenRightOpensEachLevel`, `CascaderTests.Cascader_ShouldOpenWithoutChoosing_WhenRightBrowsesAParentThatCanBeChosen`
- [x] Right opens a list without choosing it, even with `AllowParents`, and moves into the new list even when it has the same number and kinds of choices as the one it replaced. `CascaderTests.Cascader_ShouldOpenWithoutChoosing_WhenRightBrowsesAParentThatCanBeChosen`, `CascaderTests.Cascader_ShouldMoveIntoTheNewList_WhenRightOpensASiblingOfTheSameShape`
- [x] In a right-to-left page, Left opens the next list and Right goes back. `CascaderTests.Cascader_ShouldSwapTheArrows_WhenThePageIsRightToLeft`
- [x] Escape closes the lists, and closed lists stay hidden. `CascaderTests.Cascader_ShouldHideItsLists_WhenClosed`
- [x] Typing searches every level, ignoring case and accents, and lists the matching paths. `TbCascaderTests.Typing_ShouldSearchEveryLevelIgnoringAccents_WhenTheUserTypes`

## Pointer

- [x] A click on the field opens the lists, and a click on a choice opens its list or chooses it. `CascaderTests.Cascader_ShouldChooseAPathByClicking_WhenEachLevelIsClicked`
- [x] A disabled choice can't be opened or chosen. `TbCascaderTests.Pick_ShouldDoNothing_WhenTheChoiceIsDisabled`
- [x] Moving the pointer over a choice makes it the active one, and a pointer that rests over a list leaves the active choice to the keys when the list scrolls under it: a move that the browser sends for the scroll, with the pointer where it was, is left alone, such as the one WebKit sends a moment after a scroll. A move straight across to the next list is the pointer's own. `RestingPointerTests.Cascader_ShouldLeaveTheActiveOptionToTheKeys_WhenItsListScrollsUnderARestingPointer`, `RestingPointerTests.Cascader_ShouldFollowThePointer_WhenItMovesStraightAcrossToTheNextList`
- [ ] The same holds where the pointer's place isn't known. The library knows it only from the last real move, so the first move in a window that had none, or after a real move in another window, counts as the user's, whoever sent it. #594

## Touch

- [x] The same taps work; nothing needs hover. `TbCascader.razor`

## Focus

- [x] Focus stays in the input, and `aria-activedescendant` names the active choice in whichever list it's in. `CascaderTests.Cascader_ShouldChooseAPathWithTheKeys_WhenRightOpensEachLevel`
- [x] A value given from code shows its path and opens its levels. `TbCascaderTests.Render_ShouldShowTheValuesPathAndOpenItsLevels_WhenGivenAValue`
- [x] Leaving the field takes a path typed in full, and otherwise puts the value's path back. `TbCascaderTests.Change_ShouldTakeAPathTypedInFull_WhenTheFieldIsLeft`, `TbCascaderTests.Change_ShouldPutTheValuesPathBack_WhenTheTextIsNoPath`

## Not applicable

- Loading a level's choices on demand: `Items` holds the whole tree; an app with a very large tree can use a tree select.
- Choosing several paths: a cascader holds one value.
