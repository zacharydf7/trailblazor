# Toast

Pattern: [Alert](https://www.w3.org/WAI/ARIA/apg/patterns/alert/)

Familiar apps: Gmail's Undo bar, Android snackbars, Windows and macOS notifications

## Keyboard

- [x] F8 moves focus to the newest toast's first button. `ToastTests.Toaster_ShouldTakeFocusOnF8AndGiveItBackOnEscape_WhenAToastIsOpen`
- [x] Escape sends focus back to where it was and leaves the toast open. `ToastTests.Toaster_ShouldTakeFocusOnF8AndGiveItBackOnEscape_WhenAToastIsOpen`
- [x] F8 with no toast open, or with a modifier key, is left to the browser. `toaster.js`
- [x] Enter on Dismiss closes the toast and gives focus back. `ToastTests.Toaster_ShouldGiveFocusBack_WhenTheFocusedToastIsDismissed`
- [x] In a modal, F8 and Escape stay in the dialog and don't close it. `ToastTests.Toaster_ShouldAnnounceAndGiveFocusBackInsideTheDialog_WhenShownFromAModal`
- [x] Escape on a toast reached without F8 hands focus to the dialog around it. `ToastTests.Toaster_ShouldHandFocusToTheDialog_WhenEscapeIsPressedOnAToastReachedWithoutF8`

## Pointer

- [x] Dismiss closes the toast, which fades out of reach. `ToastTests.Toast_ShouldFadeOutOfReachBeforeItGoes_WhenClosed`
- [x] An action runs and closes the toast. `ToastServiceTests.Action_ShouldRunAndCloseTheToast_WhenClicked`
- [x] Resting on a toast pauses its timer, and leaving resumes it. `ToastTests.Toast_ShouldStayOpen_WhenThePointerRestsOnIt`
- [x] Resting on a toast pauses the others too, so a neighbor timing out doesn't move it from under the pointer. `ToastTests.Toasts_ShouldAllStayOpen_WhenThePointerRestsOnOne`, `ToastServiceTests.Toast_ShouldWaitToStartItsTime_WhenShownWhileTheToasterIsPaused`, `ToastServiceTests.Toast_ShouldStayPaused_WhenThePointerCrossesFromOneToastToAnother`
- [x] A toast shown from a modal can be clicked above it. `ToastTests.Toast_ShouldBeUsableAboveAModalDialog_WhenShownFromIt`
- [x] The gaps between toasts don't block the page. `trailblazor.css`
- [x] A danger toast stays until dismissed. `ToastServiceTests.Toast_ShouldStayUntilDismissedAndBeAnnouncedAssertively_WhenGivenAnError`
- [x] A toast with an action stays until dismissed, so the action can't time out unseen. `ToastServiceTests.Toast_ShouldStayOpenAndSayItsActionAndKey_WhenGivenAnAction`
- [x] Toasts past the maximum wait their turn. `ToastServiceTests.Show_ShouldQueueToastsBeyondTheMaximum_WhenMoreAreOpen`
- [x] A toast with the same key updates in place and gets its full time again. `ToastServiceTests.Show_ShouldUpdateTheOpenToastAndRestartItsTimer_WhenGivenTheKeyOfOne`
- [x] Dismissing a toast with the mouse leaves focus alone, so the other toasts' timers keep running. `ToastTests.Toasts_ShouldKeepTiming_WhenAnotherIsDismissedWithTheMouse`

## Touch

- [x] A tap on Dismiss or an action works as a click. `ToastFrame.cs`
- [ ] A swipe dismisses a toast. #150
- [x] A tap on a toast doesn't hold the timers, since a finger leaves the toast as it lifts. `ToastTests.Toast_ShouldStillCloseByItself_WhenTapped`

## Focus

- [x] A toast never takes focus by appearing. `ToastTests.Toast_ShouldAppearAnnouncedWithoutTakingFocusAndCloseByItself_WhenShown`
- [x] Focus in a toast pauses its timer. `ToastServiceTests.Toast_ShouldKeepItsRemainingTime_WhenTheToasterPausesOrFocusRestsOnIt`
- [x] A toast's time doesn't run out while its tab is hidden. `ToastTests.Toast_ShouldStayOpen_WhileThePageIsHidden`

## Not applicable

- Escape dismissing a toast: it only sends focus back, so it can't close a message by accident.
- F8 moving on to older toasts: Tab goes through the toaster from the newest one's button.
- A click on the toast's body doing anything: only its buttons act.
