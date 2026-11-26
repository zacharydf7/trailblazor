# Time picker

Pattern: an editable combobox with a list of times, inside a field with a label, help and messages; the browser's own time input before the page is interactive

Familiar apps: the start time of a calendar event, a pickup slot in a booking form

## Keyboard

- [x] Typing a time in the culture's form, such as 9:15 PM or 21:15, or in 24-hour form in any culture, sets it. `TimePickerTests.Picker_ShouldSetTheTime_WhenTheBoxIsFilled`, `TbTimePickerTests.Change_ShouldReadTheTime_WhenTypedInTheCulturesFormOrIn24HourForm`, `TbTimePickerTests.Render_ShouldShowAndReadTwentyFourHourTimes_WhenTheCultureWritesThem`, `TbTimePickerTests.Change_ShouldSetTheTime_WhenTheBoxGivesOne`
- [x] Down opens the list on the chosen time, or the first listed time after it; Down and Up move through it, and Enter picks the time in focus, leaving focus in the box. `TimePickerTests.Picker_ShouldPickATimeWithTheKeys_WhenTheListIsOpenedFromTheBox`, `TbTimePickerTests.Render_ShouldStartTheKeysAtTheChosenTimeOrTheNextListedOne_WhenATimeIsChosen`, `TbTimePickerTests.Render_ShouldStartTheKeysAtTheChosenTime_WhenTheListRunsPastMidnight`, `TbTimePickerTests.Render_ShouldStartTheKeysAtTheFirstTime_WhenNoTimeIsChosen`
- [x] Typing in the box opens the list, and leaving the box takes the typed time; picking a time after typing puts that time in the box, even the one already chosen. `TimePickerTests.Picker_ShouldShowThePickedTime_WhenTheChosenTimeIsPickedAfterTyping`, `TimePickerTests.Picker_ShouldSetTheTime_WhenTheBoxIsFilled`
- [x] Escape closes the list and keeps the time. `TimePickerTests.Picker_ShouldCloseTheListAndKeepTheTime_WhenEscapeIsPressed`
- [x] A time before the earliest or after the latest shows a message and marks the box invalid, one within them shows none, and a range across midnight takes times on either side of it. `TimePickerTests.Picker_ShouldShowAMessage_WhenTheTimeIsOutsideTheLimits`, `TbTimePickerTests.Change_ShouldShowAMessage_WhenTheTimeIsOutsideTheLimits`, `TbTimePickerTests.Change_ShouldShowNoMessage_WhenTheTimeIsWithinTheLimits`, `TbTimePickerTests.Change_ShouldTakeTimesAcrossMidnight_WhenMinIsAfterMax`
- [x] With only an earliest or only a latest time, the message names just that one, such as "Choose 9:00 AM or later." `TbTimePickerTests.Change_ShouldSayTheOneLimit_WhenOnlyMinOrMaxIsSet`
- [x] Text the box can't read shows a message, and a time with seconds reads when the step has them. `TbTimePickerTests.Change_ShouldShowAMessage_WhenTheTextIsntATime`, `TbTimePickerTests.Change_ShouldReadSeconds_WhenTheStepHasThem`

## Pointer

- [x] The clock button opens the list with focus kept in the box, and clicking a time picks it and closes the list. `TimePickerTests.Picker_ShouldOpenTheListAndPickTheClickedTime_WhenTheClockButtonIsClicked`, `TbTimePickerTests.Click_ShouldChooseTheTime_WhenATimeInTheListIsClicked`
- [x] The list holds a time for each step within the earliest and latest, or each half hour with no step or one under five minutes, and runs past midnight when the earliest is after the latest. `TbTimePickerTests.Render_ShouldShowTheTimeAndListATimeForEachStepWithinTheLimits_WhenGivenThem`, `TbTimePickerTests.Render_ShouldListEveryHalfHour_WhenTheStepIsUnderFiveMinutes`, `TbTimePickerTests.Render_ShouldRunTheListPastMidnight_WhenMinIsAfterMax`, `TbTimePickerTests.Render_ShouldListTimesWithinTimeSpanLimits_WhenBoundToATimeSpan`
- [x] A read-only or disabled picker has no clock button, and its times can't be picked. `TbTimePickerTests.Render_ShouldLeaveOutTheClockButton_WhenTheTimeCantChange`, `ValueContractTests.Field_ShouldNotChangeItsValue_WhenTurnedOffAndEveryControlIsUsed`

## Touch

- [ ] Tapping the clock button opens the list, and tapping a time picks it. UNCONFIRMED

## Focus

- [x] The box is named by the label and controls the list, the chosen time is marked selected, and the clock button is named and kept out of the tab order. `TbTimePickerTests.Render_ShouldShowTheTimeAndListATimeForEachStepWithinTheLimits_WhenGivenThem`, `TbTimePickerTests.Render_ShouldNameTheClockButtonAndKeepItOutOfTheTabOrder_WhenInteractive`
- [x] A step that isn't a whole number of minutes adds seconds to the box. `TbTimePickerTests.Render_ShouldShowSeconds_WhenTheStepIsUnderAMinute`
- [x] A static form posts the time from the browser's own time box, which carries the earliest, latest and step. `TimePickerTests.Picker_ShouldPostTheTime_WhenTheFormIsStatic`, `TbTimePickerTests.Render_ShouldBeTheBrowsersTimeBoxWithItsLimitsAndStep_WhenTheFormIsStatic`
- [x] `Start` and `End` content sit in the box before and after the input, on a static page too. `FieldAdornmentTests.Render_ShouldPutTheAppsStartAndEndFirstInTheBoxsSlots_WhenGivenStartAndEnd`

## Not applicable

- A date: use a date picker, or a date-time picker for both.
