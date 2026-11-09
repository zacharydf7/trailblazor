# Card

Pattern: none; a card is an article, and with an `Href` its title is a link

Familiar apps: the listings on a rental site, and the video tiles on a video site

## Keyboard

- [x] A card with an `Href` is one tab stop, its title's link; Tab goes on to the controls inside it. `CardTests.Card_ShouldBeOneTabStopRingedAllAround_WhenItsLinkIsFocused`
- [x] Enter on the title's link follows it, since it is a real anchor. `TbCard.razor`
- [x] A card without an `Href` takes no focus of its own. `TbCard.razor`

## Pointer

- [x] Clicking anywhere on a card with an `Href` follows its link. `CardTests.Card_ShouldFollowItsLink_WhenClickedAwayFromTheTitle`
- [x] Clicking a button inside a card with an `Href` presses the button and doesn't follow the link. `CardTests.Card_ShouldPressItsButtonWithoutFollowingTheLink_WhenTheButtonIsClicked`
- [x] Ctrl, Cmd, Shift, middle and right clicks on the card do what they do on any link, since the link is what's clicked. `TbCard.razor`
- [x] Hovering a card with an `Href` underlines its title. `trailblazor.css`
- [x] The picture shows above the title, though it comes after it in the markup. `CardTests.Card_ShouldShowItsPictureAboveTheTitle_WhenItHasMedia`
- [x] In forced-colors mode, each card keeps a border. `CardTests.Card_ShouldKeepABorder_WhenColorsAreForced`

## Touch

- [x] Tapping does what clicking does, and nothing depends on hover. `TbCard.razor`

## Focus

- [x] The focus ring on a card's link goes around the whole card. `CardTests.Card_ShouldBeOneTabStopRingedAllAround_WhenItsLinkIsFocused`
- [x] Screen readers hear an article named by its title, which is a heading at `HeadingLevel`. `TbCardTests.Render_ShouldNameTheArticleByItsHeading_WhenGivenATitle`, `TbCardTests.Render_ShouldUseTheGivenHeadingLevel_WhenOneIsSet`
- [x] A card's link is named by its title alone. `TbCardTests.Render_ShouldMakeTheTitleTheCardsLink_WhenGivenAnAddress`
- [x] A card rendered as a list item isn't given a name, which a list item can't take. `TbCardTests.Render_ShouldLeaveTheCardUnnamed_WhenItIsAListItem`

## Not applicable

- Selecting text on a card with an `Href`: the link covers the card, so dragging starts a link drag, as on most sites' cards.
- Selectable cards, chosen like radio buttons or checkboxes: a card is content, not a form control.
