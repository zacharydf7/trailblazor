# Markdown editor

Pattern: a text field with a [toolbar](https://www.w3.org/WAI/ARIA/apg/patterns/toolbar/)

Familiar apps: GitHub's comment box, with its Write and Preview tabs; Reddit's and Stack Overflow's markdown editors

## Keyboard

- [x] Ctrl+B, Ctrl+I, Ctrl+Shift+X and Ctrl+E put marks around the selection and take them off again, and the selection stays on the text. `MarkdownEditorTests.Shortcuts_ShouldPutMarksAroundTheSelectionAndTakeThemOff_WhenPressed`
- [x] Ctrl+Alt+1 to 3, Ctrl+Shift+7, 8 and 9 make the selected lines headings, lists or a quote, and plain lines again. `MarkdownEditorTests.Shortcuts_ShouldPrefixTheSelectedLines_WhenPressed`
- [x] Ctrl+K makes the selection a link, with the address selected to type over; with nothing selected, the caret waits for the label. `MarkdownEditorTests.Link_ShouldSelectTheAddress_WhenPressed`, `MarkdownEditorTests.Link_ShouldKeepBracketsAndWaitForALabel_WhenPressed`
- [x] Line commands keep each line's indentation, and code fences go on lines of their own. `MarkdownEditorTests.Shortcuts_ShouldKeepIndentationAndPutFencesOnTheirOwnLines_WhenPressed`
- [x] AltGr, which Windows reports as Ctrl+Alt, still types its characters, and on macOS, where the shortcuts use Cmd, Ctrl keeps its own text-editing keys. `MarkdownEditorTests.Shortcuts_ShouldLeaveAltGrAlone_WhenItTypesACharacter`
- [x] Ctrl+Z undoes a mark as it undoes typing. `MarkdownEditorTests.Undo_ShouldTakeAMarkOff_WhenPressedAfterIt`
- [x] Tab leaves the editor, from its plain `textarea`. `TbMarkdownEditor.razor`
- [x] The toolbar is one tab stop whose arrow keys move between its buttons, as a `TbToolbar`. `TbMarkdownEditor.razor`

## Pointer

- [x] Only the marks the preview's `Schema` keeps get a button or a shortcut. `TbMarkdownEditorTests.Render_ShouldOfferOnlyTheMarksThePreviewKeeps_WhenGivenASchema`, `MarkdownEditorTests.Shortcuts_ShouldDoNothing_WhenTheSchemaLeavesTheirMarkOut`
- [x] Each toolbar button marks up the selected text, and the value updates. `MarkdownEditorTests.Helper_ShouldPressAToolbarButtonAndReadThePreview_WhenFoundByLabel`
- [x] The Preview button shows the preview in place of the text and hides it again, and the format buttons are off meanwhile. `TbMarkdownEditorTests.ShowPreviewAsync_ShouldShowTheMarkdownAsHtmlInPlaceOfTheText_WhenPressed`
- [x] A preview beside the text follows it as it's typed. `TbMarkdownEditorTests.EnterAsync_ShouldUpdateThePreviewBesideTheText_WhenPreviewIsBeside`

## Touch

- [x] Tapping the buttons does what clicking does, and nothing needs hover. `TbMarkdownEditor.razor`

## Focus

- [x] Screen readers hear the text area named by the field's label. `TbMarkdownEditorTests.Render_ShouldShowALabeledTextAreaAndAToolbarOfMarks_WhenCalled`
- [x] The preview is a region named "Preview", which the Preview button controls. `TbMarkdownEditorTests.ShowPreviewAsync_ShouldShowTheMarkdownAsHtmlInPlaceOfTheText_WhenPressed`
- [x] A link or HTML that would run script shows as text in the preview. `TbMarkdownEditorTests.ShowPreviewAsync_ShouldShowLinksAndHtmlThatRunScriptAsText_WhenTheMarkdownHasThem`

## Not applicable

- Showing the formats while typing, without marks: that's the rich text editor.
