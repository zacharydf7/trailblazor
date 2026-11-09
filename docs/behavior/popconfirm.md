# Popconfirm

Pattern: none of its own; it is a [popover](popover.md), a non-modal dialog opened by its trigger, holding a question
and two buttons

Familiar apps: the "Delete this?" bubble beside a row's delete button in admin dashboards, GitHub's confirmations
beside a button

## Keyboard

- [x] Enter on the trigger opens the confirmation with focus on Cancel, Tab moves to the confirm button, and Enter there confirms and gives focus back to the trigger. `PopconfirmTests.Popconfirm_ShouldStartOnCancelAndConfirmWithTheKeyboard_WhenOpenedWithEnter`
- [x] Escape cancels and gives focus back to the trigger. `PopconfirmTests.Popconfirm_ShouldCancelAndGiveFocusBack_WhenEscapeIsPressed`
- [x] Tab past the confirm button closes the confirmation, which counts as canceling. `PopconfirmTests.Popconfirm_ShouldCancel_WhenTabLeavesThePanel`
- [x] Space on the trigger opens it, as on a popover. `TbPopover.razor`

## Pointer

- [x] Clicking the cancel button closes the confirmation and raises `OnCancel`. `TbPopconfirmTests.Cancel_ShouldCloseAndRaiseOnCancel_WhenClicked`, `PopconfirmTests.Helper_ShouldCancel_WhenCancelAsyncIsCalled`
- [x] Clicking elsewhere cancels. `PopconfirmTests.Popconfirm_ShouldCancel_WhenAClickLandsElsewhere`
- [x] The confirm button shows it's busy, and Cancel is off, until `OnConfirm` finishes; then the confirmation closes. `TbPopconfirmTests.Confirm_ShouldBeBusyUntilOnConfirmFinishesAndThenClose_WhenClicked`, `TbPopconfirmTests.Confirm_ShouldStayOpenAndBusy_WhenAParentThatDoesntBindOpenRendersAgain`, `PopconfirmTests.Popconfirm_ShouldStayOpenAndBusyUntilASlowConfirmFinishes_WhenConfirmed`

## Touch

- [x] Tapping the trigger and the buttons does what clicking does, and tapping elsewhere cancels, through the browser's popover handling. `TbPopover.razor`

## Focus

- [x] The confirmation is a dialog named by its question and described by its description, and its Cancel button comes first. `TbPopconfirmTests.Render_ShouldDescribeThePanelByItsDescription_WhenItHasOne`, `TbPopconfirmTests.Render_ShouldNameThePanelByItsQuestionAndPutCancelFirst_WhenCalled`
- [x] Closing the confirmation by any answer gives focus back to the trigger. `PopconfirmTests.Popconfirm_ShouldStayOpenAndBusyUntilASlowConfirmFinishes_WhenConfirmed`, `PopconfirmTests.Popconfirm_ShouldCancelAndGiveFocusBack_WhenEscapeIsPressed`

## Not applicable

- Keeping Tab inside the confirmation: it's non-modal, like a popover, so Tab moves on and cancels. For a decision that blocks the page, use a dialog's `ConfirmAsync`.
- Opening on hover: a question about an action opens only when the user asks for it.
