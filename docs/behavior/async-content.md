# Async content

Pattern: none; it is a region with a polite `status` for its loading words and `aria-busy` while a reload or retry runs

Familiar apps: a team list in a chat app that shows a spinner and then the teams, a dashboard card that says it couldn't load with a Retry button

## Keyboard

Nothing of its own: it handles no keys, and its Retry button is a native button in the tab order.

## Pointer

- [x] Retry loads again, and the failure stays while it does, its button busy. `TbAsyncContentTests.TbAsyncContent_ShouldKeepRetryFocused_WhenRetryFails`, `TbAsyncContentTests.Retry_ShouldShowTheContent_WhenTheLoadWorksTheSecondTime`

## Touch

Nothing of its own: it holds no gestures, only its Retry button.

## Focus

- [x] A failed load is announced politely and shows Retry, and `OnLoadFailed` gets the exception. `TbAsyncContentTests.TbAsyncContent_ShouldAnnounceFailure_WhenLoadThrows`
- [x] Focus stays on Retry while a retry fails, and moves to the region once a retry loads the content. `AsyncContentTests.Retry_ShouldKeepFocusUntilTheContentLoads_WhenPressedWithTheKeyboard`
- [x] `LoadingContent` is hidden from screen readers, which hear the loading words in the status instead. `TbAsyncContent.razor`

## Loading

- [x] A load that finishes within the show delay shows no spinner; a spinner that shows stays for the minimum. `TbAsyncContentTests.TbAsyncContent_ShouldShowNothing_WhenLoadFinishesWithinShowDelay`, `TbAsyncContentTests.TbAsyncContent_ShouldKeepSpinnerForMinimum_WhenShown`
- [x] A reload keeps the content in view, busy, with a bar once it's slow; a failed reload keeps the content under the failure. `TbAsyncContentTests.TbAsyncContent_ShouldKeepContent_WhenReloading`, `TbAsyncContentTests.Reload_ShouldKeepTheContentUnderTheFailure_WhenAReloadFails`
- [x] Empty and no-match content show from the app's own functions, asked once the load ends, whatever shape `Load` has. `TbAsyncContentTests.TbAsyncContent_ShouldShowEmptyContent_WhenLoadedWithNoItems`, `TbAsyncContentTests.TbAsyncContent_ShouldShowNoMatchesContent_WhenGivenHasNoMatches`, `TbAsyncContentTests.IsEmpty_ShouldReadWhatTheLoadFilled_WhenLoadIsALambdaOverALocal`
- [x] A load is canceled when the region goes away or loads again, and whatever a replaced load ends with is ignored, even one that ignored its token. `TbAsyncContentTests.TbAsyncContent_ShouldCancelLoad_WhenDisposed`, `TbAsyncContentTests.TbAsyncContent_ShouldIgnoreResult_WhenReloadedDuringLoad`, `TbAsyncContentTests.Reload_ShouldIgnoreTheReplacedLoad_WhenItFailsLateHavingIgnoredItsToken`, `TbAsyncContentTests.LoadKey_ShouldLoadAgain_WhenItChanges`, `TbAsyncContentTests.Reload_ShouldDoNothing_WhenTheRegionIsGone`
- [x] A load that times out on its own fails, rather than counting as loaded. `TbAsyncContentTests.Load_ShouldShowTheFailure_WhenTheLoadTimesOutOnItsOwn`
- [x] `Labels` replaces the sentences, and `FailedContent` replaces the message and its Retry. `TbAsyncContentTests.Labels_ShouldReplaceTheSentences_WhenGiven`, `TbAsyncContentTests.FailedContent_ShouldTakeTheMessagesPlace_WhenGiven`
- [x] On a static page it loads as the page renders and shows no Retry; while a page prerenders it waits, so the load runs once. `TbAsyncContentTests.Render_ShouldLoadAndOfferNoRetry_WhenThePageIsStatic`, `TbAsyncContent.cs`
- [x] With a `TelemetryName` it records each load's time, without the spinner's hold, and counts failures and retries. `TbAsyncContentTests.Telemetry_ShouldCountTheLoadsFailuresAndRetries_WhenNamed`
- [x] With a `TelemetryName`, each `Load` call runs in a `TbAsyncContent load` span from the `Trailblazor` source. `TelemetrySpanTests.AsyncContent_ShouldTraceItsLoad_WhenSomeoneListens`
- [x] With reduced motion, nothing on it moves: its transitions and animations take no time, apart from an animation that only fades. `ReducedMotionTests.Example_ShouldHaveNoMotion_WhenMotionIsReduced`, `AccessibilityStatusTests.Stylesheet_ShouldOverrideEveryOtherDuration_WhenMotionIsReduced`

## Not applicable

- Moving focus to the content once it loads: focus stays where the user left it, except after a Retry that loaded, whose button is gone.
