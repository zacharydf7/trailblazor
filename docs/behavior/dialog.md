# Dialog

Pattern: [Dialog (modal)](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/), [Alert dialog](https://www.w3.org/WAI/ARIA/apg/patterns/alertdialog/)

Familiar apps: Windows and macOS message boxes and tool windows, Google Docs' dialogs

## Keyboard

- [x] Tab never reaches the page behind a modal. `DialogTests.Dialog_ShouldFocusTheFirstFieldAndKeepFocusOffThePage_WhenModal`
- [x] Escape cancels the dialog. `DialogTests.Dialog_ShouldCancelAndRefocusTheOpener_WhenEscapeIsPressed`
- [x] With `DismissOnEscape` off, Escape does nothing however often it's pressed. `DialogTests.Dialog_ShouldStayOpen_WhenEscapeIsPressedTwiceOrTheBackdropIsClickedWhileLocked`
- [x] Escape closes only the newest of nested dialogs. `DialogTests.Dialog_ShouldCloseOnlyTheInnerDialog_WhenEscapeIsPressedInANestedOne`
- [x] Escape closes a menu inside first. `DialogTests.Dialog_ShouldLetEscapeCloseAMenuFirst_WhenAMenuIsOpenInside`
- [x] Escape closes an autocomplete's list inside first. `DialogTests.Dialog_ShouldCloseTheSuggestionsFirstAndCancelNext_WhenEscapeIsPressedWithThemOpen`
- [x] Escape goes to the non-modal dialog that has focus, even an older one. `DialogStateTests.Dialog_ShouldCloseTheModelessDialogWithFocus_WhenEscapeIsPressedInAnOlderOne`
- [x] Escape with focus on the page leaves a non-modal dialog open. `dialog.js`
- [x] Arrow keys on the Move button move the dialog. `DialogMoveAndResizeTests.Dialog_ShouldMoveAStepPerArrowKey_WhenItsMoveButtonHasFocus`
- [x] Arrow keys on the grip resize it. `DialogMoveAndResizeTests.Dialog_ShouldGrowAStepPerArrowKey_WhenTheGripHasFocus`

## Pointer

- [x] A click on the backdrop cancels a modal. `DialogTests.Dialog_ShouldCancel_WhenTheBackdropIsClicked`
- [x] A drag from a field that ends over the backdrop leaves the dialog open. `DialogTests.Dialog_ShouldStayOpen_WhenADragFromAFieldEndsOverTheBackdrop`
- [x] The close button cancels. `DialogServiceTests.CloseButton_ShouldCancelTheDialog_WhenClicked`
- [x] Escape, the backdrop and the close button can ask before discarding work. `DialogExtrasTests.Dialog_ShouldAskBeforeDiscardingTheNewName_WhenEscapeTheBackdropCancelOrTheCloseButtonLeaveIt`
- [x] A click outside a `ConfirmAsync` or `AlertAsync` dialog doesn't answer it. `DialogServiceTests.ConfirmAndAlert_ShouldStayOpenOnABackdropClickButTakeEscape_WhenTheAppKeepsTheDefaults`
- [x] The page behind a modal can't scroll. `DialogTests.Dialog_ShouldLockPageScroll_WhenModal`
- [x] A modal that takes away the page's scrollbar makes up for its width, so the page holds still and the backdrop reaches the window's edge. `ScrollLockTests.Modal_ShouldHoldThePageStillAndCoverTheWindow_WhenItTakesAwayAScrollbar`, `ScrollLockTests.Modal_ShouldHoldThePageStill_WhenAShortPageForcesItsScrollbar`, `ScrollLockTests.Modal_ShouldHoldThePageInPlace_WhenThePageRunsRightToLeft`, `ScrollLockTests.Modal_ShouldHoldThePageStill_WhenThePageAlreadyKeepsAGutter`, `ScrollLockTests.Page_ShouldStillKnowItsScrollbar_WhenAnEnhancedNavigationReplacesThePage`, `ScrollLockTests.Modal_ShouldHoldThePageStill_WhenAResetSetsTheRootsPaddingAndSizing`, `ScrollLockTests.Modal_ShouldHoldThePageStill_WhenContentOverflowsABodySizedToTheWindow`, `ScrollLockTests.Modal_ShouldHoldThePageStill_WhenTheWindowShrinkingGaveThePageAScrollbar`, `ScrollLockTests.Modal_ShouldKeepMakingUpForTheScrollbar_WhenTheWindowResizesWhileItsOpen`
- [x] A page with no scrollbar gets no extra room. `ScrollLockTests.Modal_ShouldAddNoRoom_WhenThePageHasNoScrollbar`, `ScrollLockTests.Modal_ShouldAddNoRoom_WhenThePageClipsItsOwnOverflow`, `ScrollLockTests.Modal_ShouldAddNoRoom_WhenThePageClipsItselfAfterHavingAScrollbar`, `ScrollLockTests.Modal_ShouldAddNoRoom_WhenAWidePageOnAPhoneReportsAWiderWindow`, `ScrollLockTests.Modal_ShouldAddNoRoom_WhenThePageHasShrunkSinceItHadAScrollbar`
- [x] A non-modal dialog leaves the scrollbar alone. `ScrollLockTests.ModelessDialog_ShouldLeaveTheScrollbarAlone_WhenThePageScrolls`
- [x] Before the page is interactive, a `TbDialog` with an `id` opens from a button with `commandfor` and `command="show-modal"`, and its close button closes it. `DialogTests.Dialog_ShouldOpenFromACommandAndCloseFromItsButton_WhenThePageIsStatic`, `TbDialogTests.Render_ShouldRenderTheDialogClosedForItsCloseButtonToClose_WhenThePageIsStaticAndItHasAnId`
- [ ] In Firefox and Safari with classic scrollbars, the page holds still too. UNCONFIRMED
- [x] An app's own `position: fixed` element holds still if it adds `--tb-scroll-lock-offset` to its right offset. `ScrollLockTests.FixedElement_ShouldHoldStill_WhenItAddsTheLockOffset`
- [ ] On a right-to-left page in a browser that puts the scrollbar on the left, the page holds still. UNCONFIRMED
- [ ] A page with its own padding, width, minimum width or a both-edges gutter on the root holds still when a modal takes away the scrollbar. UNCONFIRMED
- [ ] A page whose body scrolls under `html, body { height: 100% }` holds still when a modal takes away the scrollbar. UNCONFIRMED
- [x] The page stays usable behind a non-modal dialog. `DialogTests.Dialog_ShouldLeaveThePageUsable_WhenNotModal`
- [x] A click or focus in a non-modal dialog behind another brings it to the front. `DialogWindowsTests.Window_ShouldComeToTheFront_WhenPressed`, `DialogWindowsTests.Window_ShouldComeToTheFront_WhenFocusMovesIntoIt`, `DialogWindowsTests.Window_ShouldComeToTheFront_WhenPressedOnThePageLayer`
- [x] Coming to the front keeps focus where it is and raises no `OnFocus` or `OnBlur`. `DialogWindowsTests.Window_ShouldKeepFocusAndRaiseNoFieldEvents_WhenWindowsSwapPlaces`, `DialogWindowsTests.Window_ShouldComeToTheFrontAndKeepFocusElsewhere_WhenItsGripIsDragged`, `DialogWindowsTests.Window_ShouldFocusTheFieldOnce_WhenClickingItBringsItForward`
- [x] The press that brings a dialog forward still reaches what it landed on. `DialogWindowsTests.Window_ShouldStillTakeTheClick_WhenTheClickBringsItForward`
- [x] A dialog nested in the one coming to the front stays above it, and so do a tooltip open from it and a toast. `DialogWindowsTests.Window_ShouldKeepItsNestedWindowAboveIt_WhenPressed`, `DialogWindowsTests.Tooltip_ShouldStayAboveItsWindow_WhenTheWindowComesToTheFront`, `DialogWindowsTests.Toast_ShouldStayAboveAWindow_WhenTheWindowComesToTheFront`
- [x] A dialog behind another waits to come forward while a popover open from it is showing, so the popover isn't covered. `DialogWindowsTests.Popover_ShouldStayOnTop_WhenOpenFromAWindowBehindAnother`
- [x] A press or focus in the dialog already in front doesn't hide and re-show it. `DialogWindowsTests.Window_ShouldNotBeShownAgain_WhenItIsAlreadyInFront`
- [x] A dialog in the top layer with a modal open from it stays where it is, so the modal isn't covered. `DialogWindowsTests.Window_ShouldStayBehind_WhenAModalOpensInIt`
- [x] A press on a modal leaves a dialog opened from it above it. `DialogWindowsTests.Modal_ShouldStayBelowALaterWindow_WhenPressedOnThePageLayer`
- [x] Focus moving into a dialog nested in one behind another brings them both forward. `DialogWindowsTests.Window_ShouldComeToTheFront_WhenFocusMovesIntoAWindowNestedInIt`
- [x] On the page layer, a modal or non-modal dialog opened later stays above one that came to the front, and a modal restored after another dialog opened draws above that dialog. `DialogWindowsTests.Modal_ShouldOpenAboveARaisedWindow_WhenOnThePageLayer`, `DialogWindowsTests.Window_ShouldOpenAboveARaisedWindow_WhenOnThePageLayer`, `DialogWindowsTests.Modal_ShouldDrawAboveALaterWindow_WhenRestoredOnThePageLayer`
- [x] Dragging the header moves the dialog, and it stays inside the window. `DialogMoveAndResizeTests.Dialog_ShouldStayInsideTheWindow_WhenDraggedPastItsEdges`
- [x] Dragging the grip resizes it from its bottom-end corner. `DialogMoveAndResizeTests.Dialog_ShouldGrowFromItsBottomEndCorner_WhenTheGripIsDragged`
- [x] Minimize collapses it to its header in the corner. `DialogTests.Dialog_ShouldCollapseToItsHeaderInTheCornerAndKeepFocus_WhenMinimized`
- [x] Maximize fills the window, and Restore brings it back. `DialogTests.Dialog_ShouldFillTheWindowAndComeBack_WhenMaximizedAndRestored`
- [x] A long dialog keeps its header in view and scrolls its body. `DialogExtrasTests.Dialog_ShouldScrollItsBodyAndKeepItsHeaderInView_WhenItsContentIsLong`

## Touch

- [x] A touch drag moves the dialog, through pointer events and `touch-action: none` on the header. `DialogMoveAndResizeTests.MoveMenu_ShouldOpenWithEnter_WhenATouchDragOnTheMoveButtonEndedWithoutAClick`
- [x] A tap on the backdrop cancels a modal, as a click does. `dialog.js`
- [ ] A back gesture on a phone cancels the dialog as Escape does, asking first if the dialog asks on Escape. UNCONFIRMED
- [ ] The page behind a modal doesn't scroll under a finger. UNCONFIRMED

## Focus

- [x] Focus starts on the first field. `DialogTests.Dialog_ShouldFocusTheFirstFieldAndKeepFocusOffThePage_WhenModal`
- [x] An element marked `autofocus` starts with focus. `DialogTests.Dialog_ShouldFocusTheFieldMarkedAutofocus_WhenOneIsInside`
- [x] `InitialFocus` set to the dialog starts on the dialog itself. `DialogTests.Dialog_ShouldFocusItself_WhenItsInitialFocusIsTheDialog`
- [x] A confirmation starts on its first button, Cancel. `DialogExtrasTests.Dialog_ShouldStartOnItsFirstButton_WhenConfirming`
- [x] With nothing marked, focus starts where Tab would: on a group's checked radio, and never on a hidden or inert control. `RadioStopsTests.Dialog_ShouldStartOnTheCheckedRadio_WhenARadioGroupComesFirst`, `RadioStopsTests.Dialog_ShouldStartOnTheFirstControlTabReaches_WhenHiddenAndInertOnesComeFirst`
- [x] Closing returns focus to the opener. `DialogTests.Dialog_ShouldReturnItsValueAndRefocusTheOpener_WhenSaved`
- [x] When a click opens or closes the dialog, the button that gets focus (the dialog's first button, or the opener on closing) shows no focus ring; when a key does, it shows one. `DialogExtrasTests.Dialog_ShouldRingItsFirstButtonOnlyForTheKeyboard_WhenItOpens`, `DialogExtrasTests.Dialog_ShouldRingTheOpenerOnlyForTheKeyboard_WhenAMenuOpenedDialogCloses`
- [x] Focus moved to the page while a modal fades out stays there. `DialogTests.Dialog_ShouldLetThePageTakeInputAndKeepItsFocus_WhenAModalFadesOut`
- [x] Minimizing and restoring keep focus on the button pressed. `DialogStateTests.Dialog_ShouldLetThePageBeUsedUntilRestored_WhenAModalIsMinimized`
- [x] Switching between modal and non-modal keeps focus. `DialogStateTests.Dialog_ShouldSwitchBetweenModalAndModelessAndKeepFocus_WhenItsContentAsks`

## Not applicable

- Enter running a default button, as in an OS dialog: that comes from a form in the dialog's content.
- Tab wrapping strictly inside a modal: it follows the browser's native modal and may leave for the browser's own controls.
- Moving or resizing a modal: it already blocks the page, so there's nothing to uncover.
- Double-clicking the header to maximize, or Escape canceling a drag, as on Windows: the header buttons maximize, and Escape dismisses as usual.
- Choosing where focus goes when the opener is gone: only the app knows, and focus falls to the page.
