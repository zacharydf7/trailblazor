# Date-time picker

Pattern: a date box with the date picker dialog, beside the browser's own time input, in a group with a label, help and
messages

Familiar apps: the start of a calendar event, a ride or shuttle booking

## Keyboard

- [x] Typing a date in the field's format keeps the time, and filling the time keeps the date. A new value from the parent, or a form reset, replaces what was typed. `TbDateTimePickerTests.Render_ShouldDropTypedText_WhenTheParentSetsANewValue`, `TbDateTimePickerTests.Reset_ShouldDropTypedText_WhenTheFormResets`, `TbDateTimePickerTests.Change_ShouldKeepTheTime_WhenADateIsTyped`, `TbDateTimePickerTests.Change_ShouldKeepTheDate_WhenATimeIsGiven`, `DateTimePickerTests.Picker_ShouldKeepTheDate_WhenTheTimeBoxIsFilled`
- [x] A date with no time is midnight, and a time given first waits in its box for a date. `TbDateTimePickerTests.Change_ShouldBeMidnight_WhenADateIsTypedWithNoTime`, `TbDateTimePickerTests.Change_ShouldWaitForADate_WhenATimeIsGivenFirst`
- [x] Clearing the date clears the value and keeps the time in its box; clearing the time makes it midnight. `TbDateTimePickerTests.Change_ShouldClearTheValueAndKeepTheTime_WhenTheDateIsCleared`, `TbDateTimePickerTests.Change_ShouldBeMidnight_WhenTheTimeIsCleared`
- [x] A date that can't be read, or that IsDateDisabled rules out, shows a message, keeps the value and marks only the date box invalid; fixing it takes the message back. `TbDateTimePickerTests.Change_ShouldShowAMessageAndKeepTheValue_WhenTheDateCantBeRead`, `TbDateTimePickerTests.Change_ShouldRefuseTheDate_WhenIsDateDisabledRulesItOut`, `TbDateTimePickerTests.Change_ShouldTakeBackTheMessage_WhenTheDateIsFixed`
- [x] A value before the earliest or after the latest shows a message naming the limits; one on a limit shows none. `TbDateTimePickerTests.Change_ShouldShowAMessage_WhenTheValueIsOutsideTheLimits`, `TbDateTimePickerTests.Change_ShouldShowNoMessage_WhenTheValueIsOnTheLimit`
- [x] Enter on the calendar button opens the calendar on the chosen day; arrows move and Enter picks, keeping the time and returning focus to the button. `DateTimePickerTests.Picker_ShouldPickADayWithTheKeysAndKeepTheTime_WhenTheCalendarButtonIsPressed`
- [x] Tab moves from the date box to the calendar button, then to the time box, which is named after the label. `DateTimePickerTests.Picker_ShouldMoveFromTheDateToTheCalendarButtonToTheTime_WhenTabIsPressed`
- [x] A DateTime keeps its kind, and a DateTimeOffset keeps its offset. `TbDateTimePickerTests.Change_ShouldKeepTheKind_WhenTheDateTimeIsUtc`, `TbDateTimePickerTests.Change_ShouldKeepTheOffset_WhenBoundToADateTimeOffset`
- [x] A first DateTimeOffset takes the app's TimeProvider zone, and a TimeZone gives each value that zone's offset for its date. `TbDateTimePickerTests.Change_ShouldTakeTheOffsetOfTheAppsTimeZone_WhenTheFirstValueIsChosen`, `TbDateTimePickerTests.Change_ShouldTakeTheZonesOffsetForThatDate_WhenGivenATimeZone`

## Pointer

- [x] Clicking a day sets the date and keeps the time; days outside Min and Max are marked unavailable. `TbDateTimePickerTests.Pick_ShouldSetTheDateAndKeepTheTime_WhenADayIsClicked`, `TbDateTimePickerTests.Render_ShouldLimitTheCalendarToTheDaysOfMinAndMax_WhenGivenThem`
- [x] The clear button clears both boxes and moves focus to the date box. `TbDateTimePickerTests.Clear_ShouldClearBothBoxesAndFocusTheDateBox_WhenPressed`
- [x] A disabled or read-only picker turns off its calendar button. `TbDateTimePickerTests.Render_ShouldTurnOffTheCalendarButton_WhenTheFieldIsTurnedOff`

## Touch

- [ ] Tapping the time box opens the device's own time picker. UNCONFIRMED

## Focus

- [x] The label names the group, and each box adds "Date" or "Time". `TbDateTimePickerTests.Render_ShouldNameEachBoxAfterTheLabel_WhenInteractive`
- [x] A step that isn't a whole number of minutes adds seconds to the time box. `TbDateTimePickerTests.Render_ShouldShowSeconds_WhenTheStepIsUnderAMinute`
- [x] A static page shows the browser's own date and time box, named by the label, and a static form posts the value with no offset, which Blazor reads in the server's time zone. `TbDateTimePickerTests.Render_ShouldShowTheBrowsersDateAndTimeBox_WhenStatic`, `DateTimePickerTests.ChooseAsync_ShouldPostTheValue_WhenThePickerIsStatic`

## Not applicable

- One box for both the date and the time: the date box reads the app's format and opens a calendar, and the browser's own time box gives the keys people know for a time.
