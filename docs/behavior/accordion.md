# Accordion

Pattern: [Accordion](https://www.w3.org/WAI/ARIA/apg/patterns/accordion/)

Familiar apps: FAQ pages, settings pages with collapsible sections, the sections of VS Code's sidebar

## Keyboard

- [x] Enter or Space on a header opens or closes its item. `AccordionTests.Accordion_ShouldOpenOneItemAtATimeFromTheKeyboard_WhenEnterOrSpaceIsPressed`
- [x] Down and Up move between headers, wrapping at the ends, and Home and End go to the first and last. `AccordionTests.Accordion_ShouldMoveBetweenItsHeadersWithTheArrowsHomeAndEnd_WhenOneHasFocus`
- [x] The arrows skip disabled headers. `accordion.js`
- [x] Alt, Ctrl or Cmd with these keys stays with the browser. `accordion.js`
- [x] Keys in a panel's menu or nested accordion stay with them. `AccordionTests.Accordion_ShouldLeaveTheKeysToAMenuAndANestedAccordion_WhenTheyAreInAPanel`

## Pointer

- [x] Clicking a header opens its item and closes the open one. `TbAccordionTests.Click_ShouldOpenTheItemAndCloseTheOpenOne_WhenOnlyOneMayBeOpen`
- [x] With `Multiple`, the other items stay open. `TbAccordionTests.Click_ShouldLeaveOtherItemsOpen_WhenMultipleIsSet`
- [x] Clicking an open item's header closes it. `TbAccordionTests.Click_ShouldCloseTheItem_WhenItIsOpen`
- [x] A disabled header can't be used. `TbAccordionTests.Render_ShouldDisableTheTrigger_WhenDisabledIsSet`
- [x] Header actions run without opening or closing the item. `AccordionTests.AccordionItem_ShouldReachItsActionsAfterItsHeaderButLeaveThemToTab_WhenItHasHeaderActions`
- [x] Opening an item inside a panel leaves the item around it open. `TbAccordionTests.Click_ShouldLeaveTheItemAroundItOpen_WhenAnItemInItsPanelOpens`
- [x] A panel slides open only when motion is allowed. `AccordionTests.AccordionPanel_ShouldSlideOnlyWhenMotionIsAllowed_WhenAnItemOpens`
- [x] What was typed in a panel survives closing and opening it. `AccordionTests.Accordion_ShouldKeepWhatWasTyped_WhenAnItemIsClosedAndOpenedAgain`
- [x] Before the page is interactive, a header still opens its item and closes the open one, or leaves the others open with `Multiple`. `AccordionTests.Accordion_ShouldOpenAnItemAndCloseTheOthers_WhenThePageIsStatic`, `AccordionTests.Accordion_ShouldLeaveTheOthersOpen_WhenThePageIsStaticAndSeveralMayBeOpen`
- [x] Before the page is interactive, the browser's find opens the item whose closed panel holds a match, unless the item is disabled. `AccordionTests.Accordion_ShouldOpenTheItemHoldingTheText_WhenTheBrowserFindsItOnAStaticPage`, `TbAccordionTests.Render_ShouldKeepADisabledItemsPanelOutOfFind_WhenThePageIsStatic`

## Touch

- [x] Tapping a header opens or closes its item, since it listens for the click. `TbAccordionItem.razor`
- [ ] Opening an item below a taller open one keeps the tapped header on screen as that one closes. UNCONFIRMED

## Focus

- [x] Focus stays on the header when its item opens or closes. `TbAccordionItem.razor`
- [x] Tab reaches every enabled header, and an item's actions after its header. `AccordionTests.AccordionItem_ShouldReachItsActionsAfterItsHeaderButLeaveThemToTab_WhenItHasHeaderActions`
- [x] A closed panel's content can't be reached. `TbAccordionTests.Render_ShouldLinkEachHeaderButtonToItsHiddenRegion_WhenCollapsed`
- [x] An open panel inside hidden content, such as a loading [skeleton](skeleton.md) or a collapsed [splitter](splitter.md) pane, stays hidden with it. `HiddenContentTests.HiddenContent_ShouldHideAnOpenAccordionsPanel_WhenLoadingOrInACollapsedPane`
- [x] A focused header shows a focus ring. `trailblazor.css`
- [x] When code closes an item while focus is in its panel, focus moves to the item's header, unless the header is disabled and can't take it. `AccordionTests.AccordionItem_ShouldMoveFocusToItsHeader_WhenCodeClosesItWithFocusInItsPanel`, `AccordionTests.AccordionItem_ShouldMoveFocusToItsHeader_WhenCodeClosesAnItemOnItsOwn`, `AccordionTests.AccordionItem_ShouldLeaveFocusAlone_WhenCodeClosesItWithFocusOutsideItsPanel`

## Not applicable

- Keeping the only open item open: the pattern allows it, but here an open item always closes, so all may be closed.
- Browser find opening a closed panel once the page is interactive: the panel is inert until opened, which keeps its content out of find.
