# Menu

Pattern: [Menu button](https://www.w3.org/WAI/ARIA/apg/patterns/menu-button/), [Menu](https://www.w3.org/WAI/ARIA/apg/patterns/menubar/)

Familiar apps: Windows and macOS menus, VS Code's and Google Docs' menus

## Keyboard

- [x] Enter or Space on the trigger opens the menu on its first item. `TbMenu.razor`
- [x] Down on the trigger opens it on its first item. `MenuTests.Menu_ShouldOpenFromACustomTriggerWithTheKeyboard_WhenGivenATrigger`
- [x] Up on the trigger opens it on its last item. `MenuTests.Menu_ShouldMoveFocusWithTheKeyboard_WhenOpen`
- [x] Up and Down move through the items and wrap at the ends. `MenuTests.SubMenu_ShouldKeepTheKeysWithinEachList_WhenOpenedAndClosedWithTheArrowKeys`
- [x] Home and End jump to the first and last item, and a letter to the next item that starts with it. `MenuTests.Menu_ShouldMoveFocusWithTheKeyboard_WhenOpen`
- [x] Enter chooses the item, closes the menu and refocuses the trigger. `MenuTests.Menu_ShouldRunTheItemCloseAndRefocusTheTrigger_WhenAnItemIsChosenWithEnter`
- [x] Space chooses the item too. `MenuTests.Menu_ShouldChooseTheItem_WhenSpaceIsPressed`
- [x] Enter and Space follow a link item. `MenuTests.LinkItem_ShouldFollowItsAddress_WhenChosenWithEnterOrSpace`
- [x] A disabled item can be reached, and Enter or Space on it does nothing. `MenuTests.Menu_ShouldFocusADisabledItemButNotChooseIt_WhenEnterOrSpaceIsPressed`
- [x] Right opens a submenu on its first item, and Left closes it back to its item. `MenuTests.SubMenu_ShouldKeepTheKeysWithinEachList_WhenOpenedAndClosedWithTheArrowKeys`
- [x] In right-to-left text, Left opens a submenu and Right closes it. `MenuTests.SubMenu_ShouldOpenWithTheLeftArrow_WhenThePageIsRightToLeft`
- [x] Escape closes one level at a time, and each level returns focus to what opened it. `MenuTests.SubMenu_ShouldCloseOneLevelAtATime_WhenEscapeIsPressed`
- [x] Tab closes the menu and moves on. `MenuTests.Menu_ShouldCloseAndMoveOn_WhenTabIsPressed`
- [x] A `KeepOpen` item leaves the menu open on a click, Enter or Space. `MenuTests.Menu_ShouldStayOpen_WhenAKeepOpenItemIsChosenByClickSpaceOrEnter`
- [x] Enter or Space on an item whose submenu the pointer opened goes into the submenu. `MenuTests.SubMenu_ShouldGoIntoTheSubmenu_WhenEnterIsPressedOnItsItemAfterThePointerOpenedIt`, `MenuTests.SubMenu_ShouldGoIntoTheSubmenu_WhenSpaceIsPressedOnItsItemAfterThePointerOpenedIt`
- [x] Alt, Ctrl, Shift or Cmd with an arrow on the trigger leaves the menu closed, and the key stays with the browser. `MenuTests.Menu_ShouldStayClosed_WhenAnArrowIsPressedWithAModifierOnTheTrigger`
- [x] A trigger that's off opens by no key, including a custom one that stays focusable or is loading. `OffTriggerTests.Menu_ShouldStayClosed_WhenAnArrowIsPressedOnACustomTriggerThatIsOff`

## Pointer

- [x] A click on the trigger opens the menu with its first item focused. `MenuTests.Menu_ShouldOpenWithTheFirstItemFocused_WhenTheTriggerIsClicked`
- [x] A second click on the trigger, or a click outside, closes it. `TbMenu.razor`
- [x] A click on an item runs it and closes every open list. `MenuTests.SubMenu_ShouldRunTheItemAndCloseEveryMenu_WhenOneOfItsItemsIsChosen`
- [x] A click on a disabled item leaves the menu open and runs nothing. `MenuTests.Menu_ShouldStayOpenAndDoNothing_WhenADisabledItemIsChosen`
- [x] Resting on a submenu's item opens the submenu, and focus doesn't go into it. `MenuTests.SubMenu_ShouldOpenWithoutTakingFocus_WhenThePointerRestsOnItsItem`
- [x] Crossing an item quickly doesn't open its submenu. `MenuTests.SubMenu_ShouldNotOpen_WhenThePointerCrossesItsItemAndLeavesTheMenu`
- [x] Resting on another item closes an open submenu. `MenuTests.SubMenu_ShouldClose_WhenThePointerRestsOnAnotherItem`
- [x] A submenu stays open while the pointer heads for it across other items. `MenuTests.SubMenu_ShouldStayOpen_WhenThePointerPausesOnAnotherItemOnItsWayToTheSubmenu`
- [x] A click on an item whose submenu is open goes into the submenu. `MenuTests.SubMenu_ShouldGoIntoTheSubmenu_WhenItsItemIsClickedAfterThePointerOpenedIt`
- [x] A menu too long for the window scrolls and stays on screen. `MenuTests.Menu_ShouldStayOnScreenAndScroll_WhenNoSideHasRoomForAllItsItems`
- [x] The item under the pointer is the active one, so a key pressed next acts on it. `MenuTests.Menu_ShouldChooseTheItemUnderThePointer_WhenEnterIsPressedAfterHovering`, `MenuTests.Menu_ShouldMoveOnFromTheItemUnderThePointer_WhenAnArrowIsPressedAfterHovering`, `MenuTests.Menu_ShouldFocusTheItemUnderThePointer_WhenTheItemIsDisabled`, `MenuTests.SubMenu_ShouldCloseAndFocusTheItemUnderThePointer_WhenThePointerStaysOnAnotherItemPastTheGraceTime`
- [x] Only one item is highlighted, whether the pointer or a key moved last, besides any item whose submenu is open. `MenuTests.Menu_ShouldHighlightOnlyTheFocusedItem_WhenAKeyMovesFocusOffTheItemUnderThePointer`
- [x] The item Enter would choose stays highlighted after the pointer leaves the menu. `MenuTests.Menu_ShouldKeepTheItemEnterWouldChooseHighlighted_WhenThePointerLeaves`, `MenuTests.Menu_ShouldOutlineTheItemEnterWouldChoose_WhenColorsAreForced`
- [x] Before the script attaches, or on a static page, the item under the pointer is highlighted. `MenuTests.Menu_ShouldHighlightTheItemUnderThePointer_WhenThePageIsStatic`
- [x] Scrolling a long menu under a resting pointer doesn't move the active item until the pointer moves. `menu.js`
- [x] A key that moves focus off an item closes the submenu open from it. `MenuTests.SubMenu_ShouldCloseAndLoseItsItemsHighlight_WhenAKeyMovesFocusOffItsItem`, `MenuTests.SubMenu_ShouldClose_WhenALetterMovesFocusOffItsItem`
- [x] A key press stops the pointer's timers, so no submenu opens or closes late. `MenuTests.SubMenu_ShouldKeepFocusInside_WhenEnterGoesInBeforeTheGraceTimeRunsOut`, `MenuTests.SubMenu_ShouldNotOpen_WhenAKeyMovesFocusBeforeTheHoverDelayEnds`
- [x] A key that doesn't move focus still closes a submenu open from another item, and Escape closes only that submenu. `MenuTests.SubMenu_ShouldClose_WhenAKeyThatMovesNothingIsPressedBeforeTheHoverDelayEnds`, `MenuTests.SubMenu_ShouldCloseOneLevel_WhenEscapeIsPressedBeforeTheHoverDelayEnds`
- [x] Once a key has moved focus off an item, moving the pointer over it reopens its submenu after the hover delay. `MenuTests.SubMenu_ShouldReopenAfterTheHoverDelay_WhenThePointerMovesOnItsItemAfterAKeyClosedIt`
- [x] Moving the pointer after a key press brings focus back to the item under the pointer, unless the key went into that item's submenu. `MenuTests.Menu_ShouldFocusTheItemUnderThePointerAgain_WhenThePointerMovesAfterAKey`, `MenuTests.SubMenu_ShouldKeepFocusInside_WhenThePointerMovesOnItsItem`, `MenuTests.SubMenu_ShouldLeaveFocusOnTheItemUnderThePointer_WhenThePointerMovesOffToAnotherItem`, `MenuTests.SubMenu_ShouldClose_WhenThePointerMovesOnAnotherItemAfterAKeyWentIntoIt`
- [x] While the pointer crosses other items on the way to a submenu, focus stays on the item that opened it, then moves to the item where the pointer arrives. `MenuTests.SubMenu_ShouldKeepFocusOnItsItemUntilThePointerArrives_WhenThePointerCrossesAnotherItem`

## Touch

- [x] A tap on the trigger or an item works as a click, and a tap outside closes the menu through the browser's popover handling. `TbMenu.razor`
- [x] A tap on a submenu's item opens it, and a second tap goes in; hover handling ignores anything but a mouse. `menu.js`
- [x] A submenu in a narrow window, such as a phone's, stays on screen and usable, overlapping its menu if it must. `NarrowPlacementTests.SubMenu_ShouldStayInsideTheWindowAndUsable_WhenNeitherSideHasRoom`

## Focus

- [x] A click outside leaves focus where the click put it. `menu.js`
- [x] An item that opens a dialog leaves the trigger as the place focus returns to. `pointer-origin.js`

## Not applicable

- A menu bar, and hover switching between open menus: a menu opens from one button.
- A context menu or press, drag and release: a menu opens with a click, and an item is chosen with a second one.
- Typing several letters to match a label: the pattern asks for one, and menus are short.
- Space toggling a checkbox or radio item without closing, which the pattern makes optional: `KeepOpen` decides, for Enter and Space alike.
