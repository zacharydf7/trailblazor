# Context menu

Pattern: [Menu](https://www.w3.org/WAI/ARIA/apg/patterns/menubar/); WAI-ARIA has no context menu pattern of its own

Familiar apps: Windows and macOS context menus, VS Code's editor and explorer, Google Docs and Figma

## Keyboard

- [x] Shift+F10 or the context menu key on an element in the area opens the menu below it, on its first item. `ContextMenuTests.Key_ShouldOpenTheMenuBelowTheFocusedElementAndEscapeReturnFocus_WhenPressed`
- [x] The contextmenu event some browsers fire for those keys, as the key is pressed or let go, doesn't move the menu. `ContextMenuTests.Key_ShouldOpenTheMenuBelowTheFocusedElementAndEscapeReturnFocus_WhenPressed`
- [x] With `Disabled`, those keys are left to the browser, which opens its own menu. `ContextMenuTests.Key_ShouldBeLeftToTheBrowser_WhenTheContextMenuIsDisabled`
- [x] Enter chooses the item, closes the menu and returns focus to where it was when the menu opened. `ContextMenuTests.Item_ShouldRunCloseAndReturnFocus_WhenChosenWithEnter`
- [x] Escape closes a submenu first, then the menu, returning focus each time. `ContextMenuTests.Escape_ShouldCloseASubmenuFirstThenTheMenu_WhenPressedTwice`
- [x] The arrows, Home, End, letters, Space, Tab, submenus and right-to-left keys work as in a menu. `menu.js`

## Pointer

- [x] A right-click opens the menu with its corner at the pointer and its first item focused. `ContextMenuTests.RightClick_ShouldOpenTheMenuAtThePointerWithTheFirstItemFocused_WhenOnTheTarget`
- [x] Near the window's bottom-right corner, the menu opens above and to the left of the pointer. `ContextMenuTests.RightClick_ShouldKeepTheMenuInsideTheWindow_WhenNearItsBottomRightCorner`
- [x] In right-to-left text, the menu opens toward the left of the pointer. `ContextMenuTests.RightClick_ShouldOpenTheMenuTowardTheLeft_WhenThePageIsRightToLeft`
- [x] On an element with a `data-context-key`, the menu shows only once the items for that key are drawn. `ContextMenuTests.RightClick_ShouldShowOnlyTheKeysItems_WhenOnAnElementWithAKey`, `TbContextMenuTests.Opening_ShouldDrawTheItemsAndMarkTheMenuForTheKey_WhenTheScriptAsksForAKey`
- [x] A right-click on another key while the menu is open moves it there with that key's items, and it stays open when the button is let go. `ContextMenuTests.RightClick_ShouldMoveTheMenu_WhenAnotherKeyIsRightClickedWhileItIsOpen`
- [x] A click outside closes the menu and leaves focus where the click put it. `ContextMenuTests.Click_ShouldCloseTheMenuAndLeaveFocusWhereItWent_WhenOutsideIt`
- [x] A right-click on the menu itself opens no other menu. `menu.js`
- [x] With `Disabled`, a right-click opens the browser's own menu. `ContextMenuTests.RightClick_ShouldLeaveTheBrowsersMenu_WhenTheContextMenuIsDisabled`
- [x] A right-click on another key before the server has drawn the first one's items shows only the last key's items, even when the last is the key already drawn. `ContextMenuTests.RightClick_ShouldShowTheLastKeysItems_WhenARightClickFollowsBeforeTheServerAnswers`
- [x] Scrolling the page closes the menu, which would otherwise stay over something else. `ContextMenuTests.Scroll_ShouldCloseTheMenu_WhenThePageScrolls`
- [x] A click elsewhere, or a scroll, before the server has drawn a key's items keeps the menu from opening late. `ContextMenuTests.Click_ShouldKeepTheMenuClosed_WhenItLandsOutsideBeforeTheServerAnswers`, `ContextMenuTests.Scroll_ShouldKeepTheMenuClosed_WhenThePageScrollsBeforeTheServerAnswers`
- [x] In a context menu nested in another's area, a right-click opens only the inner menu, and the outer one's items stay as they were. `ContextMenuTests.RightClick_ShouldOpenOnlyTheInnerMenu_WhenOnANestedContextMenusArea`, `TbContextMenuTests.Opening_ShouldLeaveAnOuterMenusItemsAlone_WhenAnInnerMenuAsksForAKey`
- [x] Hovering, submenus and the item under the pointer work as in a menu. `menu.js`

## Touch

- [x] A touch that rests on the area for half a second opens the menu, which stays open as the finger lifts; a tap opens nothing. `ContextMenuTests.LongPress_ShouldOpenTheMenuAndKeepItOpen_WhenATouchRestsOnTheTargetAndLifts`
- [x] A touch that moves more than a few pixels opens nothing, even on an area that doesn't pan, such as a map. `ContextMenuTests.LongPress_ShouldOpenNothing_WhenTheTouchMovesBeforeTheDelay`
- [x] A mouse opens the menu only with its right button, not by holding the left one. `ContextMenuTests.MouseHold_ShouldOpenNothing_WhenTheLeftButtonRestsOnTheTarget`
- [x] iOS shows no callout of its own for a link or image pressed in the area. `trailblazor.css`

## Focus

- [x] Opening by pointer or key focuses the first item, and closing returns focus to what had it before. `ContextMenuTests.Key_ShouldOpenTheMenuBelowTheFocusedElementAndEscapeReturnFocus_WhenPressed`

## Not applicable

- A trigger button, `aria-haspopup` and `aria-expanded`: nothing on screen opens a context menu.
- Opening from code: a context menu opens where the user points.
