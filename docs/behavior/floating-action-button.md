# Floating action button

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/) for the floating button, and [Menu button](https://www.w3.org/WAI/ARIA/apg/patterns/menu-button/) for the speed dial, which is a menu drawn as round actions

Familiar apps: Gmail's compose button, Google Calendar's create button and the new-item button in Android's Files app

## Keyboard

- [x] Enter or Space on a floating button runs its action, as on any button. `TbFabTests.Click_ShouldRunTheAction_WhenClicked`, `TbButtonBase.razor`
- [x] Up on a speed dial's trigger opens it on its last action, and Down, Enter or Space on its first. `FabTests.SpeedDial_ShouldTakeTheMenuButtonsKeys_WhenUsedFromTheKeyboard`
- [x] Up and Down move through the actions, wrapping at the ends, and Home and End jump to the first and last. `FabTests.SpeedDial_ShouldTakeTheMenuButtonsKeys_WhenUsedFromTheKeyboard`
- [x] Escape closes the dial and returns focus to its trigger. `FabTests.SpeedDial_ShouldTakeTheMenuButtonsKeys_WhenUsedFromTheKeyboard`
- [x] Enter chooses the focused action, closes the dial and refocuses the trigger. `FabTests.SpeedDial_ShouldTakeTheMenuButtonsKeys_WhenUsedFromTheKeyboard`
- [x] Tab closes the dial and moves on, as a menu does. `menu.js`

## Pointer

- [x] A floating button keeps its corner of the window as the page scrolls, and a click runs its action. `FabTests.Fab_ShouldKeepTheWindowsCornerAndRunItsAction_WhenThePageScrolls`
- [x] In right-to-left text, a button at the end sits on the left. `FabTests.Fab_ShouldSitOnTheLeft_WhenThePageIsRightToLeft`
- [x] A click on a speed dial opens its actions above the trigger, each icon centered over the trigger's, with the first action focused and the plus turned into a cross. `FabTests.SpeedDial_ShouldOpenItsActionsAboveTheTriggerCenteredOverIt_WhenClicked`
- [x] A click on an action runs it and closes the dial. `TbSpeedDialTests.Choose_ShouldRunTheAction_WhenAnActionIsChosen`
- [x] A dial at the top of the window opens below it, and one at a start corner lines up on its start side. `TbSpeedDialTests.Render_ShouldOpenTheActionsAwayFromTheWindowsEdge_WhenGivenAPosition`

## Touch

- [x] A floating button is at least 40 by 40px at its smallest, well over WCAG's 24px minimum target size. `trailblazor.css`
- [x] A fixed button keeps clear of a phone's rounded corners and home bar. `trailblazor.css`
- [ ] The button stays clear of the home bar on a real phone. UNCONFIRMED

## Focus

- [x] Opening a speed dial focuses its first action, and closing it returns focus to the trigger. `FabTests.SpeedDial_ShouldTakeTheMenuButtonsKeys_WhenUsedFromTheKeyboard`

## Screen readers

- [x] A floating button is named by its `Label`, shown as text only when `Extended`. `TbFabTests.Render_ShouldNameTheButtonByItsLabelAndShowOnlyTheIcon_WhenNotExtended`, `TbFabTests.Render_ShouldShowTheLabelAsTheButtonsText_WhenExtended`
- [x] A speed dial's trigger is a menu button named by its `Label`, and its actions are menu items named by their text. `TbSpeedDialTests.Render_ShouldMakeTheTriggerAFloatingMenuButtonNamedByItsLabel_WhenShown`

## Not applicable

- Hiding the button as the page scrolls down: a control that moves away while someone reaches for it is harder to use, and an app can still hide it.
