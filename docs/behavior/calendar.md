# Calendar

Pattern: [Date picker dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/examples/datepicker-dialog/)'s grid, shown in place, with views of a year's months and a decade's years

Familiar apps: the month view beside a booking form, a phone's calendar app jumping to another year

## Keyboard

- [x] The grid is one tab stop after the previous, title and next buttons. It lands on the chosen day, or today, and `FocusAsync` moves focus there even before the script has attached. `TbCalendarTests.FocusAsync_ShouldFocusTheGridsTabStopWithoutTheScriptsHandle_WhenCalled`, `TbCalendarTests.Render_ShouldShowTheValuesMonthAndMarkItsDayAndToday_WhenGivenAValue`, `TbCalendarTests.Render_ShouldOpenOnToday_WhenGivenNoValue`
- [x] Left and Right move by a day, Up and Down by a week, Home and End to the ends of the week, and Page Up and Page Down by a month, or a year with Shift, bringing it into view. `CalendarTests.Calendar_ShouldMoveByDayWeekAndMonth_WhenTheArrowAndPageKeysArePressed`
- [x] Left and Right follow the reading direction. `CalendarTests.Calendar_ShouldMoveToTheNextDayWithTheLeftArrow_WhenThePageIsRightToLeft`
- [x] Keys pressed faster than the server shows each month still land on the day they move to, and Enter then picks that day. `CalendarTests.Calendar_ShouldChooseTheDayTheKeysMovedTo_WhenEnterFollowsKeysThatRanAheadOfTheServer`, `TbCalendarTests.Pick_ShouldChooseTheDayTheKeysMovedTo_WhenTheBrowserAsks`
- [x] Among the months or years, the arrows move by one and by a row of four, Home and End go to the ends of the row, Page Up and Page Down move a year or a decade, and Enter or Space opens the month or year on the same day, with focus on it. `CalendarTests.Calendar_ShouldReachADayThroughTheYearsAndMonths_WhenOnlyTheKeyboardIsUsed`, `CalendarTests.Calendar_ShouldMoveAlongRowsAndPages_WhenKeysArePressedAmongTheMonthsAndYears`
- [x] Enter or Space on the title shows the year's months, then the decade's years, and focus stays on the title. `CalendarTests.Calendar_ShouldReachADayThroughTheYearsAndMonths_WhenOnlyTheKeyboardIsUsed`
- [x] The keys stop at years 1 and 9999, and the years 1 to 9 can be reached and moved among. `CalendarTests.Calendar_ShouldStayWithinTheYears1To9999_WhenKeysMoveNearEitherEnd`
- [x] Alt, Ctrl or Cmd with these keys stays with the browser. `calendar.js`

## Pointer

- [x] Clicking a day chooses it. `TbCalendarTests.Click_ShouldChooseTheDay_WhenADayIsClicked`
- [x] Previous and next show the month, year or decade before or after, are named for it, and turn off at years 1 and 9999. `TbCalendarTests.Paging_ShouldStopAtTheLastYearADateCanHave_WhenTheValueIsInDecember9999`, `TbCalendarTests.Paging_ShouldMoveByAMonthYearOrDecade_WhenTheButtonsAreClicked`, `TbCalendarTests.Paging_ShouldNameTheButtonsForTheView_WhenTheViewChanges`
- [x] Clicking the title, then a year and a month, opens that month on the same day, with focus on it. `TbCalendarTests.Click_ShouldOpenTheYearAndThenTheMonthOnTheSameDay_WhenAYearAndAMonthArePicked`, `CalendarTests.Calendar_ShouldFocusTheSameDayOfThePickedMonth_WhenTheTitleThenAMonthAreClicked`
- [x] A day, month or year that can't be chosen is marked and leaves the date alone. `TbCalendarTests.Click_ShouldLeaveTheDateAlone_WhenTheDayCantBeChosen`, `TbCalendarTests.Click_ShouldRefuseAMonthWithNoDayThatCanBeChosen_WhenMinAndMaxAreSet`, `CalendarTests.Calendar_ShouldLeaveTheDateAlone_WhenAClosedDayIsPicked`
- [x] A read-only calendar can be browsed but not changed, and a disabled one turns every button off. `TbCalendarTests.Click_ShouldMoveFocusButKeepTheDate_WhenReadOnly`, `TbCalendarTests.Render_ShouldTurnOffEveryButton_WhenDisabled`
- [x] A date left alone keeps its time of day; a picked one is midnight. `TbCalendarTests.Click_ShouldKeepTheTimeOfDayOnlyOnTheSameDay_WhenTheValueIsADateTime`
- [x] In forced-colors mode, the chosen day takes the system's highlight. `CalendarTests.Calendar_ShouldDrawTheChosenDayInTheSystemHighlight_WhenColorsAreForced`
- [x] In forced-colors mode, today is underlined, since every cell's border takes the text color. `CalendarTests.Today_ShouldBeUnderlined_WhenColorsAreForced`

