# Date range picker

Pattern: [Date picker dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/examples/datepicker-dialog/)

Familiar apps: the date ranges on flight and hotel booking sites, Google Analytics

## Keyboard

- [x] Enter or Space on the calendar button opens the calendar, since it's a native popover button. `TbDateRangePicker.razor`
- [x] Left and Right move by a day, and Up and Down by a week. `DateRangePickerTests.Calendar_ShouldMoveByDayWeekAndMonthAndShowNewMonths_WhenKeysArePressed`
- [x] Left and Right follow the reading direction. `DateRangePickerTests.Calendar_ShouldMoveToTheNextDayWithTheLeftArrow_WhenThePageIsRightToLeft`
- [x] Home and End go to the start and end of the week. `DateRangePickerTests.Calendar_ShouldMoveByDayWeekAndMonthAndShowNewMonths_WhenKeysArePressed`
- [x] Page Up and Page Down change the month, and with Shift the year, bringing it into view. `DateRangePickerTests.Calendar_ShouldMoveByDayWeekAndMonthAndShowNewMonths_WhenKeysArePressed`
- [x] Keys pressed faster than the server shows each month still land on the day they move to, and Enter or Space then picks that day, even with more keys pressed before it shows. `DateRangePickerTests.Calendar_ShouldLandOnTheDayTheKeysMovedTo_WhenTheyRunAheadOfTheServer`, `DateRangePickerTests.Calendar_ShouldPickTheDayTheKeysMovedTo_WhenEnterOrSpaceRunsAheadOfTheServer`, `DateRangePickerTests.Calendar_ShouldPickTheDayAndKeepMoving_WhenAnArrowFollowsAnEnterThatRanAheadOfTheServer`
- [x] Alt, Ctrl or Cmd with these keys stays with the browser, so Alt+Left still goes back. `DateRangePickerTests.Calendar_ShouldLeaveTheKeysToTheBrowser_WhenAltCtrlOrCmdIsHeld`
- [x] Enter or Space picks the day, since each day is a button. `TbDateRangePicker.razor`
- [x] Escape closes the calendar, keeps the range and returns focus to the button. `DateRangePickerTests.Calendar_ShouldCloseAndKeepTheRange_WhenEscapeIsPressed`
- [x] Typed dates commit when both read in order, on Enter or on leaving the box. `DateRangePickerTests.Typing_ShouldCommitBothDates_WhenTheyReadInOrder`
- [x] A typed date may leave out leading zeros. `TbDateRangePickerTests.Change_ShouldReadTheStart_WhenItLeavesOutLeadingZeros`
- [x] While the end is being chosen, moving focus with the keys previews the range, as the pointer does. `DateRangePickerTests.Calendar_ShouldPreviewTheRange_WhenTheKeysMoveFocusToALaterDay`, `DateRangePickerTests.Calendar_ShouldPreviewTheWholeRange_WhenPagingKeepsFocusOnTheSameButton`, `DateRangePickerTests.Calendar_ShouldPreviewTheRange_WhenAnArrowFollowsEnterBeforeTheServerAnswers`

## Pointer

