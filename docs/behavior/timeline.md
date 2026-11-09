# Timeline

Pattern: none; a list of events, with nothing to operate

Familiar apps: the tracking history of a parcel, and the activity feed of an issue tracker

## Keyboard

Nothing of its own. A timeline takes no focus; links or buttons in an event's content keep their own keys.

## Pointer

- [x] A line joins each event's dot to the next, and ends at the last event. `TimelineTests.Timeline_ShouldJoinEachDotToTheNextAndEndAtTheLast_WhenRendered`
- [x] An event shows its time in the current culture, its title, its content, and a dot in its tone or with its icon. `TbTimelineTests.Render_ShouldShowTheTimeInTheCurrentCultureForPeopleAndInIsoForMachines_WhenGivenATime`, `TbTimelineTests.Render_ShouldColorTheDotAndShowItsIcon_WhenGivenAToneAndAnIcon`
- [x] In forced-colors mode, the dots and the line keep the system's text color. `TimelineTests.Timeline_ShouldKeepItsDotsAndLine_WhenColorsAreForced`

## Touch

Nothing of its own.

## Focus

- [x] Screen readers hear a list of events in order, each by its time, title and content, without the dots. `TbTimelineTests.Render_ShouldListTheEventsInOrder_WhenGivenEvents`
- [x] A time is in a `time` element whose `datetime` is ISO 8601, whatever the culture. `TbTimelineTests.Render_ShouldShowTheTimeInTheCurrentCultureForPeopleAndInIsoForMachines_WhenGivenATime`

## Not applicable

- Two columns of events, alternating sides: a single column reads in order on every screen.
- Loading older events as people scroll: the app renders the events it has, and adds a button for more.
