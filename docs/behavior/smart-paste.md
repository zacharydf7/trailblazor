# Smart paste

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/) that opens a [modal dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) where the clipboard can't be read

Familiar apps: paste-to-fill buttons on contact and address forms that read a signature or a message

## Keyboard

- [x] Enter and Space read the clipboard and fill the form. `SmartPasteTests.SmartPaste_ShouldAskForTheTextAndGiveFocusBack_WhenTheBrowserRefusesTheClipboard`
- [x] In the box, Tab moves between the text area and its buttons, and Escape closes it without filling. `TbDialog.cs`

## Pointer

- [x] Pressing it fills the form from the clipboard, without a box, where the browser shares the clipboard. `SmartPasteTests.SmartPaste_ShouldFillTheFormFromTheClipboard_WhenTheBrowserSharesIt`, `TbSmartPasteTests.Click_ShouldFillFromTheClipboardWithoutTheBox_WhenTheBrowserSharesIt`
- [x] Where the browser won't share it, or it's empty, a box asks for the text; Fill fills the form and Cancel closes the box. `TbSmartPasteTests.PasteAsync_ShouldFillEachFieldTheModelFound_WhenGivenText`, `TbSmartPasteTests.Cancel_ShouldCloseTheBoxWithoutFillingOrKeepingTheText_WhenPressed`
- [x] Each value fills its field as typing would: the field's own limits, the binding, validation and the form's dirty state follow. `TbSmartPasteTests.PasteAsync_ShouldFillEachFieldTheModelFound_WhenGivenText`, `TbSmartPasteTests.PasteAsync_ShouldKeepToEachFieldsOwnLimits_WhenAValueGoesPastThem`
- [x] A value no field takes, such as text for a number, is skipped, and names match whatever their case. `TbSmartPasteTests.PasteAsync_ShouldSkipAValueNoFieldTakes_WhenTheModelReturnsOne`
- [x] Password, disabled and read-only fields, and fields that pick from a range, such as a slider, are never filled. `TbSmartPasteTests.Extract_ShouldGetEachFieldItCanFill_WhenCalled`
- [x] A field that picks from options (a select, list box, radio group or checkbox group, single or multiple) is described with its options' texts. An answer that is one of them, in any letter case, chooses that option, as it does for the example's radio group. `TbSmartPasteTests.Extract_ShouldGetEachOptionsTextAsAChoice_WhenAFieldPicksFromOptions`, `TbSmartPasteTests.PasteAsync_ShouldChooseTheOptionsNamed_WhenTheModelAnswersWithTheirTexts`, `SmartPasteTests.SmartPaste_ShouldFillTheFormFromTheClipboard_WhenTheBrowserSharesIt`
- [x] A field that takes several options reads one text on each line and chooses them in the order named, each once. A chosen value that wasn't offered, such as a disabled option's or one that no option has, stays chosen, ahead of those named. `TbSmartPasteTests.PasteAsync_ShouldChooseTheOptionsNamed_WhenTheModelAnswersWithTheirTexts`, `TbSmartPasteTests.PasteAsync_ShouldLeaveWhatItDidNotOfferAsItWas_WhenAFieldTakesSeveral`
- [x] A disabled option, one that has content and no `Text`, and a second option with the same text aren't offered, and a field with none to offer is left out. An answer that names no offered option, or two options for a field that takes one, is skipped. `TbSmartPasteTests.Extract_ShouldGetEachOptionsTextAsAChoice_WhenAFieldPicksFromOptions`, `TbSmartPasteTests.PasteAsync_ShouldSkipAnAnswer_WhenItNamesNoOptionTheFieldOffered`
- [x] An option removed from a radio group or a checkbox group is no longer offered. `TbSmartPasteTests.Extract_ShouldGetTheOptionsThereNow_WhenAGroupsOptionWasRemoved`
- [x] While the browser asks about the clipboard and while the model reads, the button is busy and takes no second press. `TbSmartPasteTests.Click_ShouldShowItsFillingAndIgnoreAnotherPress_WhenPressedWhileTheModelReads`, `TbSmartPasteTests.Click_ShouldIgnoreAnotherPress_WhenPressedWhileTheClipboardIsRead`
- [x] A failing model shows "Couldn't fill the form" and leaves the fields as they were. `TbSmartPasteTests.PasteAsync_ShouldShowItFailed_WhenTheModelThrows`
- [x] Removing the button while the model reads cancels it. `TbSmartPasteTests.Dispose_ShouldCancelTheModel_WhenTheButtonGoesAwayWhileItReads`

## Touch

- [x] Tapping does what clicking does, and nothing needs hover. `TbSmartPaste.razor`

## Focus

- [x] The box takes focus in its text area when it opens and gives it back to the button when it closes. `SmartPasteTests.SmartPaste_ShouldAskForTheTextAndGiveFocusBack_WhenTheBrowserRefusesTheClipboard`
- [x] "Filling the form" and the result are announced and shown beside the button, unless `Labels` words them. `TbSmartPasteTests.PasteAsync_ShouldFillEachFieldTheModelFound_WhenGivenText`, `TbSmartPasteTests.Labels_ShouldWordTheButtonBoxAndStatus_WhenGiven`
- [x] The button is off on a static page and in a disabled form. `TbSmartPasteTests.Render_ShouldTurnTheButtonOff_WhenThePageIsStatic`, `TbSmartPasteTests.Render_ShouldTurnTheButtonOff_WhenTheFormIsDisabled`
