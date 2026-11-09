# Rich text editor

Pattern: none for the editing area, which is a labeled multi-line `textbox`; its toolbar follows
[Toolbar](https://www.w3.org/WAI/ARIA/apg/patterns/toolbar/)

Familiar apps: Google Docs, Word for the web, Gmail's compose box, Outlook's message editor

## Keyboard

- [x] Typed text goes in at the caret and takes the formats of the text before it. `RichTextEditorTests.Typing_ShouldLandAsShownAndTakeTheMarksBefore_WhenTypingInsideAndAfterAMarkedRun`, `RichTextEditorTests.Typing_ShouldPlaceTheTextAtTheCaret_WhenTheSameCharacterRepeatsAcrossAMarkBoundary`
- [x] Ctrl+B, Ctrl+I and Ctrl+U (Cmd on macOS) turn bold, italic and underline on or off for the selection, or for the next text typed when nothing is selected. `RichTextEditorTests.Shortcuts_ShouldToggleEachFormat_WhenPressedOverARangeOrBeforeTyping`
- [x] Strikethrough (Ctrl+Shift+X), subscript (Ctrl+,) and superscript (Ctrl+.) have shortcuts too, and each button's `aria-keyshortcuts` names its shortcut. `RichTextEditorTests.Shortcuts_ShouldToggleEachFormat_WhenPressedOverARangeOrBeforeTyping`, `RichTextEditorTests.Formats_ShouldOfferOnlyThoseAllowed_WhenTheEditorAllowsSome`, `TbRichTextEditorTests.Render_ShouldShowAButtonForEachAllowedFormat_WhenGivenFormats`
- [x] Enter starts a new paragraph, and Shift+Enter breaks the line. `RichTextEditorTests.Paragraphs_ShouldSplitBreakAndJoin_WhenEnterShiftEnterBackspaceAndDeleteArePressed`
- [x] Ctrl+E (Cmd on macOS) turns inline code on or off. `RichTextEditorTests.Code_ShouldToggle_WhenItsButtonOrShortcutIsPressed`
- [x] Enter at the end of a heading starts a plain paragraph. `RichTextEditorTests.Enter_ShouldGoOnAsAParagraph_WhenPressedAtAHeadingsEndOrInAnEmptyQuoteParagraph`
- [x] Enter in an empty list item or quote paragraph leaves the list or quote, a level at a time. `RichTextEditorTests.Lists_ShouldIndentOutdentAndLeave_WhenTheButtonsEnterAndBackspaceAreUsed`, `RichTextEditorTests.Enter_ShouldGoOnAsAParagraph_WhenPressedAtAHeadingsEndOrInAnEmptyQuoteParagraph`
- [x] Backspace at the start of a list item or quote paragraph takes the list or quote away, a level at a time, before joining paragraphs. `RichTextEditorTests.Lists_ShouldIndentOutdentAndLeave_WhenTheButtonsEnterAndBackspaceAreUsed`
- [x] Ctrl+K (Cmd on macOS) opens the link dialog for the selected text, or for the whole link the caret is in or next to. `RichTextEditorTests.Links_ShouldAddUndoEditAndRemoveALink_WhenTheLinkDialogIsUsed`
- [x] Text typed at either end of a link stays outside it, and text typed inside it joins it. `RichTextEditorTests.Links_ShouldInsertLinkedTextThatTypingJoinsOnlyInside_WhenNothingIsSelected`
- [x] Ctrl+Alt+1, 2 and 3 make a heading and Ctrl+Alt+0 a plain paragraph; Ctrl+Shift+7, 8 and 9 make a numbered list, a bulleted list and a quote (Cmd on macOS). Each block button's `aria-keyshortcuts` names its shortcut. `RichTextEditorTests.Blocks_ShouldApplyEachKind_WhenEachBlockShortcutIsPressed`, `TbRichTextEditorTests.Render_ShouldShowABlockButtonForEachAllowedBlockAndIndentButtons_WhenAListIsAllowed`
- [x] Tab and Shift+Tab indent and outdent a list item, and Escape lets Tab leave the editor from one; in a dialog, a second Escape closes the dialog. `RichTextEditorTests.Tab_ShouldIndentAndOutdentAListItemAndLeaveAfterEscape_WhenPressedInAList`, `RichTextEditorTests.Escape_ShouldLetTabLeaveAndPassTheNextEscapeToTheDialog_WhenPressedInAListItemInADialog`
- [x] Typing `# `, `## `, `### `, `> `, `- `, `* ` or `1. ` at the start of a plain paragraph turns it into that block, if the editor allows it, and undo brings back the typed text. `RichTextEditorTests.Markdown_ShouldStartEachBlockAndUndoToTheText_WhenAStartIsTypedAtAParagraphsStart`
- [ ] On keyboards that type characters with AltGr, AltGr with a digit still types its character rather than changing the block. UNCONFIRMED
- [x] Backspace at the start of a paragraph joins it to the one before, and Delete at its end joins the next. `RichTextEditorTests.Paragraphs_ShouldSplitBreakAndJoin_WhenEnterShiftEnterBackspaceAndDeleteArePressed`
- [x] Ctrl+Z undoes and Ctrl+Y or Ctrl+Shift+Z redoes, in word-sized steps, and each puts the selection back. `RichTextEditorTests.Undo_ShouldUndoAndRedoInWordSizedStepsAndRestoreTheSelection_WhenItsShortcutsArePressed`
- [x] Ctrl+Shift+V pastes plain text. `RichTextEditorTests.Paste_ShouldPastePlainText_WhenTheClipboardHasOnlyTextOrShiftIsHeld`
- [x] Ctrl+A selects all of the editor's text and nothing else on the page. `RichTextEditorTests.Deleting_ShouldMergeParagraphsAndLeaveAnEmptyValue_WhenRangesAndEverythingAreDeleted`, `RichTextEditorTests.ReadOnly_ShouldKeepItsTextButLetItBeSelected_WhenTheUserTriesToEditIt`
- [x] Outside a list, Tab leaves the editor rather than typing a tab, so keyboard users can move on. `RichTextEditorTests.Keyboard_ShouldLeaveTheEditorOnTabAndMoveAlongTheToolbarOnArrows_WhenNavigating`
- [x] In the toolbar, Left and Right move between buttons, Home and End go to the first and last, and Tab leaves it. `RichTextEditorTests.Keyboard_ShouldLeaveTheEditorOnTabAndMoveAlongTheToolbarOnArrows_WhenNavigating`, `ToolbarTests.Toolbar_ShouldMoveAcrossEveryControlWithTheArrows_WhenControlsAreNested`
- [ ] An input method's Enter commits its text without starting a new paragraph, in Safari too. UNCONFIRMED
- [x] In a read-only editor, the keys move the caret and select text but change nothing. `RichTextEditorTests.ReadOnly_ShouldKeepItsTextButLetItBeSelected_WhenTheUserTriesToEditIt`

## Pointer

- [x] Clicking the label focuses the editor. `RichTextEditorTests.Label_ShouldFocusTheEditor_WhenClicked`
- [x] A toolbar button applies its format and keeps the selection in the editor. `RichTextEditorTests.Formats_ShouldApplyAndRemove_WhenEachButtonIsPressedOverARange`
- [x] A block button turns the selected paragraphs into its heading, quote or list, or back into plain paragraphs when they all already are, and undo reverses it. `RichTextEditorTests.Blocks_ShouldApplyAndRemoveEachKind_WhenEachBlockButtonIsPressed`
- [x] The toolbar shows a button for each allowed block, and Outdent and Indent only when a list is allowed. `TbRichTextEditorTests.Render_ShouldShowABlockButtonForEachAllowedBlockAndIndentButtons_WhenAListIsAllowed`, `TbRichTextEditorTests.Render_ShouldLeaveOutTheIndentButtons_WhenNoListIsAllowed`
- [x] Plain text or paragraphs pasted into a list item or heading take that block's kind. `RichTextEditorTests.Paste_ShouldKeepTheBlock_WhenPlainParagraphsArePastedIntoAListItemOrHeading`
- [x] The Link button opens the link dialog, which adds, changes or removes the link, and undo takes the change back. `RichTextEditorTests.Links_ShouldAddUndoEditAndRemoveALink_WhenTheLinkDialogIsUsed`, `TbRichTextEditorTests.Render_ShouldShowALinkButtonThatOpensADialog_WhenLinksAreAllowed`, `TbRichTextEditorTests.RequestLink_ShouldOfferToOpenOrRemoveTheLink_WhenTheTextHasOne`
- [x] With nothing selected, the link dialog asks for the text too and inserts it as a link. `RichTextEditorTests.Links_ShouldInsertLinkedTextThatTypingJoinsOnlyInside_WhenNothingIsSelected`, `TbRichTextEditorTests.RequestLink_ShouldAskForTheTextToo_WhenNothingIsSelected`
- [x] The link dialog adds `https://` to a bare address and `mailto:` to an email address, and won't take a scheme the editor doesn't allow. `TbRichTextEditorTests.RequestLink_ShouldPutTheCompletedLinkOnTheText_WhenTheDialogIsApplied`, `TbRichTextEditorTests.RequestLink_ShouldRefuseTheLink_WhenItsSchemeIsNotAllowed`
- [x] A click on a link places the caret without following it, and Ctrl+click opens it in a new tab. `RichTextEditorTests.Links_ShouldOpenInANewTab_WhenCtrlClickedButNotWhenClicked`
- [x] Indent and Outdent move list items a level in or out. `RichTextEditorTests.Lists_ShouldIndentOutdentAndLeave_WhenTheButtonsEnterAndBackspaceAreUsed`
- [x] Typing in a nested list item, or in the item around it, changes only that item. `RichTextEditorTests.Typing_ShouldLandInTheRightItem_WhenTypingInANestedListAndTheItemAroundIt`
- [x] The toolbar's buttons show the selection's formats as pressed, and a partly formatted selection as mixed. `RichTextEditorTests.Formats_ShouldApplyAndRemove_WhenEachButtonIsPressedOverARange`, `RichTextEditorTests.Formats_ShouldShowMixedAndApplyToAll_WhenAPartlyFormattedSelectionIsMadeWithTheKeyboard`
- [x] Pasted HTML from Word, Google Docs and web pages keeps its formats and blocks, and nothing else of it. `RichTextEditorTests.Paste_ShouldKeepOnlyTheFormats_WhenPastingFromWordAndHostileHtml`, `RichTextEditorTests.Paste_ShouldReadTheSampleAsDotNetDoes_WhenACorpusSampleIsPasted`
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
- [x] When the link dialog closes, focus goes back to the editor with the selection it had. `RichTextEditorTests.Links_ShouldPutTheCaretBack_WhenTheLinkDialogIsCanceled`, `RichTextEditorTests.Links_ShouldAddUndoEditAndRemoveALink_WhenTheLinkDialogIsUsed`
- [x] `FocusAsync` moves focus to the editing area. `TbRichTextEditorTests.FocusAsync_ShouldFocusTheEditingArea_WhenCalled`
- [x] The editing area is a `textbox` with `aria-multiline`, named by the field's label. `TbRichTextEditorTests.Render_ShouldNameTheEditingAreaAsAMultiLineTextbox_WhenLabeled`
- [x] The toolbar is labeled, and its buttons say whether their format or block is on, off or mixed. `TbRichTextEditorTests.Render_ShouldShowAButtonForEachAllowedFormat_WhenGivenFormats`, `RichTextEditorTests.Formats_ShouldShowMixedAndApplyToAll_WhenAPartlyFormattedSelectionIsMadeWithTheKeyboard`, `RichTextEditorTests.Blocks_ShouldApplyAndRemoveEachKind_WhenEachBlockButtonIsPressed`
- [x] A validation error is announced as on every other field. `TbRichTextEditorTests.Change_ShouldShowTheRequiredMessage_WhenTheParagraphsHaveNoText`, `TbFieldBase.cs`
- [ ] Screen readers report the formats of the text as they read it. UNCONFIRMED

## Not applicable

- Fonts, sizes and colors: the first version keeps to its formats and links; color comes in a later stage (#357).
- Markdown-style inline shortcuts such as `**bold**`: typing them as text is common, and the toolbar and shortcuts cover the formats.
