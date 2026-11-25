# Text

Pattern: none for the text, which is text in the type scale; [Tooltip](https://www.w3.org/WAI/ARIA/apg/patterns/tooltip/) for the full text that `ShowFullText` shows

Familiar apps: none; it isn't a control

## Keyboard

- [x] With `ShowFullText`, Tab reaches text only while it is cut off, and its tooltip shows at once; Escape hides it. `TextTests.FullText_ShouldShowOnHoverAndFocusAndHideOnEscape_WhenTheTextIsCutOff`

## Pointer

- [x] With `ShowFullText`, hovering cut-off text shows its full text in a tooltip after the delay, and text that fits shows none. The text can be selected as usual. `TextTests.FullText_ShouldShowOnHoverAndFocusAndHideOnEscape_WhenTheTextIsCutOff`, `TbTextTests.Render_ShouldNeitherCutNorShowATooltip_WhenTheTextFitsMaxLength`

## Touch

- [x] A tap on cut-off text shows its tooltip, since the tap does nothing else; inside a link or button the tap is the control's. `TextTests.FullText_ShouldShowItsTooltip_WhenTapped`

## Focus

- [x] Without `ShowFullText`, it takes no focus, and `MaxLines` cuts off only what shows. `TextTests.Text_ShouldShowOnlyItsLinesWithAnEllipsis_WhenGivenMaxLines`
- [x] With `ShowFullText`, the tooltip and the tab stop come and go as resizing the box cuts the text off or lets it fit. `TextTests.FullText_ShouldComeAndGoWithTheCut_WhenTheBoxIsResized`, `TextTests.FullText_ShouldComeWithTheCut_WhenTheTextGrowsInABoxThatKeepsItsSize`, `TbTextTests.SetCutAsync_ShouldBringAndTakeTheTooltipAndTheTabStop_WhenMaxLinesTextIsMeasured`
- [x] Alone inside something that takes focus, such as a link, a button or a cell of a grid with `CellNavigation`, cut-off text with `ShowFullText` adds no tab stop of its own, and focus on what holds it shows the tooltip; two such texts in one holder each keep their own. Directly in a table's cell, it takes the column's width. `TextTests.FullText_ShouldLeaveTheTabStopToWhatHoldsIt_WhenInALinkOrAGridCell`, `TextTests.FullText_ShouldKeepEachTabStop_WhenALinkHoldsTwoCutTexts`

## Layout

- [x] Right-to-left text is cut off at its left end, and its tooltip shows. `TextTests.FullText_ShouldShowItsTooltip_WhenRightToLeftTextIsCutOff`

## Screen readers

- [x] Screen readers hear cut-off text once: the tooltip describes nothing, and screen readers read text that `MaxLength` shortens in full from a hidden copy. `TextTests.FullText_ShouldShowOnHoverAndFocusAndHideOnEscape_WhenTheTextIsCutOff`, `TextTests.MaxLength_ShouldShowTheShortenedTextAndReadTheWhole_WhenTheTextIsLonger`

## Cutting text off

- [x] `MaxLength` cuts at the last space when that keeps more than half the characters, and otherwise inside a word, counting what a reader sees as one character once. `TbTextTests.Shortened_ShouldCutAtASpaceOrElseInsideAWord_WhenGivenText`
- [x] File names, IDs and paths can be cut in the middle, keeping their start and end: to `MaxLength` as the page renders, or to fit the box with a `MaxLines` of 1, in right-to-left text too. `TbTextTests.ShortenedInTheMiddle_ShouldKeepTheStartAndTheEnd_WhenGivenText`, `TextTests.MiddleCut_ShouldKeepTheStartAndEndThatFit_WhenTheBoxIsNarrow`, `TextTests.MiddleCut_ShouldKeepTheNameAndTheExtension_WhenTheFileNameIsRightToLeft`, `TextTests.MaxLength_ShouldCutTheMiddle_WhenTruncationIsMiddle`
- [x] Copying text that `MaxLength` or a middle cut shortens gives its whole text when the selection holds nothing else. `TextTests.Copy_ShouldGiveTheWholeText_WhenTheSelectionHoldsOnlyShortenedText`
- [x] Turning a middle cut on or off while the page runs starts the script over, so the text never goes blank. `TbTextTests.Truncation_ShouldStartTheScriptAgain_WhenItChanges`, `TextTests.MiddleCut_ShouldShowTheTextAgain_WhenTurnedOff`
