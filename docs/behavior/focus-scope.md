# Focus scope

Pattern: none of its own; it gives a custom panel the focus rules of a [modal dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/)

Familiar apps: how focus moves in OS dialogs and app side panels

## Keyboard

- [x] With `Trapped`, Tab on the last control wraps to the first. `FocusScopeTests.Tab_ShouldWrapToFirstTabbable_WhenTrappedAndFocusIsOnLast`
- [x] With `Trapped`, Shift+Tab on the first control wraps to the last. `FocusScopeTests.ShiftTab_ShouldWrapToLastTabbable_WhenTrappedAndFocusIsOnFirst`
- [x] Without `Trapped`, Tab moves on to the rest of the page. `FocusScopeTests.Tab_ShouldLeaveScope_WhenNotTrapped`
- [x] When trapped scopes nest, Tab stays in the newest. `FocusScopeTests.Tab_ShouldWrapInsideInnerScope_WhenTrappedScopesAreNested`
- [x] The outer scope traps again once the inner one goes. `FocusScopeTests.Tab_ShouldWrapInsideOuterScope_WhenNestedInnerScopeIsDisposed`
- [x] A trap stands down while its scope can't be used: inside something inert, as when a newer blocking scope holds it, or behind a modal dialog. Tab then moves in what is in front of it. `BlockingScopeTests.Scope_ShouldHoldAnOlderScopeToo_WhenASecondOneBlocksAfterIt`, `BlockingScopeTests.Scope_ShouldLeaveADialogItsContentOpensUsable_WhenItBlocks`
- [x] Tab stays inside after the focused control is removed. `FocusScopeTests.Tab_ShouldStayInsideScope_WhenTrappedAndFocusedElementIsRemoved`
- [x] Tab goes nowhere in a trapped scope with no controls. `focus-scope.js`
- [x] Tab wraps when the scope's first or last stop is a radio group, whichever radio is checked or focused. `RadioStopsTests.FocusScope_ShouldWrapBothWays_WhenTheCheckedRadioIsNotTheLastInTheGroupThatEndsIt`, `RadioStopsTests.FocusScope_ShouldWrapBothWays_WhenTheCheckedRadioIsNotTheFirstInTheGroupThatStartsIt`, `RadioStopsTests.FocusScope_ShouldWrap_WhenFocusIsAnywhereInAnUncheckedGroupThatEndsIt`, `RadioStopsTests.FocusScope_ShouldWrapBackward_WhenFocusIsAnywhereInAnUncheckedGroupThatStartsIt`
- [x] Radios in different forms stay separate stops. `RadioStopsTests.FocusScope_ShouldTabToTheNextGroup_WhenTwoFormsShareARadioName`
- [x] Radios with no name stay separate stops in Chromium and Firefox. WebKit's own Tab skips the second, so forward Tab doesn't reach it there. Library radios always have a name. `RadioStopsTests.FocusScope_ShouldTabToTheNextRadio_WhenRadiosHaveNoName`
- [x] Tab wraps past a control hidden by `visibility: hidden`, which it can't reach. `RadioStopsTests.FocusScope_ShouldWrap_WhenAControlHiddenByVisibilityEndsIt`
- [x] An editable region is a stop unless it sets `tabindex="-1"`; an editable element inside it isn't. Tab wraps both ways when an editable region ends a trapped scope, and a dialog starts on one that comes first. `EditableStopsTests.FocusScope_ShouldWrapBothWays_WhenAnEditableRegionEndsIt`, `EditableStopsTests.Dialog_ShouldStartOnTheEditableRegion_WhenItComesFirst`
- [x] A player with controls is a stop, so Shift+Tab wraps to one that ends a trapped scope. `EditableStopsTests.FocusScope_ShouldWrapBackwardToThePlayer_WhenAPlayerWithControlsEndsIt`
- [x] Tab goes through a player's own controls before wrapping when the player ends a trapped scope, and Shift+Tab does the same when it starts one, even when nothing outside the scope can take focus. Focus moved outside the scope from those controls comes back to the player. WebKit gives a player no controls to Tab through. `EditableStopsTests.FocusScope_ShouldTabThroughThePlayersControlsBeforeWrapping_WhenAPlayerEndsIt`, `EditableStopsTests.FocusScope_ShouldShiftTabThroughThePlayersControlsBeforeWrapping_WhenAPlayerStartsIt`, `EditableStopsTests.Focus_ShouldReturnToThePlayer_WhenMovedOutsideFromThePlayersControls`
- [x] A `<details>` summary is a stop, so Shift+Tab reaches one that ends a trapped scope, and Tab from it wraps. `EditableStopsTests.FocusScope_ShouldReachAndWrapPastADetailsSummary_WhenOneEndsIt`

## Pointer