## Touch

- [ ] Tapping a day, the title or the buttons does what clicking does. UNCONFIRMED

## Focus

- [x] A required calendar's grid is described as "required", since a grid can't carry `aria-required`. `TbCalendarTests.Required_ShouldDescribeTheGridAsRequiredAndBeValid_WhenADateIsChosen`
- [x] Screen readers hear a group named by the label, a grid named by the label and the month, and each day by its full date. `TbCalendarTests.Render_ShouldNameTheGridByTheLabelAndTitle_WhenLabeled`
- [x] Content added to a day is read after its date. `TbCalendarTests.DayTemplate_ShouldShowUnderTheDayAndDescribeIt_WhenGiven`
- [x] With `ShowWeekNumbers`, each week's number leads its row by the culture's rule, in the Gregorian dates shown, and a week across New Year has one number in both months, which is the ISO 8601 number for a culture whose weeks start on Monday and whose first week is the first with four days of the new year; screen readers can hear the column as "Week" and each number as "Week 12". `TbCalendarTests.ShowWeekNumbers_ShouldNumberEachWeekByTheCulturesRule_WhenSet`, `TbCalendarTests.ShowWeekNumbers_ShouldGiveTheIsoNumber_WhenAWeekStartsOnTheLastDayOfTheYear`, `TbCalendarTests.ShowWeekNumbers_ShouldGiveAWeekOneGregorianNumber_WhenGivenAMonth`, `TbCalendarTests.ShowWeekNumbers_ShouldWordTheColumnAndRows_WhenGivenLabels`
- [x] A new month or value from the parent brings its month into view, the same month given again leaves the calendar where the user took it, and each month the user moves to is reported. `TbCalendarTests.Month_ShouldShowTheGivenMonth_WhenTheParentSetsIt`, `TbCalendarTests.Month_ShouldStayWhereTheUserTookIt_WhenTheParentRendersAgainWithTheMonthItGave`, `TbCalendarTests.Value_ShouldBringItsMonthIntoView_WhenTheParentChangesIt`, `TbCalendarTests.MonthChanged_ShouldReportEachMonthShown_WhenTheUserMovesThroughTheViews`
- [x] Before the page is interactive, the browser's date box stands in and posts the date. `TbCalendarTests.Render_ShouldShowTheBrowsersDateBox_WhenStatic`, `CalendarTests.ChooseAsync_ShouldPostTheDate_WhenTheCalendarIsStatic`
- [x] With `Pick`, the calendar opens on the months or the years, a pick there sets the value to the month's first day or January 1, or the first day `Min` allows in it; with a month, pressing the title still shows the decade's years; Enter or Space on a month or year picks it; and the testing helpers stop at the month or year. `TbCalendarTests.Pick_ShouldOpenOnTheMonthsAndHoldAMonthsFirstDay_WhenItIsMonth`, `TbCalendarTests.Pick_ShouldOpenOnTheYearsAndHoldJanuaryFirst_WhenItIsYear`, `TbCalendarTests.Pick_ShouldHoldTheFirstDayMinAllows_WhenMinFallsInsideTheMonth`, `TbCalendarTests.Pick_ShouldStillShowTheYearsAndPickAMonthInThem_WhenTheTitleIsPressed`, `TbCalendarTests.Pick_ShouldSetTheMonth_WhenTheBrowserAsksForAPick`, `TbCalendarTests.Pick_ShouldHoldTheFirstDayMinAllows_WhenMinFallsInsideTheYear`, `TbCalendarTests.ChooseAsync_ShouldStopAtTheMonth_WhenTheCalendarPicksMonths`, `TbCalendarTests.ChooseAsync_ShouldStopAtTheYear_WhenTheCalendarPicksYears`, `CalendarTests.ChooseAsync_ShouldStopAtTheMonth_WhenTheCalendarPicksMonths`

## Not applicable

- Choosing a range: use a date range picker.
- Opening and closing: the calendar is shown in place, not in a popup.
