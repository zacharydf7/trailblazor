# Time picker

Pattern: the browser's own time input, inside a field with a label, help and messages

Familiar apps: the start time of a calendar event, a pickup slot in a booking form

## Keyboard

- [x] Typing or filling a time in the box sets it, in 24-hour form whatever the box shows. `TimePickerTests.Picker_ShouldSetTheTime_WhenTheBoxIsFilled`, `TbTimePickerTests.Change_ShouldSetTheTime_WhenTheBoxGivesOne`
- [x] A time before the earliest or after the latest shows a message and marks the box invalid, one within them shows none, and a range across midnight takes times on either side of it. `TimePickerTests.Picker_ShouldShowAMessage_WhenTheTimeIsOutsideTheLimits`, `TbTimePickerTests.Change_ShouldShowAMessage_WhenTheTimeIsOutsideTheLimits`, `TbTimePickerTests.Change_ShouldShowNoMessage_WhenTheTimeIsWithinTheLimits`, `TbTimePickerTests.Change_ShouldTakeTimesAcrossMidnight_WhenMinIsAfterMax`
- [x] With only an earliest or only a latest time, the message names just that one, such as "Choose 9:00 AM or later." `TbTimePickerTests.Change_ShouldSayTheOneLimit_WhenOnlyMinOrMaxIsSet`
- [x] Text the box can't read shows a message, and a time with seconds reads when the step has them. `TbTimePickerTests.Change_ShouldShowAMessage_WhenTheTextIsntATime`, `TbTimePickerTests.Change_ShouldReadSeconds_WhenTheStepHasThem`

## Pointer

- [x] The box carries its earliest, latest and step, so the browser's own controls keep to them. `TbTimePickerTests.Render_ShouldShowTheTimeInTheTimeBoxWithItsLimitsAndStep_WhenGivenThem`

## Touch

- [ ] Tapping the box opens the device's own time picker. UNCONFIRMED

## Focus

- [x] The box is named by the label, and a step that isn't a whole number of minutes adds seconds. `TbTimePickerTests.Render_ShouldShowTheTimeInTheTimeBoxWithItsLimitsAndStep_WhenGivenThem`, `TbTimePickerTests.Render_ShouldShowSeconds_WhenTheStepIsUnderAMinute`
- [x] A static form posts the time. `TimePickerTests.Picker_ShouldPostTheTime_WhenTheFormIsStatic`

## Not applicable

- A list of times to choose from: the browser's time box gives the keys and parts people know; a list of fixed slots is a select.
- A date: use a date picker, or a date-time picker for both.