- [x] Focus moved to a control outside a trap, by a click or a script, comes back to where it was. `FocusScopeTests.Focus_ShouldReturnToLastFocusedInside_WhenTrappedAndFocusMovesOutside`
- [x] After a click on empty page, the next Tab goes back into a trap. `focus-scope.js`

## Touch

Nothing of its own beyond the keyboard and focus events it listens to. With `Blocking`, a tap outside does nothing, because what is outside is inert.

## Focus

- [x] Focus moves to the first control when the scope appears. `FocusScopeTests.CreateAsync_ShouldFocusFirstTabbable_WhenAutoFocusIsOn`
- [x] A field marked to take focus takes it instead. `FocusScopeExampleTests.FocusScope_ShouldFocusTheMarkedField_WhenAFieldHasAutoFocus`
- [x] Focus already inside stays put. `focus-scope.js`
- [x] A scope with no controls takes focus itself. `focus-scope.js`
- [x] `AutoFocus="false"` leaves focus alone. `FocusScopeTests.CreateAsync_ShouldLeaveFocusAlone_WhenAutoFocusIsOff`
- [x] Focus returns to where it was when the scope goes. `FocusScopeTests.DisposeAsync_ShouldRestoreFocusToOpener_WhenRestoreFocusIsOn`
- [x] Focus the user moved elsewhere stays there. `FocusScopeTests.DisposeAsync_ShouldLeaveFocusAlone_WhenFocusMovedOutsideScope`
- [x] `RestoreFocus="false"` doesn't give it back. `FocusScopeTests.DisposeAsync_ShouldLeaveFocusAlone_WhenRestoreFocusIsOff`
- [x] Focus that leaves after its control was removed returns to the first control. `FocusScopeTests.Focus_ShouldReturnToFirstTabbable_WhenTrappedAndLastFocusedElementWasRemoved`

## Blocking

- [x] With `Blocking`, everything beside the scope and beside each of its ancestors is inert while the scope is there: a press outside does nothing, Tab never lands outside, and what is held is marked `data-tb-blocked` and dims. When the scope goes, nothing is left inert and the focus is back where it was. `BlockingScopeTests.Scope_ShouldHoldEverythingOutsideItAndGiveItBack_WhenItBlocksAndThenGoes`, `TbFocusScopeTests.Blocking_ShouldHoldWhatIsOutsideOnceTheScopeHasStartedAndFreeItFirst_WhenSet`, `TbFocusScopeTests.Blocking_ShouldLeaveThePageAsItIs_WhenNotSet`
- [x] A dialog that the scope's content opens is usable, and the scope goes on holding the page once it has gone. `BlockingScopeTests.Scope_ShouldLeaveADialogItsContentOpensUsable_WhenItBlocks`
- [x] Toasts, the announcer, Blazor's own reconnect and error UI and the popups `DialogSettings.OutsidePopups` names stay usable too, through the list overlays use. `TbFocusScopeTests.Blocking_ShouldHoldWhatIsOutsideOnceTheScopeHasStartedAndFreeItFirst_WhenSet`, `blocking-region.js`
- [x] With two blocking scopes, the newer one is in force and holds the older one; the older one is in force again, with the focus back in it, when the newer one goes. `BlockingScopeTests.Scope_ShouldHoldAnOlderScopeToo_WhenASecondOneBlocksAfterIt`
- [x] An element added beside the scope, or beside one of its ancestors, after the hold has started is held too. `BlockingScopeTests.Scope_ShouldHoldWhatIsAddedBesideItLater_WhenItIsBlockingAlready`
- [x] A scope the user can't reach holds nothing, so the app is never left with nothing usable: one that doesn't show, or one inside something inert, such as a dock panel behind another tab. It holds the app, and takes the focus, once it can be reached. A scope with `display: contents` is judged by the element around it. `BlockingScopeTests.Scope_ShouldHoldThePage_WhenItHasNoBoxOfItsOwn`, `BlockingScopeTests.Scope_ShouldHoldNothingUntilItShows_WhenItStartsInsideSomethingHidden`, `BlockingScopeTests.Scope_ShouldHoldNothingUntilItsTabShows_WhenItStartsInADockPanelBehindAnotherTab`
- [x] The focus goes back to what opened the scope when it was opened and ended from the keyboard, with no press to tell the opener by. `BlockingScopeTests.Scope_ShouldGiveTheFocusBackToWhatOpenedIt_WhenItIsOpenedAndEndedFromTheKeyboard`
- [x] The app's other windows are held and dim, as under a modal dialog: a dock panel's own browser window while the scope is on the page, and the page and every other window while the scope is in a panel's window. `BlockingScopeTests.Scope_ShouldHoldTheAppsOtherWindows_WhenItBlocksOnThePageOrInAPanelsWindow`

## Not applicable

- Closing on Escape: a scope doesn't close itself, which is the panel's job.
- Closing a blocking scope from outside it: nothing outside can be used, so the scope's own content ends it.
- A role and a name: a scope adds none, so the panel gives its own.
