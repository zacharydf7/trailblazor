# Drawer

Pattern: [Dialog (modal)](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/), docked to an edge of the window

Familiar apps: the filter panel on a shop's search results, and the share sheet on a phone

## Keyboard

- [x] Opening a drawer moves focus to its first field. `DrawerTests.Drawer_ShouldCloseAndGiveFocusBack_WhenEscapeIsPressed`
- [x] Escape closes the drawer and gives focus back to the button that opened it. `DrawerTests.Drawer_ShouldCloseAndGiveFocusBack_WhenEscapeIsPressed`
- [x] Tab never reaches the page behind a modal drawer, which is a modal dialog. `DialogTests.Dialog_ShouldFocusTheFirstFieldAndKeepFocusOffThePage_WhenModal`

## Pointer

- [x] The close button closes the drawer and turns `Open` false. `TbDrawerTests.CloseButton_ShouldCloseTheDrawerAndRaiseOpenChanged_WhenPressed`
- [x] `Labels` names the header's buttons. `TbDrawerTests.Labels_ShouldWordTheHeadersButtons_WhenGiven`
- [x] `CanDismiss` can keep the drawer open. `TbDrawerTests.CloseButton_ShouldKeepTheDrawerOpen_WhenCanDismissRefuses`
- [x] A drawer at the start or end fills the window's height against its edge. `DrawerTests.Drawer_ShouldFillTheWindowsHeightAtItsEnd_WhenOpened`
- [x] A drawer at the start opens on the right in right-to-left text. `DrawerTests.Drawer_ShouldOpenOnTheRight_WhenItsAtTheStartOfRightToLeftText`
- [x] A bottom sheet spans the window's width at its bottom edge. `DrawerTests.Drawer_ShouldSpanTheBottomOfTheWindow_WhenItIsABottomSheet`
- [x] The actions stay at the bottom of the drawer, however short the content is, even when a form holds them. `DrawerTests.Drawer_ShouldKeepItsActionsAtTheBottom_WhenItsContentIsShort`, `DialogTests.Dialog_ShouldPinItsActionsToTheBottomOfTheWindow_WhenFullOrMaximized`
- [x] A drawer that isn't modal leaves the page usable beside it. `DrawerTests.Drawer_ShouldLeaveThePageUsable_WhenItIsNotModal`
- [x] A drawer's own `Modal`, `Size` and `Width` take the place of its options' when set; unset, it follows the options, and is modal and `Small` when they don't say. `TbDrawerTests.Open_ShouldPassTheDrawersSettingsToItsDialog_WhenTheyAreSet`, `TbDrawerTests.Options_ShouldReachTheDialogUnderTheDrawersOwnSettings_WhenGiven`, `TbDrawerTests.Open_ShouldOpenAModalDialogAtTheEnd_WhenTrue`

## Touch

- [x] Tapping does what clicking does, and nothing depends on hover. `TbDrawer.razor`

## Focus

- [x] Screen readers hear a dialog named by the drawer's title. `TbDrawerTests.Open_ShouldOpenAModalDialogAtTheEnd_WhenTrue`
- [x] A modal drawer makes the rest of the page inert, as a modal dialog does. `DialogTests.Dialog_ShouldFocusTheFirstFieldAndKeepFocusOffThePage_WhenModal`
- [x] A drawer slides in from its own edge and back to it as it closes, from the right edge for a start drawer in right-to-left text. `DrawerTests.Drawer_ShouldSlideFromItsOwnEdge_WhenItOpensAndCloses`
- [x] The actions stay in view at the bottom while long content scrolls. `DrawerTests.Drawer_ShouldKeepItsActionsInView_WhenItsContentScrolls`
- [x] A field focused behind the sticky actions, such as by Tab, scrolls out from behind them while they fit on one row. `DrawerTests.Drawer_ShouldScrollAFocusedFieldOutFromBehindItsActions_WhenTheyCoverIt`
- [x] In forced colors, the drawer has a border on every side, which sets it off from the page. `ForcedColorsTests.Popup_ShouldHaveABorderOnEverySide_WhenColorsAreForced`

## Not applicable

- Dragging a bottom sheet down to close it: the close button, Escape and the backdrop close it.
- Staying open as part of the page's layout, as an app's navigation does: that belongs in the app's own layout.
