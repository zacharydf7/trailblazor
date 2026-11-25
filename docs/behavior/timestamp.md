# Timestamp

Pattern: none; it is a `time` element with the moment in its `datetime`

Familiar apps: the "Expires Dec 4, 2026" line on a membership page, the sent time on an email in an inbox

## Keyboard

Nothing of its own: it is text, with nothing to press.

## Pointer

Nothing of its own: it is text.

## Touch

Nothing of its own: it is text.

## Focus

- [x] A missing value shows a dash, which screen readers hear as "Not set". `TbTimestampTests.Render_ShouldShowADashReadAsNotSet_WhenTheValueIsNull`

## Time zones

- [x] It shows its moment in its own zone, else a cascaded one, else the app's, else the clock's local zone. `TbTimestampTests.TbTimestamp_ShouldPreferOwnTimeZone_WhenGivenOne`, `TbTimestampTests.TbTimestamp_ShouldShowValueInTheCascadedTimeZone_WhenOneIsCascaded`, `TbTimestampTests.TbTimestamp_ShouldShowValueInTheAppsTimeZone_WhenNothingCloserSetsOne`
- [x] Its `datetime` holds the moment in ISO 8601 with at most three digits of a second. `TbTimestampTests.TbTimestamp_ShouldWriteIsoDateTimeAttribute_WhenCalled`
- [x] `ShowZone` adds the UTC offset, or the app's abbreviation for standard or daylight time, found by IANA or Windows id. `TbTimestampTests.TbTimestamp_ShouldShowOffset_WhenGivenShowZone`, `TbTimestampTests.ShowZone_ShouldShowTheAppsAbbreviation_WhenItGivesOneForTheZone`, `TbTimestampTests.ShowZone_ShouldShowUtcAlone_WhenTheZoneIsUtc`
- [x] The chat, the timeline and the data grid's `DateTimeOffset` columns follow the same zone, the grid in its date filter and its Excel export too. `TbTimestampTests.Chat_ShouldShowTimesInTheCascadedTimeZone_WhenOneIsCascaded`, `TbTimestampTests.TimelineItem_ShouldShowItsTimeInTheCascadedTimeZone_WhenOneIsCascaded`, `TbTimestampTests.GridColumn_ShouldShowADateTimeOffsetInTheCascadedTimeZone_WhenOneIsCascaded`, `TbTimestampTests.GridColumn_ShouldFilterAndExportADateTimeOffsetByItsShownDay_WhenAZoneIsCascaded`
- [x] The browser service reads the browser's zone. `TimestampTests.BrowserService_ShouldReadTheBrowsersTimeZone_WhenAsked`

## Before and after

- [x] `FutureText` shows while the moment is ahead and switches to `PastText` as it passes. `TbTimestampTests.TbTimestamp_ShouldUseFutureText_WhenValueIsAhead`, `TbTimestampTests.TbTimestamp_ShouldSwitchToPastText_WhenTimePasses`

## Not applicable

- Announcing the switch from future to past: the text changes in place, as a clock's does, without a live region.
