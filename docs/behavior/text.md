# Text

Pattern: none for the text, which is text in the type scale; [Tooltip](https://www.w3.org/WAI/ARIA/apg/patterns/tooltip/) for the full text that `ShowFullText` shows

Familiar apps: none; it isn't a control

## Keyboard

- [x] With `ShowFullText`, Tab reaches text only while it is cut off, and its tooltip shows at once; Escape hides it. `TextTests.FullText_ShouldShowOnHoverAndFocusAndHideOnEscape_WhenTheTextIsCutOff`

## Pointer

- [x] With `ShowFullText`, hovering cut-off text shows its full text in a tooltip after the delay, and text that fits shows none. The text can be selected as usual. `TextTests.FullText_ShouldShowOnHoverAndFocusAndHideOnEscape_WhenTheTextIsCutOff`, `TbTextTests.Render_ShouldNeitherCutNorShowATooltip_WhenTheTextFitsMaxLength`

## Touch

Nothing of its own. Touch has no hover, so the tooltip of `ShowFullText` doesn't show on a tap.

## Focus

- [x] Without `ShowFullText`, it takes no focus, and `MaxLines` cuts off only what shows. `TextTests.Text_ShouldShowOnlyItsLinesWithAnEllipsis_WhenGivenMaxLines`
- [x] With `ShowFullText`, the tooltip and the tab stop come and go as resizing the box cuts the text off or lets it fit. `TextTests.FullText_ShouldComeAndGoWithTheCut_WhenTheBoxIsResized`, `TextTests.FullText_ShouldComeWithTheCut_WhenTheTextGrowsInABoxThatKeepsItsSize`, `TbTextTests.SetCutAsync_ShouldBringAndTakeTheTooltipAndTheTabStop_WhenMaxLinesTextIsMeasured`
- [ ] Inside a data grid cell, cut-off text with `ShowFullText` doesn't fight the grid's own cell focus. #712

## Layout

- [x] Right-to-left text is cut off at its left end, and its tooltip shows. `TextTests.FullText_ShouldShowItsTooltip_WhenRightToLeftTextIsCutOff`

## Screen readers

- [x] Screen readers hear cut-off text once: the tooltip describes nothing, and screen readers read text that `MaxLength` shortens in full from a hidden copy. `TextTests.FullText_ShouldShowOnHoverAndFocusAndHideOnEscape_WhenTheTextIsCutOff`, `TextTests.MaxLength_ShouldShowTheShortenedTextAndReadTheWhole_WhenTheTextIsLonger`

## Cutting text off

- [x] `MaxLength` cuts at the last space when that keeps more than half the characters, and otherwise inside a word, counting what a reader sees as one character once. `TbTextTests.Shortened_ShouldCutAtASpaceOrElseInsideAWord_WhenGivenText`
- [ ] File names, IDs and paths can be cut in the middle, keeping their start and end. #712
- [ ] Copying text that `MaxLength` shortens gives its whole text. #712
