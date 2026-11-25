# Copy button

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/), with the result announced

Familiar apps: the copy button beside a command in a package registry, a code host's copy-link button

## Keyboard

- [x] Enter and Space press it, as any button; focus stays on it while it shows how the copy went. `TbButtonBase.razor`

## Pointer

- [x] A press copies `Text`, shows a check and "Copied" for two seconds, then offers to copy again; a second press within them shows its own result for the whole moment. `TbCopyButtonTests.TbCopyButton_ShouldCopyTheTextAndSayItCopied_WhenPressed`, `TbCopyButtonTests.TbCopyButton_ShouldOfferToCopyAgain_WhenTheMomentPasses`, `TbCopyButtonTests.TbCopyButton_ShouldShowTheResultForTheWholeMoment_WhenPressedAgainBeforeItResets`, `CopyButtonScenarioTests.InstallCommand_ShouldReachTheClipboard_WhenCopied`
- [x] A refused copy shows an error mark and "Couldn't copy" (the mark alone with `IconOnly`), in the danger color, so it isn't shown by color alone. `TbCopyButtonTests.TbCopyButton_ShouldSayItCouldntCopy_WhenTheBrowserRefuses`, `TbCopyButtonTests.IconOnly_ShouldShowAnotherIcon_WhenTheBrowserRefuses`

## Touch

Nothing of its own: a tap presses it, as any button.

## Focus

- [x] With `IconOnly`, its words name it and show in its tooltip. `TbCopyButtonTests.IconOnly_ShouldNameTheIconByItsWords_WhenSet`, `TbIconButton.razor`
- [x] `Copyable` on a text field adds one at the field's end, heard with the field's visible label. `TbCopyButtonTests.Copyable_ShouldCopyTheFieldsText_WhenTheTextFieldsButtonIsPressed`

## Announcements

- [x] A copy says "Copied", and a refused one "Couldn't copy", politely. `TbCopyButtonTests.TbCopyButton_ShouldCopyTheTextAndSayItCopied_WhenPressed`, `TbCopyButtonTests.TbCopyButton_ShouldSayItCouldntCopy_WhenTheBrowserRefuses`

## Rendering

- [x] Before the page is interactive it's turned off, since copying runs in the browser. `TbCopyButtonTests.Render_ShouldBeOff_WhenThePageIsStatic`
- [x] With a `TelemetryName` it counts copies, never what was copied. `TbCopyButtonTests.Telemetry_ShouldCountCopies_WhenNamed`
- [x] A button that goes away while it copies says nothing and leaves nothing running. `TbCopyButtonTests.CopyAsync_ShouldLeaveNothingRunning_WhenTheButtonGoesAwayMidCopy`
