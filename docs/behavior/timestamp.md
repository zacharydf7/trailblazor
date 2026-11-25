# Timestamp

Pattern: none; it is a `time` element with the moment in its `datetime`, and a relative one is a tooltip's trigger

Familiar apps: the "Expires Dec 4, 2026" line on a membership page, the sent time on an email in an inbox

## Keyboard

- [x] A relative timestamp is a tab stop, so its tooltip shows on keyboard focus, and Escape hides it. Other formats have nothing to press. `TimestampTests.Relative_ShouldBeDescribedByTheExactTime_WhenFocused`

## Pointer

- [x] Resting the pointer on a relative timestamp shows its exact time; other formats are plain text. `tooltip.js`

## Touch

- [x] A tap on a relative timestamp shows its exact time, since the tap does nothing else. `TimestampTests.Relative_ShouldShowTheExactTime_WhenTapped`

## Focus

- [x] A missing value shows a dash, which screen readers hear as "Not set". `TbTimestampTests.Render_ShouldShowADashReadAsNotSet_WhenTheValueIsNull`
- [x] A relative timestamp's exact time is its description. `TbTimestampTests.TbTimestamp_ShouldDescribeExactTime_WhenRelative`, `TimestampTests.Relative_ShouldBeDescribedByTheExactTime_WhenFocused`

## Time zones

- [x] It shows its moment in its own zone, else a cascaded one, else the app's, else the clock's local zone. `TbTimestampTests.TbTimestamp_ShouldPreferOwnTimeZone_WhenGivenOne`, `TbTimestampTests.TbTimestamp_ShouldShowValueInTheCascadedTimeZone_WhenOneIsCascaded`, `TbTimestampTests.TbTimestamp_ShouldShowValueInTheAppsTimeZone_WhenNothingCloserSetsOne`
- [x] Its `datetime` holds the moment in ISO 8601 with at most three digits of a second. `TbTimestampTests.TbTimestamp_ShouldWriteIsoDateTimeAttribute_WhenCalled`
- [x] `ShowZone` adds the UTC offset, or the app's abbreviation for standard or daylight time, found by IANA or Windows id. `TbTimestampTests.TbTimestamp_ShouldShowOffset_WhenGivenShowZone`, `TbTimestampTests.ShowZone_ShouldShowTheAppsAbbreviation_WhenItGivesOneForTheZone`, `TbTimestampTests.ShowZone_ShouldShowUtcAlone_WhenTheZoneIsUtc`
- [x] The chat, the timeline and the data grid's `DateTimeOffset` columns follow the same zone, the grid in its date filter and its Excel export too. `TbTimestampTests.Chat_ShouldShowTimesInTheCascadedTimeZone_WhenOneIsCascaded`, `TbTimestampTests.TimelineItem_ShouldShowItsTimeInTheCascadedTimeZone_WhenOneIsCascaded`, `TbTimestampTests.GridColumn_ShouldShowADateTimeOffsetInTheCascadedTimeZone_WhenOneIsCascaded`, `TbTimestampTests.GridColumn_ShouldFilterAndExportADateTimeOffsetByItsShownDay_WhenAZoneIsCascaded`
- [x] The browser service reads the browser's zone. `TimestampTests.BrowserService_ShouldReadTheBrowsersTimeZone_WhenAsked`

## Before and after

- [x] `FutureText` shows while the moment is ahead and switches to `PastText` as it passes. `TbTimestampTests.TbTimestamp_ShouldUseFutureText_WhenValueIsAhead`, `TbTimestampTests.TbTimestamp_ShouldSwitchToPastText_WhenTimePasses`

## Relative

- [x] It says how far the moment is from now, in minutes, hours, days, months or years, or "just now". `TbTimestampTests.TbTimestamp_ShouldShowRelativeText_WhenGivenRelative`
- [x] It keeps up as time passes, on one timer for every relative timestamp on the page, which ticks those with the same wait together and leaves a timestamp that went away. `TbTimestampTests.Relative_ShouldKeepUp_WhenTimePasses`, `TimestampTickerTests.Wait_ShouldFireTheEarlierOwnerFirst_WhenItRegistersSecond`, `TimestampTickerTests.Stop_ShouldKeepTheTickFromFiring_WhenCalledBeforeItIsDue`, `TimestampTickerTests.Fire_ShouldCallEveryDueOwnerOnce_WhenTwoAreDueTogether`

## Not applicable

- Announcing the switch from future to past, or a relative timestamp's new words: the text changes in place, as a clock's does, without a live region.
