# Highlight

Pattern: none; text with `mark` elements, which have no keyboard of their own

Familiar apps: the matched words in GitHub's and Google Drive's search results

## Keyboard

Nothing of its own: it is text, and the controls around it keep their keys.

## Pointer

Nothing of its own: it is text.

## Touch

Nothing of its own: it is text.

## Output

- [x] Each word of the search is marked wherever it appears, in any capitals. `TbHighlightTests.Render_ShouldMarkEachWordWhereverItAppears_WhenSearched`
- [x] Matches that overlap or touch make one mark. `TbHighlightTests.Render_ShouldJoinOverlappingMatches_WhenWordsOverlap`
- [x] A match longer than the search word, such as an e followed by a separate accent for é, is marked whole. `TbHighlightTests.Render_ShouldMarkTheWholeMatch_WhenItIsLongerThanTheSearch`
- [x] With `CaseSensitive`, only words with the same capitals are marked. `TbHighlightTests.Render_ShouldMarkOnlyTheSameCapitals_WhenCaseSensitive`

## Focus

- [x] The marks don't split the text, so it is found by its whole text. `TbHighlightTests.Render_ShouldMarkEachWordWhereverItAppears_WhenSearched`
- [x] In forced colors, a match takes the system's highlight colors. `HighlightTests.Highlight_ShouldUseTheSystemHighlight_WhenColorsAreForced`
