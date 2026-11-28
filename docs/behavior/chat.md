# Chat

Pattern: [Feed](https://www.w3.org/WAI/ARIA/apg/patterns/feed/) of messages, with a composer

Familiar apps: a team chat such as Slack or Teams, a support chat on a store's site, an assistant whose replies stream in

## Keyboard

- [x] Tab reaches the messages at the one focus last rested on, or else the newest. `ChatTests.Chat_ShouldMoveBetweenMessagesAndLeaveTheFeed_WhenPageKeysAndCtrlEndOrHomeArePressed`, `ChatTests.Feed_ShouldKeepItsTabStopOnTheNewest_WhenAMessageArrivesBeforeAnyIsFocused`
- [x] Page Down and Page Up move to the next or previous message. `ChatTests.Chat_ShouldMoveBetweenMessagesAndLeaveTheFeed_WhenPageKeysAndCtrlEndOrHomeArePressed`
- [x] Ctrl+End moves to the first control after the messages, and Ctrl+Home to the last one before them. `ChatTests.Chat_ShouldMoveBetweenMessagesAndLeaveTheFeed_WhenPageKeysAndCtrlEndOrHomeArePressed`
- [x] Enter sends, Shift+Enter starts a new line, and the box clears once the message is sent. `ChatTests.Composer_ShouldSendOnEnterAndStartALineOnShiftEnter_WhenTyping`, `TbChatTests.Send_ShouldRaiseOnSendWithTheTrimmedTextAndClearTheBox_WhenSubmitted`
- [x] The Enter that ends an input method's composition doesn't send. `ChatTests.Composer_ShouldNotSend_WhenEnterEndsAnInputMethodsComposition`
- [x] Typing fast keeps every character. `ChatTests.Composer_ShouldSendOnEnterAndStartALineOnShiftEnter_WhenTyping`

## Pointer

- [x] The send button stays off while the box is blank and no file is chosen. `TbChatTests.Send_ShouldDoNothingAndStayOff_WhenTheBoxIsBlank`
- [x] The attach button chooses files to go with the next message, and each chosen file can be taken back out. `TbChatTests.Attach_ShouldSendTheChosenFilesAndLetOneBeRemoved_WhenFilesAreChosen`, `ChatTests.Helper_ShouldSendWithAFileAndReadTheMessages_WhenUsed`
- [x] The view follows new messages while the newest is in view; scrolled up, it stays put and a "Newest messages" button takes the user back. `ChatTests.Feed_ShouldFollowNewMessagesOnlyWhileAtTheNewest_WhenTheyArrive`

## Touch

- [ ] Tapping the box, the buttons and a file's link does what clicking does. UNCONFIRMED

## Focus

- [x] Screen readers hear a region and a feed named by the label, and each message as an article named by its author and time, with its place and the total. `TbChatTests.Render_ShouldShowEachMessageAsAnArticleNamedByItsAuthorAndTime_WhenGivenMessages`
- [x] A new message from someone else is read out with its author; the user's own and the ones there at first aren't. `TbChatTests.Messages_ShouldReadOutOnlyNewOnesFromOthers_WhenTheyArrive`
- [x] A reply that streams in is read out once it ends, and it and the feed are busy meanwhile. `TbChatTests.Streaming_ShouldReadOutTheReplyOnceAndMarkItBusyMeanwhile_WhenItStopsStreaming`
- [x] Who is typing shows under the messages. `TbChatTests.Typing_ShouldNameWhoIsTyping_WhenGivenAuthors`
- [x] A message's files show as links. `TbChatTests.Attachments_ShouldShowAsLinks_WhenAMessageHasFiles`
- [x] A message's time shows in the chat's `TimeZone`, else the shared time zone. `TbTimestampTests.Chat_ShouldShowTimesInTheCascadedTimeZone_WhenOneIsCascaded`, `TbChatTests.Time_ShouldShowInTheChatsTimeZone_WhenOneIsGiven`
- [x] In forced colors, each message keeps its border. `ForcedColorsTests.Example_ShouldBorderEachMessage_WhenColorsAreForced`

## Not applicable

- Arrow keys between messages: the feed pattern leaves them to scrolling and to the controls inside each message.
- Editing or deleting a sent message: the app owns its messages, and can offer that in a `MessageTemplate`.
