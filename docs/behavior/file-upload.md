# File upload

Pattern: none in the Authoring Practices; it's the browser's own file input

Familiar apps: a native file input, the drop zones in GitHub and Google Drive

## Keyboard

- [x] Tab reaches the file input, and Enter or Space opens the browser's picker, since it's the native input. `FileUploadTests.Input_ShouldOpenTheBrowsersPicker_WhenTabbedToAndAKeyIsPressed`
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
- [x] A disabled or read-only field turns off the picker and every button. `TbFileUploadTests.Render_ShouldTurnOffThePickerAndEveryButton_WhenReadOnly`, `TbFileUploadSendTests.Cancel_ShouldBeOff_WhenTheFieldIsReadOnly`
- [x] `Hidden` keeps the field out of sight and out of the tab order. `TbFileUploadTests.Render_ShouldKeepTheFieldOutOfSightAndReachButShowWhyFilesWereLeftOut_WhenHidden`
- [x] A file's cancel button becomes its retry button when the upload stops, and the reverse, so focus stays on it. `FileUploadTests.CancelAndRetry_ShouldKeepTheFocusOnTheFilesButton_WhenPressed`
- [x] Screen readers hear when each file is sent or fails, and nothing when one is canceled or as its progress moves. `TbFileUploadSendTests.Upload_ShouldShowProgressThenUploaded_WhenTheUploadSucceeds`, `TbFileUploadSendTests.Retry_ShouldSendTheFileAgain_WhenItsUploadFailed`, `TbFileUploadSendTests.Cancel_ShouldStopTheUploadAndOfferRetry_WhenPressed`
- [x] In forced colors, the drop zone keeps its dashed border. `ForcedColorsTests.Example_ShouldBoxEachField_WhenColorsAreForced`

## Uploading

- [x] With `Upload`, each file shows its progress and a cancel button while it is sent, then "Uploaded", or "Couldn't upload" or "Canceled" with a retry button. `TbFileUploadSendTests.Upload_ShouldShowProgressThenUploaded_WhenTheUploadSucceeds`, `TbFileUploadSendTests.Retry_ShouldSendTheFileAgain_WhenItsUploadFailed`, `TbFileUploadSendTests.Cancel_ShouldStopTheUploadAndOfferRetry_WhenPressed`
- [x] At most `MaxConcurrent` files are sent at once, three unless set, and the others wait their turn; with `AutoUpload` off they wait for the Upload button. `TbFileUploadSendTests.Upload_ShouldSendAtMostMaxConcurrentAtOnce_WhenMoreFilesAreChosen`, `TbFileUploadSendTests.Upload_ShouldWaitForTheUploadButton_WhenAutoUploadIsOff`
- [x] Clearing the field or resetting its form stops every upload, and removing a waiting file keeps it from being sent. `TbFileUploadSendTests.Clear_ShouldStopTheUploads_WhenTheFieldIsEmptied`, `TbFileUploadSendTests.Reset_ShouldStopTheUploadsAndEndTheWait_WhenTheFormResets`, `TbFileUploadSendTests.Remove_ShouldKeepAWaitingFileFromBeingSent_WhenPressed`
- [x] A failed `Upload` is logged as a warning with its exception, and counted in `trailblazor.control.failed` with the action `upload` when the field has a `TelemetryName`. `LoadFailureTests.FileUpload_ShouldLogAWarningAndCountTheFailure_WhenItsUploadThrows`
- [x] A real file's stream reaches `Upload` on the server and in WebAssembly. `FileUploadTests.Upload_ShouldSendTheFileThroughItsStream_WhenChosenOnTheServerOrInWebAssembly`
- [x] `FileTemplate` shows a file in place of its name and size, and the testing helpers still find it by name. `TbFileUploadSendTests.FileTemplate_ShouldShowInPlaceOfTheNameAndSize_WhenGiven`

## Not applicable

- Adding to the listed files on a second choice: a native input replaces them, and the docs say so.
- Pasting a file and dropping outside the zone: the zone is the only drop target, as on a native input.
