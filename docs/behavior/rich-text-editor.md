# Rich text editor

Pattern: none for the editing area, which is a labeled multi-line `textbox`; its toolbar follows
[Toolbar](https://www.w3.org/WAI/ARIA/apg/patterns/toolbar/)

Familiar apps: Google Docs, Word for the web, Gmail's compose box, Outlook's message editor

## Keyboard

- [x] Typed text goes in at the caret and takes the formats of the text before it. `RichTextEditorTests.Typing_ShouldLandAsShownAndTakeTheMarksBefore_WhenTypingInsideAndAfterAMarkedRun`, `RichTextEditorTests.Typing_ShouldPlaceTheTextAtTheCaret_WhenTheSameCharacterRepeatsAcrossAMarkBoundary`
- [x] Ctrl+B, Ctrl+I and Ctrl+U (Cmd on macOS) turn bold, italic and underline on or off for the selection, or for the next text typed when nothing is selected. `RichTextEditorTests.Shortcuts_ShouldToggleEachFormat_WhenPressedOverARangeOrBeforeTyping`
- [x] Strikethrough (Ctrl+Shift+X), subscript (Ctrl+,) and superscript (Ctrl+.) have shortcuts too, and each button's `aria-keyshortcuts` names its shortcut. `RichTextEditorTests.Shortcuts_ShouldToggleEachFormat_WhenPressedOverARangeOrBeforeTyping`, `RichTextEditorTests.Formats_ShouldOfferOnlyThoseAllowed_WhenTheEditorAllowsSome`, `TbRichTextEditorTests.Render_ShouldShowAButtonForEachAllowedFormat_WhenGivenFormats`
- [x] Enter starts a new paragraph, and Shift+Enter breaks the line. `RichTextEditorTests.Paragraphs_ShouldSplitBreakAndJoin_WhenEnterShiftEnterBackspaceAndDeleteArePressed`
- [x] Backspace at the start of a paragraph joins it to the one before, and Delete at its end joins the next. `RichTextEditorTests.Paragraphs_ShouldSplitBreakAndJoin_WhenEnterShiftEnterBackspaceAndDeleteArePressed`
- [x] Ctrl+Z undoes and Ctrl+Y or Ctrl+Shift+Z redoes, in word-sized steps, and each puts the selection back. `RichTextEditorTests.Undo_ShouldUndoAndRedoInWordSizedStepsAndRestoreTheSelection_WhenItsShortcutsArePressed`
- [x] Ctrl+Shift+V pastes plain text. `RichTextEditorTests.Paste_ShouldPastePlainText_WhenTheClipboardHasOnlyTextOrShiftIsHeld`
- [x] Ctrl+A selects all of the editor's text and nothing else on the page. `RichTextEditorTests.Deleting_ShouldMergeParagraphsAndLeaveAnEmptyValue_WhenRangesAndEverythingAreDeleted`, `RichTextEditorTests.ReadOnly_ShouldKeepItsTextButLetItBeSelected_WhenTheUserTriesToEditIt`
- [x] Tab leaves the editor rather than typing a tab, so keyboard users can move on. `RichTextEditorTests.Keyboard_ShouldLeaveTheEditorOnTabAndMoveAlongTheToolbarOnArrows_WhenNavigating`
- [x] In the toolbar, Left and Right move between buttons, Home and End go to the first and last, and Tab leaves it. `RichTextEditorTests.Keyboard_ShouldLeaveTheEditorOnTabAndMoveAlongTheToolbarOnArrows_WhenNavigating`, `ToolbarTests.Toolbar_ShouldMoveAcrossEveryControlWithTheArrows_WhenControlsAreNested`
- [ ] An input method's Enter commits its text without starting a new paragraph, in Safari too. UNCONFIRMED
- [x] In a read-only editor, the keys move the caret and select text but change nothing. `RichTextEditorTests.ReadOnly_ShouldKeepItsTextButLetItBeSelected_WhenTheUserTriesToEditIt`

## Pointer

- [x] Clicking the label focuses the editor. `RichTextEditorTests.Label_ShouldFocusTheEditor_WhenClicked`
- [x] A toolbar button applies its format and keeps the selection in the editor. `RichTextEditorTests.Formats_ShouldApplyAndRemove_WhenEachButtonIsPressedOverARange`
- [x] The toolbar's buttons show the selection's formats as pressed, and a partly formatted selection as mixed. `RichTextEditorTests.Formats_ShouldApplyAndRemove_WhenEachButtonIsPressedOverARange`, `RichTextEditorTests.Formats_ShouldShowMixedAndApplyToAll_WhenAPartlyFormattedSelectionIsMadeWithTheKeyboard`
- [x] Pasted HTML from Word, Google Docs and web pages keeps its formats, and nothing else of it. `RichTextEditorTests.Paste_ShouldKeepOnlyTheFormats_WhenPastingFromWordAndHostileHtml`, `RichTextEditorTests.Paste_ShouldReadTheSampleAsDotNetDoes_WhenACorpusSampleIsPasted`
- [x] Dragging selected text moves it, and text dropped from elsewhere goes in cleaned like a paste. `RichTextEditorTests.Drop_ShouldMoveDraggedTextAndCleanDroppedHtml_WhenTextIsDropped`
- [ ] The browser's own menu works: cut, copy, paste and spelling suggestions. UNCONFIRMED

## Touch

- [ ] A tap focuses the editor and opens the on-screen keyboard. UNCONFIRMED
- [ ] Autocorrect and predictive text on Android and iOS keyboards leave the text as the keyboard showed it, with nothing lost or doubled. UNCONFIRMED
- [ ] Selection handles select text, and the toolbar applies formats to it. UNCONFIRMED

## Focus

- [x] The whole field box shows the focus ring while focus is inside the editor or its toolbar. `trailblazor.css`
- [x] A disabled editor can't take focus, and its toolbar is disabled. `TbRichTextEditorTests.Render_ShouldTakeADisabledEditorOutOfTheTabOrder_WhenDisabled`, `RichTextEditorTests.Label_ShouldLeaveFocusAlone_WhenTheEditorIsDisabled`
- [x] A read-only editor takes focus and its text can be selected, and its toolbar is disabled. `TbRichTextEditorTests.Render_ShouldKeepAReadOnlyEditorFocusableButItsToolbarDisabled_WhenReadOnly`, `RichTextEditorTests.ReadOnly_ShouldKeepItsTextButLetItBeSelected_WhenTheUserTriesToEditIt`
- [x] `FocusAsync` moves focus to the editing area. `TbRichTextEditorTests.FocusAsync_ShouldFocusTheEditingArea_WhenCalled`
- [x] The editing area is a `textbox` with `aria-multiline`, named by the field's label. `TbRichTextEditorTests.Render_ShouldNameTheEditingAreaAsAMultiLineTextbox_WhenLabeled`
- [x] The toolbar is labeled, and its buttons say whether their format is on, off or mixed. `TbRichTextEditorTests.Render_ShouldShowAButtonForEachAllowedFormat_WhenGivenFormats`, `RichTextEditorTests.Formats_ShouldShowMixedAndApplyToAll_WhenAPartlyFormattedSelectionIsMadeWithTheKeyboard`
- [x] A validation error is announced as on every other field. `TbRichTextEditorTests.Change_ShouldShowTheRequiredMessage_WhenTheParagraphsHaveNoText`, `TbFieldBase.cs`
- [ ] Screen readers report the formats of the text as they read it. UNCONFIRMED

## Not applicable

- Tab to indent: the first version has no lists to indent, and a Tab that stays in the editor would trap keyboard users.
- Fonts, sizes and colors: the first version keeps to the six formats; color comes in a later stage (#357).
- Markdown-style shortcuts such as `**` or `# `: they come with headings and lists (#355).
