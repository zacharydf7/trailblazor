# Countdown

Pattern: none; it is a sentence and buttons, announced through the app's announcer rather than a live region

Familiar apps: a "You'll be taken to sign in in 10 seconds" page after a password reset, a bank's "Your session is about to end" warning with "Stay signed in"

## Keyboard

Nothing of its own: its buttons are native buttons in the tab order.

## Pointer

- [x] It pauses while the pointer is over it, and goes on once the pointer leaves. `TbCountdownTests.TbCountdown_ShouldPause_WhenFocusOrThePointerIsInside`, `CountdownTests.TbCountdown_ShouldPause_WhileThePointerOrFocusIsInside`
- [x] Go now acts at once, going to `Href` and running `OnElapsed` once, with `CountdownEnd.GoNow`. `TbCountdownTests.TbCountdown_ShouldNavigateAtOnce_WhenGoNowIsPressed`, `TbCountdownTests.TbCountdown_ShouldRunOnElapsedOnce_WhenGoNowIsPressed`
- [x] Extend runs `OnExtend`, starts the time over and announces it. `TbCountdownTests.TbCountdown_ShouldRestart_WhenExtended`

## Touch

- [ ] A touch pauses it as the pointer does, until the finger lifts. #332

## Focus

- [x] It pauses while focus is inside it, and goes on once focus leaves. `TbCountdownTests.TbCountdown_ShouldPause_WhenFocusOrThePointerIsInside`, `CountdownTests.TbCountdown_ShouldPause_WhileThePointerOrFocusIsInside`
- [x] Stay stops it for good, and focus moves to the countdown, which then reads "Stayed on this page". `TbCountdownTests.TbCountdown_ShouldStop_WhenStayIsPressed`, `CountdownScenarioTests.ResetRedirect_ShouldStop_WhenTheUserStays`

## Timing

- [x] When the time runs out it goes to `Href`, and `OnElapsed` gets `CountdownEnd.TimeRanOut`; an `OnElapsed` that throws then reaches the renderer. `TbCountdownTests.TbCountdown_ShouldNavigate_WhenTimeRunsOut`, `TbCountdownTests.OnElapsed_ShouldGetTimeRanOut_WhenTimeRunsOut`, `TbCountdownTests.OnElapsed_ShouldHandTheErrorToTheRenderer_WhenItThrowsAsTimeRunsOut`
- [x] The time left shows in seconds under a minute and in whole minutes, rounded up, from a minute on. `TbCountdownTests.Text_ShouldShowSecondsUnderAMinuteAndRoundUpToMinutes_WhenGivenTimeLeft`, `TbCountdownTests.TbCountdown_ShouldShowTheNewMinute_WhenAMinuteBoundaryPasses`
- [x] It announces its sentence once as it starts, and a countdown longer than 40 seconds once more with 20 seconds left. `TbCountdownTests.TbCountdown_ShouldAnnounceOnce_WhenStarted`, `TbCountdownTests.TbCountdown_ShouldAnnounceAgainNearTheEnd_WhenLong`
- [x] It can't render without a way to stop or extend it, as WCAG 2.2.1 asks. `TbCountdownTests.Render_ShouldThrow_WhenARequiredSettingIsMissing`, `TbCountdownTests.ShowStay_ShouldLeaveGoNowAndExtend_WhenFalse`
- [x] On a static page it shows the full time and Go now as a link, and never acts. `TbCountdownTests.TbCountdown_ShouldRenderLinkWithoutTimer_WhenStatic`
- [x] Its timer stops when it goes away. `DisposeContractTests.Dispose_ShouldLeaveNothingRunning_WhenTheComponentIsDisposed`
- [x] With a `TelemetryName` it counts what the user did. `TbCountdownTests.Telemetry_ShouldCountWhatTheUserDid_WhenNamed`

## Not applicable

- Reduced motion: nothing moves; only its text changes.
