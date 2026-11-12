# Theme picker

Pattern: a [toggle group](toggle-group.md), so toggle buttons from [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/) with one tab stop

Familiar apps: the Light, Dark and System choice in GitHub's appearance settings and in Windows

## Keyboard

- [x] Tab enters on the pressed choice and leaves in one stop. `ToggleGroupTests.ToggleGroup_ShouldBeOneTabStopLandingOnThePressedItem_WhenTabbedThrough`
- [x] Left, Right, Home and End move between the choices without picking one. `ToggleGroupTests.ToggleGroup_ShouldMoveFocusAndPressWithTheKeys_WhenTheArrowsHomeEndAndSpaceAreUsed`
- [x] Enter and Space pick the focused choice. `TbThemePicker.razor`

## Pointer

- [x] A click picks the mode, and the page changes at once. `ThemeTests.ThemePicker_ShouldShowAndRememberTheChosenMode_WhenAnItemIsPressed`
- [x] The choice comes back after a reload, before the page is interactive. `ThemeTests.ThemePicker_ShouldShowAndRememberTheChosenMode_WhenAnItemIsPressed`
- [x] Clicking the pressed choice keeps it, so one always stays picked. `TbThemePicker.razor`
- [x] The device's setting, once picked, follows the device as it changes. `trailblazor.css`
- [x] The picker follows a mode set elsewhere through `IThemeService`. `TbThemePicker.razor`
- [x] A hovered choice looks different from the pressed one, and hover shows its name. `ToggleItemsTests.ThemePicker_ShouldShowAChoicesNameAndTellItFromThePressedOne_WhenHovered`

## Touch

- [x] A tap picks a choice through the same click a mouse sends, at a target of at least 26px. `TbToggleItem.razor`

## Focus

- [x] Nothing is pressed until the picker has read the page. `TbThemePickerTests.Render_ShouldPressNothing_WhenItHasNotReadThePageYet`
- [x] Once read, the remembered mode is pressed. `TbThemePickerTests.Click_ShouldPressTheLoadedModeThenSetTheOnePressed_WhenInteractive`
- [x] Focus stays on the chosen item after a pick. `TbThemePicker.razor`
- [x] Keyboard focus shows a choice's name. `ToggleItemsTests.ThemePicker_ShouldShowTheNextChoicesName_WhenTheKeysMoveFocus`
- [x] `Labels` renames the choices. `TbThemePickerTests.Labels_ShouldNameTheButtons_WhenGiven`

## Not applicable

- Following a choice made in another tab: the other tab picks it up on its next page load.
