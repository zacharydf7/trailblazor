# Alert

Pattern: [Alert](https://www.w3.org/WAI/ARIA/apg/patterns/alert/)

Familiar apps: the banners at the top of a settings page or a form that failed to save

## Keyboard

- [x] Tab reaches the alert's actions and then its close button. `TbAlert.razor`
- [x] Enter on the close button closes it. `FeedbackTests.Alert_ShouldClose_WhenItsCloseButtonIsActivatedByKeyboard`

## Pointer

- [x] A click on the close button asks to close the alert. `TbAlertTests.Click_ShouldRaiseOnClose_WhenTheCloseButtonIsClicked`
- [x] With no `OnClose` there is no close button. `TbAlertTests.Render_ShouldBeAPoliteInfoAlertWithoutAClose_WhenNoParametersAreSet`

## Touch

Nothing of its own. The close button and actions are native buttons, so a tap is a click.

## Focus

- [x] An alert that appears doesn't take focus. `TbAlert.razor`
- [x] Screen readers hear a danger or warning alert at once, without focus moving. `TbAlertTests.Render_ShouldUseTheAlertRole_WhenGivenAnUrgentTone`
- [x] Other tones are polite. `TbAlertTests.Render_ShouldBeAPoliteInfoAlertWithoutAClose_WhenNoParametersAreSet`
- [x] The close button has a name. `TbAlertTests.Click_ShouldRaiseOnClose_WhenTheCloseButtonIsClicked`

## Not applicable

- Moving focus when an alert closes: only the app knows what should take it, and the browser keeps Tab in place.
- Closing by itself after a time: an alert stays until it's closed, and a toast is for messages that go away.
- Escape closing it: the alert pattern has no keys, and an alert doesn't hold focus the way a dialog does.
