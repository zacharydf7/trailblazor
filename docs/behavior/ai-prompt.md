# AI prompt

Pattern: none; a labeled box and buttons, with responses in a list

Familiar apps: the "Ask AI" box in Notion and Google Docs, GitHub Copilot's chat input, Kendo UI's AIPrompt

## Keyboard

- [x] Enter in the box generates, and Shift+Enter starts a new line. `AiPromptTests.Prompt_ShouldGenerateOnEnterAndStartALineOnShiftEnter_WhenTyping`
- [x] Escape stops the response that's generating. `AiPromptTests.Escape_ShouldStopTheResponse_WhenItStreams`
- [x] The Enter that ends an input method's composition doesn't generate. `AiPromptTests.Prompt_ShouldNotGenerate_WhenEnterEndsAComposition`
- [x] Tab moves through the box, the generate button, the suggestions and each response's buttons, in that order. `TbAiPrompt.razor`

## Pointer

- [x] A suggestion fills the box and puts focus in it, ready to change. `AiPromptTests.Suggestion_ShouldFillTheBoxAndFocusIt_WhenClicked`
- [x] The generate button turns into Stop while a response generates, and back once it ends; Stop works even when the app turns the prompt off meanwhile. `TbAiPromptTests.Prompt_ShouldMarkTheResponseBusyAndOfferStop_WhenItStreams`, `TbAiPromptTests.Stop_ShouldStillWork_WhenThePromptIsTurnedOffMidResponse`
- [x] Copy puts a response's text on the clipboard and says "Copied". `TbAiPromptTests.Copy_ShouldCopyTheTextAndSaySo_WhenPressed`
- [x] Retry generates the response again in its place. `TbAiPromptTests.Retry_ShouldGenerateAgainInPlace_WhenPressedOnAnOlderResponse`
- [x] Insert hands the response to the app, and shows only when the app sets `OnInsert`. `TbAiPromptTests.Insert_ShouldRaiseOnInsertWithTheResponse_WhenPressed`

## Touch

- [x] The same buttons work by tap; nothing needs hover or a long press. `TbAiPrompt.razor`

## Focus

- [x] Focus on the generate button stays put when it turns into Stop and back. `AiPromptTests.Generate_ShouldKeepFocus_WhenItTurnsIntoStopAndBack`
- [x] A response's start, end, stop or failure is read out, not each chunk, and the response is busy while it streams. `TbAiPromptTests.Prompt_ShouldStreamTheResponseAndSayWhenItIsReady_WhenGenerated`, `TbAiPromptTests.Prompt_ShouldMarkTheResponseBusyAndOfferStop_WhenItStreams`
- [x] A failed response says so and offers Retry, and the app's template gets the error. `TbAiPromptTests.Prompt_ShouldMarkTheResponseFailedAndKeepTheError_WhenGenerateThrows`
- [x] In forced colors, the prompt box and each response keep their borders. `ForcedColorsTests.Example_ShouldKeepEachPartsBorders_WhenColorsAreForced`, `AiPromptTests.Response_ShouldKeepItsBorder_WhenColorsAreForced`

## Not applicable

- A conversation's history and its feed keys: that's a chat, a different control.
- Rendering Markdown: the app's `OutputTemplate` draws a response however it likes.
