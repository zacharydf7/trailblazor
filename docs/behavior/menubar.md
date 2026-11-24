# Menubar

Pattern: [Menubar](https://www.w3.org/WAI/ARIA/apg/patterns/menubar/)

Familiar apps: the File, Edit and View menus of desktop apps, Google Docs' menus

## Keyboard

- [x] Tab reaches the bar once, at the menu it was last on, and Tab again leaves it. `MenubarTests.Menubar_ShouldMoveAlongTheBarWithTheArrowKeys_WhenTheBarHasFocus`
- [x] Right and Left move along the bar and wrap around; Home and End go to its ends. `MenubarTests.Menubar_ShouldMoveAlongTheBarWithTheArrowKeys_WhenTheBarHasFocus`
- [x] Down, Enter or Space opens a menu on its first item. `MenubarTests.Menubar_ShouldMoveToTheMenuBeside_WhenRightOrLeftIsPressedInAnOpenMenu`
- [x] Right and Left in an open menu close it and open the menu beside it, on its first item. `MenubarTests.Menubar_ShouldMoveToTheMenuBeside_WhenRightOrLeftIsPressedInAnOpenMenu`
- [x] Escape closes the menu and goes back to its trigger on the bar. `MenubarTests.Menubar_ShouldMoveToTheMenuBeside_WhenRightOrLeftIsPressedInAnOpenMenu`
- [x] In right-to-left text, Left moves to the next menu. `MenubarTests.Menubar_ShouldSwapTheArrows_WhenThePageIsRightToLeft`

## Pointer

- [x] With one menu open, pointing at another opens it instead. `MenubarTests.Menubar_ShouldOpenTheMenuPointedAt_WhenAnotherIsOpen`
- [x] A click on a trigger opens or closes its menu, as a menu's does. `TbMenu.razor`

## Touch

- [x] A tap on a trigger opens its menu, even with another open; nothing needs hover. `MenubarTests.Menubar_ShouldOpenTheMenuTapped_WhenAnotherIsOpen`
- [x] `Size` steps the height of the menus' triggers on the bar with density, and `ItemSize` sizes the items inside each menu. A menu that sets its own `Size` or `ItemSize` keeps it. `SizeTests.Size_ShouldStepThePartsHeightWithDensity_WhenSmallMediumOrLarge`, `TbMenubarTests.Sizes_ShouldReachEachMenusTriggerAndList_WhenTheMenuSetsNoneOfItsOwn`

## Focus

- [x] Only one trigger is in the tab order, and a menu that goes away passes the tab stop on. `MenubarTests.Menubar_ShouldKeepOneTabStop_WhenTheMenuHoldingItGoesAway`
- [x] Before the page is interactive, every trigger is in the tab order. `TbMenubarTests.Render_ShouldLeaveEveryTriggerInTheTabOrder_WhenTheScriptHasNotRun`

## Not applicable

- Menus that open on hover alone: the bar opens one on click or keys, then follows the pointer.