- [x] Clicking around the date boxes, such as on the field's padding, focuses the start box, unless it lands on a button. `FieldBoxTests.Box_ShouldFocusItsFirstInput_WhenItHoldsTwo`
- [x] A press in the open calendar leaves it open and doesn't move focus to the start box. `FieldBoxTests.Box_ShouldLeaveItsPanelOpenAndItsInputUnfocused_WhenThePanelIsPressed`
- [x] The first click starts the range, and the second ends it, commits once and closes. `TbDateRangePickerTests.Pick_ShouldRaiseTheRangeOnce_WhenTheEndIsPicked`
- [x] After a click that doesn't focus the day, as in Safari on a Mac, the keys still move from the picked day, and a click on a month button leaves focus where it was. `DateRangePickerTests.Calendar_ShouldMoveOnFromThePickedDay_WhenAClickLeavesFocusOnThePage`, `DateRangePickerTests.Calendar_ShouldKeepOneTabStop_WhenAMonthButtonClickLeavesFocusOnThePage`
- [x] A second click on an earlier day starts over from that day. `TbDateRangePickerTests.Pick_ShouldStartAgain_WhenTheSecondDayIsEarlier`
- [x] A second click on the same day makes a one-day range. `TbDateRangePicker.cs`
- [x] A day that can't be chosen ignores the click. `TbDateRangePickerTests.Pick_ShouldIgnoreTheDay_WhenItCantBeChosen`
- [x] Hovering a later day previews the range. `DateRangePickerTests.Calendar_ShouldPreviewTheRange_WhenThePointerRestsOnALaterDay`
- [x] Closing mid-range drops the start and the preview. `DateRangePickerTests.Calendar_ShouldDropThePreview_WhenClosedAndOpenedAgain`
- [x] The month buttons page the calendar and leave a day in the tab order. `TbDateRangePickerTests.PreviousMonth_ShouldKeepADayInTheTabOrder_WhenPagingPastTheEarliestDate`
- [x] A preset commits its range and closes. `DateRangePickerTests.Preset_ShouldCommitItsRangeAndClose_WhenChosen`
- [x] Clicking elsewhere or the calendar button again closes without changing the range, rather than reopening. `DateRangePickerTests.Calendar_ShouldCloseAndKeepTheRange_WhenItsButtonIsClickedAgain`, `DateRangePickerTests.Calendar_ShouldCloseAndKeepTheRange_WhenClickedOutside`, `DateRangePickerTests.Calendar_ShouldCloseAndKeepTheRange_WhenItsButtonIsTappedAgain`

## Touch

- [ ] A tap on the field's padding focuses the start box. UNCONFIRMED
- [x] A tap picks a day through the same click handler. `date-range-picker.js`
- [x] A press that turns into a scroll leaves no range preview drawn. `DateRangePickerTests.Calendar_ShouldDropThePreview_WhenAPressBecomesAScroll`
- [x] On a narrow screen the months wrap, and the panel stays within the screen and scrolls. `trailblazor.css`

## Focus

- [x] Opening puts focus on the range's start, or today, or the nearest allowed day. `DateRangePickerTests.Calendar_ShouldOpenOnTheStartAndCommitTheRange_WhenTwoDaysArePicked`
- [x] A calendar clicked open before its script has loaded is placed and takes focus once it has, and an opening reported twice doesn't move focus a second time. `DateRangePickerTests.Calendar_ShouldPlaceItselfAndTakeFocus_WhenOpenedBeforeItsScriptHasLoaded`, `DateRangePickerTests.Calendar_ShouldLeaveFocusWhereItIs_WhenAnOpeningIsReportedTwice`
- [x] Only one day is in the tab order, across both months. `TbDateRangePickerTests.Render_ShouldPutOneDayInTheTabOrder_WhenTwoMonthsShow`
- [x] A day that can't be chosen can still take focus. `TbDateRangePicker.razor`
- [x] Committing a range returns focus to the calendar button. `DateRangePickerTests.Calendar_ShouldOpenOnTheStartAndCommitTheRange_WhenTwoDaysArePicked`
- [x] Tabbing out of the calendar closes it, including Shift+Tab back to the calendar button. `DateRangePickerTests.Calendar_ShouldClose_WhenShiftTabMovesFocusBackToItsButton`
- [x] Clearing moves focus to the start box. `DateRangePickerTests.Clear_ShouldEmptyBothDatesAndFocusTheStart_WhenClicked`
- [x] A read-only field keeps its boxes read only and turns off its buttons. `TbDateRangePickerTests.Render_ShouldTurnOffItsButtonsAndLeaveOutClear_WhenReadOnlyAndClearable`
- [x] A click on a blank part of the calendar, such as a month's title, leaves focus where it was, so the keys still work and Escape returns focus to the calendar button. `DateRangePickerTests.Calendar_ShouldKeepItsKeys_WhenABlankPartIsPressed`

## Not applicable

- Tab staying inside the calendar, as in the pattern's modal: it's a popover, and tabbing out closes it so it doesn't cover what's next.
- Opening when a box is clicked or focused: the boxes are for typing, and the button opens the calendar, as in the pattern.
- A second click on an earlier day swapping the ends: it starts a new range from that day.
