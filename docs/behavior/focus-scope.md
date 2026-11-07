# Focus scope

Pattern: none of its own; it gives a custom panel the focus rules of a [modal dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/)

Familiar apps: how focus moves in OS dialogs and app side panels

## Keyboard

- [x] With `Trapped`, Tab on the last control wraps to the first. `FocusScopeTests.Tab_ShouldWrapToFirstTabbable_WhenTrappedAndFocusIsOnLast`
- [x] With `Trapped`, Shift+Tab on the first control wraps to the last. `FocusScopeTests.ShiftTab_ShouldWrapToLastTabbable_WhenTrappedAndFocusIsOnFirst`
- [x] Without `Trapped`, Tab moves on to the rest of the page. `FocusScopeTests.Tab_ShouldLeaveScope_WhenNotTrapped`
- [x] When trapped scopes nest, Tab stays in the newest. `FocusScopeTests.Tab_ShouldWrapInsideInnerScope_WhenTrappedScopesAreNested`
- [x] The outer scope traps again once the inner one goes. `FocusScopeTests.Tab_ShouldWrapInsideOuterScope_WhenNestedInnerScopeIsDisposed`
- [x] Tab stays inside after the focused control is removed. `FocusScopeTests.Tab_ShouldStayInsideScope_WhenTrappedAndFocusedElementIsRemoved`
- [x] Tab goes nowhere in a trapped scope with no controls. `focus-scope.js`
- [ ] Tab wraps when the scope's first or last stop is a radio group, whichever radio is checked. #299

## Pointer

- [x] Focus moved to a control outside a trap, by a click or a script, comes back to where it was. `FocusScopeTests.Focus_ShouldReturnToLastFocusedInside_WhenTrappedAndFocusMovesOutside`
- [x] After a click on empty page, the next Tab goes back into a trap. `focus-scope.js`

## Touch

Nothing of its own. It listens only to keyboard and focus events.

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

## Not applicable

- Closing on Escape: a scope doesn't close itself, which is the panel's job.
- Blocking clicks or screen readers on the page: a scope only handles focus, and a dialog makes the page inert.
- A role and a name: a scope adds none, so the panel gives its own.
