# File upload

Pattern: none in the Authoring Practices; it's the browser's own file input

Familiar apps: a native file input, the drop zones in GitHub and Google Drive

## Keyboard

- [x] Tab reaches the file input, and Enter or Space opens the browser's picker, since it's the native input. `TbFileUpload.razor`
- [x] The zone shows a focus ring while the input has keyboard focus. `trailblazor.css`
- [x] Each listed file's remove button and the clear button are reachable and named. `FileUploadTests.Upload_ShouldHaveNoAccessibilityViolations_WhenFilesAreListed`

## Pointer

- [x] Clicking anywhere on the zone opens the picker, since the input covers it. `trailblazor.css`
- [x] Clicking the label opens the picker, since it points at the input. `TbFileUpload.razor`
- [x] The zone highlights while a file is dragged over it, and stops when it leaves. `FileUploadTests.Zone_ShouldShowItIsReady_WhenAFileIsDraggedOverIt`
- [x] Dropping a file chooses it, and one of another type is left out with a reason. `FileUploadTests.Choose_ShouldLeaveOutAndExplainOtherTypes_WhenDroppedFilesDontMatchAccept`
- [x] Dropping a file on a disabled or read-only zone does nothing, rather than the browser opening the file. `FileUploadTests.Zone_ShouldRefuseADrop_WhenTheFieldIsDisabled`, `FileUploadTests.Zone_ShouldRefuseADrop_WhenTheFieldIsReadOnly`, `FileUploadTests.Zone_ShouldRefuseADrop_WhenADisabledFieldIsOnAStaticPage`, `FileUploadTests.Zone_ShouldRefuseADrop_WhenTheDragTargetsTheTextOfItsPrompt`, `FileUploadTests.Zone_ShouldShowNoDropAndGetNoDrop_WhenAFileIsDraggedOverADisabledField`
- [ ] In Firefox and Safari, a real file dropped on a disabled or read-only zone leaves the page where it is. UNCONFIRMED
- [x] Canceling the picker keeps the listed files, since only a change event updates them. `TbFileUpload.cs`
- [x] Choosing again replaces the files. `TbFileUploadTests.Choose_ShouldReplaceTheEarlierFiles_WhenFilesAreChosenAgain`
- [x] Clearing lets the same file be chosen again. `FileUploadTests.Clear_ShouldLetTheSameFileBeChosenAgain_WhenAFileWasCleared`
- [x] Dropping a file again after removing it takes it back. `FileUploadTests.Drop_ShouldTakeTheFileAgain_WhenItWasRemoved`, `FileUploadTests.Remove_ShouldEmptyTheInputAndKeepTheOtherFilesReadable_WhenOneOfSeveralIsRemoved`
- [x] Without `DropZone` it's a button that takes one file. `FileUploadTests.Button_ShouldTakeOneFile_WhenThereIsNoDropZone`
- [x] A click handler can open the picker from code. `FileUploadTests.Open_ShouldOpenThePickerFromCodeAndLetItBeCleared_WhenButtonsAsk`
- [ ] A disabled or read-only zone shows no ready highlight while a file is dragged over it. UNCONFIRMED

## Touch

- [x] A tap on the native input opens the device's own picker, which honors `accept`. `TbFileUpload.razor`

## Focus

- [x] Removing a file moves focus to the input, since its button goes with it. `FileUploadTests.Remove_ShouldDropTheFileAndFocusTheInput_WhenClicked`
- [x] The clear button moves focus to the input. `TbFileUpload.cs`
- [x] A disabled or read-only field turns off the picker and every button. `TbFileUploadTests.Render_ShouldTurnOffThePickerAndEveryButton_WhenReadOnly`
- [x] `Hidden` keeps the field out of sight and out of the tab order. `TbFileUploadTests.Render_ShouldKeepTheFieldOutOfSightAndReachButShowWhyFilesWereLeftOut_WhenHidden`

## Not applicable

- Adding to the listed files on a second choice: a native input replaces them, and the docs say so.
- Pasting a file and dropping outside the zone: the zone is the only drop target, as on a native input.
- Upload progress: the field only chooses files, and the app sends them.
