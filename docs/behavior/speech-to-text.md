# Speech to text

Pattern: [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/), a toggle pressed while it listens

Familiar apps: the microphone buttons in Google Docs' voice typing and Gmail's search on phones

## Keyboard

- [x] Enter and Space start listening, and stop it. `SpeechToTextTests.SpeechToText_ShouldFillTheFieldFromWhatIsSaid_WhenListening`

## Pointer

- [x] Pressing it starts listening and pressing it again stops. `TbSpeechToTextTests.StartAsync_ShouldListenAndSayItIs_WhenPressed`
- [x] After a phrase or an error, the browser stops it: the button unpresses and "Stopped listening" is announced. `TbSpeechToTextTests.Stopped_ShouldUnpressTheButtonAndSaySo_WhenTheBrowserStops`
- [x] Stopping and quickly starting again keeps listening, even when the earlier recognition ends late. `SpeechToTextTests.SpeechToText_ShouldKeepListening_WhenStartedAgainBeforeTheLastStopEnds`, `TbSpeechToTextTests.Stopped_ShouldKeepTheButtonPressed_WhenAnEarlierRecognitionEndsLate`
- [x] Disabling it while it listens stops listening. `TbSpeechToTextTests.Disabled_ShouldStopListening_WhenSetWhileListening`
- [x] Each phrase heard is handed to the app. `SpeechToTextTests.SpeechToText_ShouldFillTheFieldFromWhatIsSaid_WhenListening`, `TbSpeechToTextTests.SayAsync_ShouldHandThePhraseToTheApp_WhenListening`
- [x] Why it stopped without hearing anything is handed to the app. `TbSpeechToTextTests.Errors_ShouldReachTheApp_WhenRecognitionFails`

## Touch

- [x] Tapping does what clicking does, and nothing needs hover. `TbSpeechToText.razor`

## Focus

- [x] The button is named "Dictate", unless `Label` names it, and is pressed while it listens. `TbSpeechToTextTests.Render_ShouldNameTheButtonDictate_WhenGivenNoLabel`, `TbSpeechToTextTests.StartAsync_ShouldListenAndSayItIs_WhenPressed`
- [x] "Listening" and "Stopped listening" are announced, unless `Labels` words them. `TbSpeechToTextTests.StartAsync_ShouldListenAndSayItIs_WhenPressed`, `TbSpeechToTextTests.Labels_ShouldWordTheAnnouncements_WhenGiven`
- [x] Where the browser recognizes no speech, the button stays hidden. `SpeechToTextTests.SpeechToText_ShouldStayHidden_WhenTheBrowserRecognizesNoSpeech`, `TbSpeechToTextTests.Render_ShouldStayHidden_WhenTheScriptHasntFoundSpeechRecognition`
