# Masonry

Pattern: none; a layout that keeps its cards in the order given

Familiar apps: Pinterest's boards and Google Keep's notes

## Keyboard

- [x] Tab moves through the cards' controls in the order the cards are given, across the top first. `MasonryTests.Masonry_ShouldKeepTheTabOrderOfItsCards_WhenPlaced`

## Pointer

- [x] Each card goes under the shortest column, so cards of different heights leave no gaps. `MasonryTests.Masonry_ShouldPlaceEachCardUnderTheShortestColumn_WhenPlaced`
- [x] Padding on the masonry stays around the cards. `MasonryTests.Masonry_ShouldPlaceTheCardsInsideItsPadding_WhenPadded`
- [x] A narrower masonry fits fewer columns and places the cards again. `MasonryTests.Masonry_ShouldPlaceTheCardsAgain_WhenItNarrows`
- [x] A card that grows, such as when its image loads, moves the cards under it. `MasonryTests.Masonry_ShouldPlaceTheCardsAgain_WhenACardGrows`
- [x] A card added later is placed with the rest. `MasonryTests.Masonry_ShouldPlaceANewCard_WhenOneIsAdded`
- [x] A masonry hidden and shown again, such as in a tab, shows its cards in place right away. `MasonryTests.Masonry_ShouldShowItsCardsPlaced_WhenShownAgainAfterBeingHidden`

## Touch

- [x] Nothing needs hover; the cards' own controls work as they do anywhere. `TbMasonry.cs`

## Focus

- [x] Screen readers read the cards in the order given, and a `ul` masonry is a list. `TbMasonryTests.Render_ShouldKeepTheCardsInOrder_WhenCalled`, `TbMasonryTests.Render_ShouldRenderAListOfCards_WhenAsAList`
- [x] Before the page is interactive, the cards show in a plain grid in the same order. `trailblazor.css`
