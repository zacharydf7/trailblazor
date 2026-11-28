# In-place editor

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/) for the value, then the editor's own field

Familiar apps: the editable titles and fields in Jira, Trello and Notion

## Keyboard

- [x] Enter or Space on the value shows the editor and moves focus to its first control. `InPlaceEditorTests.InPlaceEditor_ShouldEditWithTheKeyboard_WhenEnterAndEscapeArePressed`
- [x] Enter in the editor saves and focus goes back to the value. `InPlaceEditorTests.InPlaceEditor_ShouldEditWithTheKeyboard_WhenEnterAndEscapeArePressed`
- [x] Escape in the editor throws the draft away and focus goes back to the value. `InPlaceEditorTests.InPlaceEditor_ShouldEditWithTheKeyboard_WhenEnterAndEscapeArePressed`
- [x] Enter and Escape don't reach a form or dialog around the editor. `InPlaceEditorTests.InPlaceEditor_ShouldKeepEnterFromTheFormAroundIt_WhenSaving`, `InPlaceEditorTests.InPlaceEditor_ShouldEditWithTheKeyboard_WhenEnterAndEscapeArePressed`

## Pointer

- [x] Clicking the value shows the editor; the save and cancel buttons save or throw the draft away. `TbInPlaceEditorTests.SaveAsync_ShouldSetTheValueToTheDraft_WhenPressed`, `TbInPlaceEditorTests.CancelAsync_ShouldKeepTheValue_WhenPressed`
- [x] A disabled editor shows its value without letting it be edited. `TbInPlaceEditorTests.Render_ShouldNotEdit_WhenDisabled`

## Touch

- [x] Tapping does what clicking does, and nothing needs hover. `TbInPlaceEditor.razor`

## Focus

- [x] The value is a button named "Edit", the label and the value or placeholder it shows. `TbInPlaceEditorTests.Render_ShouldShowTheValueInAButtonNamedForEditingIt_WhenCalled`
- [x] The editor is a group named by the label, with buttons named Save and Cancel. `TbInPlaceEditorTests.EditAsync_ShouldShowTheEditorWithTheDraft_WhenPressed`
- [x] In forced colors, the value keeps a border, so it reads as editable. `ForcedColorsTests.Example_ShouldBoxEachField_WhenColorsAreForced`
