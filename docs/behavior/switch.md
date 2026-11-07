# Switch

Pattern: [Switch](https://www.w3.org/WAI/ARIA/apg/patterns/switch/)

Familiar apps: the toggles in phone settings, Windows Settings and macOS System Settings

## Keyboard

- [x] Space toggles it. `CheckboxTests.Controls_ShouldShowTheStateTheServerHolds_WhenEachUpdateIsSlowToComeBack`
- [x] Space on a read-only switch changes nothing. `read-only.js`
- [x] Fast presses leave it showing the value the server holds. `CheckboxTests.Controls_ShouldShowTheStateTheServerHolds_WhenEachUpdateIsSlowToComeBack`

## Pointer

- [x] Clicking the switch or its label toggles it. `CheckboxTests.Switch_ShouldOpenTheLinkWithoutToggling_WhenALinkInItsRichLabelIsClicked`
- [x] The thumb slides to the far edge of the track when on, at every size. `CheckboxTests.Switch_ShouldSlideItsThumbToTheTracksFarEdge_WhenOnAtEverySize`
- [x] The on position follows the reading direction. `CheckboxTests.Switch_ShouldPutTheThumbAtTheEndOfTheReadingDirection_WhenOn`
- [x] Clicking a link in a rich label opens the link without toggling. `CheckboxTests.Switch_ShouldOpenTheLinkWithoutToggling_WhenALinkInItsRichLabelIsClicked`
- [x] Clicking a read-only switch changes nothing. `ReadOnlyTests.ReadOnlySwitchAndRangeSlider_ShouldKeepTheirValues_WhenTheUserTriesToChangeThem`
- [x] A disabled switch ignores clicks. `SwitchExampleTests.StatesExample_ShouldMarkOneSwitchReadOnlySoItPostsAndDisableTheOther_WhenRendered`

## Touch

- [x] A tap on the switch or label toggles it, since it is a native checkbox inside its `<label>`, at least 24px tall. `TbCheckbox.razor`

## Focus

- [x] It shows a focus ring for keyboard focus. `trailblazor.css`
- [x] A read-only switch stays focusable. `TbCheckbox.razor`
- [x] A disabled switch can't take focus. `TbCheckbox.razor`

## Not applicable

- Enter: optional in the pattern. A switch is a native checkbox, so Enter submits its form, as on any checkbox.
- Dragging the thumb, as on a phone: a tap does the same thing, and the pattern doesn't ask for it.
- A mixed state: a switch is bound to a `bool`.
