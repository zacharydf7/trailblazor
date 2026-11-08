# Toggle group

Pattern: toggle buttons from [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/), with the one tab stop of [Toolbar](https://www.w3.org/WAI/ARIA/apg/patterns/toolbar/)

Familiar apps: Word's text alignment and its bold, italic and underline buttons

## Keyboard

- [x] Tab enters on the pressed item and leaves in one stop. `ToggleGroupTests.ToggleGroup_ShouldBeOneTabStopLandingOnThePressedItem_WhenTabbedThrough`
- [x] With nothing pressed, Tab enters on the first enabled item. `roving-focus.js`
- [x] Left and Right move focus and wrap, and Home and End go to the first and last. `ToggleGroupTests.ToggleGroup_ShouldMoveFocusAndPressWithTheKeys_WhenTheArrowsHomeEndAndSpaceAreUsed`
- [x] The arrows only move focus; the value changes on Enter or Space. `ToggleGroupTests.ToggleGroup_ShouldMoveFocusAndPressWithTheKeys_WhenTheArrowsHomeEndAndSpaceAreUsed`
- [x] Enter and Space press the focused item, and the arrows skip disabled items. `ToggleGroupTests.MultiToggleGroup_ShouldSkipDisabledItemsAndPressSeveral_WhenUsedWithTheKeys`
- [x] A vertical group moves with Up and Down, and Left and Right do nothing. `ToggleGroupTests.ToggleGroup_ShouldMoveWithUpAndDown_WhenVertical`
- [x] Left and Right swap in right-to-left text. `ToggleGroupTests.ToggleGroup_ShouldSwapLeftAndRight_WhenTheGroupIsRightToLeft`
- [x] Alt, Ctrl or Cmd with an arrow stays with the browser. `roving-focus.js`
- [x] In a toolbar, the toolbar's arrows move through the items. `ToolbarTests.Toolbar_ShouldMoveAcrossEveryControlWithTheArrows_WhenControlsAreNested`
- [x] In a popup inside a toolbar, the group keeps its own arrows. `ToolbarTests.ToggleGroup_ShouldKeepItsOwnKeys_WhenInAPopupInsideAToolbar`

## Pointer

- [x] A click presses only that item in a single-selection group. `TbToggleGroupTests.Click_ShouldPressOnlyTheClickedItem_WhenAnotherItemIsClicked`
- [x] Clicking the pressed item keeps it pressed and raises nothing. `TbToggleGroupTests.Click_ShouldKeepTheItemPressedWithoutRaisingValueChanged_WhenThePressedItemIsClickedAgain`
- [x] Clicking the pressed item clears a clearable group. `ToggleGroupTests.ToggleGroup_ShouldClearThePressedItem_WhenItIsPressedAgainInAClearableGroup`
- [x] Clicks add and remove items in a multiple-selection group. `TbToggleGroupTests.Click_ShouldAddAndRemoveItems_WhenAMultipleGroupsItemsAreClicked`
- [x] A disabled group's items ignore clicks. `TbToggleGroupTests.Render_ShouldTurnEveryItemOff_WhenTheGroupIsDisabled`
- [x] A hovered item that isn't pressed looks different from a pressed one, so turning an item off shows at once under the pointer. `ToggleItemsTests.Item_ShouldLookDifferentFromAPressedOne_WhenItIsHovered`, `ToggleItemsTests.Item_ShouldShowItTurnedOff_WhenClickedWithThePointerStillOnIt`
- [x] Hover shows the name of an item that has only an icon. `ToggleItemsTests.Item_ShouldShowItsName_WhenAnIconOnlyItemIsHovered`
- [x] Icon-only items stay joined, and a pressed one stays lifted, even though each is wrapped in a tooltip. `ToggleItemsTests.Item_ShouldStayJoinedToItsNeighbors_WhenInARow`, `ToggleItemsTests.Item_ShouldStayJoinedToItsNeighbors_WhenInAColumn`, `ToggleItemsTests.Item_ShouldLiftAPressedOneOverItsNeighbors_WhenJoined`

## Touch

- [x] A tap presses an item through the same click a mouse sends, at a target at least 26px high. `TbToggleItem.razor`
- [ ] The hover color doesn't stay on after a tap, which would make an item just turned off look pressed. UNCONFIRMED

## Focus

- [x] The tab stop stays on the last focused item when focus leaves and comes back. `ToggleGroupTests.ToggleGroup_ShouldKeepTheTabStopOnTheLastFocusedItem_WhenFocusLeavesAndComesBack`
- [x] When the item holding the tab stop turns off while the group is hidden, the pressed or first enabled item takes it. `ToggleItemsTests.ToggleGroup_ShouldKeepATabStop_WhenTheFocusedItemTurnsOffWhileHidden`
- [x] A click moves the tab stop to the clicked item. `roving-focus.js`
- [x] Until focus has been inside the group, the tab stop follows a value set from code. `roving-focus.js`
- [x] A focused item is lifted over its neighbors, so all of its ring shows. `ToggleItemsTests.Item_ShouldLiftAFocusedOneOverItsNeighbors_WhenJoined`
- [x] Keyboard focus shows the name of an item that has only an icon. `ToggleItemsTests.Item_ShouldShowItsName_WhenTheKeysMoveFocusToAnIconOnlyItem`

## Not applicable

- Arrows that press as they move: a [radio group](radio-group.md) does that, and an item here may start something costly such as a map reload.
- Typing a letter to jump to an item: a group has a handful of items and no familiar app does it.
