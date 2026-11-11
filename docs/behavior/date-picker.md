# Date picker

Pattern: [Date picker dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/examples/datepicker-dialog/), with a box to type the date

Familiar apps: the date field of a booking or travel form, a due-date field in a task app

## Keyboard

- [x] Typing a date in the field's format and leaving the box sets it; text it can't read, or a date that can't be chosen, shows a message and keeps the date. `TbDatePickerTests.Change_ShouldSetTheDate_WhenTheTextIsInTheFormat`, `TbDatePickerTests.Change_ShouldShowAMessageAndKeepTheDate_WhenTheTextCantBeRead`, `TbDatePickerTests.Change_ShouldRefuseTheDate_WhenItCantBeChosen`
- [x] A typed date may leave out leading zeros or give the year in two digits, and the format hint uses the localizer's letters. `TbDatePickerTests.Change_ShouldReadTheDate_WhenItLeavesOutLeadingZerosOrTwoYearDigits`, `LocalizationTests.DatePicker_ShouldWriteItsFormatHintInTheLocalizersLetters_WhenTheyAreTranslated`
- [x] Enter or Space on the calendar button opens the calendar with focus on the chosen date, or today, each time. `DatePickerTests.Picker_ShouldOpenOnTheChosenDayAndPickWithTheKeys_WhenTheCalendarButtonIsPressed`, `DatePickerTests.Picker_ShouldCloseKeepingTheDateAndReopenOnIt_WhenEscapeIsPressed`
- [x] The arrows, Home, End, Page Up and Page Down move as in a calendar, and Enter or Space picks the day, even the chosen one, closes the calendar and returns focus to the button. `DatePickerTests.Picker_ShouldOpenOnTheChosenDayAndPickWithTheKeys_WhenTheCalendarButtonIsPressed`, `DatePickerTests.Picker_ShouldCloseKeepingTheDateAndReopenOnIt_WhenEscapeIsPressed`
- [x] Escape closes the calendar without changing the date. `DatePickerTests.Picker_ShouldCloseKeepingTheDateAndReopenOnIt_WhenEscapeIsPressed`

## Pointer

- [x] Clicking a day in the calendar sets it; picking the chosen day again takes back a message about typed text. `TbDatePickerTests.Pick_ShouldSetTheDate_WhenADayIsClicked`, `TbDatePickerTests.Pick_ShouldTakeBackTheMessageAboutTypedText_WhenTheChosenDayIsPickedAgain`
- [x] The clear button clears the date and moves focus to the box. `TbDatePickerTests.Clear_ShouldClearTheDateAndFocusTheBox_WhenPressed`

## Touch

- [ ] Tapping the calendar button and a day does what clicking does. UNCONFIRMED

## Focus

- [x] `@bind-Open` opens and closes the calendar from code, and reports when it closes, whether the user dismisses it or picks a day. Only a new value from the parent opens or closes it. `PickersOpenTests.Picker_ShouldOpenFromCodeAndReportAClose_WhenItsOpenIsBound`, `TbDatePickerTests.Open_ShouldFollowOnlyANewValueFromTheParent_WhenTheUserClosesTheCalendar`, `TbDatePickerTests.Pick_ShouldCloseTheCalendarAndReportIt_WhenADayIsPicked`
- [x] A read-only picker can't be changed: the box can't be edited and the calendar button turns off. `TbDatePickerTests.Render_ShouldTurnOffTheBoxAndButton_WhenReadOnly`
- [x] The calendar's grid, not an autofocus mark, tells the popover where focus goes, so a field marked AutoFocus after the picker still takes focus as a dialog opens. `TbDatePickerTests.Render_ShouldMarkTheGridForThePopoverToFocus_WhenInteractive`
- [x] The box is named by the label and shows the format as its placeholder. `TbDatePickerTests.Render_ShouldShowTheDateInTheFormatWithItAsThePlaceholder_WhenGivenADate`
- [x] Before the page is interactive, the browser's own date box stands in and posts the date. `TbDatePickerTests.Render_ShouldShowTheBrowsersDateBox_WhenStatic`, `DatePickerTests.ChooseAsync_ShouldPostTheDate_WhenThePickerIsStatic`

## Not applicable

- Choosing a range: use a date range picker.
- A time of day: the date picker chooses dates; a newly picked or typed date is midnight.
